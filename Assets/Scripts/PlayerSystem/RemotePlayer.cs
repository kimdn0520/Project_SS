using UnityEngine;
using DG.Tweening;

public class RemotePlayer : PlayerController
{
    private int _id;
    private Vector2 _targetPosition;

    public void Init(int id)
    {
        _id = id;
        name = $"RemotePlayer_{id}";
        SetColor(Color.red);
        
        // [자동 설정] 리모트 플레이어는 물리 엔진의 영향을 받지 않고 서버 위치를 따라야 함
        if (Rb != null)
        {
            Rb.bodyType = RigidbodyType2D.Kinematic;
            Rb.simulated = true;
        }
        
        StateMachine.ChangeState(new PlayerIdleState(this, StateMachine));
    }

    public void SetState(Vector2 position, bool isSprinting, Vector2 moveInput)
    {
        _targetPosition = position;
        IsSprinting = isSprinting;
        MoveInput = moveInput;

        // Visual interpolation
        transform.DOMove(new Vector3(position.x, position.y, 0), 0.1f).SetEase(Ease.Linear);
    }

    protected override void FixedUpdate()
    {
        // 리모트 플레이어는 물리 이동을 하지 않으므로 FixedUpdate 호출을 스킵하거나 최소화
    }
}
