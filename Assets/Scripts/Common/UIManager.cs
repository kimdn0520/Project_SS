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
    
    [Header("Colors")]
    [SerializeField] private Color normalStaminaColor = Color.yellow;
    [SerializeField] private Color exhaustedStaminaColor = Color.red;

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
            
            if (staminaFillImage != null)
            {
                staminaFillImage.color = (current <= 0) ? exhaustedStaminaColor : normalStaminaColor;
            }
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
