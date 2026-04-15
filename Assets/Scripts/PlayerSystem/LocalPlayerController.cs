using UnityEngine;

public class LocalPlayerController : PlayerController
{
    private Vector2 _serverPosition;
    private const float POSITION_LERP_SPEED = 15f;
    private const float SNAP_THRESHOLD = 3.0f;

    [Header("Weapon Settings (Sephiria Style)")]
    [SerializeField] private float orbitDistance = 0.5f;      
    [SerializeField] private float weaponRotationOffset = -90f; 
    [SerializeField] private float guardPoseOffset = -15f;    
    [SerializeField] private float lerpSpeed = 25f;           

    private IWeapon currentWeapon;

    protected override void Awake()
    {
        if (visualsTransform == null) visualsTransform = transform.Find("Visuals");
        base.Awake();
        _serverPosition = transform.position;
    }

    private void Start()
    {
        PlayerStatsSO defaultStats = Resources.Load<PlayerStatsSO>("Data/PlayerStats/Warrior");
        if (defaultStats != null) ApplyStats(defaultStats);

        if (weaponSocket == null)
        {
            weaponSocket = transform.Find("Weapon_Socket");
            if (weaponSocket == null) weaponSocket = transform.Find("Visuals/Weapon_Socket");
        }

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

                float initialZ = (currentWeapon != null) ? currentWeapon.AngleOffset : weaponRotationOffset;
                swordInstance.transform.localRotation = Quaternion.Euler(0, 0, initialZ);
            }
        }

        StateMachine.ChangeState(new PlayerIdleState(this, StateMachine));
    }

    protected override void Update()
    {
        HandleInput();
        UpdateFacing();         
        UpdateWeaponRotation(); 
        
        // 서버 위치로 부드럽게 보간 (순간이동 방지 핵심 로직)
        if (Vector2.Distance(transform.position, _serverPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, _serverPosition, Time.deltaTime * POSITION_LERP_SPEED);
        }

        base.Update();
    }

    private void UpdateWeaponRotation()
    {
        if (weaponSocket == null || IsAttacking || IsInRecovery || IsWeaponLocked) return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 localMousePos = transform.InverseTransformPoint(mouseWorldPos);
        float angle = Mathf.Atan2(localMousePos.y, localMousePos.x) * Mathf.Rad2Deg;

        Vector3 targetSocketPos = new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad) * orbitDistance,
            Mathf.Sin(angle * Mathf.Deg2Rad) * orbitDistance,
            0
        );
        weaponSocket.localPosition = Vector3.Lerp(weaponSocket.localPosition, targetSocketPos, Time.deltaTime * lerpSpeed);
        weaponSocket.localScale = new Vector3(1, (localMousePos.x < 0) ? -1f : 1f, 1);
        
        float finalAngle = angle + (localMousePos.x < 0 ? -guardPoseOffset : guardPoseOffset);
        weaponSocket.localRotation = Quaternion.Lerp(weaponSocket.localRotation, Quaternion.Euler(0, 0, finalAngle), Time.deltaTime * lerpSpeed);
    }

    protected override void UpdateFacing()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (mousePos.x < transform.position.x && IsFacingRight) Flip();
        else if (mousePos.x > transform.position.x && !IsFacingRight) Flip();
    }

    private void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        MoveInput = new Vector2(h, v).normalized;
        IsSprinting = Input.GetKey(KeyCode.LeftShift);
        IsGuarding = Input.GetMouseButton(1); 

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (mouseWorldPos - transform.position);
        float aimAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        NetworkManager.Instance.SendInput(MoveInput, aimAngle, IsAttacking);
    }

    public void SyncState(Vector2 serverPos, float stamina, float maxStamina)
    {
        _serverPosition = serverPos;
        CurrentStamina = stamina;
        MaxStamina = maxStamina;
        
        if (UIManager.Instance != null) UIManager.Instance.UpdateStamina(CurrentStamina, MaxStamina);

        if (Vector2.Distance(transform.position, serverPos) > SNAP_THRESHOLD) 
        {
            transform.position = serverPos;
        }
    }
}
