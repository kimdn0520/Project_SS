using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Text;
using System.IO;

public static class DeepRendererHunter
{
    public static void Hunt()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");
        GameObject ppPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/PlayPage.prefab");
        GameObject tempInst = (GameObject)PrefabUtility.InstantiatePrefab(ppPrefab);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== DEEP RENDERER HUNT ===");

        // 모든 Renderer
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"Total Renderers: {renderers.Length}");
        foreach (var r in renderers)
        {
            string matName = (r.sharedMaterial != null) ? r.sharedMaterial.name : "null";
            string texName = (r.sharedMaterial != null && r.sharedMaterial.mainTexture != null) ? r.sharedMaterial.mainTexture.name : "null";
            sb.AppendLine($"[Renderer] type: {r.GetType().Name} | name: {GetPath(r.transform)} | enabled: {r.enabled} | active: {r.gameObject.activeInHierarchy} | order: {r.sortingOrder} | mat: {matName} | tex: {texName} | bounds: {r.bounds}");
        }

        // 모든 UI Graphic
        UnityEngine.UI.Graphic[] graphics = Object.FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"Total UI Graphics: {graphics.Length}");
        foreach (var g in graphics)
        {
            var img = g as UnityEngine.UI.Image;
            var raw = g as UnityEngine.UI.RawImage;
            string extra = "";
            if (img != null) extra = $"spr: {(img.sprite != null ? img.sprite.name : "null")}";
            if (raw != null) extra = $"tex: {(raw.texture != null ? raw.texture.name : "null")}";
            sb.AppendLine($"[UI] type: {g.GetType().Name} | name: {GetPath(g.transform)} | active: {g.gameObject.activeInHierarchy} | color: {g.color} | {extra} | rect: {g.rectTransform.rect}");
        }

        Object.DestroyImmediate(tempInst);
        File.WriteAllText("Assets/Editor/deep_hunt_result.txt", sb.ToString());
        Debug.Log("Deep hunt complete, saved to deep_hunt_result.txt");
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
