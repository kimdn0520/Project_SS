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
    private Collider2D _weaponCollider;

    private void Awake()
    {
        // 최상위 혹은 자식 오브젝트에서 콜라이더를 찾습니다.
        _weaponCollider = GetComponentInChildren<Collider2D>();
        if (_weaponCollider != null)
        {
            _weaponCollider.isTrigger = true;
            _weaponCollider.enabled = false; // 평상시엔 꺼둠
        }
    }

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
        if (_weaponCollider != null) _weaponCollider.enabled = true;
    }

    public void DisableHit()
    {
        isHitEnabled = false;
        if (_weaponCollider != null) _weaponCollider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (!isHitEnabled) return;

        // 레이어 체크 (hitLayers에 포함된 레이어인지 확인)
        if (((1 << collider.gameObject.layer) & hitLayers) == 0) return;

        if (hitTargets.Contains(collider.gameObject)) return;

        // 1. 적이 IDamageable 인터페이스를 가지고 있는지 확인
        IDamageable target = collider.GetComponent<IDamageable>();
        if (target != null)
        {
            // [Immediate Feedback] 클라이언트 즉시 피드백
            if (HitStopManager.Instance != null) HitStopManager.Instance.Stop(0.05f);
            if (CameraShake.Instance != null) CameraShake.Instance.ImpactShake();

            int targetId = -1;
            var remotePlayer = collider.GetComponent<RemotePlayer>();
            if (remotePlayer != null) targetId = remotePlayer.PlayerId;
            
            var dummyMonster = collider.GetComponent<DummyMonster>();
            if (dummyMonster != null) targetId = dummyMonster.MonsterId;
            
            if (targetId != -1)
            {
                if (NetworkManager.Instance != null)
                {
                    NetworkManager.Instance.SendHit(targetId);
                }
            }

            Debug.Log($"<color=cyan>[Combat]</color> Trigger hit confirmed on {collider.name}!");
            hitTargets.Add(collider.gameObject);
        }
    }

    // Update()와 DetectHits()는 더 이상 필요 없으므로 제거 (물리 엔진이 처리)
}
