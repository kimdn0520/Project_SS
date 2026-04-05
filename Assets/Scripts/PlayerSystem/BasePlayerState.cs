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
        if (player.IsGuarding && player.CurrentStamina > 0)
        {
            stateMachine.ChangeState(new PlayerGuardState(player, stateMachine));
            return;
        }

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
        if (player.IsGuarding && player.CurrentStamina > 0)
        {
            stateMachine.ChangeState(new PlayerGuardState(player, stateMachine));
            return;
        }

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
        if (player.IsGuarding && player.CurrentStamina > 0)
        {
            stateMachine.ChangeState(new PlayerGuardState(player, stateMachine));
            return;
        }

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

public class PlayerGuardState : BasePlayerState
{
    public PlayerGuardState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        player.IsGuarding = true;
        player.GuardStartTime = Time.time;
        // 방어 애니메이션 트리거 등 추가 가능
        Debug.Log("[State] Enter Guard State");
    }

    public override void Exit()
    {
        player.IsGuarding = false;
        Debug.Log("[State] Exit Guard State");
    }

    public override void Update()
    {
        if (!player.IsGuarding || player.CurrentStamina <= 0)
        {
            if (player.MoveInput.sqrMagnitude > 0.01f)
                stateMachine.ChangeState(new PlayerMoveState(player, stateMachine));
            else
                stateMachine.ChangeState(new PlayerIdleState(player, stateMachine));
        }
    }

    public override void FixedUpdate()
    {
        // 방어 중 이동 속도 감소 적용 (Server 로직과 맞춤)
        float targetSpeed = player.MoveSpeed * 0.3f;
        Vector2 targetVelocity = player.MoveInput * targetSpeed;

        player.Rb.linearVelocity = Vector2.MoveTowards(
            player.Rb.linearVelocity, 
            targetVelocity, 
            DECELERATION * Time.fixedDeltaTime
        );
    }
}
