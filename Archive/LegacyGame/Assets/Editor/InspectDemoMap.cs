using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;

public static class InspectDemoMap
{
    public static void Inspect()
    {
        GameObject mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Layer Lab/2D Maps - Simple Sidescroll/Extenstions/Simple Sidescroll1/Prefabs/Adaptive_MapFocused/Map1_AdaptiveView.prefab");
        if (mapPrefab == null)
        {
            Debug.LogError("Map1_AdaptiveView.prefab not found!");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== Map1_AdaptiveView Inspection ===");
        PrintRecursive(mapPrefab.transform, 0, sb);
        File.WriteAllText("Assets/Editor/map1_structure.txt", sb.ToString());
        Debug.Log("Saved to Assets/Editor/map1_structure.txt");
    }

    private static void PrintRecursive(Transform t, int depth, StringBuilder sb)
    {
        string indent = new string(' ', depth * 2);
        string srInfo = "";
        var sr = t.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            srInfo = $" | SR: spr={sr.sprite?.name}, order={sr.sortingOrder}, size={sr.size}, drawMode={sr.drawMode}, color={sr.color}";
        }
        sb.AppendLine($"{indent}- {t.name}: localPos={t.localPosition}, scale={t.localScale}{srInfo}");
        for (int i = 0; i < t.childCount; i++)
        {
            PrintRecursive(t.GetChild(i), depth + 1, sb);
        }
    }
}
