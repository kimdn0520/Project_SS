using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "ScriptableObjects/PlayerStats")]
public class PlayerStatsSO : ScriptableObject
{
    public string ClassName;
    
    [Header("Movement")]
    public float MoveSpeed = 5f;
    public float SprintMultiplier = 1.6f;
    
    [Header("Health & Stamina")]
    public float MaxHP = 100f;
    public float MaxStamina = 100f;
    public float CurrentStamina = 100f; // 초기 스테미나 값
    public float StaminaRegenRate = 15f;
    public float SprintStaminaCost = 20f;
    
    [Header("Combat Defaults")]
    public float BaseDamage = 10f;
    public float AttackStaminaCost = 15f;
    public float GuardStaminaCost = 10f;       // 방어 시 초당 소모 스테미나
    public float GuardDamageReduction = 0.5f;  // 피해 감소율 (0.5 = 50% 감소)
    public float PerfectParryWindow = 0.2f;   // 패링 판정 시간 (초)
}
