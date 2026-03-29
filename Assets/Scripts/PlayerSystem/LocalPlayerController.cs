using UnityEngine;

public class LocalPlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5.0f;
    private Vector2 _lastMoveInput;

    private void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector2 moveDir = new Vector2(h, v).normalized;

        // 1. Predictive Movement (클라이언트 측에서 즉시 이동)
        if (moveDir.sqrMagnitude > 0)
        {
            transform.Translate(moveDir * moveSpeed * Time.deltaTime);
        }

        // 2. 서버로 입력 전송 (매 프레임 혹은 입력 변화 시)
        if (moveDir != _lastMoveInput || moveDir.sqrMagnitude > 0)
        {
            _lastMoveInput = moveDir;
            NetworkManager.Instance.SendInput(moveDir);
        }
    }

    // 서버의 공인된 위치로 보정 (필요 시 호출)
    public void Reconcile(Vector2 serverPosition)
    {
        float dist = Vector2.Distance(transform.position, serverPosition);
        if (dist > 0.5f) // 오차가 클 때만 강제 보정
        {
            transform.position = serverPosition;
        }
    }
}
