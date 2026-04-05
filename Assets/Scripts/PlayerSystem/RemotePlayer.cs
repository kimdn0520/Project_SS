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

        // [비주얼] 방어 중일 때 색상 변경 (실무에서는 애니메이션 트리거 사용)
        if (IsGuarding)
        {
            SetColor(new Color(1f, 0.5f, 0.5f)); // 연한 빨강 (방어 중)
        }
        else
        {
            SetColor(Color.red);
        }
    }

    protected override void FixedUpdate()
    {
        // 리모트 플레이어는 물리 이동을 하지 않으므로 FixedUpdate 호출을 스킵하거나 최소화
    }
}
