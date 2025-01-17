using UnityEngine;

public interface IDriveState : IState { }

public class GripState : IDriveState
{
    private SO_VehicleData m_vehicleData;

    public GripState( SO_VehicleData data )
    {
        this.m_vehicleData = data;
    }

    public void OnStateEnter()
    {
    }

    public void OnStateExit()
    {
    }

    public void OnStateFixedUpdate()
    {
    }

    public void OnStateUpdate()
    {
    }
}