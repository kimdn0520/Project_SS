using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SceneInspector
{
    public static void Inspect()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");
        GameObject playPage = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/PlayPage.prefab");
        if (playPage == null) return;

        Transform env = playPage.transform.Find("Game_Root/Environment");
        if (env != null)
        {
            Debug.Log("[ENVIRONMENT INSPECT]");
            PrintHierarchy(env, 0);
        }
    }

    private static void PrintHierarchy(Transform t, int depth)
    {
        string indent = new string(' ', depth * 2);
        string extra = "";
        var sr = t.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            extra = $" (SR: sprite={sr.sprite?.name}, order={sr.sortingOrder}, size={sr.size}, drawMode={sr.drawMode})";
        }
        Debug.Log($"{indent}- {t.name}: pos={t.localPosition}, scale={t.localScale}{extra}");
        for (int i = 0; i < t.childCount; i++)
        {
            PrintHierarchy(t.GetChild(i), depth + 1);
        }
    }
}
