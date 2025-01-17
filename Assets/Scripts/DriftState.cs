using UnityEngine;

public class DriftState : IDriveState
{
    private SO_VehicleData m_vehicleData;

    public DriftState( SO_VehicleData data )
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
