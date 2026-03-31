using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class PlayerController : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] protected Transform visualsTransform;
    [SerializeField] protected SpriteRenderer spriteRenderer;

    [Header("Stats")]
    public float MoveSpeed = 5f;
    public float SprintMultiplier = 1.6f;
    public float MaxStamina = 100f;
    public float CurrentStamina = 100f;
    public float StaminaRegenRate = 10f;
    public float SprintStaminaCost = 25f;

    public Rigidbody2D Rb { get; protected set; }
    public Animator Animator { get; protected set; }
    public StateMachine StateMachine { get; protected set; }
    
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

    private void UpdateFacing()
    {
        if (MoveInput.x > 0 && !IsFacingRight) Flip();
        else if (MoveInput.x < 0 && IsFacingRight) Flip();
    }

    private void Flip()
    {
        if (visualsTransform == null) return;

        IsFacingRight = !IsFacingRight;
        Vector3 scale = visualsTransform.localScale;
        scale.x *= -1;
        visualsTransform.localScale = scale;
    }

    public void SetColor(Color color)
    {
        if (spriteRenderer != null) spriteRenderer.color = color;
    }
}
