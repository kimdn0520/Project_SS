using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ProjectSS.Expedition;
public static class AlignPanelClose
{
    public static void Execute()
    {
        const string path="Assets/Resources/Prefabs/PlayPage.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            ProjectSS.Expedition.Editor.WindowSkinArt.AlignPanelClose(root.GetComponent<PlayPage>());
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
        var live=Object.FindFirstObjectByType<PlayPage>();
        if(Application.isPlaying&&live!=null)ProjectSS.Expedition.Editor.WindowSkinArt.AlignPanelClose(live);
    }
    public static string Inspect()
    {
        var common=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Popups/PopupCommon.prefab");
        return string.Join("\n",common.GetComponentsInChildren<RectTransform>(true).Where(r=>r.name=="bg"||r.name=="Close").Select(r=>r.name+" position="+r.anchoredPosition+" size="+r.sizeDelta+" pivot="+r.pivot+" anchors="+r.anchorMin+" / "+r.anchorMax));
    }
}
