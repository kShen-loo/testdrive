using UnityEngine;

public class CarController : MonoBehaviour
{
    public float maxSpeed = 20f;            // Maximum forward speed
    public float acceleration = 5f;        // Speed increment per second when pressing forward
    public float deceleration = 10f;       // Speed decrement per second when not pressing forward
    public float maxRotationSpeed = 100f;  // Maximum steering rotation speed
    public float rotationAcceleration = 50f; // Rotation increment per second
    public float rotationDeceleration = 70f; // Rotation decrement per second
    public float gravity = 9.8f;           // Gravity force
    public float raycastDistance = 2f;     // Distance to check for ground below
    public float rotationLerpSpeed = 10f;  // Speed of rotation adjustment to match ground
    public float oppositeDirectionSnapBackValue = 2.5f;

    private float currentSpeed = 0f;       // Current forward speed
    private float currentRotationSpeed = 0f; // Current rotation speed
    private CharacterController characterController;
    private Vector3 velocity;              // Movement velocity, including gravity

    private void Awake()
    {
        this.RecordOrigin();
    }

    void Start()
    {
        // Get the CharacterController component
        characterController = GetComponent<CharacterController>();

        if ( characterController == null )
        {
            Debug.LogError( "CharacterController component is missing from this GameObject." );
        }
    }

    void Update()
    {
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
        currentRotationSpeed = Mathf.Clamp( currentRotationSpeed, -maxRotationSpeed, maxRotationSpeed );

        // Apply gravity
        if ( !characterController.isGrounded )
        {
            velocity.y -= gravity * Time.deltaTime;
        }
        else
        {
            velocity.y = 0f; // Reset vertical velocity when grounded
        }

        // Move forward
        Vector3 forwardMovement = transform.forward * currentSpeed;
        velocity.x = forwardMovement.x;
        velocity.z = forwardMovement.z;

        // Apply movement
        characterController.Move( velocity * Time.deltaTime );

        // Rotate (steer) only if the car is moving
        if ( currentSpeed > 0.1f ) // Allow rotation only when the car is moving
        {
            transform.Rotate( 0, currentRotationSpeed * Time.deltaTime, 0 );
        }

        // Adjust car's rotation to follow the ground normal
        if ( this.m_align )
        {
            AlignToGround();
        }

        if ( Input.GetKeyDown( KeyCode.Tab ) )
        {
            this.m_align = false;
            this.ResetToOrigin();
            this.m_align = true;
        }
    }

    private bool m_align = true;
    [SerializeField] private LayerMask m_layer;

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
            transform.rotation = Quaternion.Lerp( transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime );
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
}
