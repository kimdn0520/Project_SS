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
    public virtual void FixedUpdate() 
    {
        // 물리 속도를 0으로 유지하여 transform 이동을 방해하지 않게 합니다.
        if (player.Rb != null && player.Rb.bodyType == RigidbodyType2D.Dynamic)
        {
            player.Rb.linearVelocity = Vector2.zero;
        }
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

        // 이동 처리
        float speed = player.MoveSpeed;
        player.transform.Translate(player.MoveInput * speed * Time.deltaTime);

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

        // 이동 처리 (스프린트 속도 적용)
        float speed = player.MoveSpeed * player.SprintMultiplier;
        player.transform.Translate(player.MoveInput * speed * Time.deltaTime);

        if (!player.IsSprinting || player.CurrentStamina <= 0)
        {
            stateMachine.ChangeState(new PlayerMoveState(player, stateMachine));
        }
    }
}
