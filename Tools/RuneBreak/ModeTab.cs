using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using RuneBreak;

// Editor-only prefab migration and visual review; not included in the player.
public static class ModeTab
{
    public static void Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit play mode before editing the prefab.");
        const string path="Assets/Resources/Prefabs/MainPage.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var page=root.GetComponent<RuneLobbyPage>();
            var tab=page.tabs.Single(t=>t.id=="cards"||t.id=="modes");
            var button=tab.background.GetComponentInParent<RuneRaisedButton>();
            tab.id="modes";
            tab.content.name="Tab_modes";
            button.name="Nav_modes";
            tab.label.text="모드";
            tab.content.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Title").text="모드";
            foreach(var icon in tab.content.GetComponentsInChildren<RuneLobbyIcon>(true)
                .Concat(button.GetComponentsInChildren<RuneLobbyIcon>(true)))
            {
                icon.kind="modes";
                icon.SetVerticesDirty();
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
        Debug.Log("MODES TAB: MainPage prefab updated.");
    }

    public static void Preview()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Enter play mode to preview.");
        var manager=UnityEngine.Object.FindFirstObjectByType<PageManager>();
        manager.ChangeImmediate(UIPageType.MainPage);
        var page=manager.GetPage<RuneLobbyPage>(UIPageType.MainPage);
        page.CanvasGroup.alpha=1;
        var tab=page.tabs.Single(t=>t.id=="modes");
        tab.background.GetComponentInParent<RuneRaisedButton>().onClick.Invoke();
        if(!tab.content.activeSelf||page.tabs.Count(t=>t.content.activeSelf)!=1||
            !tab.selectedMarker.activeSelf||tab.background.sprite!=page.selectedTabSkin||
            tab.label.text!="모드"||tab.icon.kind!="modes")
            throw new InvalidOperationException("Modes tab did not activate correctly.");
        Capture(page,"Modes-tab");
        page.SelectTab(page.homeIndex);
        page.showcase.RestartPreview();
        Capture(page,"Lobby-modes");
        Debug.Log("MODES TAB PASS: button activates the Modes page, label/icon and selected style are correct; returned Home.");
    }

    private static void Capture(RuneLobbyPage page,string name)
    {
        Canvas.ForceUpdateCanvases();
        var camera=page.Canvas.worldCamera;
        var target=new RenderTexture(540,960,24);
        var previous=camera.targetTexture;
        var active=RenderTexture.active;
        var image=new Texture2D(540,960,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=target;
            camera.Render();
            RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,540,960),0,0);
            image.Apply();
            File.WriteAllBytes("Tools/RuneBreak/QA/"+name+".png",image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=previous;
            RenderTexture.active=active;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
