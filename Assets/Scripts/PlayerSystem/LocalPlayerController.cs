using UnityEngine;

public class LocalPlayerController : PlayerController
{
    private const float RECONCILIATION_THRESHOLD = 0.8f; // 이 거리보다 멀어질 때만 강제 보정
    private const float POSITION_SMOOTHING = 5.0f;       // 보정 시 부드러움 정도

    [Header("Weapon Settings (Sephiria Style)")]
    [SerializeField] private float orbitDistance = 0.5f;      // 캐릭터 중심에서 자루까지의 거리
    [SerializeField] private float weaponRotationOffset = -90f; // 기본 오프셋 (IWeapon 없을 때 대비)
    [SerializeField] private float guardPoseOffset = -15f;    // 마우스 방향 대비 검이 휘어진 정도 (가드 자세 느낌)
    [SerializeField] private float lerpSpeed = 25f;           // 위치/회전 보정 속도

    private IWeapon currentWeapon;

    protected override void Awake()
    {
        if (visualsTransform == null) visualsTransform = transform.Find("Visuals");
        base.Awake();
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

        // [자동 설정] weaponSocket이 비어 있다면 자식 중에서 찾아봅니다.
        if (weaponSocket == null)
        {
            // [중요] Visuals 자식이 아닌 루트 자식에서 먼저 찾습니다. (Flip 영향 방지)
            weaponSocket = transform.Find("Weapon_Socket");
            if (weaponSocket == null) weaponSocket = transform.Find("Visuals/Weapon_Socket");
            
            if (weaponSocket == null)
            {
                var combat = GetComponentInChildren<PlayerCombat>();
                if (combat != null) weaponSocket = combat.weaponSocket;
            }

            if (weaponSocket != null)
                Debug.Log($"<color=cyan>[LocalPlayer]</color> WeaponSocket found: {weaponSocket.name}");
        }

        // [무기 생성] 게임 시작 시 소드 프리팹 생성
        if (weaponSocket != null)
        {
            GameObject swordPrefab = Resources.Load<GameObject>("Prefabs/Sword");
            if (swordPrefab != null)
            {
                GameObject swordInstance = Instantiate(swordPrefab, weaponSocket);
                currentWeapon = swordInstance.GetComponent<IWeapon>();
                
                // 1. [위치 보정] 자루(Pivot)가 소켓 원점에 오도록 설정
                if (currentWeapon != null && currentWeapon.Pivot != null)
                {
                    // Pivot의 로컬 좌표만큼 빼서, Pivot 오브젝트가 소켓의 (0,0)에 오도록 함
                    swordInstance.transform.localPosition = -currentWeapon.Pivot.localPosition;
                }
                else
                {
                    swordInstance.transform.localPosition = Vector3.zero;
                }

                // 2. [각도 보정] 생성 시 무기 고유의 AngleOffset을 Z축에 적용 (유저 제안 방식)
                float initialZ = (currentWeapon != null) ? currentWeapon.AngleOffset : weaponRotationOffset;
                swordInstance.transform.localRotation = Quaternion.Euler(0, 0, initialZ);
                
                Debug.Log($"<color=green>[LocalPlayer]</color> Weapon instantiated (Pos/Rot Offset Applied): {swordInstance.name}");
            }
            else
            {
                Debug.LogError("<color=red>[LocalPlayer]</color> Failed to load Sword prefab from Resources/Prefabs/Sword");
            }
        }
        else
        {
            Debug.LogWarning("<color=orange>[LocalPlayer]</color> WeaponSocket is null! Please assign it in Inspector or name a child 'Weapon_Socket'");
        }

        StateMachine.ChangeState(new PlayerIdleState(this, StateMachine));
    }

    protected override void Update()
    {
        HandleInput();
        UpdateFacing();         // 마우스 위치에 따른 캐릭터 반전
        UpdateWeaponRotation(); // 세피리아 스타일 무기 궤도 회전
        base.Update();
    }

    private void UpdateWeaponRotation()
    {
        if (weaponSocket == null || IsAttacking || IsInRecovery || IsWeaponLocked) return;

        // 1. 마우스의 '로컬' 위치 계산 (루트가 Flip 되지 않으므로 월드 방향과 일치)
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 localMousePos = transform.InverseTransformPoint(mouseWorldPos);
        
        // 2. 로컬 각도 계산
        float angle = Mathf.Atan2(localMousePos.y, localMousePos.x) * Mathf.Rad2Deg;

        // 3. 궤도(Orbit) 위치 계산 (로컬 기준)
        Vector3 targetSocketPos = new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad) * orbitDistance,
            Mathf.Sin(angle * Mathf.Deg2Rad) * orbitDistance,
            0
        );
        weaponSocket.localPosition = Vector3.Lerp(weaponSocket.localPosition, targetSocketPos, Time.deltaTime * lerpSpeed);

        // 4. 웨폰 반전 및 회전 적용
        // [핵심] localMousePos.x의 부호를 보고 무기 날의 상하 반전을 결정합니다.
        weaponSocket.localScale = new Vector3(1, (localMousePos.x < 0) ? -1f : 1f, 1);
        
        // 5. 최종 회전 (angle을 그대로 써서 마우스 방향을 향하게 함)
        float finalAngle = angle;
        
        // 가드 오프셋 대칭 적용
        finalAngle += (localMousePos.x < 0 ? -guardPoseOffset : guardPoseOffset);
        
        // [수정] 회전에도 Lerp 적용하여 공격 종료 후 부드럽게 복귀
        weaponSocket.localRotation = Quaternion.Lerp(weaponSocket.localRotation, Quaternion.Euler(0, 0, finalAngle), Time.deltaTime * lerpSpeed);
    }

    protected override void UpdateFacing()
    {
        // 로컬 플레이어는 마우스 방향에 맞춰 캐릭터(Visuals) Flip
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        if (mousePos.x < transform.position.x && IsFacingRight) Flip();
        else if (mousePos.x > transform.position.x && !IsFacingRight) Flip();
    }

    private float GetCurrentAimAngle()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (mouseWorldPos - transform.position);
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    private void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // [수정] 공격 중 이동 차단 해제 (자유 이동 허용)
        MoveInput = new Vector2(h, v).normalized;

        IsSprinting = Input.GetKey(KeyCode.LeftShift);
        IsGuarding = Input.GetMouseButton(1); 

        float aimAngle = GetCurrentAimAngle();

        // 입력값과 에임 각도를 서버로 전송
        NetworkManager.Instance.SendInput(MoveInput, aimAngle, IsAttacking);
    }

    public void SyncState(Vector2 serverPos, float stamina, float maxStamina)
    {
        CurrentStamina = stamina;
        MaxStamina = maxStamina;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateStamina(CurrentStamina, MaxStamina);
        }

        float dist = Vector2.Distance(transform.position, serverPos);

        // [수정] 미끄러지는 느낌 방지
        // 0.8m 이상의 큰 차이가 날 때만 서버 위치로 강제 보정합니다.
        // 그 미만의 작은 오차는 클라이언트의 부드러운 물리 이동을 우선시합니다.
        if (dist > 0.8f) 
        {
            transform.position = serverPos;
        }
        // Lerp 보정을 삭제하여 물리 엔진과의 충돌(미끄러짐)을 원천 차단합니다.
    }

    // PlayerController의 MoveSpeed 속성을 동적으로 조절하기 위해 재정의하거나 로직 추가
    public float GetCurrentSpeed()
    {
        float speed = MoveSpeed;
        if (CurrentStamina <= 0) speed *= 0.5f; // 탈진 패널티
        return speed;
    }
}
