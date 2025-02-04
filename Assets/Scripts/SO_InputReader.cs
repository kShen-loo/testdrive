using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu( menuName = "InputReader" )]
public class SO_InputReader : ScriptableObject, InputSystem_Actions.IPlayerActions, InputSystem_Actions.IUIActions
{
    private InputSystem_Actions m_gameInput;

    public Action<InputAction.CallbackContext> eventOnAttack;
    public Action<InputAction.CallbackContext> eventOnMove;
    public Action<InputAction.CallbackContext> eventOnJump;

    // ================================================================================

    #region initialization

    private void OnEnable()
    {
        if ( this.m_gameInput == null )
        {
            this.m_gameInput = new InputSystem_Actions();
            this.m_gameInput.Player.SetCallbacks( this );
            this.m_gameInput.UI.SetCallbacks( this );
        }
    }

    #endregion initialization

    // ================================================================================

    #region public functions

    public void SetInputGameplayMode()
    {
        this.m_gameInput.Player.Enable();
        this.m_gameInput.UI.Disable();
    }

    public void SetInputUIMode()
    {
        this.m_gameInput.UI.Enable();
        this.m_gameInput.Player.Disable();
    }

    #endregion public functions

    // ================================================================================

    public void OnAttack( InputAction.CallbackContext context )
    {
        this.eventOnAttack?.Invoke( context );
    }

    public void OnInteract( InputAction.CallbackContext context )
    {
    }

    public void OnJump( InputAction.CallbackContext context )
    {
        this.eventOnJump?.Invoke( context );
    }

    public void OnLook( InputAction.CallbackContext context )
    {
    }

    public void OnMove( InputAction.CallbackContext context )
    {
        this.eventOnMove?.Invoke( context );
    }

    // ================================================================================

    public void OnCancel( InputAction.CallbackContext context )
    {
    }

    public void OnClick( InputAction.CallbackContext context )
    {
    }

    public void OnMiddleClick( InputAction.CallbackContext context )
    {
    }

    public void OnNavigate( InputAction.CallbackContext context )
    {
    }

    public void OnPoint( InputAction.CallbackContext context )
    {
    }

    public void OnRightClick( InputAction.CallbackContext context )
    {
    }

    public void OnScrollWheel( InputAction.CallbackContext context )
    {
    }

    public void OnSubmit( InputAction.CallbackContext context )
    {
    }

    public void OnTrackedDeviceOrientation( InputAction.CallbackContext context )
    {
    }

    public void OnTrackedDevicePosition( InputAction.CallbackContext context )
    {
    }

    // ================================================================================
}
