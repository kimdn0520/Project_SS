using UnityEngine;
using System.Text;
using System.IO;

public static class RuntimeSpriteInspector
{
    public static void Inspect()
    {
        SpriteRenderer[] srs = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Total SpriteRenderers: {srs.Length}");
        foreach (var sr in srs)
        {
            if (sr.gameObject.activeInHierarchy && sr.enabled)
            {
                sb.AppendLine($"[SR] path: {GetPath(sr.transform)} | sprite: {sr.sprite?.name} | order: {sr.sortingOrder} | worldPos: {sr.transform.position} | bounds: {sr.bounds} | size: {sr.size} | drawMode: {sr.drawMode}");
            }
        }
        File.WriteAllText("Assets/Editor/runtime_srs.txt", sb.ToString());
        Debug.Log("Saved runtime_srs.txt");
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
