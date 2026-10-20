using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using RuneBreak;
using RuneBreak.Editor;

public static class LobbyPreview
{
    public static void Rebuild()=>RuneProjectBuilder.RebuildLobby();
    private static RuneLobbyPage Page()
    {
        if(!Application.isPlaying)throw new Exception("Enter Play mode first.");
        var m=UnityEngine.Object.FindFirstObjectByType<PageManager>();m.ChangeImmediate(UIPageType.MainPage);
        var p=m.GetPage<RuneLobbyPage>(UIPageType.MainPage);p.CanvasGroup.alpha=1;return p;
    }
    public static void CaptureHome(){var p=Page();p.showcase.RestartPreview();Capture(p,"Lobby-dark-teal.png");}
    public static void Validate()
    {
        var p=Page();int next=PlayerPrefs.GetInt("RuneBreak.Next",0),clears=PlayerPrefs.GetInt("RuneBreak.Clears",0);
        var all=p.GetComponentsInChildren<Transform>(true);int count=all.Length;
        string[] removed={"ProgressBar","Subtitle","TryMechanicsLabel","OriginalPixelBoard","FloatingRuneGarden"};
        foreach(var t in all)if(removed.Contains(t.name)||t.name.StartsWith("TryMechanic_"))throw new Exception("Removed content remains: "+t.name);
        if(all.Count(t=>t.name.StartsWith("LeftShortcut_"))!=3||all.Count(t=>t.name.StartsWith("RightShortcut_"))!=3)throw new Exception("Expected both sidebars.");
        if(p.playLabel.text!="Lv."+(Mathf.Clamp(next,0,p.catalog.CampaignCount-1)+1))throw new Exception("Level label incorrect.");
        var demo=p.showcase;bool reduced=RunePreferences.Reduced;
        try{
            foreach(bool mode in new[]{false,true}){
                RunePreferences.Reduced=mode;demo.RestartPreview();
                for(int cycle=0;cycle<4;cycle++){
                    demo.Interact();for(int frame=0;frame<180;frame++)demo.AdvancePresentation(1f/60);
                    if(demo.Landed!=6)throw new Exception("Burst did not clear six runes.");
                    demo.RestartPreview();
                }
                demo.Interact();demo.AdvancePresentation(.1f);p.SelectTab(0);p.SelectTab(2);
                if(demo.Launched!=0||demo.Landed!=0)throw new Exception("Preview did not reset on tab reentry.");
            }
            demo.RestartPreview();p.OpenSettings();demo.Interact();for(int i=0;i<120;i++)demo.AdvancePresentation(.02f);
            if(demo.Launched!=0)throw new Exception("Modal did not pause preview.");PopupManager.Clear();
            for(int i=0;i<5;i++){p.SelectTab(i);if(p.tabs.Count(t=>t.content.activeSelf)!=1)throw new Exception("Tab visibility");}p.SelectTab(2);
            if(count!=p.GetComponentsInChildren<Transform>(true).Length)throw new Exception("Hierarchy changed.");
            if(next!=PlayerPrefs.GetInt("RuneBreak.Next",0)||clears!=PlayerPrefs.GetInt("RuneBreak.Clears",0))throw new Exception("Preview changed progress.");
            Debug.Log("LOBBY QA PASS: both sidebars, removed content, Lv. label, eight burst loops, reduced motion, modal pause, tab cancellation, constant hierarchy, unchanged campaign progress.");
        }finally{RunePreferences.Reduced=reduced;PopupManager.Clear();p.SelectTab(2);demo.RestartPreview();}
        Capture(p,"Lobby-dark-teal.png");
    }
    public static void PlayButton()
    {
        var p=Page();p.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Play").onClick.Invoke();
    }
    public static void CheckPlayAndReturn()
    {
        var m=UnityEngine.Object.FindFirstObjectByType<PageManager>();if(m.CurrentPageType!=UIPageType.PlayPage)throw new Exception("Play button failed.");
        var p=m.GetPage<RunePlayPage>(UIPageType.PlayPage);if(p.pool==null||p.LevelIndex!=Mathf.Min(PlayerPrefs.GetInt("RuneBreak.Next",0),p.catalog.CampaignCount-1))throw new Exception("Gameplay pool/level changed.");
        p.Home();Debug.Log("LOBBY PLAY BUTTON PASS: current level launched and gameplay pool intact.");
    }
    public static void Layout()
    {
        var p=Page();Canvas.ForceUpdateCanvases();var targets=p.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name=="Play"||t.name.StartsWith("Nav_")||t.name.StartsWith("LeftShortcut_")||t.name.StartsWith("RightShortcut_")||t.name=="Settings");
        foreach(var rect in targets){var corners=new Vector3[4];rect.GetWorldCorners(corners);foreach(var c in corners){var q=RectTransformUtility.WorldToScreenPoint(p.Canvas.worldCamera,c);if(q.x<Screen.safeArea.xMin-.1f||q.y<Screen.safeArea.yMin-.1f||q.x>Screen.safeArea.xMax+.1f||q.y>Screen.safeArea.yMax+.1f)throw new Exception("Clipped "+rect.name+" at "+q);}}
        ScreenCapture.CaptureScreenshot("Tools/RuneBreak/QA/Lobby-layout-"+Screen.width+"x"+Screen.height+".png");Debug.Log("LOBBY LAYOUT PASS: "+Screen.width+"x"+Screen.height);
    }
    public static void Film()
    {
        var p=Page();p.showcase.RestartPreview();Directory.CreateDirectory("Temp/LobbyPreview");
        for(int i=0;i<144;i++){if(i==30)p.showcase.Interact();p.showcase.AdvancePresentation(.05f);Capture(p,"../../../Temp/LobbyPreview/"+i.ToString("000")+".png");}
        p.showcase.RestartPreview();Debug.Log("LOBBY FILM: 144 Unity frames saved.");
    }
    private static void Capture(RuneLobbyPage p,string name)
    {
        Canvas.ForceUpdateCanvases();var cam=p.Canvas.worldCamera;var target=new RenderTexture(540,960,24);var previous=cam.targetTexture;var active=RenderTexture.active;var texture=new Texture2D(540,960,TextureFormat.RGB24,false);
        try{cam.targetTexture=target;cam.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,540,960),0,0);texture.Apply();File.WriteAllBytes("Tools/RuneBreak/QA/"+name,texture.EncodeToPNG());}
        finally{cam.targetTexture=previous;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(texture);}
    }
}
