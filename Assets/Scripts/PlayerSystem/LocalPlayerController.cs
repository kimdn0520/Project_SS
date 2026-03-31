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
        // [클래스 시스템] 기본값으로 Warrior 스탯 로드
        PlayerStatsSO defaultStats = Resources.Load<PlayerStatsSO>("Data/PlayerStats/Warrior");
        if (defaultStats != null)
        {
            ApplyStats(defaultStats);
        }
        else
        {
            Debug.LogError("Failed to load default Warrior stats from Resources/Data/PlayerStats/Warrior");
        }

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
        
        // UI 업데이트
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateStamina(CurrentStamina, MaxStamina);
        }

        // [지터 해결] 이전처럼 오차가 클 때만 강제로 위치를 고정합니다.
        float dist = Vector2.Distance(transform.position, serverPos);
        if (dist > 0.5f)
        {
            transform.position = serverPos;
        }
    }

    // PlayerController의 MoveSpeed 속성을 동적으로 조절하기 위해 재정의하거나 로직 추가
    public float GetCurrentSpeed()
    {
        float speed = MoveSpeed;
        if (CurrentStamina <= 0) speed *= 0.5f; // 탈진 패널티
        return speed;
    }
}
