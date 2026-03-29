using UnityEngine;
using DG.Tweening;

public class RemotePlayer : MonoBehaviour
{
    private int _id;
    private Vector2 _targetPosition;

    public void Init(int id)
    {
        _id = id;
        name = $"RemotePlayer_{id}";
    }

    public void SetTargetPosition(Vector2 position)
    {
        _targetPosition = position;
        
        // DOTween을 사용하여 서버의 위치로 부드럽게 이동 (Reconciliation/Interpolation 기초)
        transform.DOMove(new Vector3(position.x, position.y, 0), 0.1f).SetEase(Ease.Linear);
    }
}
