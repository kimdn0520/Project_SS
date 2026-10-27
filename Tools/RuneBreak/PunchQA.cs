using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RuneBreak;
using RuneBreak.Editor;

public static class PunchQA
{
    [Serializable] public class Cases { public Case[] cases; }
    [Serializable] public class Case {public string dir;public bool reduced;public Vector2 from,edge,to,position;public float time,duration;public RuneReboundMotion.Pose pose;}
    private static void Check(bool ok,string message){if(!ok)throw new Exception("PUNCH QA: "+message);}
    public static void Motion()
    {
        var data=Newtonsoft.Json.JsonConvert.DeserializeObject<Cases>(File.ReadAllText("Tools/RuneBreak/Source/BlockPunch/motion-samples.json"));float maxPosition=0,maxPose=0;
        foreach(var c in data.cases)
        {
            var m=RuneReboundMotion.Create(c.from,c.edge,c.to,c.dir,c.reduced);var p=m.GetPose(c.time);
            float error=Vector2.Distance(m.Sample(c.time),c.position);maxPosition=Mathf.Max(maxPosition,error);
            float poseError=new[]{Mathf.Abs(p.sx-c.pose.sx),Mathf.Abs(p.sy-c.pose.sy),Mathf.Abs(p.angle-c.pose.angle),Mathf.Abs(p.size-c.pose.size),Mathf.Abs(p.lift-c.pose.lift),Mathf.Abs(p.flash-c.pose.flash)}.Max();maxPose=Mathf.Max(maxPose,poseError);
            Check(Mathf.Abs(m.Duration-c.duration)<.00001f,"duration");Check(error<.002f,"position "+c.dir+" "+c.time+" "+error);Check(poseError<.002f,"pose "+c.dir+" "+c.time+" "+poseError);
        }
        File.WriteAllText("Tools/RuneBreak/QA/block-punch-motion.json","{\"samples\":"+data.cases.Length+",\"maxPositionError\":"+maxPosition+",\"maxPoseError\":"+maxPose+"}");
        RuneValidation.Rules();Debug.Log("PUNCH MOTION PASS: "+data.cases.Length+" HTML samples, max position error "+maxPosition);
    }
    private static RunePlayPage Page()
    {
        Check(Application.isPlaying,"Enter Play mode");var manager=UnityEngine.Object.FindFirstObjectByType<PageManager>();manager.ChangeImmediate(UIPageType.PlayPage,0);
        var p=manager.GetPage<RunePlayPage>(UIPageType.PlayPage);p.CanvasGroup.alpha=1;return p;
    }
    private static void Tick(RunePlayPage p,float duration){int frames=Mathf.CeilToInt(duration*120);for(int i=0;i<frames;i++)p.AdvancePresentation(duration/frames);}
    public static void Preview()
    {
        var p=Page();p.LoadLevel(0);Tick(p,.02f);Capture(p,"BlockPunch-PlayPage.png");
        int id=Enumerable.Range(0,p.Rules.Level.blocks.Length).First(i=>p.Rules.Apply(p.State,new RuneMove(RuneMoveKind.Board,i),out _,out var shot,out _)&&shot.store);
        p.Tap(id,false);var f=p.pool.GetComponentsInChildren<RuneFlight>(true).First(x=>x.gameObject.activeSelf);
        Check(f.HasRebound,"new flight must rebound");Check(p.State.slots[f.shot.slot]==id,"slot was not reserved at tap");
        Tick(p,f.ReboundContactTime-.02f+.028f);Capture(p,"BlockPunch-Impact.png");Tick(p,.085f);Capture(p,"BlockPunch-Rebound.png");Tick(p,1);Capture(p,"BlockPunch-Stored.png");
        Check(p.ActiveFlights==0,"flight not returned");Check(p.slots[0].label.text!="...","slot still pending after arrival");
        p.LoadLevel(0);Debug.Log("PUNCH PREVIEW PASS");
    }
    public static void Runtime()
    {
        var p=Page();int total=0;bool audio=RunePreferences.Audio,reducedBefore=RunePreferences.Reduced;int clears=PlayerPrefs.GetInt("RuneBreak.Clears",0),next=PlayerPrefs.GetInt("RuneBreak.Next",0);RunePreferences.Audio=false;
        try
        {
            foreach(bool reduced in new[]{false,true})
            {
                RunePreferences.Reduced=reduced;
                for(int li=0;li<p.catalog.levels.Length;li++)
                {
                    p.LoadLevel(li);var route=RuneSolver.Solve(p.Rules,p.State);Check(route.status==RuneSolveStatus.Solved,"solver");
                    foreach(var move in route.route)
                    {
                        // Board taps overlap; a pending stored rune waits for its own arrival.
                        if(move.kind==RuneMoveKind.Slot)Tick(p,1.6f);
                        Check(p.Rules.Apply(p.State,move,out var expected,out _,out _),"oracle move");
                        p.Tap(move.kind==RuneMoveKind.Slot?Array.IndexOf(p.State.slots,move.id):move.id,move.kind==RuneMoveKind.Slot);
                        Check(p.State.taps==expected.taps&&p.State.board==expected.board&&p.State.spent.SequenceEqual(expected.spent)&&p.State.slots.SequenceEqual(expected.slots),"animation changed logical reservation");total++;
                        Tick(p,.018f);
                    }
                    Tick(p,4);Check(p.State.phase==RunePhase.Won&&p.ActiveFlights==0,"level did not finish "+li);
                    Check(p.gateCounts.All(t=>!t.enabled),"cleared counter still visible");
                    Check(p.pool.GetComponentsInChildren<RuneFlight>(true).Length==48,"pool changed size");
                }
            }
            File.WriteAllText("Tools/RuneBreak/QA/block-punch-runtime.json","{\"levels\":18,\"moves\":"+total+",\"reducedAndFullMotion\":true,\"reservedOnTap\":true,\"pooledFlights\":48}");
        }
        finally{RunePreferences.Audio=audio;RunePreferences.Reduced=reducedBefore;PlayerPrefs.SetInt("RuneBreak.Clears",clears);PlayerPrefs.SetInt("RuneBreak.Next",next);PlayerPrefs.Save();p.LoadLevel(0);}
        Debug.Log("PUNCH RUNTIME PASS: "+total+" moves, normal/reduced motion, every campaign/mechanic level.");
    }
    public static void Film()
    {
        var p=Page();p.LoadLevel(0);Directory.CreateDirectory("Temp/BlockPunchFilm");bool audio=RunePreferences.Audio;RunePreferences.Audio=false;
        try
        {
            for(int frame=0;frame<80;frame++)
            {
                if(frame==8){int id=Enumerable.Range(0,p.Rules.Level.blocks.Length).First(i=>p.Rules.Apply(p.State,new RuneMove(RuneMoveKind.Board,i),out _,out var s,out _)&&s.store);p.Tap(id,false);}
                p.AdvancePresentation(1f/60);Capture(p,"../../../Temp/BlockPunchFilm/"+frame.ToString("000")+".png");
            }
        }
        finally{RunePreferences.Audio=audio;p.LoadLevel(0);}
    }
    public static void FinalChecks()
    {
        RuneValidation.Prefabs();RuneValidation.RuntimeVolley();RuneValidation.RuntimePresentationReset();
        var p=Page();p.LoadLevel(0);
        int id=Enumerable.Range(0,p.Rules.Level.blocks.Length).First(i=>p.Rules.Apply(p.State,new RuneMove(RuneMoveKind.Board,i),out _,out var shot,out _)&&shot.side==0&&!shot.layerBreak);
        p.Tap(id,false);Check(p.Rules.Gate(p.State,0).hp==1,"damage not reserved at tap");Check(p.gateCounts[0].text=="2","visual should wait for impact");Tick(p,1);
        Check(p.gateCounts[0].text=="1","gate must show single remaining number");Capture(p,"BlockPunch-Gate-1.png");
        Check(p.gateCounts.All(t=>!t.text.Contains("/")),"fraction counter returned");p.LoadLevel(0);
        var lobby=p.pages.GetPage<RuneLobbyPage>(UIPageType.MainPage);
        Check(p.art.punchStyle&&p.art!=AssetDatabase.LoadAssetAtPath<RuneArtLibrary>("Assets/RuneBreak/Data/RuneArt.asset"),"shared art was replaced");
        Check(!AssetDatabase.LoadAssetAtPath<RuneArtLibrary>("Assets/RuneBreak/Data/RuneArt.asset").punchStyle,"legacy art library was overwritten");
        Check(p.pool.GetComponentsInChildren<RuneFlight>(true).All(f=>f.ribbon!=null&&f.shadow!=null&&f.afterImages.Length==2),"missing baked dependencies");
        Debug.Log("PUNCH FINAL PASS: prefab references, reset/reuse, stable hierarchy, 2 -> 1 count, legacy library preserved.");
    }
    public static void Capture(RunePlayPage p,string name)
    {
        Canvas.ForceUpdateCanvases();var camera=p.Canvas.worldCamera;var previous=camera.targetTexture;var active=RenderTexture.active;var target=new RenderTexture(540,960,24);var image=new Texture2D(540,960,TextureFormat.RGB24,false);
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,540,960),0,0);image.Apply();File.WriteAllBytes("Tools/RuneBreak/QA/"+name,image.EncodeToPNG());}
        finally{camera.targetTexture=previous;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);}
    }
}
