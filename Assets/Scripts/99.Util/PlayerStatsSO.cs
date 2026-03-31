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
    public float StaminaRegenRate = 15f;
    public float SprintStaminaCost = 20f;
    
    [Header("Combat Defaults")]
    public float BaseDamage = 10f;
    public float AttackStaminaCost = 15f;
}
