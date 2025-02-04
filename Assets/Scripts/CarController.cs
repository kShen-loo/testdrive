using TMPro;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header( "Data" )]
    [SerializeField] private SO_VehicleData m_data;
    public StateMachine<IDriveState> m_stateMachine = new StateMachine<IDriveState>();

    [Header( "Physics & Gravity" )]
    public float gravity = 9.8f;           // Gravity force
    public float raycastDistance = 2f;     // Distance to check for ground below
    public float rotationLerpSpeed = 10f;  // Speed of rotation adjustment to match ground
    [SerializeField] private LayerMask m_layer;
    [SerializeField] private float m_groundCheckDistance;
    [SerializeField] private float m_groundAlignSmoothTime = 0.2f;
    private float m_groundAlignVelocity = 0f;

    [Header( "Camera" )]
    [SerializeField] private Transform m_cinemachineFollowTarget;

    private CharacterController characterController;
    private Vector3 velocity;              // Movement velocity, including gravity
    private Vector3 m_driftOrigin;
    public float currentSpeed = 0f;       // Current forward speed
    public float currentRotationSpeed = 0f; // Current rotation speed
    public float targetSteeringAngle;
    private bool m_align = true;

    [Header( "Input" )]
    [SerializeField] private PlayerInputManager m_playerInputManager;

    [Header( "Target Transform" )]
    public Transform target;  // The transform to offset

    [Header( "Offset Settings" )]
    private float distance = 5f;               // Distance from the target
    [Range( -180f, 180f )]
    public float rotationOffset = 0;  // Euler rotation offset
    private float rotationVelocity = 0f;
    private float currentVelocity;
    private float currentVelocity2;
    private Vector3 m_oriPos;
    private Quaternion m_oriRot;
    private Vector3 m_oriScale;

    // ====================================================================================================

    #region monobehaviour

    private void OnDrawGizmos()
    {
        this.DrawCarForwardVector();
    }

    private void Awake()
    {
        this.RecordOrigin();
        this.m_driftOrigin = this.m_cinemachineFollowTarget.transform.localPosition;
        this.m_stateMachine.RegisterState( typeof( GripState ), new GripState( this.m_data ) );
        this.m_stateMachine.RegisterState( typeof( DriftState ), new DriftState( this.m_data ) );
        this.m_stateMachine.SetState<GripState>();
    }

    void Start()
    {
        // Get the CharacterController component
        this.characterController = GetComponent<CharacterController>();

        if ( characterController == null )
        {
            Debug.LogError( "CharacterController component is missing from this GameObject." );
        }
    }

    void Update()
    {
        if ( this.m_playerInputManager.accelerate > 0 )
            currentSpeed += this.m_data.acceleration * this.m_playerInputManager.accelerate * Time.deltaTime;
        else
            currentSpeed -= this.m_data.deceleration * Time.deltaTime;

        if ( this.m_playerInputManager.brake > 0 )
        {
            currentSpeed -= this.m_data.brakeSpeed * this.m_playerInputManager.brake * Time.deltaTime;
        }

        // Clamp forward speed
        currentSpeed = Mathf.Clamp( currentSpeed, 0f, this.m_data.maxSpeed );

        this.UpdateSteeringAngle( this.m_playerInputManager.moveDelta.x );

        this.UpdateVehicleRotation();
        this.ClampVehicleRotation();

        switch ( this.m_stateMachine.currentState )
        {
            case GripState:
                if ( this.currentRotationSpeed > this.m_data.gripStateData.maxRotationAngle * this.m_data.enterDriftRequirementMultiplier
                    || this.currentRotationSpeed < -this.m_data.gripStateData.maxRotationAngle * this.m_data.enterDriftRequirementMultiplier )
                    this.m_stateMachine.SetState<DriftState>();
                break;
            case DriftState:
                if ( this.currentRotationSpeed > -this.m_data.driftStateData.maxRotationAngle * this.m_data.exitDriftRequirementMultiplier
                        && this.currentRotationSpeed < this.m_data.driftStateData.maxRotationAngle * this.m_data.exitDriftRequirementMultiplier )
                {
                    this.m_stateMachine.SetState<GripState>();
                }
                break;
        }

        this.UpdateVehicleGravity();

        // Move forward
        Vector3 cameraPos = this.m_cinemachineFollowTarget.transform.position;
        cameraPos.y = this.transform.position.y;

        //Vector3 forwardMovement = transform.forward * currentSpeed;

        // Change forward direction between car forward / towards camera
        Vector3 forwardMovement = Vector3.zero;
        if ( this.m_stateMachine.currentState.GetType() == typeof( GripState ) )
        {
            //forwardMovement = transform.forward * currentSpeed;
            forwardMovement = Vector3.Lerp( this.transform.forward, ( cameraPos - this.transform.position ).normalized, Time.deltaTime * this.m_data.carRotationNeutralizeSpeed ) * currentSpeed;
            //forwardMovement = ( cameraPos - this.transform.position ).normalized * currentSpeed;
        }
        else
        {
            //forwardMovement = Vector3.Lerp( this.transform.forward, ( cameraPos - this.transform.position ).normalized, Time.deltaTime * this.m_data.carRotationNeutralizeSpeed ) * currentSpeed;
            forwardMovement = transform.forward * currentSpeed;
            //forwardMovement = ( cameraPos - this.transform.position ).normalized * currentSpeed;
        }

        velocity.x = forwardMovement.x;
        velocity.z = forwardMovement.z;

        // Apply movement
        characterController.Move( velocity * Time.deltaTime );

        this.DrawDrivingVelocity( velocity.normalized );

        // Rotate (steer) only if the car is moving
        if ( currentSpeed > 0.1f ) // Allow rotation only when the car is moving
        {
            transform.Rotate( 0, currentRotationSpeed * Time.deltaTime, 0 );
            //transform.Rotate( 0, currentRotationSpeed, 0 );
        }

        // Adjust car's rotation to follow the ground normal
        if ( this.m_align )
        {
            AlignToGround();
        }

        this.UpdateDriftCameraAngleValue();
        this.UpdateCameraFollowTargetAngle();

        this.ResetPositionInputUpdate();
    }

    #endregion monobehaviour

    // ====================================================================================================

    private void UpdateVehicleGravity()
    {
        if ( !characterController.isGrounded )
        {
            velocity.y -= gravity * Time.deltaTime;
        }
        else
        {
            velocity.y = 0f; // Reset vertical velocity when grounded
        }
    }

    private void ClampVehicleRotation()
    {
        // Clamp rotation speed
        float rotationAngleCap = 1;
        switch ( this.m_stateMachine.currentState )
        {
            case GripState: rotationAngleCap = this.m_data.gripStateData.maxRotationAngle; break;
            case DriftState: rotationAngleCap = this.m_data.driftStateData.maxRotationAngle; break;
        }

        this.currentRotationSpeed = Mathf.Clamp( currentRotationSpeed, -rotationAngleCap, rotationAngleCap );
    }

    private void UpdateVehicleRotation()
    {
        float oppositeDirMultiplier = 1f;
        float rotationAccel = 0f;
        float rotationDecel = 0f;
        switch ( this.m_stateMachine.currentState )
        {
            case GripState:     
                oppositeDirMultiplier = this.m_data.gripStateData.oppositeDirMultiplier;
                rotationAccel = this.m_data.gripStateData.rotationAcceleration;
                rotationDecel = this.m_data.gripStateData.rotationDeceleration; break;
            case DriftState:    
                oppositeDirMultiplier = this.m_data.driftStateData.oppositeDirMultiplier;
                rotationAccel = this.m_data.driftStateData.rotationAcceleration;
                rotationDecel = this.m_data.driftStateData.rotationDeceleration; break;
        }

        // Handle rotation acceleration and deceleration
        float moveDelta = this.m_playerInputManager.moveDelta.x;
        float steeringInput = 0f;
        if ( this.m_playerInputManager.moveDelta.x < 0 )
        {
            steeringInput = this.currentRotationSpeed > 0 ? -Mathf.Abs( moveDelta * oppositeDirMultiplier ) : -Mathf.Abs( moveDelta );
        }
        else if ( this.m_playerInputManager.moveDelta.x > 0 )
        {
            steeringInput = this.currentRotationSpeed < 0 ? Mathf.Abs( moveDelta * oppositeDirMultiplier ) : Mathf.Abs( moveDelta );
        }

        if ( steeringInput != 0 && this.currentSpeed > 0.1f ) // Increment rotation speed while turning
        {
            currentRotationSpeed += steeringInput * rotationAccel * Time.deltaTime;
        }
        else // Decrement rotation speed when no input
        {
            if ( currentRotationSpeed > 0 )
            {
                currentRotationSpeed -= rotationDecel * Time.deltaTime;
                currentRotationSpeed = Mathf.Max( currentRotationSpeed, 0f );
            }
            else if ( currentRotationSpeed < 0 )
            {
                currentRotationSpeed += rotationDecel * Time.deltaTime;
                currentRotationSpeed = Mathf.Min( currentRotationSpeed, 0f );
            }
        }
    }

    private void ResetPositionInputUpdate()
    {
        if ( Input.GetKeyDown( KeyCode.Tab ) )
        {
            this.m_align = false;
            this.ResetToOrigin();
            this.m_align = true;
        }
    }

    private void AlignToGround()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up; // Start ray slightly above the car

        if ( Physics.Raycast( rayOrigin, Vector3.down, out hit, raycastDistance, this.m_layer ) )
        {
            // Get the ground's normal
            Vector3 groundNormal = hit.normal;

            // Compute target rotation based on the ground normal
            Quaternion targetRotation = Quaternion.FromToRotation( transform.up, groundNormal ) * transform.rotation;

            // Smoothly rotate towards the target rotation (SmoothDamp-like)
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                m_groundAlignSmoothTime * 100f * Time.deltaTime  // Adjust multiplier for speed
            );
        }
    }

    private void RecordOrigin()
    {
        this.m_oriPos = this.transform.position;
        this.m_oriRot = this.transform.rotation;
        this.m_oriScale = this.transform.localScale;
    }

    private void ResetToOrigin()
    {
        this.transform.position = m_oriPos;
        this.transform.rotation = m_oriRot;
        this.transform.localScale = m_oriScale;

        this.currentRotationSpeed = 0;
        this.currentSpeed = 0;
    }

    private float GetDriftCameraValue()
    {
        float maxAngle = 0f;
        float minAngle = 0f;
        switch ( this.m_stateMachine.currentState )
        {
            case GripState:
                minAngle = this.m_data.gripStateData.minEvaluationAngle;
                maxAngle = this.m_data.gripStateData.maxEvaluationAngle;
                break;
            case DriftState:
                minAngle = this.m_data.driftStateData.minEvaluationAngle;
                maxAngle = this.m_data.driftStateData.maxEvaluationAngle;
                break;
        }
        
        if ( this.currentRotationSpeed > minAngle && this.currentRotationSpeed <= maxAngle )
        {
            return this.GetPercentageBetween( this.currentRotationSpeed, minAngle, maxAngle );
        }
        else if ( this.currentRotationSpeed < minAngle && this.currentRotationSpeed >= -maxAngle )
        {
            return -this.GetPercentageBetween( this.currentRotationSpeed, minAngle, -maxAngle );
        }

        return 0;
    }

    private void UpdateDriftCameraAngleValue()
    {
        float rotOffset = 0f;
        float smoothTime = 0f;
        switch ( this.m_stateMachine.currentState )
        {
            case GripState: 
                rotOffset = this.m_data.gripStateData.rotationOffset;
                smoothTime = this.m_data.gripStateData.cameraSmoothTime; 
                break;
            case DriftState: 
                rotOffset = this.m_data.driftStateData.rotationOffset;
                smoothTime = this.m_data.driftStateData.cameraSmoothTime; 
                break;
        }

        //this.rotationOffset = Mathf.Lerp( -rotOffset, rotOffset, this.NormalizeMinusOneToOne( this.GetDriftCameraValue() ) );
        this.rotationOffset = Mathf.SmoothDamp(
                            this.rotationOffset,
                            Mathf.Lerp( -rotOffset, rotOffset, this.NormalizeMinusOneToOne( this.GetDriftCameraValue() ) ),
                            ref rotationVelocity,
                            smoothTime
                            );

        if ( Mathf.Abs( this.rotationOffset ) < 0.01f )
        {
            this.rotationOffset = 0f;
        }
    }

    void UpdateCameraFollowTargetAngle()
    {
        // Target Y rotation
        float targetY = transform.eulerAngles.y + rotationOffset;

        // Smoothly damp the Y-axis rotation
        float smoothTime = 0f;
        switch ( this.m_stateMachine.currentState )
        {
            case GripState: smoothTime = this.m_data.gripStateData.cameraSmoothTime; break;
            case DriftState: smoothTime = this.m_data.driftStateData.cameraSmoothTime; break;
        }

        float smoothY = Mathf.SmoothDampAngle( target.eulerAngles.y, targetY, ref currentVelocity, smoothTime );

        // Apply smoothed rotation
        target.rotation = Quaternion.Euler( 0f, smoothY, 0f );

        // Update position
        Quaternion offsetRotation = Quaternion.Euler( 0f, rotationOffset, 0f );
        Vector3 offsetPosition = transform.position + ( offsetRotation * transform.forward * distance );
        target.position = offsetPosition;

        if ( currentSpeed > 0f )
        {
            // Rotate car towards camera
            Vector3 direction = ( this.m_cinemachineFollowTarget.position - transform.position ).normalized;
            Quaternion lookRotation = Quaternion.LookRotation( direction );
            float angleDifference = Vector3.Angle( transform.forward, direction );

            if ( this.m_stateMachine.currentState.GetType() == typeof( GripState ) )
            {
                transform.rotation = Quaternion.Lerp( transform.rotation, lookRotation, Time.deltaTime * this.m_data.cameraNeutralizeSpeed );

                //Vector3 direction = ( this.m_cinemachineFollowTarget.position - transform.position ).normalized;
                //float targetAngle = Mathf.Atan2( direction.x, direction.z ) * Mathf.Rad2Deg; // Get target angle
                //float smoothAngle = Mathf.SmoothDampAngle( transform.eulerAngles.y, targetAngle, ref currentVelocity2, this.m_data.cameraNeutralizeSpeed * Time.deltaTime );
                //transform.rotation = Quaternion.Euler( 0, smoothAngle, 0 ); // Apply rotation only on Y-axis
            }
        }
    }

    private void UpdateSteeringAngle( float input )
    {
        SWheelData gripStateData = this.m_data.gripStateData;
        SWheelData driftStateData = this.m_data.driftStateData;

        switch ( this.m_stateMachine.currentState )
        {
            case GripState: this.targetSteeringAngle = gripStateData.steeringAngleCurve.Evaluate( Mathf.Abs( input ) ) * gripStateData.maxRotationAngle * input; break;
            case DriftState: this.targetSteeringAngle = driftStateData.steeringAngleCurve.Evaluate( Mathf.Abs( input ) ) * driftStateData.maxRotationAngle * input; break;
        }
    }

    // ====================================================================================================

    #region gizmos

    private void DrawCarForwardVector()
    {
        Gizmos.color = Color.green;

        Vector3 cameraPos = this.m_cinemachineFollowTarget.transform.position;
        cameraPos.y = this.transform.position.y;
        Vector3 forwardMovement = ( cameraPos - this.transform.position ).normalized * currentSpeed;

        Gizmos.DrawLine( this.transform.position, this.transform.position + forwardMovement );
    }

    private void DrawDrivingVelocity( Vector3 forward )
    {
        Debug.DrawLine( this.transform.position, ( this.transform.position + ( forward * currentSpeed) ), Color.yellow );
    }

    #endregion gizmos

    // ====================================================================================================

    #region helpers

    private float NormalizeMinusOneToOne(float value)
    {
        return ( value + 1f ) / 2f;
    }

    public float GetPercentageBetween( float value, float min, float max )
    {
        if ( Mathf.Approximately( max, min ) )
        {
            Debug.LogWarning( "Min and Max cannot be the same value." );
            return 0f;
        }

        return Mathf.Clamp01( ( value - min ) / ( max - min ) );
    }

    #endregion helpers

    // ====================================================================================================
}
