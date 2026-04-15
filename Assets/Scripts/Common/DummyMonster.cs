using UnityEngine;
using System.Collections;

public class DummyMonster : MonoBehaviour, IDamageable
{
    [Header("Monster Identity")]
    public int MonsterId;
    public float MaxHealth = 100f;
    public float CurrentHealth = 100f;

    [Header("Sync Settings")]
    [SerializeField] private float interpolationSpeed = 10f;
    private Vector2 _targetPosition;
    private Vector2 _moveInput;
    private MonsterState _currentState;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color chaseColor = new Color(1f, 0.8f, 0.8f);
    [SerializeField] private Color idleColor = Color.white;
    
    private Color _originalColor;
    private Rigidbody2D _rb;
    private Animator _anim;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) _originalColor = spriteRenderer.color;
        
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponentInChildren<Animator>();
        _targetPosition = transform.position;
    }

    private void Update()
    {
        // 서버 위치로 보간 이동
        transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * interpolationSpeed);
        UpdateVisuals();
    }

    public void Init(int id)
    {
        MonsterId = id;
        gameObject.name = $"Monster_{id}";
    }

    public void SetState(Vector2 position, Vector2 moveInput, MonsterState state)
    {
        _targetPosition = position;
        _moveInput = moveInput;
        
        if (_currentState != state)
        {
            _currentState = state;
            Debug.Log($"<color=yellow>[Monster {MonsterId}]</color> State changed to: {state}");
        }
    }

    public void PlayAttackEffect(Vector2 targetPos)
    {
        Debug.Log($"<color=red>[Monster {MonsterId}]</color> ATTACK! Target: {targetPos}");
        if (_anim != null) _anim.SetTrigger("Attack");
        if (spriteRenderer != null) StartCoroutine(FlashColor(Color.yellow, 0.3f));
    }

    private void UpdateVisuals()
    {
        // 방향 전환
        if (_moveInput.x > 0.01f) transform.localScale = new Vector3(1, 1, 1);
        else if (_moveInput.x < -0.01f) transform.localScale = new Vector3(-1, 1, 1);

        // 상태별 색상 변화 (디버깅용)
        if (spriteRenderer != null)
        {
            spriteRenderer.color = _currentState == MonsterState.Chase ? chaseColor : idleColor;
        }

        if (_anim != null)
        {
            _anim.SetBool("IsMoving", _moveInput.magnitude > 0.01f);
            _anim.SetInteger("State", (int)_currentState);
        }
    }

    public void TakeDamage(float damage, Vector2 knockback)
    {
        CurrentHealth -= damage;
        if (spriteRenderer != null) StartCoroutine(FlashColor(Color.red, 0.1f));
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.AddForce(knockback, ForceMode2D.Impulse);
        }
    }

    private IEnumerator FlashColor(Color color, float duration)
    {
        Color prevColor = spriteRenderer.color;
        spriteRenderer.color = color;
        yield return new WaitForSeconds(duration);
        spriteRenderer.color = prevColor;
    }

    [Header("AI Debug (Match with Server)")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float attackRange = 1.8f;

    private void OnDrawGizmos()
    {
        // 공격 범위 표시 (서버와 동일한 1.8f)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 감지 범위 표시 (서버와 동일한 10.0f)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // 이동 방향 표시
        if (_moveInput.magnitude > 0)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + _moveInput);
        }
    }
}
