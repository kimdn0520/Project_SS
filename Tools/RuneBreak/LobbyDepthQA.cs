using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RuneBreak;
using RuneBreak.Editor;

public static class LobbyDepthQA
{
    private static void Check(bool ok,string message){if(!ok)throw new Exception("LOBBY DEPTH: "+message);}

    public static void Build()
    {
        Check(!Application.isPlaying,"Exit play mode");
        RuneProjectBuilder.RebuildLobby();
        ApplyPlayTypography();
        var splash=EditorSceneManager.OpenScene("Assets/Scenes/Splash.unity",OpenSceneMode.Additive);
        foreach(var go in splash.GetRootGameObjects())
            foreach(var text in go.GetComponentsInChildren<TMP_Text>(true))RuneFontAuthoring.Style(text);
        EditorSceneManager.SaveScene(splash);EditorSceneManager.CloseScene(splash,true);
        AssetDatabase.SaveAssets();
    }
    public static void ApplyPlayTypography()
    {
        Check(!Application.isPlaying,"Exit play mode");
        const string path="Assets/Resources/Prefabs/PlayPage.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))RuneFontAuthoring.Style(text);
            var page=root.GetComponent<RunePlayPage>();
            page.stageNumber.rectTransform.sizeDelta=new Vector2(240,36);
            page.stageNumber.rectTransform.anchoredPosition=new Vector2(150,-60);
            page.occupancy.rectTransform.sizeDelta=new Vector2(37,16);
            page.occupancy.rectTransform.anchoredPosition=new Vector2(400,-444);
            foreach(var pin in root.GetComponent<RuneSafeLayout>().pins)
                if(pin.target==page.stageNumber.rectTransform)pin.position=page.stageNumber.rectTransform.anchoredPosition;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }

    public static void Validate()
    {
        Check(Application.isPlaying,"Enter play mode");
        var manager=UnityEngine.Object.FindFirstObjectByType<PageManager>();
        manager.ChangeImmediate(UIPageType.MainPage);
        var page=manager.GetPage<RuneLobbyPage>(UIPageType.MainPage);page.CanvasGroup.alpha=1;
        int count=page.GetComponentsInChildren<Transform>(true).Length;
        foreach(var tab in Enumerable.Range(0,page.tabs.Length))
        {
            page.SelectTab(tab);
            Check(page.tabs.Count(t=>t.content.activeSelf)==1,"Tab content visibility");
            Check(page.tabs.Count(t=>t.selectedMarker.activeSelf)==1,"Selected tab marker");
            Check(page.tabs[tab].background.sprite==page.selectedTabSkin,"Selected tab skin");
            Canvas.ForceUpdateCanvases();CheckText(page.gameObject);
        }
        page.SelectTab(page.homeIndex);page.showcase.RestartPreview();
        Canvas.ForceUpdateCanvases();
        var button=page.GetComponentsInChildren<RuneRaisedButton>().Single(b=>b.name=="Play");
        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
        var before=button.face.anchoredPosition;
        button.OnPointerDown(pointer);
        Check(button.face.anchoredPosition.y==before.y-button.pressDepth,"Button does not depress");
        Capture(page,"Lobby-depth-pressed");
        button.OnPointerUp(pointer);
        Check(button.face.anchoredPosition==before,"Button did not release");
        button.OnPointerDown(pointer);button.gameObject.SetActive(false);button.gameObject.SetActive(true);
        Check(button.face.anchoredPosition==before,"Disabled button kept depressed position");
        Check(page.GetComponentsInChildren<Transform>(true).Length==count,"Hierarchy grew at runtime");
        foreach(var b in page.GetComponentsInChildren<RuneRaisedButton>(true))
            Check(b.image.type==Image.Type.Sliced&&b.image.sprite.border.x>0,"Missing 9-slice: "+b.name);
        foreach(var p in new[]{"MainPage","PlayPage"})
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/"+p+".prefab");
            Check(prefab.GetComponentsInChildren<TMP_Text>(true).All(t=>t.font==RuneFontAuthoring.Bold),"Non-bold text: "+p);
        }
        CheckText(page.gameObject);
        var animation=PopupManager.Instance.GlobalAnimation;PopupManager.Instance.GlobalAnimation=null;
        try{page.OpenSettings();Canvas.ForceUpdateCanvases();CheckText(((RuneSettingsPanel)PopupManager.CurrentPopup).gameObject);Capture(page,"Lobby-depth-settings");}
        finally{PopupManager.Clear();PopupManager.Instance.GlobalAnimation=animation;}
        page.showcase.RestartPreview();Capture(page,"Lobby-depth");
        Debug.Log("LOBBY DEPTH PASS: five tab states, 9-slice skins, pointer press/release, disabled reset, stable hierarchy, all page labels bold, no visible text overflow.");
    }
    private static void CheckText(GameObject root)
    {
        foreach(var text in root.GetComponentsInChildren<TMP_Text>())
        {
            text.ForceMeshUpdate();
            Check(!text.isTextOverflowing,"Text overflow: "+text.name+" / "+text.text);
        }
    }
    public static void CaptureHome()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<RuneLobbyPage>();
        PopupManager.Clear();page.SelectTab(page.homeIndex);page.showcase.RestartPreview();
        Capture(page,"Lobby-depth");
    }
    public static void PlayText()
    {
        var manager=UnityEngine.Object.FindFirstObjectByType<PageManager>();
        manager.ChangeImmediate(UIPageType.PlayPage,0);
        var page=manager.GetPage<RunePlayPage>(UIPageType.PlayPage);page.CanvasGroup.alpha=1;
        Canvas.ForceUpdateCanvases();CheckText(page.gameObject);
        var animation=PopupManager.Instance.GlobalAnimation;PopupManager.Instance.GlobalAnimation=null;
        try{page.OpenSettings();Canvas.ForceUpdateCanvases();CheckText(((RuneSettingsPanel)PopupManager.CurrentPopup).gameObject);}
        finally{PopupManager.Clear();PopupManager.Instance.GlobalAnimation=animation;}
        Debug.Log("BOLD PLAY TEXT PASS: visible gameplay labels and settings have no overflow.");
    }
    public static void Layout()
    {
        var manager=UnityEngine.Object.FindFirstObjectByType<PageManager>();
        Check(manager.CurrentPageType==UIPageType.MainPage&&!manager.IsChanging,"Show MainPage and let its first layout frame finish before capturing.");
        var page=manager.GetPage<RuneLobbyPage>(UIPageType.MainPage);page.CanvasGroup.alpha=1;
        PopupManager.Clear();page.SelectTab(page.homeIndex);page.showcase.RestartPreview();
        Canvas.ForceUpdateCanvases();CheckText(page.gameObject);
        foreach(var button in page.GetComponentsInChildren<RuneRaisedButton>())
        {
            var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
            foreach(var corner in corners)
            {
                var point=RectTransformUtility.WorldToScreenPoint(page.Canvas.worldCamera,corner);
                Check(point.x>=Screen.safeArea.xMin-.1f&&point.x<=Screen.safeArea.xMax+.1f&&point.y>=Screen.safeArea.yMin-.1f&&point.y<=Screen.safeArea.yMax+.1f,"Button clipped: "+button.name+" point="+point+" safe="+Screen.safeArea);
            }
        }
        ScreenCapture.CaptureScreenshot("Tools/RuneBreak/QA/Lobby-depth-"+Screen.width+"x"+Screen.height+".png");
        Debug.Log("LOBBY DEPTH LAYOUT PASS: "+Screen.width+"x"+Screen.height);
    }
    private static void Capture(RuneLobbyPage page,string name)
    {
        Canvas.ForceUpdateCanvases();
        var camera=page.Canvas.worldCamera;var target=new RenderTexture(540,960,24);
        var previous=camera.targetTexture;var active=RenderTexture.active;var image=new Texture2D(540,960,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,540,960),0,0);image.Apply();
            File.WriteAllBytes("Tools/RuneBreak/QA/"+name+".png",image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=previous;RenderTexture.active=active;
            target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
