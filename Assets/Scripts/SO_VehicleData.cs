using UnityEngine;

[System.Serializable]
public struct SWheelData
{
    [Header( "Vehicle/Wheel Rotation" )]
    public float maxRotationAngle;
    public float rotationAcceleration;
    public float rotationDeceleration;
    public float oppositeDirMultiplier;
    public AnimationCurve steeringAngleCurve;

    [Header( "Camera Angle" )]
    public float rotationOffset;
    public float minEvaluationAngle;
    public float maxEvaluationAngle;
    public float cameraSmoothTime;
}

[CreateAssetMenu(fileName = "SO_VehicleData", menuName = "Scriptable Objects/SO_VehicleData")]
public class SO_VehicleData : ScriptableObject
{
    [Header( "Speed" )]
    [SerializeField] private float m_maxSpeed = 20f;
    [SerializeField] private float m_acceleration = 5f;   
    [SerializeField] private float m_deceleration = 10f;
    [SerializeField] private float m_brakeSpeed = 15f;
    [SerializeField] private float m_cameraNeutralizeSpeed = 1f;
    [SerializeField] private float m_carRotationNeutralizeSpeed = 1f;

    [Header("Drift")]
    [SerializeField] private float m_enterDriftRequirementMultiplier = 0.8f;
    [SerializeField] private float m_exitDriftRequirementMultiplier = 0.5f;


    [Header( "Grip/Drift State" )]
    [SerializeField] private SWheelData m_gripStateData;
    [SerializeField] private SWheelData m_driftStateData;

    public float enterDriftRequirementMultiplier => this.m_enterDriftRequirementMultiplier;
    public float exitDriftRequirementMultiplier => this.m_exitDriftRequirementMultiplier;
    public float maxSpeed => this.m_maxSpeed;
    public float acceleration => this.m_acceleration;
    public float deceleration => this.m_deceleration;
    public float brakeSpeed => this.m_brakeSpeed;
    public float cameraNeutralizeSpeed => this.m_cameraNeutralizeSpeed;
    public float carRotationNeutralizeSpeed => this.m_carRotationNeutralizeSpeed;
    public SWheelData gripStateData => this.m_gripStateData;
    public SWheelData driftStateData => this.m_driftStateData;
}
