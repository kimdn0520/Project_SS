using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    public Transform weaponSocket;
    public WeaponDataSO weaponData;
    
    [Header("Settings")]
    public float attackCooldown = 0.1f;

    private PlayerController player;
    private Sword currentSword;
    private float lastAttackTime;
    
    // 0: Idle, 1: Attack 1 finished, 2: Attack 2 finished
    private int comboIndex = 0;
    private float lastComboTime;
    private bool isInternalAttacking = false;

    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
        if (player == null) Debug.LogError("<color=red>[Combat]</color> PlayerController를 찾을 수 없습니다!");

        if (weaponData == null)
        {
            weaponData = Resources.Load<WeaponDataSO>("Data/WeaponData/DefaultSword");
        }

        if (weaponData == null)
            Debug.LogError("<color=red>[Combat]</color> WeaponDataSO 로드 실패! 'Resources/Data/WeaponData/DefaultSword' 에셋이 있는지 확인하세요.");

        if (weaponSocket == null && player != null)
        {
            weaponSocket = player.WeaponSocket;
        }
    }

    private void Update()
    {
        if (player is LocalPlayerController)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Attack();
            }
        }
        
        // [수정] 콤보 인덱스가 0보다 크면(공격 중이거나 공격 직후면) 유효 시간 체크
        if (comboIndex > 0 && Time.time > lastComboTime + (weaponData != null ? weaponData.ComboWindow : 0.5f) && !isInternalAttacking)
        {
            comboIndex = 0; // 콤보 완전 리셋
            player.IsWeaponLocked = false; // 무기 고정 해제
            Debug.Log("<color=white>[Combat]</color> Combo Reset. Weapon Unlocked.");
        }
    }

    public void Attack()
    {
        if (player == null || weaponData == null || weaponSocket == null) return;
        if (isInternalAttacking || player.IsGuarding) return;
        
        if (Time.time < lastAttackTime + 0.05f) return;

        player.IsWeaponLocked = true;
        isInternalAttacking = true;
        player.IsAttacking = true;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        // [수정] 현재 공격 순서 결정 (0 -> 1 -> 2 -> 다시 1)
        int currentAttack = (comboIndex == 0 || comboIndex == 2) ? 1 : 2;
        comboIndex = currentAttack; 
        
        lastComboTime = Time.time;
        lastAttackTime = Time.time;

        float flipModifier = (weaponSocket.localScale.y < 0) ? -1f : 1f;
        Vector2 attackDir = weaponSocket.right;
        
        if (player.Rb != null) StartCoroutine(LungeRoutine(attackDir));

        if (currentSword == null) currentSword = player.GetComponentInChildren<Sword>();
        if (currentSword != null) currentSword.EnableHit(attackDir);

        // 2. 휘두르기 애니메이션
        float startAngle = weaponSocket.localEulerAngles.z;
        
        // 콤보 1(-), 콤보 2(+) 부호 유지
        float angleDelta = (currentAttack == 1) ? -weaponData.SwingAngle : weaponData.SwingAngle;
        float targetAngle = startAngle + (angleDelta * flipModifier);
        
        float elapsed = 0f;
        while (elapsed < weaponData.SwingSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / weaponData.SwingSpeed;
            float curveT = weaponData.SwingCurve.Evaluate(t);
            
            float currentAngle = Mathf.LerpAngle(startAngle, targetAngle, curveT);
            weaponSocket.localRotation = Quaternion.Euler(0, 0, currentAngle);
            yield return null;
        }
        
        weaponSocket.localRotation = Quaternion.Euler(0, 0, targetAngle);

        if (currentSword != null) currentSword.DisableHit();
        
        player.IsAttacking = false;

        // 후딜레이
        yield return new WaitForSeconds(weaponData.RecoveryTime);

        isInternalAttacking = false;
        lastComboTime = Time.time; // 공격 종료 후부터 콤보 윈도우 시작
        
        Debug.Log($"<color=grey>[Combat]</color> Attack {currentAttack} Finished.");
    }

    private IEnumerator LungeRoutine(Vector2 direction)
    {
        float elapsed = 0f;
        Vector2 lungeVelocity = direction * (weaponData.LungeDistance / weaponData.LungeDuration);
        
        while (elapsed < weaponData.LungeDuration)
        {
            elapsed += Time.deltaTime;
            player.Rb.linearVelocity = lungeVelocity;
            yield return null;
        }
        player.Rb.linearVelocity = Vector2.zero;
    }
}
