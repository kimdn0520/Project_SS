using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSS.Expedition;
using ProjectSS.Expedition.Editor;
public static class UpdateVerticalPlay
{
    public static object Execute()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Stop play before editing");
        const string path="Assets/Resources/Prefabs/PlayPage.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try{VerticalPlayLayout.Configure(root.GetComponent<PlayPage>());PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");return "Vertical mine / inventory / equipment popup baked";
    }
}
