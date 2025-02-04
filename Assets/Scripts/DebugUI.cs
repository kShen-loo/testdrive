using TMPro;
using UnityEngine;

public class DebugUI : MonoBehaviour
{
    [SerializeField] private PlayerInputManager m_inputManager;
    [SerializeField] private CarController m_carController;

    [SerializeField] private TextMeshProUGUI m_rotationAngleUI;
    [SerializeField] private TextMeshProUGUI m_driftStatusUI;
    [SerializeField] private TextMeshProUGUI m_driftCameraAngleUI;
    [SerializeField] private TextMeshProUGUI m_accelerateUI;
    [SerializeField] private TextMeshProUGUI m_brakingUI;
    [SerializeField] private TextMeshProUGUI m_steeringUI;
    [SerializeField] private TextMeshProUGUI m_speedUI;

    [SerializeField] private TextMeshProUGUI m_targetSteeringAngleUI;

    private void Update()
    {
        if ( this.m_carController )
        {
            bool drift = this.m_carController.m_stateMachine.currentState.GetType() == typeof( DriftState );
            this.m_driftStatusUI.color = drift ? Color.green : Color.red;
            this.m_driftStatusUI.text = "Drift Status: " + drift.ToString();
            this.m_targetSteeringAngleUI.text = "Steering Angle: " + this.m_carController.targetSteeringAngle.ToString();
        }

        if ( this.m_inputManager )
        {
            this.m_rotationAngleUI.text = "Current Rotation Angle: " + this.m_carController.currentRotationSpeed.ToString();
            this.m_driftCameraAngleUI.text = "Drift Camera Value: " + this.m_carController.rotationOffset.ToString();
            this.m_accelerateUI.text = "Throttle: " + this.m_inputManager.accelerate.ToString();
            this.m_brakingUI.text = "Brake: " + this.m_inputManager.brake.ToString();
            this.m_steeringUI.text = "Steer: " + this.m_inputManager.moveDelta.x.ToString();
            this.m_speedUI.text = "Speed: " + ( ( int )this.m_carController.currentSpeed ).ToString();
        }
    }
}
