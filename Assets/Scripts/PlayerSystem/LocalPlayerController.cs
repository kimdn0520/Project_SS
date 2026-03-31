using UnityEngine;

public class LocalPlayerController : PlayerController
{
    private const float RECONCILIATION_THRESHOLD = 0.8f; // 이 거리보다 멀어질 때만 강제 보정
    private const float POSITION_SMOOTHING = 5.0f;       // 보정 시 부드러움 정도

    protected override void Awake()
    {
        base.Awake();
        SetColor(Color.green);
    }

    private void Start()
    {
        StateMachine.ChangeState(new PlayerIdleState(this, StateMachine));
    }

    protected override void Update()
    {
        HandleInput();
        base.Update();
    }

    private void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        MoveInput = new Vector2(h, v).normalized;

        IsSprinting = Input.GetKey(KeyCode.LeftShift);

        // 입력값이 있을 때만 서버로 전송 (트래픽 최적화 기초)
        NetworkManager.Instance.SendInput(MoveInput);
    }

    public void SyncState(Vector2 serverPos, float stamina)
    {
        CurrentStamina = stamina;
        
        // [지터 해결] 이전처럼 오차가 클 때만 강제로 위치를 고정합니다.
        float dist = Vector2.Distance(transform.position, serverPos);
        if (dist > 0.5f)
        {
            transform.position = serverPos;
        }
    }
}
