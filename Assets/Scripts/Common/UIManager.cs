using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIManager : SingletonMonoBehaviour<UIManager>
{
    [Header("HUD")]
    [SerializeField] private Slider staminaBar;
    [SerializeField] private Slider hpBar;
    [SerializeField] private Image staminaFillImage;
    
    protected override void Awake()
    {
        base.Awake();
    }

    public void UpdateStamina(float current, float max)
    {
        if (staminaBar != null)
        {
            staminaBar.maxValue = max;
            staminaBar.value = current;
        }
    }

    public void UpdateHP(float current, float max)
    {
        if (hpBar != null)
        {
            hpBar.maxValue = max;
            hpBar.value = current;
        }
    }
}
