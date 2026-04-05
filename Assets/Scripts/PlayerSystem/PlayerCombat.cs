using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    public Transform weaponSocket;
    public GameObject currentWeaponPrefab;
    
    [Header("Settings")]
    public float attackCooldown = 0.5f;
    public float lungeForce = 7f;
    public float lungeDuration = 0.12f;

    private PlayerController player;
    private Sword currentSword;
    private float lastAttackTime;

    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
    }

    private void Update()
    {
        // 로컬 플레이어인 경우에만 입력 처리 (추후 NetworkManager와 연동 필요)
        if (player is LocalPlayerController)
        {
            if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
            }
        }
    }

    public void Attack()
    {
        if (player == null || player.IsAttacking || player.IsGuarding) return;

        lastAttackTime = Time.time;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        player.IsAttacking = true;
        
        // 1. 공격 방향 결정 (마우스 방향)
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 attackDir = ((Vector2)(mousePos - transform.position)).normalized;

        // 2. 공격 시 전진 (Lunge)
        if (player.Rb != null)
        {
            player.Rb.AddForce(attackDir * lungeForce, ForceMode2D.Impulse);
        }

        // 3. 무기 히트 판정 활성화
        if (currentSword == null) currentSword = GetComponentInChildren<Sword>();
        if (currentSword != null)
        {
            currentSword.EnableHit(attackDir);
        }

        // TODO: 애니메이션 트리거
        // if (player.Animator != null) player.Animator.SetTrigger("Attack");

        yield return new WaitForSeconds(lungeDuration);

        // 4. 히트 판정 종료
        if (currentSword != null)
        {
            currentSword.DisableHit();
        }

        player.IsAttacking = false;
    }
}
