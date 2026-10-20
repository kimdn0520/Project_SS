using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RuneBreak;
using RuneBreak.Editor;
public static class RuneSceneQA
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void PrepareSplash()
    {
        Check(!Application.isPlaying,"Stop Play mode first");
        AssetDatabase.ImportAsset("Assets/Scenes/Splash.unity",ImportAssetOptions.ForceSynchronousImport);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Splash.unity",true){guid=new GUID(AssetDatabase.AssetPathToGUID("Assets/Scenes/Splash.unity"))},new EditorBuildSettingsScene("Assets/Scenes/Play.unity",true){guid=new GUID(AssetDatabase.AssetPathToGUID("Assets/Scenes/Play.unity"))}};
        PlayerPrefs.SetInt("RuneBreak.Clears",0);PlayerPrefs.SetInt("RuneBreak.Next",0);PlayerPrefs.Save();
        EditorSceneManager.OpenScene("Assets/Scenes/Splash.unity");SetSize(540,960);AssetDatabase.SaveAssets();
    }
    public static void CheckLobby()
    {
        var manager=UnityEngine.Object.FindFirstObjectByType<PageManager>();Check(manager!=null&&manager.CurrentPageType==UIPageType.MainPage,"Splash did not reach MainPage");Check(manager.PageCount==2,"Page count");
        var lobby=manager.GetPage<RuneLobbyPage>(UIPageType.MainPage);for(int i=0;i<lobby.tabs.Length;i++){lobby.SelectTab(i);Check(lobby.tabs.Count(t=>t.content.activeSelf)==1,"Exactly one tab must be active");}lobby.SelectTab(lobby.homeIndex);
        ScreenCapture.CaptureScreenshot("Tools/RuneBreak/QA/MainPage.png");Debug.Log("RUNE SPLASH/LOBBY PASS: scene transition, five tabs, home selected.");
    }
    public static void ShowPlay()
    {
        var manager=UnityEngine.Object.FindFirstObjectByType<PageManager>();manager.ChangeImmediate(UIPageType.PlayPage,5);
    }
    public static void PrepareInteraction()
    {
        var p=UnityEngine.Object.FindFirstObjectByType<RunePlayPage>();PopupManager.Clear();p.Restart();
    }
    public static void Interaction()
    {
        var p=UnityEngine.Object.FindFirstObjectByType<RunePlayPage>();Check(p!=null,"Play missing");
        var forbidden=new[]{"Undo","Restart","Hint","NorthForecastPanel","EastForecastPanel","NorthLayers","EastLayers","Stage","Previous","Next","Stats","Message"};
        foreach(var t in p.GetComponentsInChildren<Transform>(true))Check(!forbidden.Contains(t.name),"Prototype UI remains: "+t.name);
        int id=Enumerable.Range(0,p.Rules.Level.blocks.Length).First(i=>p.Rules.IsFree(p.State,i));
        Vector2 point=RectTransformUtility.WorldToScreenPoint(p.Canvas.worldCamera,p.blockRects[id].position);
        var pointer=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        Check(hits.Count>0,"No rune raycast");var button=hits[0].gameObject.GetComponent<Button>();Check(button!=null&&button.gameObject==p.blocks[id].gameObject,"Rune touch blocked by "+hits[0].gameObject.name);
        ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);Check(p.State.taps==1,"Serialized rune button not wired");
        var settings=p.GetComponentsInChildren<Button>(true).First(b=>b.name=="Settings");settings.onClick.Invoke();Check(p.IsModalBlocked,"Settings listener missing");
        double deadline=EditorApplication.timeSinceStartup+.25;
        void AfterModalRender()
        {
        if(EditorApplication.timeSinceStartup<deadline)return;EditorApplication.update-=AfterModalRender;
        Canvas.ForceUpdateCanvases();hits.Clear();EventSystem.current.RaycastAll(pointer,hits);Check(hits.Count>0&&hits[0].gameObject!=button.gameObject,"Modal failed to block board");
        var panel=(RuneSettingsPanel)PopupManager.CurrentPopup;bool music=RunePreferences.Music,audio=RunePreferences.Audio;
        panel.musicToggle.isOn=!music;panel.audioToggle.isOn=!audio;
        Check(RunePreferences.Music==!music&&RunePreferences.Audio==!audio,"Setting event binding");
        panel.musicToggle.isOn=music;panel.audioToggle.isOn=audio;panel.Close();p.Restart();
        Check(p.GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("ReadyBoosterSlot_"))==3,"Booster count");Debug.Log("RUNE INTERACTION PASS: actual UI raycast/click, settings, modal input block, three boosters, prototype controls absent.");
        }
        EditorApplication.update+=AfterModalRender;
    }
    public static void Portrait(){SetSize(540,960);}
    public static void Tall(){SetSize(540,1200);}
    public static void Tablet(){SetSize(768,1024);}
    public static void Landscape(){SetSize(1280,720);}
    public static void Capture()
    {
        var p=UnityEngine.Object.FindFirstObjectByType<RunePlayPage>();Check(p!=null,"Play missing");
        var safe=Screen.safeArea;foreach(var t in p.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name=="Settings"||t.name.StartsWith("ReadyBoosterSlot_")||t.name=="Cabinet_Original480x584"))
        {
            var corners=new Vector3[4];t.GetWorldCorners(corners);foreach(var corner in corners){var point=RectTransformUtility.WorldToScreenPoint(p.Canvas.worldCamera,corner);Check(point.x>=safe.xMin-1&&point.y>=safe.yMin-1&&point.x<=safe.xMax+1&&point.y<=safe.yMax+1,"Safe-area clipping: "+t.name);}
        }
        ScreenCapture.CaptureScreenshot($"Tools/RuneBreak/QA/PlayPage-{Screen.width}x{Screen.height}.png");Debug.Log($"RUNE LAYOUT PASS: {Screen.width}x{Screen.height}, board/settings/boosters within safe area.");
    }
    private static void SetSize(int width,int height)
    {
        var asm=typeof(UnityEditor.Editor).Assembly;var type=asm.GetType("UnityEditor.GameViewSizes");var singleton=type.BaseType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
        var groupType=asm.GetType("UnityEditor.GameViewSizeGroupType");var group=type.GetMethod("GetGroup").Invoke(singleton,new[]{Enum.Parse(groupType,"Standalone")});
        var sizeType=asm.GetType("UnityEditor.GameViewSize");var kind=asm.GetType("UnityEditor.GameViewSizeType");var size=Activator.CreateInstance(sizeType,new object[]{Enum.Parse(kind,"FixedResolution"),width,height,$"Rune QA {width}x{height}"});
        int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
        var viewType=asm.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(viewType);viewType.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,count);view.Focus();view.Repaint();
    }
    public static void Finalize()
    {
        Check(!Application.isPlaying,"Stop Play mode first");PlayerPrefs.SetInt("RuneBreak.Clears",0);PlayerPrefs.SetInt("RuneBreak.Next",0);PlayerPrefs.Save();
        RuneValidation.Rules();RuneValidation.Prefabs();EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");SetSize(540,960);
    }
}
