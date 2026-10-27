using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RuneBreak;
public static class LobbyPunchQA
{
    private static void Check(bool ok,string message){if(!ok)throw new Exception("LOBBY PUNCH: "+message);}
    private static RuneLobbyPage Page()
    {
        Check(Application.isPlaying,"Enter Play mode");var manager=UnityEngine.Object.FindFirstObjectByType<PageManager>();manager.ChangeImmediate(UIPageType.MainPage);var page=manager.GetPage<RuneLobbyPage>(UIPageType.MainPage);page.CanvasGroup.alpha=1;return page;
    }
    public static void Preview(){var p=Page();p.SelectTab(p.homeIndex);p.showcase.RestartPreview();Capture(p,"BlockPunch-MainPage.png");}
    public static void Validate()
    {
        var p=Page();int children=p.GetComponentsInChildren<Transform>(true).Length;int next=PlayerPrefs.GetInt("RuneBreak.Next",0),clears=PlayerPrefs.GetInt("RuneBreak.Clears",0);
        Check(p.punchStyle&&p.showcase.art.punchStyle,"skin assignment");Check(p.showcase.pool.GetComponentsInChildren<RuneFlight>(true).Length==6,"baked flight pool");
        string[] removed={"TouchToBurst","PunchSubtitle","PunchPreviewFrame","PunchPreviewInset","PunchPreviewHint"};
        Check(!p.GetComponentsInChildren<Transform>(true).Any(t=>removed.Contains(t.name)),"removed showcase UI remains");
        Check(!p.showcase.stage.GetComponentsInChildren<Graphic>(true).Any(g=>g.raycastTarget),"showcase intercepts input");
        foreach(string name in new[]{"CoinArtwork","HeartArtwork"}){var image=p.transform.Find("SafeContent/"+name).GetComponent<Image>();Check(image.sprite!=null&&image.sprite.texture.width>=144,"currency icon missing");}
        for(int i=0;i<p.tabs.Length;i++)
        {
            p.SelectTab(i);Check(p.tabs.Count(t=>t.content.activeSelf)==1,"tab visibility");Check(p.tabs.Count(t=>t.selectedMarker.activeSelf)==1,"tab marker");Check(p.tabs[i].background.sprite==p.selectedTabSkin,"selected skin");CheckText(p);
            if(i==0)Capture(p,"BlockPunch-MainPage-Shop.png");if(p.tabs[i].id=="modes")Capture(p,"BlockPunch-MainPage-Modes.png");
        }
        p.SelectTab(p.homeIndex);p.showcase.RestartPreview();
        Canvas.ForceUpdateCanvases();
        foreach(var rune in p.showcase.runeRects){var tap=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(p.Canvas.worldCamera,rune.position)};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(tap,hits);Check(!hits.Any(h=>h.gameObject.GetComponentInParent<Button>()!=null),"center still clickable");foreach(var hit in hits)ExecuteEvents.Execute(hit.gameObject,tap,ExecuteEvents.pointerClickHandler);}
        Check(p.showcase.Launched==0,"tap triggered showcase");int peakShards=0;
        for(int i=0;i<300;i++){p.showcase.AdvancePresentation(.02f);peakShards=Math.Max(peakShards,p.showcase.effects.ActiveShards);if(i==86)Capture(p,"BlockPunch-MainPage-Shatter.png");}
        Check(p.showcase.Landed==6,"automatic preview must clear all six blocks");Check(peakShards>=12&&peakShards<=96,"bounded fragments missing");Check(p.showcase.gateCount.text=="1"||p.showcase.gateCount.text=="2","gate must show single number");
        for(int i=0;i<125;i++)p.showcase.AdvancePresentation(.02f);
        Check(p.showcase.Landed==0&&p.showcase.Launched==0&&p.showcase.effects.ActiveShards==0,"automatic loop did not reset");
        p.showcase.effects.ShatterBlock(new Vector2(240,59),"red",true,true);Check(p.showcase.effects.ActiveShards==4,"reduced-motion fragment count");p.showcase.effects.Tick(.5f);Check(p.showcase.effects.ActiveShards==0,"fragments did not expire");p.showcase.RestartPreview();
        p.SelectTab(0);Check(p.showcase.pool.GetComponentsInChildren<RuneFlight>(true).All(f=>!f.gameObject.activeSelf),"tab switch leaked flights");p.SelectTab(p.homeIndex);
        var play=p.GetComponentsInChildren<RuneRaisedButton>(true).Single(b=>b.name=="Play");var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};var original=play.face.anchoredPosition;
        play.OnPointerDown(pointer);Check(play.face.anchoredPosition.y==original.y-play.pressDepth,"pressed button");play.OnPointerUp(pointer);Check(play.face.anchoredPosition==original,"released button");
        var animation=PopupManager.Instance.GlobalAnimation;PopupManager.Instance.GlobalAnimation=null;
        try{p.OpenSettings();Check(p.IsModalBlocked,"settings popup not opened");int launched=p.showcase.Launched;p.showcase.AdvancePresentation(2);Check(p.showcase.Launched==launched,"preview not paused behind modal");}
        finally{PopupManager.Clear();PopupManager.Instance.GlobalAnimation=animation;}
        Check(play.onClick.GetPersistentEventCount()==1,"play listener");
        Check(p.GetComponentsInChildren<Transform>(true).Length==children,"runtime grew hierarchy");
        Check(PlayerPrefs.GetInt("RuneBreak.Next",0)==next&&PlayerPrefs.GetInt("RuneBreak.Clears",0)==clears,"preview changed campaign progress");
        p.showcase.RestartPreview();Capture(p,"BlockPunch-MainPage.png");
        File.WriteAllText("Tools/RuneBreak/QA/lobby-punch-runtime.json","{\"tabs\":5,\"previewBlocks\":6,\"poolInstances\":6,\"automaticOnly\":true,\"automaticReset\":true,\"frameAndHintsRemoved\":true,\"peakShards\":"+peakShards+",\"reducedMotionShards\":4,\"hierarchyStable\":true,\"settingsPause\":true,\"progressUnchanged\":true}");Debug.Log("LOBBY PUNCH PASS: automatic-only loop, fragments, no frame/hints, currency art, tabs, pool, popup pause, progress preservation.");
    }
    public static void StartPlay(){var p=Page();p.GetComponentsInChildren<RuneRaisedButton>().Single(b=>b.name=="Play").onClick.Invoke();}
    public static void VerifyPlay(){var manager=UnityEngine.Object.FindFirstObjectByType<PageManager>();Check(manager.CurrentPageType==UIPageType.PlayPage&&!manager.IsChanging,"play navigation did not finish");Check(manager.GetPage<RunePlayPage>(UIPageType.PlayPage).art.punchStyle,"play skin changed");manager.ChangeImmediate(UIPageType.MainPage);Debug.Log("LOBBY PUNCH NAVIGATION PASS: authored Play button reached the existing gameplay page.");}
    private static void CheckText(RuneLobbyPage page){Canvas.ForceUpdateCanvases();foreach(var text in page.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();Check(!text.isTextOverflowing,"text overflow: "+text.name+" / "+text.text);}}
    public static void Layout()
    {
        var p=Page();p.showcase.RestartPreview();Canvas.ForceUpdateCanvases();CheckText(p);
        foreach(var rect in p.GetComponentsInChildren<RectTransform>().Where(t=>t.name=="Settings"||t.name=="NavigationDock"||t.name=="Play"||t.name=="PunchPreviewFrame"))
        {var corners=new Vector3[4];rect.GetWorldCorners(corners);foreach(var c in corners){var point=RectTransformUtility.WorldToScreenPoint(p.Canvas.worldCamera,c);Check(point.x>=Screen.safeArea.xMin-1&&point.x<=Screen.safeArea.xMax+1&&point.y>=Screen.safeArea.yMin-1&&point.y<=Screen.safeArea.yMax+1,"safe area: "+rect.name);}}
        ScreenCapture.CaptureScreenshot("Tools/RuneBreak/QA/BlockPunch-MainPage-"+Screen.width+"x"+Screen.height+".png");Debug.Log("LOBBY PUNCH LAYOUT PASS: "+Screen.width+"x"+Screen.height);
    }
    public static void Film()
    {
        var p=Page();p.showcase.RestartPreview();Directory.CreateDirectory("Temp/LobbyPunchFilm");
        for(int frame=0;frame<210;frame++){p.showcase.AdvancePresentation(.04f);Capture(p,"../../../Temp/LobbyPunchFilm/"+frame.ToString("000")+".png");}
        p.showcase.RestartPreview();
    }
    public static void Capture(RuneLobbyPage p,string name)
    {
        Canvas.ForceUpdateCanvases();var camera=p.Canvas.worldCamera;var prior=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(540,960,24);var tex=new Texture2D(540,960,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,540,960),0,0);tex.Apply();File.WriteAllBytes("Tools/RuneBreak/QA/"+name,tex.EncodeToPNG());}
        finally{camera.targetTexture=prior;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
}
