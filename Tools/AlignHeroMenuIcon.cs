using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ProjectSS.Expedition;
public static class AlignHeroMenuIcon
{
    public static void Execute()
    {
        const string path="Assets/Resources/Prefabs/PlayPage.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            ProjectSS.Expedition.Editor.MenuPolishArt.AlignHeroMenuIcon(root.GetComponent<PlayPage>());
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }
    public static void Capture(){CaptureAsync();}
    static async Cysharp.Threading.Tasks.UniTaskVoid CaptureAsync()
    {
        var page=Object.FindFirstObjectByType<PlayPage>();
        ProjectSS.Expedition.Editor.MenuPolishArt.AlignHeroMenuIcon(page);
        page.OpenMenu(0);
        await Cysharp.Threading.Tasks.UniTask.Delay(250);
        await Cysharp.Threading.Tasks.UniTask.WaitForEndOfFrame(page);
        var shot=ScreenCapture.CaptureScreenshotAsTexture();
        System.IO.File.WriteAllBytes("PrototypeQA/hero-menu-icon.png",shot.EncodeToPNG());Object.Destroy(shot);
    }
    public static string Inspect()
    {
        var page=AssetDatabase.LoadAssetAtPath<PlayPage>("Assets/Resources/Prefabs/PlayPage.prefab");
        return string.Join("\n",page.menuIcons.Select(t=>{var r=(RectTransform)t;var im=t.GetComponent<Image>();return t.name+" pos="+r.anchoredPosition+" size="+r.sizeDelta+" pivot="+r.pivot+" scale="+r.localScale+" sprite="+AssetDatabase.GetAssetPath(im.sprite);}));
    }
}
