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

        float dist = Vector2.Distance(transform.position, serverPos);

        // [Stop Protection] 
        // 입력이 없고(정지 상태), 오차가 크지 않으면(0.5m 이내) 서버의 보정을 무시합니다.
        // 이렇게 하면 멈췄을 때 뒤로 튕기는 현상이 사라집니다.
        bool isMoving = MoveInput.sqrMagnitude > 0.01f;
        if (!isMoving && dist < 0.5f)
        {
            return;
        }

        // 차이가 너무 클 때만 강제 순간이동
        if (dist > 1.2f) 
        {
            transform.position = serverPos;
        }
        else if (dist > 0.02f) // 이동 중이거나 오차가 클 때만 부드럽게 보정
        {
            transform.position = Vector2.Lerp(transform.position, serverPos, Time.deltaTime * 15f);
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
