using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 상단 자동 전투의 적 몬스터 액터.
/// 우측에서 등장하여 용사에게 접근 후 공격/피격/사망 생명주기를 수행합니다.
/// </summary>
public class EnemyMonsterActor : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private string monsterName = "Skeleton";
    [SerializeField] private int curHp;
    [SerializeField] private int maxHp;
    [SerializeField] private int atkPower = 10;
    [SerializeField] private float moveSpeed = 2.0f;

    public string MonsterName => monsterName;
    public int CurHp => curHp;
    public int MaxHp => maxHp;
    public int AtkPower => atkPower;
    public bool IsDead => curHp <= 0;

    public event Action<int, int> OnHpChanged;
    public event Action OnDied;

    private Vector3 initialPos;
    private CancellationTokenSource monsterCts;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        initialPos = transform.localPosition;
    }

    public void Setup(string name, int hp, int atk, Vector3 spawnPos)
    {
        monsterName = name;
        maxHp = hp;
        curHp = hp;
        atkPower = atk;
        transform.localPosition = spawnPos;
        gameObject.SetActive(true);

        if (visualRoot != null)
        {
            visualRoot.localScale = new Vector3(-Mathf.Abs(visualRoot.localScale.x), visualRoot.localScale.y, visualRoot.localScale.z); // 좌측 응시
        }

        OnHpChanged?.Invoke(curHp, maxHp);
    }

    /// <summary>
    /// 목표 X 좌표까지 걸어서 접근
    /// </summary>
    public async UniTask MoveToTargetXAsync(float targetX, CancellationToken ct)
    {
        if (animator != null && animator.runtimeAnimatorController != null && animator.HasState(0, Animator.StringToHash("Walk")))
        {
            animator.Play("Walk", 0, 0f);
        }

        while (transform.localPosition.x > targetX && !ct.IsCancellationRequested)
        {
            float step = moveSpeed * Time.deltaTime;
            transform.localPosition = new Vector3(
                Mathf.MoveTowards(transform.localPosition.x, targetX, step),
                transform.localPosition.y,
                transform.localPosition.z);

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        if (animator != null && animator.runtimeAnimatorController != null && animator.HasState(0, Animator.StringToHash("Idle")))
        {
            animator.Play("Idle", 0, 0f);
        }
    }

    /// <summary>
    /// 공격 모션
    /// </summary>
    public async UniTask PerformAttackAsync(CancellationToken ct)
    {
        if (animator != null && animator.runtimeAnimatorController != null && animator.HasState(0, Animator.StringToHash("Attack")))
        {
            animator.Play("Attack", 0, 0f);
        }

        Vector3 pos = visualRoot.localPosition;
        try
        {
            visualRoot.localPosition = pos + new Vector3(-0.2f, 0f, 0f);
            await UniTask.Delay(100, cancellationToken: ct);
            visualRoot.localPosition = pos;
            await UniTask.Delay(150, cancellationToken: ct);
        }
        catch (OperationCanceledException)
        {
            visualRoot.localPosition = pos;
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        curHp = Mathf.Max(0, curHp - damage);
        OnHpChanged?.Invoke(curHp, maxHp);

        PlayHitShakeAsync().Forget();

        if (curHp <= 0)
        {
            DieAsync().Forget();
        }
    }

    private async UniTaskVoid PlayHitShakeAsync()
    {
        if (animator != null && animator.runtimeAnimatorController != null && animator.HasState(0, Animator.StringToHash("Hit")))
        {
            animator.Play("Hit", 0, 0f);
        }

        Vector3 orig = visualRoot.localPosition;
        visualRoot.localPosition = orig + new Vector3(0.12f, 0.05f, 0f);
        await UniTask.Delay(50);
        if (visualRoot != null) visualRoot.localPosition = orig;
    }

    private async UniTaskVoid DieAsync()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            if (animator.HasState(0, Animator.StringToHash("Dead")))
            {
                animator.Play("Dead", 0, 0f);
            }
            else if (animator.HasState(0, Animator.StringToHash("Die")))
            {
                animator.Play("Die", 0, 0f);
            }
        }

        OnDied?.Invoke();

        // 0.4초 후 페이드아웃 및 풀 반환
        await UniTask.Delay(400);
        gameObject.SetActive(false);
    }
}
