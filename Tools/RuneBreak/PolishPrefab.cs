using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RuneBreak;
using RuneBreak.Editor;
public static class PolishRunePrefab
{
    public static void Execute()
    {
        const string path="Assets/Resources/Prefabs/PlayPage.prefab";var root=PrefabUtility.LoadPrefabContents(path);var p=root.GetComponent<RunePlayPage>();
        var safe=root.GetComponent<RuneSafeLayout>();RuneProjectBuilder.ConfigurePins(p,safe.content);
        for(int i=0;i<p.catalog.levels[0].blocks.Length;i++){var c=RuneFlight.Center(p.catalog.levels[0].blocks[i]);p.blockRects[i].anchoredPosition=new Vector2(c.x,-c.y-2);}
        foreach(var slot in p.slots)slot.gemRect.anchoredPosition=new Vector2(30,-32);
        PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");
    }
}
