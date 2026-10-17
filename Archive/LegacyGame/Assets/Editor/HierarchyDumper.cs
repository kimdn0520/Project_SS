using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Text;
using System.IO;

public static class HierarchyDumper
{
    public static void Execute()
    {
        Dump();
    }

    public static void Dump()
    {
        StringBuilder sb = new StringBuilder();
        EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");
        Camera cam = Camera.main;
        if (cam != null)
        {
            sb.AppendLine($"Play Scene Main Camera: clear={cam.clearFlags}, bg={cam.backgroundColor}, ortho={cam.orthographic}, size={cam.orthographicSize}");
        }
        else
        {
            sb.AppendLine("Play Scene Camera.main IS NULL!");
        }
        File.WriteAllText("Assets/Editor/play_cam_info.txt", sb.ToString());
        Debug.Log("Play cam info dumped");
    }

    private static void DumpTransformDetail(Transform t, int depth, StringBuilder sb)
    {
        string indent = new string(' ', depth * 2);
        string detail = "";
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            string sprName = sr.sprite != null ? sr.sprite.name : "null";
            detail += $" [SpriteRenderer: spr={sprName}, sort={sr.sortingOrder}, color={sr.color}, mode={sr.drawMode}, size={sr.size}]";
        }
        sb.AppendLine($"{indent}- {t.name} (active={t.gameObject.activeSelf}): pos={t.localPosition}, scale={t.localScale}{detail}");
        for (int i = 0; i < t.childCount; i++)
        {
            DumpTransformDetail(t.GetChild(i), depth + 1, sb);
        }
    }

    private static void DumpTransform(Transform t, int depth, StringBuilder sb)
    {
        string indent = new string(' ', depth * 2);
        string comps = "";
        foreach (var c in t.GetComponents<Component>())
        {
            if (c != null) comps += $"[{c.GetType().Name}] ";
        }
        sb.AppendLine($"{indent}- {t.name} (active={t.gameObject.activeSelf}): pos={t.localPosition}, scale={t.localScale} {comps}");
        for (int i = 0; i < t.childCount; i++)
        {
            DumpTransform(t.GetChild(i), depth + 1, sb);
        }
    }
}
