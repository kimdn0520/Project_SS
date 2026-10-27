using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RuneBreak;

public static class BlockUiQA
{
    private static void Check(bool value,string message){if(!value)throw new Exception("BLOCK UI: "+message);}
    public static void Execute()
    {
        var manager=UnityEngine.Object.FindFirstObjectByType<PageManager>();
        var lobby=manager.GetPage<RuneLobbyPage>(UIPageType.MainPage);var play=manager.GetPage<RunePlayPage>(UIPageType.PlayPage);
        int next=PlayerPrefs.GetInt("RuneBreak.Next",0),clears=PlayerPrefs.GetInt("RuneBreak.Clears",0);bool hasNext=PlayerPrefs.HasKey("RuneBreak.Next"),hasClears=PlayerPrefs.HasKey("RuneBreak.Clears");
        bool music=RunePreferences.Music,audio=RunePreferences.Audio;var animation=PopupManager.Instance.GlobalAnimation;PopupManager.Instance.GlobalAnimation=null;
        try
        {
            manager.ChangeImmediate(UIPageType.MainPage);lobby.CanvasGroup.alpha=1;lobby.showcase.RestartPreview();
            Check(lobby.playLabel.text=="Lv. "+(Mathf.Clamp(next,0,lobby.catalog.CampaignCount-1)+1),"level label");Check(!lobby.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.StartsWith("STAGE ")),"extra CTA text remains");
            var playButton=lobby.playLabel.GetComponentInParent<RuneRaisedButton>();var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};var resting=playButton.face.anchoredPosition;
            playButton.OnPointerDown(pointer);Check(playButton.face.anchoredPosition.y==resting.y-7,"block button press depth");Capture(lobby.Canvas,"BlockCrash-PlayButton-Pressed.png");playButton.OnPointerUp(pointer);Check(playButton.face.anchoredPosition==resting,"button release");
            CheckText(lobby.gameObject);Capture(lobby.Canvas,"BlockCrash-MainPage.png");
            lobby.OpenSettings();var settings=(RuneSettingsPanel)PopupManager.CurrentPopup;Check(settings!=null&&lobby.IsModalBlocked,"lobby popup not opened");
            Check(settings.musicToggle.graphic is Image&&settings.audioToggle.graphic is Image,"authored switch artwork");
            CheckText(settings.gameObject);Capture(settings.Canvas,"BlockCrash-Settings-Lobby.png");
            settings.musicToggle.isOn=!music;settings.audioToggle.isOn=!audio;Check(RunePreferences.Music==!music&&RunePreferences.Audio==!audio,"toggle callbacks");
            Capture(settings.Canvas,"BlockCrash-Settings-Toggled.png");settings.musicToggle.isOn=music;settings.audioToggle.isOn=audio;
            settings.lobbyActions.GetComponent<Button>().onClick.Invoke();Check(!PopupManager.Instance.HasOpenPopups,"close button callback");
            manager.ChangeImmediate(UIPageType.PlayPage,Mathf.Clamp(next,0,play.catalog.CampaignCount-1));play.CanvasGroup.alpha=1;
            play.OpenSettings();settings=(RuneSettingsPanel)PopupManager.CurrentPopup;Check(settings.playActions.activeSelf&&!settings.lobbyActions.activeSelf,"play settings actions");CheckText(settings.gameObject);Capture(settings.Canvas,"BlockCrash-Settings-Play.png");
            settings.playActions.transform.Find("Resume").GetComponent<Button>().onClick.Invoke();Check(!PopupManager.Instance.HasOpenPopups,"resume callback");
            int level=play.LevelIndex;
            foreach(var phase in new[]{RunePhase.Won,RunePhase.Lost})
            {
                play.LoadLevel(0);play.State.phase=phase;typeof(RunePlayPage).GetMethod("ShowResult",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(play,null);
                Check(play.resultEmblem.sprite==play.art.Rune(phase==RunePhase.Won?"green":"red",phase==RunePhase.Won?"N":"S"),"result color");
                CheckText(play.result);foreach(var button in play.result.GetComponentsInChildren<Button>(true))Check(button.onClick.GetPersistentEventCount()==1,"result callback missing");Capture(play.Canvas,phase==RunePhase.Won?"BlockCrash-Result-Win.png":"BlockCrash-Result-Loss.png");
            }
            play.LoadLevel(level);Debug.Log("BLOCK UI PASS: level text, press/release, both settings contexts, toggles, close/resume, win/loss layouts and callbacks.");
        }
        finally
        {
            PopupManager.Clear();PopupManager.Instance.GlobalAnimation=animation;RunePreferences.Music=music;RunePreferences.Audio=audio;
            if(hasNext)PlayerPrefs.SetInt("RuneBreak.Next",next);else PlayerPrefs.DeleteKey("RuneBreak.Next");if(hasClears)PlayerPrefs.SetInt("RuneBreak.Clears",clears);else PlayerPrefs.DeleteKey("RuneBreak.Clears");PlayerPrefs.Save();
            play.LoadLevel(Mathf.Clamp(next,0,play.catalog.CampaignCount-1));manager.ChangeImmediate(UIPageType.MainPage);lobby.showcase.RestartPreview();
        }
    }
    private static void CheckText(GameObject root){Canvas.ForceUpdateCanvases();foreach(var text in root.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();Check(!text.isTextOverflowing,"text overflow: "+text.name+" / "+text.text);}}
    private static void Capture(Canvas canvas,string name)
    {
        Canvas.ForceUpdateCanvases();var camera=canvas.worldCamera;var prior=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(540,960,24);var tex=new Texture2D(540,960,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,540,960),0,0);tex.Apply();File.WriteAllBytes("Tools/RuneBreak/QA/"+name,tex.EncodeToPNG());}
        finally{camera.targetTexture=prior;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
}
