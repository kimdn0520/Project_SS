using UnityEditor;
using UnityEngine;

public class PlayerStatsGenerator
{
    [MenuItem("ProjectSS/Generate Player Stats")]
    public static void Generate()
    {
        string path = "Assets/Resources/Data/PlayerStats";
        if (!AssetDatabase.IsValidFolder(path))
        {
            System.IO.Directory.CreateDirectory(Application.dataPath + "/Resources/Data/PlayerStats");
            AssetDatabase.Refresh();
        }

        CreateStats("Warrior", 4f, 150f, 12f);
        CreateStats("Mage", 5f, 100f, 20f);
        CreateStats("Rogue", 6.5f, 80f, 25f);

        Debug.Log("Player Stats Created Successfully!");
    }

    private static void CreateStats(string className, float speed, float stamina, float regen)
    {
        PlayerStatsSO stats = ScriptableObject.CreateInstance<PlayerStatsSO>();
        stats.ClassName = className;
        stats.MoveSpeed = speed;
        stats.MaxStamina = stamina;
        stats.CurrentStamina = stamina;
        stats.StaminaRegenRate = regen;

        AssetDatabase.CreateAsset(stats, $"Assets/Resources/Data/PlayerStats/{className}.asset");
    }
}
