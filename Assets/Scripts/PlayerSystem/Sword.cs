using UnityEngine;
using System.Collections.Generic;

public class Sword : MonoBehaviour
{
    [Header("Hit Detection")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private Vector2 hitBoxSize = new Vector2(1.2f, 1.5f);
    [SerializeField] private Vector2 hitBoxOffset = new Vector2(0.8f, 0f);

    private List<GameObject> hitTargets = new List<GameObject>();
    private bool isHitEnabled = false;
    private Vector2 attackDirection;

    private void Start()
    {
        // 프리팹 기반이므로 Start에서는 특별한 생성 로직을 넣지 않음
        // (필요 시 효과음 로드 등을 수행 가능)
    }

    public void EnableHit(Vector2 direction)
    {
        hitTargets.Clear();
        isHitEnabled = true;
        attackDirection = direction;
    }

    public void DisableHit()
    {
        isHitEnabled = false;
    }

    private void Update()
    {
        if (!isHitEnabled) return;

        DetectHits();
    }

    private void DetectHits()
    {
        // 무기 소켓의 회전을 고려하여 OverlapBox 생성
        // transform.right가 무기의 앞방향이라고 가정 (Socket 회전에 따라 결정됨)
        Vector2 boxCenter = (Vector2)transform.position + (Vector2)(transform.right * hitBoxOffset.x) + (Vector2)(transform.up * hitBoxOffset.y);
        Collider2D[] results = Physics2D.OverlapBoxAll(boxCenter, hitBoxSize, transform.eulerAngles.z, hitLayers);

        foreach (var collider in results)
        {
            if (hitTargets.Contains(collider.gameObject)) continue;

            // [실무] IDamageable 인터페이스 호출
            IDamageable target = collider.GetComponent<IDamageable>();
            if (target != null)
            {
                Vector2 knockback = attackDirection * knockbackForce;
                target.TakeDamage(damage, knockback);
                
                Debug.Log($"<color=red>[Combat]</color> Sword hit: {collider.name} for {damage} damage");
                hitTargets.Add(collider.gameObject);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 에디터에서 히트 박스 영역 시각화
        Gizmos.color = Color.red;
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(hitBoxOffset, hitBoxSize);
        Gizmos.matrix = oldMatrix;
    }
}
