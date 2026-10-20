using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RuneBreak;
using RuneBreak.Editor;

public static class PopupQA
{
    private const string Report="Tools/RuneBreak/QA/popup-runtime-report.json";
    public static void Build()=>RuneProjectBuilder.RebuildSettingsPopup();
    public static void BuildVisuals()=>RuneProjectBuilder.RebuildSettingsPopupOnly();
    private static void Check(bool value,string message){if(!value)throw new Exception("POPUP QA: "+message);}
    private static async UniTask Settle(){await UniTask.Delay(250,ignoreTimeScale:true);await UniTask.Yield();}
    public static void Run()
    {
        File.WriteAllText(Report,"{\"status\":\"running\"}");RunAsync().Forget();
    }
    public static void OpenPreview()
    {
        var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();PopupManager.Clear();
        if(pages.CurrentPageType!=UIPageType.MainPage)pages.ChangeImmediate(UIPageType.MainPage);
        pages.GetPage<RuneLobbyPage>(UIPageType.MainPage).OpenSettings();
    }
    public static void Layout()
    {
        var popup=(RuneSettingsPanel)PopupManager.CurrentPopup;Check(popup!=null&&!PopupManager.IsChanging,"Wait for popup to open");
        ValidateText(popup);
        var corners=new Vector3[4];
        foreach(var rect in new[]{popup.panel,(RectTransform)popup.panel.Find("TitlePlate")})
        {
            rect.GetWorldCorners(corners);
            foreach(var corner in corners)
            {
                var point=RectTransformUtility.WorldToScreenPoint(popup.Canvas.worldCamera,corner);
                Check(Screen.safeArea.Contains(point),"Panel/title clipped at "+point);
            }
        }
        foreach(var point in new[]{new Vector2(2,2),new Vector2(Screen.width-2,2),new Vector2(2,Screen.height-2),new Vector2(Screen.width-2,Screen.height-2)})
        {
            var pointer=new PointerEventData(EventSystem.current){position=point};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Check(hits.Count>0&&hits[0].gameObject.transform.IsChildOf(popup.transform),"Screen corner not blocked: "+point);
        }
        ScreenCapture.CaptureScreenshot("Tools/RuneBreak/QA/SettingsPopup-"+Screen.width+"x"+Screen.height+".png");
        Debug.Log("POPUP LAYOUT PASS: "+Screen.width+"x"+Screen.height+"; panel inside safe area, all screen corners block page input.");
    }
    private static async UniTaskVoid RunAsync()
    {
        bool music=RunePreferences.Music,audio=RunePreferences.Audio,reduced=RunePreferences.Reduced,paths=RunePreferences.Paths;
        float volume=RunePreferences.Volume;
        var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();
        var manager=PopupManager.Instance;
        try
        {
            Check(Application.isPlaying&&pages!=null&&manager!=null,"Play scene not ready");
            Check(manager.PopupParent.name=="PopupParent","Missing authored parent");
            var lobby=pages.GetPage<RuneLobbyPage>(UIPageType.MainPage);
            var play=pages.GetPage<RunePlayPage>(UIPageType.PlayPage);
            var pool=play.pool;var sprites=SpriteManager.ExistingInstance;
            Check(lobby.popups==manager&&play.popups==manager,"Pages do not reference the scene manager");
            Check(sprites!=null&&pool!=null,"Shared sprite/pool managers missing");
            int lobbyCount=lobby.GetComponentsInChildren<Transform>(true).Length,playCount=play.GetComponentsInChildren<Transform>(true).Length;
            var prefab=AssetDatabase.LoadAssetAtPath<RuneSettingsPanel>(RuneProjectBuilder.SettingsPopupPath);
            int prefabCount=prefab.GetComponentsInChildren<Transform>(true).Length;
            Check(lobby.GetComponentsInChildren<RuneSettingsPanel>(true).Length==0&&play.GetComponentsInChildren<RuneSettingsPanel>(true).Length==0,"Embedded settings remain");
            PopupManager.Clear();pages.ChangeImmediate(UIPageType.MainPage);await Settle();
            for(int i=0;i<10;i++)lobby.OpenSettings();
            Check(lobby.IsModalBlocked,"Input not blocked during opening");
            int launches=lobby.showcase.Launched;lobby.showcase.Interact();lobby.showcase.AdvancePresentation(2);
            Check(lobby.showcase.Launched==launches,"Lobby preview continued under popup");
            await Settle();
            var popup=Current(manager,prefabCount);
            ValidateText(popup);Capture(popup.Canvas.worldCamera,"SettingsPopup-lobby.png");
            popup.musicToggle.isOn=!music;popup.audioToggle.isOn=!audio;
            Check(RunePreferences.Music==!music&&RunePreferences.Audio==!audio,"Audio events not connected");
            Check(RunePreferences.Reduced==reduced&&RunePreferences.Paths==paths&&Mathf.Approximately(RunePreferences.Volume,volume),"Hidden preferences changed");
            popup.GetComponentsInChildren<RuneRaisedButton>().Single(b=>b.name=="Close").onClick.Invoke();
            Check(manager.IsBlockingInput,"Close animation lets input through");await Settle();
            Check(popup==null&&manager.LiveInstanceCount==0&&manager.PopupParent.childCount==0,"Closed instance was retained");
            lobby.OpenSettings();await Settle();popup=Current(manager,prefabCount);
            Check(popup.musicToggle.isOn==!music&&popup.audioToggle.isOn==!audio,"Reopened popup lost preferences");
            popup.OnClickCurtain();await Settle();Check(popup==null,"Curtain did not destroy popup");

            pages.ChangeImmediate(UIPageType.PlayPage,0);await Settle();play.OpenSettings();
            int taps=play.State.taps;var positions=play.blockRects.Select(r=>r.anchoredPosition).ToArray();
            play.Tap(0,false);play.AdvancePresentation(2);
            Check(play.State.taps==taps&&play.blockRects.Select(r=>r.anchoredPosition).SequenceEqual(positions),"Gameplay not paused during popup opening");
            await Settle();popup=Current(manager,prefabCount);
            ValidateText(popup);Capture(popup.Canvas.worldCamera,"SettingsPopup-play.png");
            var data=new PointerEventData(EventSystem.current){position=new Vector2(4,4)};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Check(hits.Count>0&&hits[0].gameObject.transform.IsChildOf(popup.transform),"Fullscreen curtain does not block page input");
            var back=typeof(PageManager).GetMethod("HandleBackInput",BindingFlags.Instance|BindingFlags.NonPublic);
            back.Invoke(pages,null);back.Invoke(pages,null);await Settle();
            Check(pages.CurrentPageType==UIPageType.PlayPage&&popup==null,"Back escaped through to page");

            play.OpenSettings();await Settle();popup=Current(manager,prefabCount);
            popup.GetComponentsInChildren<RuneRaisedButton>().Single(b=>b.name=="Close").onClick.Invoke();await Settle();
            Check(pages.CurrentPageType==UIPageType.PlayPage&&manager.PopupParent.childCount==0,"Close did not return to gameplay");
            pages.ChangeImmediate(UIPageType.MainPage);await Settle();
            // Cancel an opening popup, then immediately open one for the new page.
            lobby.OpenSettings();pages.ChangeImmediate(UIPageType.PlayPage,0);play.OpenSettings();await Settle();
            popup=Current(manager,prefabCount);ValidateText(popup);
            popup.Dismiss();pages.ChangeImmediate(UIPageType.MainPage);lobby.OpenSettings();await Settle();
            popup=Current(manager,prefabCount);ValidateText(popup);
            int instance=popup.GetInstanceID();
            PopupManager.Queue(RuneSettingsPanel.PrefabName);Check(manager.QueuedCount==1,"Queue not retained");
            popup.Dismiss();await UniTask.Delay(450,ignoreTimeScale:true);
            popup=Current(manager,prefabCount);Check(popup.GetInstanceID()!=instance&&manager.QueuedCount==0,"Queue did not create the next authored instance");
            PopupManager.Clear();await UniTask.Yield();
            Check(manager.PopupParent.childCount==0&&manager.LiveInstanceCount==0&&!manager.IsBlockingInput,"Clear leaked popup state");
            Check(lobbyCount==lobby.GetComponentsInChildren<Transform>(true).Length&&playCount==play.GetComponentsInChildren<Transform>(true).Length,"Page hierarchy changed");
            Check(play.pool==pool&&SpriteManager.ExistingInstance==sprites,"Pool or sprite manager replaced");
            File.WriteAllText(Report,"{\"status\":\"passed\",\"rapidOpenRequests\":10,\"livePopupLimit\":1,\"cachedHierarchyObjects\":"+prefabCount+",\"destroyOnClose\":true,\"preferencesPersist\":true,\"modalPause\":true,\"backRouting\":true,\"pageExitCancellation\":true,\"queue\":true,\"poolAndSpritePreserved\":true}");
            Debug.Log("POPUP QA PASS: title plaque, music/SFX/Close only, cached prefab, saved audio settings, destruction, pause, back, cancellation, queue, stable hierarchy and shared managers.");
        }
        catch(Exception e){File.WriteAllText(Report,"{\"status\":\"failed\",\"error\":\""+e.Message.Replace("\\","\\\\").Replace("\"","\\\"")+"\"}");Debug.LogException(e);}
        finally
        {
            RunePreferences.Music=music;RunePreferences.Audio=audio;RunePreferences.Reduced=reduced;RunePreferences.Paths=paths;RunePreferences.Volume=volume;PlayerPrefs.Save();
            PopupManager.Clear();
            if(pages!=null)pages.ChangeImmediate(UIPageType.MainPage);
        }
    }
    private static RuneSettingsPanel Current(PopupManager manager,int count)
    {
        Check(PopupManager.OpenedCount==1&&manager.LiveInstanceCount==1&&manager.PopupParent.childCount==1,"Duplicate/missing instance");
        var popup=PopupManager.CurrentPopup as RuneSettingsPanel;
        Check(popup!=null&&popup.transform.parent==manager.PopupParent,"Wrong parent/type");
        Check(popup.GetComponentsInChildren<Transform>(true).Length==count,"Runtime hierarchy constructed");
        Check(!PopupManager.IsChanging,"Transition did not finish");
        return popup;
    }
    private static void ValidateText(RuneSettingsPanel popup)
    {
        Canvas.ForceUpdateCanvases();
        var labels=popup.GetComponentsInChildren<TMP_Text>();
        Check(labels.Select(t=>t.text).OrderBy(t=>t).SequenceEqual(new[]{"설정","배경음","효과음","닫기"}.OrderBy(t=>t)),"Unexpected settings content");
        Check(popup.GetComponentsInChildren<Toggle>(true).Length==2&&popup.GetComponentsInChildren<Slider>(true).Length==0,"Unexpected settings controls");
        var plate=(RectTransform)popup.panel.Find("TitlePlate");
        Check(plate.anchoredPosition.y>0&&plate.anchoredPosition.y<plate.rect.height,"Title plaque does not overlap top border");
        foreach(var text in labels)
        {text.ForceMeshUpdate();Check(text.font==RuneFontAuthoring.Bold&&!text.isTextOverflowing,"Text overflow or wrong font: "+text.name);}
    }
    private static void Capture(Camera camera,string name)
    {
        Canvas.ForceUpdateCanvases();var target=new RenderTexture(540,960,24);var previous=camera.targetTexture;var active=RenderTexture.active;
        var texture=new Texture2D(540,960,TextureFormat.RGB24,false);
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,540,960),0,0);texture.Apply();File.WriteAllBytes("Tools/RuneBreak/QA/"+name,texture.EncodeToPNG());}
        finally{camera.targetTexture=previous;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(texture);}
    }
}
