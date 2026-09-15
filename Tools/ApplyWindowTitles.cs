using UnityEditor;
using UnityEngine;
using ProjectSS.Expedition;
using ProjectSS.Expedition.Editor;
public static class ApplyWindowTitles
{
    public static void Capture(){CaptureAsync();}
    static async Cysharp.Threading.Tasks.UniTaskVoid CaptureAsync()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();
        page.OpenMenu(3);await Shot(page,"challenge");
        page.OpenMenu(1);page.OpenHero(0);page.SelectSlot(0);await Shot(page,"equipment");
        PopupManager.Clear();page.OpenMenu(0);
    }
    static async Cysharp.Threading.Tasks.UniTask Shot(PlayPage page,string name)
    {
        await Cysharp.Threading.Tasks.UniTask.Delay(500);
        await Cysharp.Threading.Tasks.UniTask.WaitForEndOfFrame(page);
        var image=UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();
        System.IO.File.WriteAllBytes("PrototypeQA/title-"+name+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);
    }
    public static void Execute()
    {
        const string path="Assets/Resources/Prefabs/PlayPage.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try{WindowTitleArt.Configure(root.GetComponent<PlayPage>());PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }
}
