using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectSS.Expedition;
using ProjectSS.Expedition.Editor;
public static class UpdateDigFeel
{
    public static object Execute()
    {
        if (EditorApplication.isPlaying) throw new System.Exception("Stop play before editing");
        const string path = "Assets/Resources/Prefabs/PlayPage.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var page = root.GetComponent<PlayPage>();
            bool alreadyUpdated = page.digButton.transform.Find("PressableFace") != null;
            DigButtonArt.Configure(page);
            if (!alreadyUpdated) page.miner.transform.localPosition += Vector3.down * .14f;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");
        return "Updated cap, fixed input root, miner lowered 14 reference pixels";
    }
}
