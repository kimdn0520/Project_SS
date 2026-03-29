using UnityEngine;

public abstract class BasePlayerState : IState
{
    protected PlayerController player;
    protected StateMachine stateMachine;

    public BasePlayerState(PlayerController player, StateMachine stateMachine)
    {
        this.player = player;
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void FixedUpdate() { }

    public virtual void Update()
    {
        // ...
    }
}