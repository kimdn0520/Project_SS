using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using RuneBreak;

public static class MechanicPreview
{
    private static RunePlayPage Page()
    {
        var manager=UnityEngine.Object.FindFirstObjectByType<PageManager>();
        if(manager==null||!Application.isPlaying)throw new Exception("Enter Play mode first.");
        manager.ChangeImmediate(UIPageType.PlayPage,6);
        return manager.GetPage<RunePlayPage>(UIPageType.PlayPage);
    }
    public static void Initial()
    {
        var p=Page();for(int i=6;i<9;i++){p.LoadLevel(i);Tick(p,.02f);Capture(p.Canvas.worldCamera,"Mechanic-"+p.Rules.Level.mechanic+".png");}
        var manager=p.pages;manager.ChangeImmediate(UIPageType.MainPage);var lobby=manager.GetPage<RuneLobbyPage>(UIPageType.MainPage);Capture(lobby.Canvas.worldCamera,"Mechanic-home.png");
        Debug.Log("RUNE MECHANIC PREVIEWS: four captures saved.");
    }
    public static void Chain(){var p=Page();p.LoadLevel(6);}
    public static void Mirror(){var p=Page();p.LoadLevel(7);}
    public static void Stack(){var p=Page();p.LoadLevel(8);}
    public static void Home(){var p=Page();p.pages.ChangeImmediate(UIPageType.MainPage);}
    public static void Layout()
    {
        var manager=UnityEngine.Object.FindFirstObjectByType<PageManager>();var p=manager.GetPage<RunePlayPage>(UIPageType.PlayPage);
        Canvas.ForceUpdateCanvases();var targets=new[]{p.cabinet,p.mechanicHint.rectTransform,p.stageNumber.rectTransform}.Concat(p.GetComponentsInChildren<RectTransform>(true).Where(r=>r.name=="Settings"||r.name.StartsWith("ReadyBoosterSlot_"))).ToArray();
        foreach(var rect in targets){var corners=new Vector3[4];rect.GetWorldCorners(corners);foreach(var corner in corners){var point=RectTransformUtility.WorldToScreenPoint(p.Canvas.worldCamera,corner);if(!Screen.safeArea.Contains(point)&&point!=Screen.safeArea.max)throw new Exception("Layout clipped: "+rect.name+" at "+point);}}
        ScreenCapture.CaptureScreenshot("Tools/RuneBreak/QA/Mechanic-layout-"+Screen.width+"x"+Screen.height+".png");Debug.Log("RUNE MECHANIC LAYOUT PASS: "+Screen.width+"x"+Screen.height);
    }
    private static void Tick(RunePlayPage p,float advance)
    {
        int frames=Mathf.Max(1,Mathf.CeilToInt(advance*60));for(int i=0;i<frames;i++)p.AdvancePresentation(advance/frames);
    }
    public static void Film()
    {
        var p=Page();bool reduced=RunePreferences.Reduced,audio=RunePreferences.Audio;RunePreferences.Reduced=false;RunePreferences.Audio=false;
        p.CanvasGroup.alpha=1;UnityEngine.Random.InitState(840);
        try
        {
            for(int demo=0;demo<3;demo++)
            {
                p.LoadLevel(6+demo);string folder="../../../Temp/RuneBreakPreview/"+p.Rules.Level.mechanic;Directory.CreateDirectory("Tools/RuneBreak/QA/"+folder);
                for(int frame=0;frame<100;frame++)
                {
                    if(demo==0){if(frame==12)p.Tap(1,false);if(frame==28)p.Tap(0,false);if(frame==42)p.Tap(2,false);if(frame==65)p.Tap(3,false);if(frame==75)p.Tap(4,false);if(frame==85)p.Tap(0,true);}
                    if(demo==1){if(frame==15)p.Tap(0,false);if(frame==55)p.Tap(1,false);}
                    if(demo==2){if(frame==15)p.Tap(2,false);if(frame==45)p.Tap(3,false);if(frame==65)p.Tap(0,false);}
                    p.AdvancePresentation(.05f);Capture(p.Canvas.worldCamera,folder+"/"+frame.ToString("000")+".png");
                }
            }
        }
        finally{RunePreferences.Reduced=reduced;RunePreferences.Audio=audio;p.LoadLevel(6);}
        Debug.Log("RUNE MECHANIC FILM: 300 real Unity frames rendered, 20 fps.");
    }
    public static void Effects()
    {
        var p=Page();bool reduced=RunePreferences.Reduced;RunePreferences.Reduced=false;
        try
        {
            p.LoadLevel(6);p.Tap(1,false);Tick(p,1);p.Tap(0,false);Tick(p,1);p.Tap(2,false);Tick(p,.65f);Capture(p.Canvas.worldCamera,"Mechanic-chain-break.png");
            p.LoadLevel(7);p.Tap(0,false);var f=Array.Find(p.GetComponentsInChildren<RuneFlight>(true),q=>q.gameObject.activeSelf);float turn=144f/(144+243);Tick(p,f.duration*(.1f+.9f*turn)+.025f);Capture(p.Canvas.worldCamera,"Mechanic-mirror-turn.png");
            p.LoadLevel(8);p.Tap(2,false);Tick(p,.10f);Capture(p.Canvas.worldCamera,"Mechanic-stack-reveal.png");
        }
        finally{RunePreferences.Reduced=reduced;p.LoadLevel(6);}
    }
    private static void Capture(Camera camera,string name)
    {
        Canvas.ForceUpdateCanvases();var target=new RenderTexture(540,960,24);var previous=camera.targetTexture;var active=RenderTexture.active;
        var image=new Texture2D(540,960,TextureFormat.RGB24,false);
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,540,960),0,0);image.Apply();Directory.CreateDirectory("Tools/RuneBreak/QA");File.WriteAllBytes("Tools/RuneBreak/QA/"+name,image.EncodeToPNG());}
        finally{camera.targetTexture=previous;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);}
    }
}
