using TMPro;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header( "Speed" )]
    public float maxSpeed = 20f;            // Maximum forward speed
    public float acceleration = 5f;        // Speed increment per second when pressing forward
    public float deceleration = 10f;       // Speed decrement per second when not pressing forward

    [Header( "Angle" )]
    public float maxRotationAngle = 100f;  // Maximum steering rotation speed
    public float maxDriftRotationAngle = 85f;
    public float rotationAcceleration = 50f; // Rotation increment per second
    public float rotationDeceleration = 70f; // Rotation decrement per second
    public float oppositeDirectionSnapBackValue = 2.5f;

    [Header( "Drift" )]
    [SerializeField] private float m_enterDriftRequirementMultiplier = 0.8f;
    [SerializeField] private float m_exitDriftRequirementMultiplier = 0.6f;
    //[SerializeField] private float m_driftCounterSteerMultiplier = 1.5f;      // NOTE: Currently unused

    [Header( "Physics & Gravity" )]
    public float gravity = 9.8f;           // Gravity force
    public float raycastDistance = 2f;     // Distance to check for ground below
    public float rotationLerpSpeed = 10f;  // Speed of rotation adjustment to match ground
    [SerializeField] private LayerMask m_layer;
    [SerializeField] private float m_groundCheckDistance;
    [SerializeField] private LayerMask m_groundLayer;

    [Header( "Camera" )]
    [SerializeField] private Transform m_cinemachineFollowTarget;
    [SerializeField] private Transform m_cinemachineFollowTargetTEMP;   // Duplicated transform that is unmodified in code to obtain reference transform
    //[SerializeField] private Vector3 m_driftCameraOffset;

    public bool grounded { get { return this.IsGrounded(); } }
    private Rigidbody m_rb;
    private CharacterController characterController;
    private Vector3 velocity;              // Movement velocity, including gravity
    private Vector3 m_driftOrigin;
    private float currentSpeed = 0f;       // Current forward speed
    private float currentRotationSpeed = 0f; // Current rotation speed
    private bool m_align = true;
    private bool m_isDrifting = false;

    [Header( "Debug" )]
    [SerializeField] private TextMeshProUGUI m_debugUGUI;
    [SerializeField] private TextMeshProUGUI m_debugDriftUGUI;
    [SerializeField] private TextMeshProUGUI m_debugDriftCamera;
    [SerializeField] private bool m_alwaysDrifting;

    // ====================================================================================================

    #region monobehaviour

    private void OnDrawGizmos()
    {
        this.DrawCarForwardVector();
        this.DrawMaxCameraAngleGizmos();
    }

    private void Awake()
    {
        this.RecordOrigin();
        this.m_driftOrigin = this.m_cinemachineFollowTarget.transform.localPosition;
    }

    void Start()
    {
        // Get the CharacterController component
        this.m_rb = GetComponent<Rigidbody>();
        this.characterController = GetComponent<CharacterController>();

        if ( characterController == null )
        {
            Debug.LogError( "CharacterController component is missing from this GameObject." );
        }
    }

    void Update()
    {
        if ( Input.GetKeyDown( KeyCode.Space ) )
        {
            this.m_alwaysDrifting = !this.m_alwaysDrifting;
            return;
        }

        // Handle forward acceleration and deceleration
        if ( Input.GetKey( KeyCode.W ) ) // Pressing forward
        {
            currentSpeed += acceleration * Time.deltaTime;
        }
        else // Not pressing forward
        {
            currentSpeed -= deceleration * Time.deltaTime;
        }

        // Clamp forward speed
        currentSpeed = Mathf.Clamp( currentSpeed, 0f, maxSpeed );
         
        // Handle rotation acceleration and deceleration
        float steeringInput = 0f;
        if ( Input.GetKey( KeyCode.A ) ) // A key to steer left
        {
            steeringInput = this.currentRotationSpeed > 0 ? -1f * this.oppositeDirectionSnapBackValue : -1f;
        }
        else if ( Input.GetKey( KeyCode.D ) ) // D key to steer right
        {
            steeringInput = this.currentRotationSpeed < 0 ? 1f * this.oppositeDirectionSnapBackValue : 1f;
        }

        if ( steeringInput != 0 && this.currentSpeed > 0.1f ) // Increment rotation speed while turning
        {
            currentRotationSpeed += steeringInput * rotationAcceleration * Time.deltaTime;
        }
        else // Decrement rotation speed when no input
        {
            if ( currentRotationSpeed > 0 )
            {
                currentRotationSpeed -= rotationDeceleration * Time.deltaTime;
                currentRotationSpeed = Mathf.Max( currentRotationSpeed, 0f );
            }
            else if ( currentRotationSpeed < 0 )
            {
                currentRotationSpeed += rotationDeceleration * Time.deltaTime;
                currentRotationSpeed = Mathf.Min( currentRotationSpeed, 0f );
            }
        }

        // Clamp rotation speed
        float rotationSpeedCap = this.maxRotationAngle;

        if ( this.m_alwaysDrifting )
        {
            this.m_isDrifting = true;
            rotationSpeedCap = this.maxDriftRotationAngle;
        }
        else
        {
            //if ( Input.GetKeyDown( KeyCode.S ) || Input.GetKeyDown( KeyCode.Space ) )
            //{
            //    if ( this.currentRotationSpeed > this.maxRotationAngle * this.m_enterDriftRequirementMultiplier 
            //        || this.currentRotationSpeed < -this.maxRotationAngle * this.m_enterDriftRequirementMultiplier )
            //        this.m_isDrifting = true;
            //}

            //if ( this.currentRotationSpeed > -this.maxRotationAngle * this.m_exitDriftRequirementMultiplier 
            //    && this.currentRotationSpeed < this.maxRotationAngle * this.m_exitDriftRequirementMultiplier )
            //{
            //    this.m_isDrifting = false;
            //}

            this.m_isDrifting = false;

            rotationSpeedCap = this.m_isDrifting ? this.maxDriftRotationAngle : this.maxRotationAngle;
        }

        currentRotationSpeed = Mathf.Clamp( currentRotationSpeed, -rotationSpeedCap, rotationSpeedCap );

        // Apply gravity
        if ( !characterController.isGrounded )
        {
            velocity.y -= gravity * Time.deltaTime;
        }
        else
        {
            velocity.y = 0f; // Reset vertical velocity when grounded
        }

        //if ( !this.grounded )
        //{
        //    velocity.y -= gravity * Time.deltaTime;
        //}
        //else
        //{
        //    velocity.y = 0f; // Reset vertical velocity when grounded
        //}

        // Move forward
        Vector3 forwardMovement = transform.forward * currentSpeed;
        velocity.x = forwardMovement.x;
        velocity.z = forwardMovement.z;

        // Apply movement
        //this.m_rb.MovePosition( velocity * Time.deltaTime );
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

        this.UpdateDebugText();

            this.UpdateDriftCameraAngleValue();
            this.ApplyYOffset();

        if ( Input.GetKeyDown( KeyCode.Tab ) )
        {
            this.m_align = false;
            this.ResetToOrigin();
            this.m_align = true;
        }
    }

    #endregion monobehaviour

    // ====================================================================================================

    private bool IsGrounded()
    {
        return Physics.Raycast( this.transform.position, Vector3.down, this.m_groundCheckDistance + 0.1f, this.m_groundLayer );
    }

    private void UpdateDebugText()
    {
        if ( this.m_debugUGUI != null )
            this.m_debugUGUI.text = "Current Rotation Angle: " + this.currentRotationSpeed;

        if ( this.m_debugDriftUGUI != null )
            this.m_debugDriftUGUI.text = "Drift Status: " + this.m_isDrifting;

        if ( this.m_debugDriftCamera )
            this.m_debugDriftCamera.text = "Drift Camera Value: " + this.rotationOffset;
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

            // Smoothly interpolate to the target rotation
            //transform.rotation = Quaternion.Lerp( transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime );
            transform.rotation = targetRotation;
        }
    }

    private Vector3 m_oriPos;
    private Quaternion m_oriRot;
    private Vector3 m_oriScale;

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

    private Vector3 GetCameraLocalPosition()
    {
        return this.m_cinemachineFollowTargetTEMP.position;
    }

    private float GetDriftCameraValue()
    {
        if ( this.currentRotationSpeed > 0 && this.currentRotationSpeed <= this.maxDriftRotationAngle )
        {
            return this.GetPercentageBetween( this.currentRotationSpeed, 0, this.maxDriftRotationAngle );
        }
        else if ( this.currentRotationSpeed < 0 && this.currentRotationSpeed >= -this.maxDriftRotationAngle )
        {
            return -this.GetPercentageBetween( this.currentRotationSpeed, 0, -this.maxDriftRotationAngle );
        }

        return 0;
    }

    [Header( "Target Transform" )]
    public Transform target;  // The transform to offset

    [Header( "Offset Settings" )]
    public float distance = 5f;               // Distance from the target
    [Range( -180f, 180f )]
    public float rotationOffset = 0;  // Euler rotation offset
    public float rotOffsetA;
    public float rotOffsetB;

    private void UpdateDriftCameraAngleValue()
    {
        //Vector3 driftA = this.m_driftOrigin + -this.m_driftCameraOffset;
        //Vector3 driftB = this.m_driftOrigin + this.m_driftCameraOffset;
        //this.m_cinemachineFollowTarget.transform.localPosition = Vector3.Lerp( driftA, driftB, this.NormalizeMinusOneToOne( this.GetDriftCameraValue() ) );
        rotationOffset = Mathf.Lerp( rotOffsetA, rotOffsetB, this.NormalizeMinusOneToOne( this.GetDriftCameraValue() ) );
    }

    /// <summary>
    /// Offsets the target's position and Y-axis rotation relative to the player.
    /// </summary>
    void ApplyYOffset()
    {
        if ( !this.m_isDrifting )
            return;

        // Apply rotation offset on the Y-axis (-180 to 180)
        Quaternion offsetRotation = Quaternion.Euler( 0f, rotationOffset, 0f );

        // Calculate the new position based on the offset
        Vector3 offsetPosition = transform.position + ( offsetRotation * transform.forward * distance );

        // Apply the calculated position to the target
        target.position = offsetPosition;

        // Align the target's Y-axis rotation with the offset
        target.rotation = Quaternion.Euler( 0f, transform.eulerAngles.y + rotationOffset, 0f );
    }

    // ====================================================================================================

    #region gizmos

    private void DrawMaxCameraAngleGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere( this.m_cinemachineFollowTarget.position, 0.2f );
        //Gizmos.DrawLine( this.GetCameraLocalPosition(), this.GetCameraLocalPosition() + -this.m_driftCameraOffset.x * this.m_cinemachineFollowTargetTEMP.right );
        //Gizmos.DrawLine( this.GetCameraLocalPosition(), this.GetCameraLocalPosition() + this.m_driftCameraOffset.x * this.m_cinemachineFollowTargetTEMP.right );
        //Gizmos.DrawLine( this.transform.position, this.GetCameraLocalPosition() + -this.m_driftCameraOffset.x * this.m_cinemachineFollowTargetTEMP.right );
        //Gizmos.DrawLine( this.transform.position, this.GetCameraLocalPosition() + this.m_driftCameraOffset.x * this.m_cinemachineFollowTargetTEMP.right );
    }

    private void DrawCarForwardVector()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine( this.transform.position, this.transform.position + this.transform.forward );
    }

    private void DrawDrivingVelocity( Vector3 forward )
    {
        Debug.DrawLine( this.transform.position, ( this.transform.position + forward ), Color.yellow );
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
