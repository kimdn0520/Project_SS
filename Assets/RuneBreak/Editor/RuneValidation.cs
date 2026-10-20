using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RuneBreak.Editor
{
    public static class RuneValidation
    {
        [Serializable] public class Oracle {public Report[] reports;}
        [Serializable] public class Report {public string id,capacity4;public Trace[] traces;}
        [Serializable] public class Trace {public ActionData action;public StateData state;}
        [Serializable] public class ActionData {public string type,id;}
        [Serializable] public class Spent {public int N,E;}
        [Serializable] public class StateData {public string[] board,slots;public Spent spent;public string phase;public int taps,direct,stored,relaunch,peak;}
        [Serializable] public class ValidationReport {public int oracleTransitions,solvedLevels,scenarios;public string[] details;}
        private static void Require(bool condition,string message){if(!condition)throw new Exception("RUNE VALIDATION: "+message);}
        [MenuItem("Rune Break/Validate rules against HTML")]
        public static void Rules()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<RuneLevelCatalog>("Assets/RuneBreak/Data/RuneLevels.asset");
            var oracle=JsonUtility.FromJson<Oracle>(File.ReadAllText("Assets/RuneBreak/Data/oracle.json"));
            int transitions=0,scenarios=0;var details=new List<string>();
            for(int li=0;li<catalog.levels.Length;li++)
            {
                var level=catalog.levels[li];var rules=new RuneRules(level);var state=rules.Initial();var report=oracle.reports[li];
                foreach(var trace in report.traces)
                {
                    int id=Array.FindIndex(level.blocks,b=>b.id==trace.action.id);var move=new RuneMove(trace.action.type=="slot"?RuneMoveKind.Slot:RuneMoveKind.Board,id);
                    Require(rules.Apply(state,move,out var next,out _,out _),$"L{li+1}: oracle action rejected");state=next;Compare(level,state,trace.state,$"L{li+1}/move {state.taps}");transitions++;
                }
                Require(state.phase==RunePhase.Won,"Oracle did not win");
                var answer=RuneSolver.Solve(rules,rules.Initial());Require(answer.status==RuneSolveStatus.Solved,$"L{li+1}: solver {answer.status} ({answer.nodes} nodes)");
                state=rules.Initial();foreach(var move in answer.route){Require(rules.Apply(state,move,out var next,out _,out _),"Solver emitted invalid action");state=next;}Require(state.phase==RunePhase.Won,"Solver replay did not win");
                var smaller=RuneSolver.Solve(rules,rules.Initial(),4);Require(smaller.status==(report.capacity4=="dead"?RuneSolveStatus.Dead:RuneSolveStatus.Solved),$"L{li+1}: capacity-4 differs: {smaller.status}");
                details.Add($"Level {li+1}: {report.traces.Length} original transitions match; solver {answer.route.Count} moves/{answer.nodes} nodes; capacity4={smaller.status}.");
                // Exercise both successful and rejected inputs, including WAIT and full-pouch overflow.
                state=rules.Initial();var random=new System.Random(310+li);
                for(int k=0;k<600;k++)
                {
                    if(state.phase!=RunePhase.Playing||random.Next(12)==0)state=rules.Initial();
                    var before=state;var snapshot=before.Copy();int id=random.Next(level.blocks.Length);bool slot=random.Next(3)==0;var action=new RuneMove(slot?RuneMoveKind.Slot:RuneMoveKind.Board,id);
                    bool ok=rules.Apply(before,action,out var after,out var shot,out var reason);
                    Require(before.board==snapshot.board&&before.spent.SequenceEqual(snapshot.spent)&&before.slots.SequenceEqual(snapshot.slots),"Apply mutated historical snapshot");
                    if(!ok){Require(ReferenceEquals(before,after),"Rejected move replaced state");scenarios++;continue;}
                    Require(after.taps==before.taps+1,"Tap accounting");
                    if(shot.overflow)Require(before.Filled==5&&after.phase==RunePhase.Lost,"Overflow boundary");
                    if(shot.store)Require(after.Filled<=5&&after.phase==RunePhase.Playing,"Fifth slot must be safe");
                    if(shot.side>=0)Require(after.spent[shot.side]==before.spent[shot.side]+1,"Damage reservation");
                    state=after;scenarios++;
                }
            }
            var output=new ValidationReport {oracleTransitions=transitions,solvedLevels=catalog.levels.Length,scenarios=scenarios,details=details.ToArray()};Directory.CreateDirectory("Tools/RuneBreak/QA");File.WriteAllText("Tools/RuneBreak/QA/rules-report.json",JsonUtility.ToJson(output,true));Debug.Log("RUNE RULES PASS: "+JsonUtility.ToJson(output));
        }
        private static void Compare(RuneLevel level,RuneState state,StateData expected,string label)
        {
            uint mask=0;foreach(string id in expected.board)mask|=1u<<Array.FindIndex(level.blocks,b=>b.id==id);Require(mask==state.board,label+" board");
            for(int i=0;i<5;i++){string id=expected.slots[i];int wanted=string.IsNullOrEmpty(id)?-1:Array.FindIndex(level.blocks,b=>b.id==id);Require(state.slots[i]==wanted,label+" slot "+i);}
            Require(state.spent[0]==expected.spent.N&&state.spent[1]==expected.spent.E,label+" seal reservation");
            Require((state.phase==RunePhase.Won?"won":state.phase==RunePhase.Lost?"lost":"play")==expected.phase,label+" phase");
            Require(state.taps==expected.taps&&state.direct==expected.direct&&state.stored==expected.stored&&state.relaunch==expected.relaunch&&state.peak==expected.peak,label+" counters");
        }
        public static void Prefabs()
        {
            foreach(string name in new[]{"MainPage","PlayPage"})
            {
                var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/"+name+".prefab");Require(root!=null,name+" missing");
                foreach(var tr in root.GetComponentsInChildren<Transform>(true))Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject)==0,name+" missing script at "+tr.name);
                var page=root.GetComponent<SceneBase>();Require(page.Canvas!=null&&page.CanvasGroup!=null,name+" missing cached canvas");
            }
            var play=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/PlayPage.prefab").GetComponent<RunePlayPage>();
            Require(play.blocks.Length==28&&play.slots.Length==5,"Board or storage count");Require(play.GetComponentsInChildren<RuneFlight>(true).Length==48,"Pool count");Require(play.sounds.All(c=>c!=null),"Missing audio");
            Require(play.art.gates.All(g=>g.sprite!=null),"Missing gate sprite");
            var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();Require(scenes.SequenceEqual(new[]{"Assets/Scenes/Splash.unity","Assets/Scenes/Play.unity"}),"Build scene order");
            Debug.Log("RUNE PREFABS PASS: all cached references, 48 flights, 5 recast slots, eight sound clips, Splash -> Play.");
        }
        public static void RuntimeStart()
        {
            Require(Application.isPlaying,"Enter Play mode first");var manager=UnityEngine.Object.FindFirstObjectByType<PageManager>();Require(manager!=null,"PageManager missing");Require(manager.PageCount==2,"Two pages required");manager.ChangeImmediate(UIPageType.PlayPage,5);
        }
        public static void RuntimeVolley()
        {
            var p=UnityEngine.Object.FindFirstObjectByType<RunePlayPage>();Require(p!=null,"PlayPage missing");p.LoadLevel(5);int children=p.GetComponentsInChildren<Transform>(true).Length;
            var solution=RuneSolver.Solve(p.Rules,p.State);int fired=0;
            foreach(var move in solution.route){if(move.kind==RuneMoveKind.Slot)break;p.Tap(move.id,false);fired++;}
            Require(p.ActiveFlights==fired,"All quick taps must reserve a flight");Require(p.State.taps==fired,"All quick taps must reserve rules");p.Undo();Require(p.ActiveFlights==0,"Undo must cancel pooled flights");Require(p.State.taps==fired-1,"Undo must restore snapshot");
            Require(p.GetComponentsInChildren<Transform>(true).Length==children,"Runtime created hierarchy objects");p.Restart();Debug.Log("RUNE RUNTIME VOLLEY PASS: "+fired+" rapid launches, undo cancellation, hierarchy stable.");
        }
        public static void RuntimePresentationReset()
        {
            Require(Application.isPlaying,"Enter Play mode first");
            var manager=UnityEngine.Object.FindFirstObjectByType<PageManager>();manager.ChangeImmediate(UIPageType.PlayPage,0);
            var p=manager.GetPage<RunePlayPage>(UIPageType.PlayPage);
            bool reduced=RunePreferences.Reduced;
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var timer=typeof(RunePlayPage).GetField("clock",flags);
            var update=typeof(RunePlayPage).GetMethod("Update",flags);
            var baked=p.GetComponentsInChildren<RuneFlight>(true);
            int children=p.GetComponentsInChildren<Transform>(true).Length;
            void Tick()=>update.Invoke(p,null);
            void StartBlockedFeedback()
            {
                int blocked=Enumerable.Range(0,p.Rules.Level.blocks.Length).Last(i=>!p.Rules.IsFree(p.State,i));
                // Simulate a blocked tap late in the old level, then advance into its visible feedback.
                timer.SetValue(p,12f);p.Tap(blocked,false);timer.SetValue(p,12.05f);Tick();
                Require(p.blockRects[blocked].localScale!=Vector3.one,"Blocked feedback did not start");
                p.Press(blocked,false,true);p.Hover(blocked,false);
            }
            void RequireRestingBoard(string context)
            {
                Require(p.ActiveFlights==0&&baked.All(f=>!f.gameObject.activeSelf),context+": flight leaked");
                Require(!p.highlight.enabled&&!p.pathImage.enabled,context+": input highlight leaked");
                Require(p.gateFlashes.All(g=>g.color.a==0),context+": impact flash leaked");
                for(int i=0;i<p.Rules.Level.blocks.Length;i++)
                {
                    var center=RuneFlight.Center(p.Rules.Level.blocks[i]);var rect=p.blockRects[i];
                    Require(Vector2.Distance(rect.anchoredPosition,new Vector2(center.x,-center.y-2))<.001f,context+": rune position "+i);
                    Require(Vector3.Distance(rect.localScale,Vector3.one)<.001f,context+": rune scale "+i);
                }
            }
            try
            {
                RunePreferences.Reduced=false;PopupManager.Clear();
                for(int level=0;level<p.catalog.CampaignCount;level++)
                {
                    p.LoadLevel(level);StartBlockedFeedback();p.Next();
                    RequireRestingBoard("Next from level "+(level+1));
                    for(int frame=0;frame<10;frame++){Tick();RequireRestingBoard("Next first frames");}
                }
                p.LoadLevel(0);StartBlockedFeedback();p.Restart();RequireRestingBoard("Restart");Tick();RequireRestingBoard("Restart first frame");
                StartBlockedFeedback();p.Tap(Enumerable.Range(0,p.Rules.Level.blocks.Length).First(i=>p.Rules.IsFree(p.State,i)),false);
                Require(p.ActiveFlights==1,"Expected a flight before Undo");p.Undo();RequireRestingBoard("Undo");Tick();RequireRestingBoard("Undo first frame");
                StartBlockedFeedback();manager.ChangeImmediate(UIPageType.MainPage);manager.ChangeImmediate(UIPageType.PlayPage,0);
                RequireRestingBoard("Page reentry");Tick();RequireRestingBoard("Page reentry first frame");
                var reused=new HashSet<RuneFlight>();
                for(int i=0;i<baked.Length+2;i++)
                {
                    p.LoadLevel(0);p.Tap(Enumerable.Range(0,p.Rules.Level.blocks.Length).First(id=>p.Rules.IsFree(p.State,id)),false);
                    Require(p.ActiveFlights==1,"Expected a rented flight");var flight=baked.Single(f=>f.gameObject.activeSelf);reused.Add(flight);
                    p.Next();RequireRestingBoard("Level change during flight");
                }
                Require(reused.Count==baked.Length,"Pool did not reuse all baked flights");
                Require(p.GetComponentsInChildren<Transform>(true).Length==children,"Reset created hierarchy objects");
                Debug.Log("RUNE PRESENTATION RESET PASS: six level transitions, first-frame transforms, restart/undo/page reentry, 50 interrupted launches, all 48 baked flights reused, stable hierarchy.");
            }
            finally{RunePreferences.Reduced=reduced;p.LoadLevel(0);}
        }
        public static void RuntimeAutoSolve()
        {
            var p=UnityEngine.Object.FindFirstObjectByType<RunePlayPage>();Require(p!=null,"PlayPage missing");
            EditorApplication.update-=Step;EditorApplication.update+=Step;
        }
        private static double nextAction;
        private static void Step()
        {
            if(!Application.isPlaying){EditorApplication.update-=Step;return;}
            if(EditorApplication.timeSinceStartup<nextAction)return;
            nextAction=EditorApplication.timeSinceStartup+.06;
            var p=UnityEngine.Object.FindFirstObjectByType<RunePlayPage>();if(p==null)return;
            if(p.State.phase==RunePhase.Won&&p.ActiveFlights==0){EditorApplication.update-=Step;Debug.Log("RUNE LIVE SOLVE PASS: Level "+(p.LevelIndex+1)+", taps "+p.State.taps+", peak "+p.State.peak);return;}
            if(p.State.phase!=RunePhase.Playing)return;var solution=RuneSolver.Solve(p.Rules,p.State);Require(solution.status==RuneSolveStatus.Solved,"Live solver dead");var move=solution.route[0];p.Tap(move.kind==RuneMoveKind.Slot?Array.IndexOf(p.State.slots,move.id):move.id,move.kind==RuneMoveKind.Slot);
        }
    }
}
