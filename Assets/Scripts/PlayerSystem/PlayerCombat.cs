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
    
    private int comboIndex = 0;
    private float lastComboTime;
    private bool isInternalAttacking = false;

    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
        if (weaponData == null) weaponData = Resources.Load<WeaponDataSO>("Data/WeaponData/DefaultSword");
        if (weaponSocket == null && player != null) weaponSocket = player.WeaponSocket;
    }

    private void Update()
    {
        if (player is LocalPlayerController)
        {
            if (Input.GetMouseButtonDown(0)) Attack();
        }
        
        if (comboIndex > 0 && Time.time > lastComboTime + (weaponData != null ? weaponData.ComboWindow : 0.5f) && !isInternalAttacking)
        {
            comboIndex = 0;
            player.IsWeaponLocked = false;
        }
    }

    public void Attack()
    {
        if (player == null || weaponData == null || weaponSocket == null) return;
        if (isInternalAttacking || player.IsGuarding) return;
        
        // [추가] 스태미너 체크 (부족 시 공격 불가)
        if (player.CurrentStamina < weaponData.AttackStaminaCost) 
        {
            Debug.Log($"[Combat] Stamina too low! Need: {weaponData.AttackStaminaCost}, Current: {player.CurrentStamina}");
            return;
        }

        // [추가] 클라이언트 예측 (즉시 차감)
        player.CurrentStamina -= weaponData.AttackStaminaCost;

        if (Time.time < lastAttackTime + 0.05f) return;

        player.IsWeaponLocked = true;
        isInternalAttacking = true;
        player.IsAttacking = true;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        int currentAttack = (comboIndex == 0 || comboIndex == 2) ? 1 : 2;
        comboIndex = currentAttack; 
        
        lastComboTime = Time.time;
        lastAttackTime = Time.time;

        float flipModifier = (weaponSocket.localScale.y < 0) ? -1f : 1f;
        Vector2 attackDir = weaponSocket.right;
        
        if (currentSword == null) currentSword = player.GetComponentInChildren<Sword>();
        if (currentSword != null) currentSword.EnableHit(attackDir);

        float startAngle = weaponSocket.localEulerAngles.z;
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
        
        if (currentAttack == 1)
        {
            // 1타 종료: 콤보 연계를 위해 즉시 다음 입력을 허용합니다.
            isInternalAttacking = false;
        }
        else
        {
            // 2타 종료 (콤보 끝): RecoveryTime만큼 대기한 후 다음 사이클(1타) 입력을 허용합니다.
            yield return new WaitForSeconds(weaponData.RecoveryTime);
            isInternalAttacking = false;
            comboIndex = 0; // 콤보 초기화
            
            // 콤보가 완전히 끝났으므로 무기 고정을 풀어 마우스를 다시 따라가게 합니다.
            player.IsWeaponLocked = false;
        }
        
        lastComboTime = Time.time;
    }
}
