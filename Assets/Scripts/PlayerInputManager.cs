using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    [SerializeField] private SO_InputReader m_inputReader;
    public Vector2 moveDelta { get; private set; }
    public float accelerate { get; private set; }
    public float brake { get; private set; }

    private void OnEnable()
    {
        this.m_inputReader.eventOnMove += this.GetPlayerMoveDelta_Private;
        this.m_inputReader.eventOnAttack += this.GetPlayerAccelerate_Private;
        this.m_inputReader.eventOnJump += this.GetPlayerBrake_Private;
    }

    private void OnDisable()
    {
        this.m_inputReader.eventOnMove -= this.GetPlayerMoveDelta_Private;
        this.m_inputReader.eventOnAttack -= this.GetPlayerAccelerate_Private;
        this.m_inputReader.eventOnJump -= this.GetPlayerBrake_Private;
    }

    private void Start()
    {
        this.m_inputReader.SetInputGameplayMode();
    }

    private void GetPlayerBrake_Private( InputAction.CallbackContext context )
    {
        this.brake = context.ReadValue<float>();
    }

    private void GetPlayerAccelerate_Private( InputAction.CallbackContext context )
    {
        this.accelerate = context.ReadValue<float>();
    }

    private void GetPlayerMoveDelta_Private( InputAction.CallbackContext context )
    {
        this.moveDelta = context.ReadValue<Vector2>();
    }
}
