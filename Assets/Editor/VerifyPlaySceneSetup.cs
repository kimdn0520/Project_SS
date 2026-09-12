using UnityEngine;
using UnityEditor;

public static class VerifyPlaySceneSetup
{
    public static void Execute()
    {
        GameObject mgrs = GameObject.Find("[--- MANAGERS ---]");
        if (mgrs != null)
        {
            var comps = mgrs.GetComponents<Component>();
            foreach (var c in comps)
            {
                Debug.Log($"[Verify] Manager component: {c.GetType().Name}");
            }
        }

        PageManager pm = Object.FindAnyObjectByType<PageManager>();
        if (pm != null)
        {
            Debug.Log($"[Verify] PageManager found: {pm.name}");
        }

        GameObject playPage = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/PlayPage.prefab");
        if (playPage != null)
        {
            Debug.Log($"[Verify] PlayPage.prefab found, children count: {playPage.transform.childCount}");
            foreach (Transform t in playPage.transform)
            {
                Debug.Log($"[Verify] PlayPage child: {t.name} (type: {t.GetType().Name})");
            }
        }
    }
}
