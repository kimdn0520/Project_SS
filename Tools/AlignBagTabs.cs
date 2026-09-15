using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using ProjectSS.Expedition;
using ProjectSS.Expedition.Editor;

public static class AlignBagTabs
{
    public static void Capture(){CaptureAsync();}
    static async Cysharp.Threading.Tasks.UniTaskVoid CaptureAsync()
    {
        var page=Object.FindFirstObjectByType<PlayPage>();
        page.OpenMenu(2);
        MenuPolishArt.AlignBagTabs(page.inventoryView);
        await Cysharp.Threading.Tasks.UniTask.WaitForEndOfFrame(page);
        var shot=ScreenCapture.CaptureScreenshotAsTexture();
        System.IO.File.WriteAllBytes("PrototypeQA/bag-tabs-centered.png",shot.EncodeToPNG());
        Object.Destroy(shot);
    }
    public static string Inspect()
    {
        var page=AssetDatabase.LoadAssetAtPath<PlayPage>("Assets/Resources/Prefabs/PlayPage.prefab");
        return string.Join("\n",page.inventoryView.tabs.SelectMany(t=>t.GetComponentsInChildren<RectTransform>(true)).Select(r=>r.name+" pos="+r.anchoredPosition+" size="+r.sizeDelta+" anchors="+r.anchorMin+" / "+r.anchorMax));
    }
    public static void Execute()
    {
        const string path="Assets/Resources/Prefabs/PlayPage.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            MenuPolishArt.AlignBagTabs(root.GetComponent<PlayPage>().inventoryView);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }
}
