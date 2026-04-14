using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class PlayerController : MonoBehaviour, IDamageable
{
    [Header("Visuals")]
    [SerializeField] protected Transform visualsTransform;
    [SerializeField] protected Transform weaponSocket;
    [SerializeField] protected SpriteRenderer spriteRenderer;

    [Header("Stats")]
    public float MaxHP = 100f;
    public float CurrentHP = 100f;
    public float MoveSpeed = 5f;
    public float SprintMultiplier = 1.6f;
    public float MaxStamina = 100f;
    public float CurrentStamina = 100f;
    public float StaminaRegenRate = 10f;
    public float SprintStaminaCost = 25f;
    public float GuardStaminaCost = 10f;
    public float GuardDamageReduction = 0.5f;

    public bool IsAttacking { get; set; }
    public bool IsInRecovery { get; set; }
    public bool IsGuarding { get; set; }
    public bool IsWeaponLocked { get; set; } // 추가: 공격/콤보 중 무기 회전 고정 여부
    public float GuardStartTime { get; set; }
    public float PerfectParryWindow { get; set; } = 0.2f;

    public Rigidbody2D Rb { get; protected set; }
    public Animator Animator { get; protected set; }
    public StateMachine StateMachine { get; protected set; }
    public Transform WeaponSocket => weaponSocket;
    
    public Vector2 MoveInput { get; set; }
    public bool IsSprinting { get; set; }
    public bool IsFacingRight { get; private set; } = true;

    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        
        // [자동 설정] 탑다운 2D 물리 기본 설정
        if (Rb != null)
        {
            Rb.gravityScale = 0f;
            Rb.freezeRotation = true;
            Rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        if (visualsTransform != null)
        {
            Animator = visualsTransform.GetComponent<Animator>();
            spriteRenderer = visualsTransform.GetComponent<SpriteRenderer>();
        }
        
        StateMachine = new StateMachine();
    }

    protected virtual void Update()
    {
        StateMachine.Update();
        UpdateFacing();
    }

    protected virtual void FixedUpdate()
    {
        StateMachine.FixedUpdate();
    }

    protected virtual void UpdateFacing()
    {
        if (MoveInput.x > 0 && !IsFacingRight) Flip();
        else if (MoveInput.x < 0 && IsFacingRight) Flip();
    }

    protected void Flip()
    {
        IsFacingRight = !IsFacingRight;
        if (visualsTransform != null)
        {
            Vector3 scale = visualsTransform.localScale;
            scale.x *= -1;
            visualsTransform.localScale = scale;
        }
    }

    public virtual void TakeDamage(float damage, Vector2 knockback)
    {
        float finalDamage = damage;

        if (IsGuarding)
        {
            // [방향성 방어 로직] 
            // 넉백 방향(공격이 가해진 방향의 반대)과 캐릭터가 바라보는 방향(무기 방향)을 비교
            Vector2 attackDir = -knockback.normalized;
            Vector2 lookDir = weaponSocket.right; // 무기의 정면 방향

            float dot = Vector2.Dot(attackDir, lookDir);

            // 내적 값이 0.5 이상이면 약 60도 범위 내의 방어로 간주
            if (dot > 0.5f)
            {
                // [패링 로직] 방어 시작 직후(PerfectParryWindow)라면 데미지 0 및 특수 효과
                float timeSinceGuard = Time.time - GuardStartTime;
                if (timeSinceGuard <= PerfectParryWindow)
                {
                    finalDamage = 0;
                    Debug.Log($"<color=cyan>[Combat]</color> PERFECT PARRY! Time: {timeSinceGuard:F3}s");
                    // 패링 성공 시 공격자에게 경직을 주거나 효과음 재생 로직 추가 가능
                    return; 
                }

                finalDamage *= (1f - GuardDamageReduction);
                Debug.Log($"[Combat] Guarded! Dot: {dot:F2}, Reduced damage to {finalDamage}");
            }
            else
            {
                Debug.Log($"[Combat] Guard Failed (Wrong Direction). Dot: {dot:F2}");
            }
        }

        CurrentHP -= finalDamage;
        Rb.AddForce(knockback, ForceMode2D.Impulse);

        Debug.Log($"[Combat] Player took {finalDamage} damage. Current HP: {CurrentHP}");

        if (CurrentHP <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Debug.Log("[Combat] Player Died");
        // 사망 로직 (리스폰 등)
    }

    public void ApplyStats(PlayerStatsSO stats)
    {
        if (stats == null) return;

        MaxHP = stats.MaxHP;
        CurrentHP = stats.MaxHP;
        MoveSpeed = stats.MoveSpeed;
        SprintMultiplier = stats.SprintMultiplier;
        MaxStamina = stats.MaxStamina;
        CurrentStamina = stats.MaxStamina;
        StaminaRegenRate = stats.StaminaRegenRate;
        SprintStaminaCost = stats.SprintStaminaCost;
        GuardStaminaCost = stats.GuardStaminaCost;
        GuardDamageReduction = stats.GuardDamageReduction;
        PerfectParryWindow = stats.PerfectParryWindow;
        
        Debug.Log($"[PlayerController] {stats.ClassName} Stats Applied!");
    }

    public void SetColor(Color color)
    {
        if (spriteRenderer != null) spriteRenderer.color = color;
    }
}
