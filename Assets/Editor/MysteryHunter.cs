using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Text;
using System.IO;

public static class MysteryHunter
{
    public static void Hunt()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");

        StringBuilder sb = new StringBuilder();

        // 1. 모든 카메라
        Camera[] cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"=== Cameras ({cams.Length}) ===");
        foreach (var c in cams)
        {
            sb.AppendLine($"Cam: {c.name}, active: {c.gameObject.activeInHierarchy}, clear: {c.clearFlags}, bg: {c.backgroundColor}, cullingMask: {c.cullingMask}, depth: {c.depth}");
        }

        // 2. RenderSettings
        sb.AppendLine($"=== RenderSettings ===");
        sb.AppendLine($"skybox: {RenderSettings.skybox?.name}, ambient: {RenderSettings.ambientLight}");

        // 3. 씬의 모든 루트 오브젝트
        sb.AppendLine($"=== Root GameObjects ===");
        foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            sb.AppendLine($"Root: {go.name}, active: {go.activeSelf}");
        }

        // 4. 모든 SpriteRenderer (비활성 포함)
        sb.AppendLine($"=== All SpriteRenderers in Scene ===");
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            sb.AppendLine($"SR: {GetPath(sr.transform)} | spr: {sr.sprite?.name} | order: {sr.sortingOrder} | color: {sr.color} | localPos: {sr.transform.localPosition} | scale: {sr.transform.localScale}");
        }

        // 5. PlayPage 프리팹 안의 모든 SpriteRenderer
        GameObject ppPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/PlayPage.prefab");
        if (ppPrefab != null)
        {
            sb.AppendLine($"=== PlayPage.prefab SpriteRenderers ===");
            foreach (var sr in ppPrefab.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sb.AppendLine($"Prefab SR: {GetPath(sr.transform)} | spr: {sr.sprite?.name} | order: {sr.sortingOrder} | color: {sr.color} | localPos: {sr.transform.localPosition} | scale: {sr.transform.localScale} | size: {sr.size} | drawMode: {sr.drawMode}");
            }
        }

        File.WriteAllText("Assets/Editor/mystery_result.txt", sb.ToString());
        Debug.Log("Hunt complete, saved to mystery_result.txt");
    }

    private static string GetPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            p = t.name + "/" + p;
        }
        return p;
    }
}
