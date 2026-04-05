using UnityEngine;
using System.Collections.Generic;

public class Sword : MonoBehaviour, IWeapon
{
    [Header("Weapon Settings")]
    [SerializeField] private float angleOffset = 25f; // 검 스프라이트 기본 방향 오프셋
    [SerializeField] private Transform pivot;         // 검 자루(Hilt) 위치

    [Header("Hit Detection")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private Vector2 hitBoxSize = new Vector2(1.2f, 1.5f);
    [SerializeField] private Vector2 hitBoxOffset = new Vector2(0.8f, 0f);

    public float AngleOffset => angleOffset;
    public Transform Pivot => pivot;

    private List<GameObject> hitTargets = new List<GameObject>();
    private bool isHitEnabled = false;
    private Vector2 attackDirection;

    private void Start()
    {
        // Pivot이 비어있다면 자기 자신으로 설정 (기본값)
        if (pivot == null) pivot = transform;
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
        Vector2 boxCenter = (Vector2)transform.position + (Vector2)(transform.right * hitBoxOffset.x) + (Vector2)(transform.up * hitBoxOffset.y);
        Collider2D[] results = Physics2D.OverlapBoxAll(boxCenter, hitBoxSize, transform.eulerAngles.z, hitLayers);

        foreach (var collider in results)
        {
            if (hitTargets.Contains(collider.gameObject)) continue;

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
}
