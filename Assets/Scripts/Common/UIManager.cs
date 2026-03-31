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
        #if UNITY_EDITOR
        CheckAndGeneratePlayerStats();
        #endif
    }

    private void CheckAndGeneratePlayerStats()
    {
#if UNITY_EDITOR
        string path = "Assets/Resources/Data/PlayerStats";
        // 전사 데이터가 없으면 자동 생성
        if (!System.IO.File.Exists(Application.dataPath + "/Resources/Data/PlayerStats/Warrior.asset"))
        {
            CreateStats("Warrior", 4.5f, 150f, 12f);
            CreateStats("Mage", 5f, 100f, 18f);
            CreateStats("Rogue", 6.5f, 80f, 25f);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Default Player Stats in Resources/Data/PlayerStats");
        }
#endif
    }

    private void CreateStats(string className, float speed, float stamina, float regen)
    {
#if UNITY_EDITOR
        PlayerStatsSO stats = ScriptableObject.CreateInstance<PlayerStatsSO>();
        stats.ClassName = className;
        stats.MoveSpeed = speed;
        stats.MaxStamina = stamina;
        stats.CurrentStamina = stamina;
        stats.StaminaRegenRate = regen;

        AssetDatabase.CreateAsset(stats, $"Assets/Resources/Data/PlayerStats/{className}.asset");
#endif
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
