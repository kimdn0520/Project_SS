using UnityEngine;

public abstract class BasePlayerState : IState
{
    protected PlayerController player;
    protected StateMachine stateMachine;
    
    // 실무형 가감속 상수 (값이크면 더 빠릿함)
    protected const float ACCELERATION = 120f; 
    protected const float DECELERATION = 100f;

    public BasePlayerState(PlayerController player, StateMachine stateMachine)
    {
        this.player = player;
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    
    public virtual void FixedUpdate() 
    {
        // [실무형 Direct Velocity 제어]
        // 1. 목표 속도 계산
        float targetSpeed = player.MoveSpeed;
        if (player.IsSprinting && player.CurrentStamina > 0) targetSpeed *= player.SprintMultiplier;
        if (player.CurrentStamina <= 0) targetSpeed *= 0.5f;

        Vector2 targetVelocity = player.MoveInput * targetSpeed;

        // 2. 가속/감속 처리 (MoveTowards를 사용하여 선형적으로 속도 변경)
        float accelRate = (player.MoveInput.sqrMagnitude > 0.01f) ? ACCELERATION : DECELERATION;
        
        player.Rb.linearVelocity = Vector2.MoveTowards(
            player.Rb.linearVelocity, 
            targetVelocity, 
            accelRate * Time.fixedDeltaTime
        );
    }

    public abstract void Update();
}

public class PlayerIdleState : BasePlayerState
{
    public PlayerIdleState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine) { }

    public override void Update()
    {
        if (player.MoveInput.sqrMagnitude > 0.01f)
        {
            stateMachine.ChangeState(new PlayerMoveState(player, stateMachine));
        }
    }
}

public class PlayerMoveState : BasePlayerState
{
    public PlayerMoveState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine) { }

    public override void Update()
    {
        if (player.MoveInput.sqrMagnitude < 0.01f)
        {
            stateMachine.ChangeState(new PlayerIdleState(player, stateMachine));
            return;
        }

        if (player.IsSprinting && player.CurrentStamina > 0)
        {
            stateMachine.ChangeState(new PlayerSprintState(player, stateMachine));
        }
    }
}

public class PlayerSprintState : BasePlayerState
{
    public PlayerSprintState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine) { }

    public override void Update()
    {
        if (player.MoveInput.sqrMagnitude < 0.01f)
        {
            stateMachine.ChangeState(new PlayerIdleState(player, stateMachine));
            return;
        }

        if (!player.IsSprinting || player.CurrentStamina <= 0)
        {
            stateMachine.ChangeState(new PlayerMoveState(player, stateMachine));
        }
    }
}
