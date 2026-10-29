using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RuneBreak;
using RuneBreak.Editor;

public static class MechanicQA
{
    [Serializable] public class Fixtures {public RuneLevel[] levels;public Case[] cases;public Route[] routes;}
    [Serializable] public class Case {public int level;public RuneValidation.StateData before,after;public RuneValidation.ActionData action;public bool ok;public string reason;public string[] unlocked,revealed;}
    [Serializable] public class Route {public int level,x,y;public string id,side;public bool loop;public RuneCell[] turns;}
    private static void Check(bool condition,string message){if(!condition)throw new Exception("MECHANIC QA: "+message);}
    private static int Id(RuneLevel level,string id)=>Array.FindIndex(level.blocks,b=>b.id==id);
    private static uint Mask(RuneLevel level,string[] ids){uint m=0;foreach(string id in ids)m|=1u<<Id(level,id);return m;}
    private static RuneState State(RuneLevel l,RuneValidation.StateData s)=>new RuneState{board=Mask(l,s.board),slots=s.slots.Select(id=>string.IsNullOrEmpty(id)?-1:Id(l,id)).ToArray(),spent=new[]{s.spent.N,s.spent.E},phase=s.phase=="won"?RunePhase.Won:s.phase=="lost"?RunePhase.Lost:RunePhase.Playing,taps=s.taps,direct=s.direct,stored=s.stored,relaunch=s.relaunch,peak=s.peak};
    private static void Same(RuneState a,RuneState b,string context)
    {Check(a.board==b.board&&a.phase==b.phase&&a.slots.SequenceEqual(b.slots)&&a.spent.SequenceEqual(b.spent)&&a.taps==b.taps&&a.direct==b.direct&&a.stored==b.stored&&a.relaunch==b.relaunch&&a.peak==b.peak,context);}
    public static void Rules()
    {
        RuneValidation.Rules();RuneValidation.Prefabs();
        var data=Newtonsoft.Json.JsonConvert.DeserializeObject<Fixtures>(File.ReadAllText("Temp/RuneBreakTests/mechanic-fixtures.json"));
        Check(data!=null&&data.levels!=null&&data.cases!=null&&data.routes!=null,"Fixture root did not deserialize: levels="+(data?.levels?.Length.ToString()??"null")+", cases="+(data?.cases?.Length.ToString()??"null")+", routes="+(data?.routes?.Length.ToString()??"null"));
        var rules=data.levels.Select(l=>new RuneRules(l)).ToArray();int n=0;
        foreach(var c in data.cases)
        {
            try
            {
            Check(c!=null&&c.before!=null&&c.after!=null&&c.action!=null,"Missing fixture "+n);
            var r=rules[c.level];var before=State(r.Level,c.before);var snapshot=before.Copy();var move=new RuneMove(c.action.type=="slot"?RuneMoveKind.Slot:RuneMoveKind.Board,Id(r.Level,c.action.id));
            bool ok=r.Apply(before,move,out var after,out var shot,out var reason);
            Check(ok==c.ok,"Acceptance "+n);Same(before,snapshot,"Input mutation "+n);Same(after,State(r.Level,c.after),"Oracle state "+n);
            if(ok){Check(shot.unlocked==Mask(r.Level,c.unlocked),"Unlock reservation "+n);Check(shot.revealed==Mask(r.Level,c.revealed),"Reveal reservation "+n);}
            else Check(reason==c.reason,"Rejection reason "+n+": "+reason+" / "+c.reason);
            }
            catch(Exception e){throw new Exception("Fixture #"+n+" level "+c.level+" action "+c.action.id+" (unlocked="+(c.unlocked==null?"null":c.unlocked.Length.ToString())+"): "+e,e);}
            n++;
        }
        Debug.Log("RUNE MECHANIC fixture states compared: "+n);
        Check(data.routes!=null,"Routes not deserialized");
        foreach(var r in data.routes)
        {
            Check(r!=null&&r.turns!=null,"Route turns not deserialized: "+(r==null?"null":r.id));
            var path=rules[r.level].Routes[Id(data.levels[r.level],r.id)];Check(path.loop==r.loop,"Loop detection");
            Check(path.turns.Length==r.turns.Length,"Mirror count");for(int i=0;i<r.turns.Length;i++)Check(path.turns[i].x==r.turns[i].x&&path.turns[i].y==r.turns[i].y,"Mirror location");
            if(!r.loop)Check(path.exitDir==r.side&&path.exitX==r.x&&path.exitY==r.y,"Reflected exit");
        }
        var chain=new RuneLevel{size=6,seals=new RuneSeals{N=Array.Empty<RuneLayer>(),E=new[]{new RuneLayer{color="blue",hp=1}}},blocks=new[]{new RuneBlock{id="sealed",x=2,y=2,dir="E",color="blue",chainSide="E",chainLayer=0}}};
        var locked=new RuneRules(chain);Check(RuneSolver.Solve(locked,locked.Initial()).status==RuneSolveStatus.Dead,"Circular chain dependency must be unsolvable");
        File.WriteAllText("Tools/RuneBreak/QA/mechanic-csharp-report.json","{\"oracleCases\":"+n+",\"geometryRoutes\":"+data.routes.Length+",\"circularChain\":\"dead\",\"result\":\"passed\"}");
        Debug.Log("RUNE MECHANICS RULES PASS: "+n+" independent oracle cases; "+data.routes.Length+" routes, both mirror diagonals/all directions, loops, blocked reflected paths, stacked chains.");
    }
    private static void Drain(RunePlayPage p){for(int i=0;i<600&&p.ActiveFlights>0;i++)p.AdvancePresentation(1f/60);Check(p.ActiveFlights==0,"Flight failed to arrive");}
    public static void Runtime()
    {
        Check(Application.isPlaying,"Enter Play mode");var manager=UnityEngine.Object.FindFirstObjectByType<PageManager>();manager.ChangeImmediate(UIPageType.PlayPage,6);var p=manager.GetPage<RunePlayPage>(UIPageType.PlayPage);
        int clears=PlayerPrefs.GetInt("RuneBreak.Clears",0),next=PlayerPrefs.GetInt("RuneBreak.Next",0),children=p.GetComponentsInChildren<Transform>(true).Length;
        bool audio=RunePreferences.Audio,reduced=RunePreferences.Reduced;var transition=manager.Transition;RunePreferences.Audio=false;
        manager.Transition=null;p.CanvasGroup.alpha=1;
        int solves=0,peakFlights=0;
        try
        {
            p.LoadLevel(6);int count=p.State.taps;p.Tap(3,false);Check(p.State.taps==count,"Chained rune launched too early");
            p.Tap(1,false);p.Tap(0,false);p.Tap(2,false);Check(!p.Rules.IsChained(p.State,3),"Chain must unlock on reserved damage");
            p.Tap(3,false);Check((p.State.board&(1u<<3))==0,"Rapid input after logical chain break was rejected");Check(p.ActiveFlights==4,"Rapid chain volley must be pooled");Drain(p);
            for(int pass=0;pass<2;pass++)for(int li=6;li<9;li++)
            {
                RunePreferences.Reduced=pass==1;p.LoadLevel(li);var expected=p.State;var solution=RuneSolver.Solve(p.Rules,p.State);Check(solution.status==RuneSolveStatus.Solved,"Demo not solvable");
                foreach(var move in solution.route)
                {
                    if(move.kind==RuneMoveKind.Slot)Drain(p);
                    Check(p.Rules.Apply(expected,move,out var after,out _,out _),"Solver action rejected");
                    p.Tap(move.kind==RuneMoveKind.Slot?Array.IndexOf(p.State.slots,move.id):move.id,move.kind==RuneMoveKind.Slot);
                    Same(p.State,after,"Rapid presentation diverged from rules");expected=after;peakFlights=Math.Max(peakFlights,p.ActiveFlights);
                    if(pass==1)p.AdvancePresentation(.11f);
                }
                Drain(p);for(int i=0;i<40;i++)p.AdvancePresentation(1f/60);Check(p.State.phase==RunePhase.Won&&PopupManager.CurrentPopup is RuneResultPanel,"Demo result not presented");
                Check(p.GetComponentsInChildren<Transform>(true).Length==children,"Runtime hierarchy grew");solves++;
            }
            RunePreferences.Reduced=false;p.LoadLevel(8);Check(!p.blocks[3].gameObject.activeSelf&&p.stackPreviews[2].enabled,"Covered rune must be preview-only");
            p.Tap(2,false);Check(p.blocks[3].gameObject.activeSelf&&!p.stackPreviews[2].enabled,"Lower rune not revealed immediately");
            p.OpenSettings();int taps=p.State.taps;var positions=p.blockRects.Select(r=>r.anchoredPosition).ToArray();p.Tap(3,false);p.AdvancePresentation(2);Check(p.State.taps==taps&&p.blockRects.Select(r=>r.anchoredPosition).SequenceEqual(positions),"Settings did not pause input/presentation");PopupManager.Clear();
            p.LoadLevel(7);p.Tap(0,false);p.AdvancePresentation(.1f);p.LoadLevel(6);Check(p.ActiveFlights==0&&p.mirrorGlints.All(g=>g.color.a==0),"Mirror flight/flash leaked between demos");
            Check(p.chainWraps[3].enabled&&p.chainWraps[4].enabled,"Chains not restored on reload");
            p.LoadLevel(8);Canvas.ForceUpdateCanvases();p.Canvas.worldCamera.Render();
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(p.Canvas.worldCamera,p.previewRects[2].position),button=PointerEventData.InputButton.Left};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Check(hits.Count>0&&hits[0].gameObject==p.blocks[2].gameObject,"Stack preview raycast at "+pointer.position+": "+string.Join(",",hits.Select(h=>h.gameObject.name))+"; expected "+p.blocks[2].gameObject.name+" / active "+p.blocks[2].gameObject.activeInHierarchy+" depth "+p.blocks[2].depth+" cull "+p.blocks[2].canvasRenderer.cull+" target "+p.blocks[2].raycastTarget+" rect "+RectTransformUtility.RectangleContainsScreenPoint(p.blockRects[2],pointer.position,p.Canvas.worldCamera));
            ExecuteEvents.Execute(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);Check(p.State.taps==1,"Stack UI click was not wired");
            manager.ChangeImmediate(UIPageType.MainPage);var lobby=manager.GetPage<RuneLobbyPage>(UIPageType.MainPage);
            for(int i=0;i<3;i++){lobby.PlayDemo(i);Check(p.LevelIndex==6+i,"Editor demo navigation");manager.ChangeImmediate(UIPageType.MainPage);}
            Check(PlayerPrefs.GetInt("RuneBreak.Clears",0)==clears&&PlayerPrefs.GetInt("RuneBreak.Next",0)==next,"Demo altered campaign progress");
            File.WriteAllText("Tools/RuneBreak/QA/mechanic-runtime-report.json","{\"solves\":"+solves+",\"peakPooledFlights\":"+peakFlights+",\"stableHierarchyObjects\":"+children+",\"rapidUnlock\":true,\"stackRaycast\":true,\"settingsPause\":true,\"demoNavigation\":true,\"campaignUnchanged\":true}");
            Debug.Log("RUNE MECHANIC RUNTIME PASS: "+solves+" live solves, rapid unlock, pooled flights, reduced motion, reset, real stack-preview raycast/click, settings pause, editor demo navigation, campaign unchanged.");
        }
        finally{RunePreferences.Audio=audio;RunePreferences.Reduced=reduced;manager.ChangeImmediate(UIPageType.MainPage);manager.GetPage<RuneLobbyPage>(UIPageType.MainPage).CanvasGroup.alpha=1;p.CanvasGroup.alpha=1;manager.Transition=transition;}
    }
    public static void ClickMirror()
    {
        var manager=UnityEngine.Object.FindFirstObjectByType<PageManager>();manager.ChangeImmediate(UIPageType.MainPage);
        manager.GetPage<RuneLobbyPage>(UIPageType.MainPage).PlayDemo(1);
    }
    public static void CheckMirror()
    {
        var manager=UnityEngine.Object.FindFirstObjectByType<PageManager>();var p=manager.GetPage<RunePlayPage>(UIPageType.PlayPage);
        Check(manager.CurrentPageType==UIPageType.PlayPage&&!manager.IsChanging&&p.LevelIndex==7&&p.CanvasGroup.alpha==1,"Real fade navigation did not finish");Debug.Log("RUNE MECHANIC FADE NAVIGATION PASS");
    }
    public static void PrepareSplash()
    {
        Check(!Application.isPlaying,"Stop Play mode first");UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Splash.unity");
    }
    public static void CheckSplash()
    {
        var manager=UnityEngine.Object.FindFirstObjectByType<PageManager>();Check(manager!=null&&manager.CurrentPageType==UIPageType.MainPage&&manager.PageCount==2,"Splash did not reach lobby");
        var lobby=manager.GetPage<RuneLobbyPage>(UIPageType.MainPage);Check(lobby.hero!=null&&lobby.hero.isActiveAndEnabled&&lobby.showcase==null,"Floating lobby brand missing after boot");
        Debug.Log("RUNE MECHANIC SPLASH PASS: Splash -> Play -> Home; floating title and blocks, two cached pages.");
    }
}
