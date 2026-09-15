using UnityEditor;
using ProjectSS.Expedition;
using ProjectSS.Expedition.Editor;
public static class ApplySidebar
{
    public static void Execute()
    {
        const string path="Assets/Resources/Prefabs/PlayPage.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try { SidebarIconArt.Configure(root.GetComponent<PlayPage>());PrefabUtility.SaveAsPrefabAsset(root,path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }
}
