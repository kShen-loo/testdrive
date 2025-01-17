using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public interface IState
{
    public void OnStateEnter();
    public void OnStateUpdate();
    public void OnStateFixedUpdate();
    public void OnStateExit();
}

/// <summary>
/// This class is used as a generic class based state machine, with using SetState<Type>() as your main way to create and switch between types
/// IState is used as a base state for inheritance, any concrete states that are going to be used with this state machine need to implement IState as a base interface.
/// Eg. ICharacterState : IState
/// 
/// </summary>
/// <typeparam name="TState"></typeparam>
public class StateMachine<TState> where TState : class, IState
{
    private Dictionary<System.Type, TState> m_stateDict = new Dictionary<Type, TState>();
    private TState m_currentState;
    private TState m_previousState;
    public TState currentState { get { return this.m_currentState; } }
    public TState previousState { get { return this.m_previousState; } }
    public Action eventOnStateChanged;
    public StateMachine() { }

    public void RegisterState( System.Type type, TState state )
    {
        if ( !this.m_stateDict.ContainsKey( type ) )
        {
            this.m_stateDict.Add( type, state );
        }
    }

    public void DeregisterState( System.Type type, TState state )
    {
        if ( this.m_stateDict.ContainsKey( type ) )
        {
            this.m_stateDict.Remove( type );
        }
    }

    public void SetState<T>()
    {
        // We return if desired state is the same as current state
        if ( null != this.m_currentState )
        {
            if ( typeof( T ) == this.currentState.GetType() )
                return;
        }

        if ( this.m_stateDict.ContainsKey( typeof( T ) ) )
        {
            this.HandleStateActions( this.m_stateDict[typeof( T )] );
        }
        else
        {
            UnityEngine.Debug.Log( "No state with this type exists" );
        }
    }

    public void SetState<T>( params object[] parameters ) where T : TState
    {
        // We return if desired state is the same as current state
        if ( null != this.m_currentState )
        {
            if ( typeof( T ) == this.currentState.GetType() )
                return;
        }

        if ( this.m_stateDict.ContainsKey( typeof( T ) ) )
        {
            TState state = this.m_stateDict[typeof( T )];
            if ( null == state )
            {
                state = this.CreateStateInstance<T>( parameters );
                this.m_stateDict.Add( typeof( T ), state );
            }

            this.HandleStateActions( state );
        }
        else
        {
            TState instance = this.CreateStateInstance<T>( parameters );
            this.m_stateDict.Add( typeof( T ), instance );
            this.HandleStateActions( instance );
        }
    }

    private TState CreateStateInstance<T>( params object[] parameters ) where T : TState
    {
        return ( T )Activator.CreateInstance( typeof( T ), parameters );
    }

    private void HandleStateActions( TState state )
    {
        this.m_previousState = this.currentState;
        this.m_previousState?.OnStateExit();

        this.m_currentState = state;
        this.m_currentState?.OnStateExit();

        this.eventOnStateChanged?.Invoke();
    }
}
