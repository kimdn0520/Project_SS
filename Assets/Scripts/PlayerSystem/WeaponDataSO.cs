using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "ScriptableObjects/WeaponData")]
public class WeaponDataSO : ScriptableObject
{
    public string WeaponName;
    
    [Header("Swing Settings")]
    public float SwingAngle = 120f;       // 휘두르는 총 각도
    public float SwingSpeed = 0.15f;      // 휘두르는 시간 (초)
    public float RecoveryTime = 0.2f;     // 공격 후 원래 자세로 돌아오는 시간
    public float ComboWindow = 0.5f;      // 다음 콤보를 입력할 수 있는 유효 시간
    
    [Header("Lunge Settings")]
    public float LungeDistance = 0.4f;    // 전진 거리
    public float LungeDuration = 0.1f;    // 전진에 걸리는 시간
    
    [Header("Visual Settings")]
    public AnimationCurve SwingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Stamina Settings")]
    public float AttackStaminaCost = 15f;
}
