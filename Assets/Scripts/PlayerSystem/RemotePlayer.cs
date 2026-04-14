using UnityEngine;
using DG.Tweening;

public class RemotePlayer : PlayerController
{
    private int _id;
    private Vector2 _targetPosition;

    [Header("Weapon Settings (Remote Sync)")]
    [SerializeField] private float orbitDistance = 0.5f;
    [SerializeField] private float guardPoseOffset = -15f;
    [SerializeField] private float lerpSpeed = 15f; // 원격 플레이어는 조금 더 부드럽게

    private float _targetAimAngle;
    private bool _remoteIsAttacking;
    private IWeapon currentWeapon;

    public void Init(int id)
    {
        _id = id;
        name = $"RemotePlayer_{id}";
        
        // [자동 설정] 리모트 플레이어는 물리 엔진의 영향을 받지 않고 서버 위치를 따라야 함
        if (Rb != null)
        {
            Rb.bodyType = RigidbodyType2D.Kinematic;
            Rb.simulated = true;
        }

        // [무기 소켓 자동 설정]
        if (weaponSocket == null)
        {
            weaponSocket = transform.Find("Weapon_Socket");
            if (weaponSocket == null) weaponSocket = transform.Find("Visuals/Weapon_Socket");
        }

        // [무기 생성]
        if (weaponSocket != null)
        {
            GameObject swordPrefab = Resources.Load<GameObject>("Prefabs/Sword");
            if (swordPrefab != null)
            {
                GameObject swordInstance = Instantiate(swordPrefab, weaponSocket);
                currentWeapon = swordInstance.GetComponent<IWeapon>();
                
                if (currentWeapon != null && currentWeapon.Pivot != null)
                    swordInstance.transform.localPosition = -currentWeapon.Pivot.localPosition;
                else
                    swordInstance.transform.localPosition = Vector3.zero;

                float initialZ = (currentWeapon != null) ? currentWeapon.AngleOffset : -90f;
                swordInstance.transform.localRotation = Quaternion.Euler(0, 0, initialZ);
            }
        }
        
        StateMachine.ChangeState(new PlayerIdleState(this, StateMachine));
    }

    public void SetState(Vector2 position, float stamina, float maxStamina, bool isSprinting, Vector2 moveInput, float aimAngle, bool isAttacking)
    {
        _targetPosition = position;
        CurrentStamina = stamina;
        MaxStamina = maxStamina;
        IsSprinting = isSprinting;
        MoveInput = moveInput;
        _targetAimAngle = aimAngle;

        // 공격 트리거 (서버에서 받은 공격 입력을 클라이언트 비주얼로 표현)
        if (isAttacking && !_remoteIsAttacking)
        {
            var combat = GetComponentInChildren<PlayerCombat>();
            if (combat != null)
            {
                combat.Attack();
            }
        }
        _remoteIsAttacking = isAttacking;

        // Visual interpolation
        transform.DOMove(new Vector3(position.x, position.y, 0), 0.1f).SetEase(Ease.Linear);
    }

    protected override void Update()
    {
        base.Update();
        UpdateRemoteWeaponRotation();
    }

    private void UpdateRemoteWeaponRotation()
    {
        if (weaponSocket == null) return;

        // 1. 서버에서 받은 각도를 기반으로 궤도 위치 계산
        // RemotePlayer는 이미 UpdateFacing()에서 에임 방향에 따라 visualsTransform이 Flip 되어 있음.
        // 루트는 Flip 되지 않으므로 월드 방향과 일치함.

        // 로컬 마우스 위치 시뮬레이션 (루트 기준)
        float rad = _targetAimAngle * Mathf.Deg2Rad;
        Vector3 worldAimPos = transform.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * 10f;
        Vector3 localAimPos = transform.InverseTransformPoint(worldAimPos);

        float localAngle = Mathf.Atan2(localAimPos.y, localAimPos.x) * Mathf.Rad2Deg;

        Vector3 targetSocketPos = new Vector3(
            Mathf.Cos(localAngle * Mathf.Deg2Rad) * orbitDistance,
            Mathf.Sin(localAngle * Mathf.Deg2Rad) * orbitDistance,
            0
        );
        weaponSocket.localPosition = Vector3.Lerp(weaponSocket.localPosition, targetSocketPos, Time.deltaTime * lerpSpeed);

        // 2. 웨폰 반전 및 회전 적용
        // [핵심] localAimPos.x의 부호를 보고 무기 날의 상하 반전을 결정합니다.
        weaponSocket.localScale = new Vector3(1, (localAimPos.x < 0) ? -1f : 1f, 1);
        
        float finalAngle = localAngle;
        if (IsGuarding)
        {
            finalAngle += (localAimPos.x < 0 ? -guardPoseOffset : guardPoseOffset);
        }
        
        weaponSocket.localRotation = Quaternion.Lerp(weaponSocket.localRotation, Quaternion.Euler(0, 0, finalAngle), Time.deltaTime * lerpSpeed);
    }

    protected override void UpdateFacing()
    {
        // 리모트 플레이어는 에임 각도에 따라 캐릭터 Flip
        // -90 ~ 90도 사이면 오른쪽, 그 외는 왼쪽
        bool shouldFaceRight = _targetAimAngle > -90f && _targetAimAngle <= 90f;

        if (shouldFaceRight && !IsFacingRight) Flip();
        else if (!shouldFaceRight && IsFacingRight) Flip();
    }

    protected override void FixedUpdate()
    {
        // 리모트 플레이어는 물리 이동을 하지 않으므로 FixedUpdate 호출을 스킵하거나 최소화
    }
}
