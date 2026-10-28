using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using RuneBreak;
using Cysharp.Threading.Tasks;

public static class SurfaceMotionQA
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Field(object obj,string name)=>obj.GetType().GetField(name,Private).GetValue(obj);
    static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,Private).SetValue(obj,value);
    static void Check(bool ok,string message){if(!ok)throw new Exception("SURFACE MOTION: "+message);}
    static void Tick(RunePlayPage p,float t){for(int i=0,n=Mathf.CeilToInt(t*120);i<n;i++){if(p.State.phase!=RunePhase.Playing)Set(p,"finishAt",float.PositiveInfinity);p.AdvancePresentation(t/n);}}
    static void State(RuneState state,JToken oracle)
    {
        Check(state.board==(uint)oracle["board"]&&state.spent.SequenceEqual(oracle["spent"].ToObject<int[]>())&&state.slots.SequenceEqual(oracle["slots"].ToObject<int[]>()),"HTML automatic reservation mismatch");
        Check(state.phase.ToString().ToLowerInvariant()==((string)oracle["phase"]=="play"?"playing":(string)oracle["phase"]),"HTML phase mismatch");
        Check(state.taps==(int)oracle["taps"]&&state.direct==(int)oracle["direct"]&&state.stored==(int)oracle["stored"]&&state.relaunch==(int)oracle["relaunch"]&&state.peak==(int)oracle["peak"],"HTML action counters");
    }
    public static void Execute()
    {
        Check(Application.isPlaying,"requires Play mode");var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();PopupManager.Clear();pages.ChangeImmediate(UIPageType.PlayPage,0);var p=pages.GetPage<RunePlayPage>(UIPageType.PlayPage);p.CanvasGroup.alpha=1;
        bool audio=RunePreferences.Audio,reduced=RunePreferences.Reduced,hasClears=PlayerPrefs.HasKey("RuneBreak.Clears"),hasNext=PlayerPrefs.HasKey("RuneBreak.Next");int clears=PlayerPrefs.GetInt("RuneBreak.Clears"),next=PlayerPrefs.GetInt("RuneBreak.Next");
        int transitions=0,boards=0,relaunches=0,samples=0;float maxError=0;
        try
        {
            RunePreferences.Audio=false;var data=JObject.Parse(File.ReadAllText("Tools/RuneBreak/QA/surface-motion-oracle.json"));
            foreach(var test in data["cases"])
            {
                var level=JsonUtility.FromJson<RuneLevel>(test["level"].ToString());var rules=new RuneRules(level);var state=rules.Initial();var solution=RuneAutoRules.Solve(rules,state);
                Check(solution.status==RuneSolveStatus.Solved&&solution.route.Select(m=>m.id).SequenceEqual(test["steps"].Select(t=>(int)t["id"])),"HTML auto solver route");
                foreach(var step in test["steps"]){State(state,step["before"]);Check(RuneAutoRules.Step(rules,state,(int)step["id"],out state),"board step rejected");State(state,step["after"]);transitions++;}
            }
            // Compare actual particles against deterministic snapshots from unmodified HTML DepthFX.
            var fx=p.effects;fx.Clear();Set(fx,"depthSeed",1847u);
            foreach(var test in data["particles"])
            {
                fx.Clear();fx.Emit(new Vector2(250,74),"blue",(bool)test["big"],(bool)test["reduced"],(int)test["side"]);int frame=0;
                foreach(var snapshot in test["frames"])
                {
                    int target=Mathf.RoundToInt((float)snapshot["time"]*60);while(frame<target){fx.Tick(1f/60);frame++;}
                    var particles=(Array)Field(fx,"depthShards");Check(fx.ActiveShards==snapshot["pieces"].Count(),"particle count");int i=0;
                    foreach(var expected in snapshot["pieces"])
                    {
                        var value=particles.GetValue(i++);var type=value.GetType();
                        foreach(var pair in new[]{new[]{"position","x","y","z"},new[]{"velocity","vx","vy","vz"},new[]{"rotation","rx","ry","rz"},new[]{"spin","wx","wy","wz"},new[]{"size","sx","sy","sz"}})
                        {
                            var v=(Vector3)type.GetField(pair[0]).GetValue(value);for(int ch=0;ch<3;ch++){float error=Mathf.Abs(v[ch]-(float)expected[pair[ch+1]]);maxError=Mathf.Max(maxError,error);Check(error<.004f,"particle motion "+pair[ch+1]+" "+error);samples++;}
                        }
                        Check((int)type.GetField("bounces").GetValue(value)==(int)expected["bounces"]&&(int)type.GetField("mesh").GetValue(value)==(int)expected["mesh"],"bounce or mesh mismatch");
                    }
                }
            }
            for(int i=0;i<20;i++)fx.Emit(new Vector2(250,74),"blue",true,false,0);Check(fx.ActiveShards==190,"bounded fragments");
            using(var vh=new VertexHelper()){typeof(RuneFxGraphic).GetMethod("OnPopulateMesh",Private,null,new[]{typeof(VertexHelper)},null).Invoke(fx,new object[]{vh});Check(vh.currentVertCount<65535,"mesh vertex limit");}
            Tick(p,2);Check(fx.ActiveShards==0,"fragment cleanup");
            foreach(bool motionReduced in new[]{false,true})
            {
                RunePreferences.Reduced=motionReduced;
                for(int level=0;level<p.catalog.levels.Length;level++)
                {
                    p.LoadLevel(level);int objects=p.GetComponentsInChildren<Transform>(true).Length;var solution=RuneAutoRules.Solve(p.Rules,p.State);Check(solution.status==RuneSolveStatus.Solved,"live auto solver "+level);
                    foreach(var move in solution.route)
                    {
                        Check(move.kind==RuneMoveKind.Board,"manual storage input in route");p.Tap(move.id,false);boards++;Tick(p,2.5f);
                    }
                    Tick(p,2);Check(p.State.phase==RunePhase.Won&&p.ActiveFlights==0,"automatic level completion "+level);relaunches+=p.State.relaunch;
                    Check(p.pool.GetComponentsInChildren<RuneFlight>(true).Length==48&&p.GetComponentsInChildren<Transform>(true).Length==objects,"runtime hierarchy grew");
                }
            }
            // Ready entries must wait for both arrival and the displayed gate, then launch left to right.
            p.LoadLevel(0);var rule=p.Rules;var ready=rule.Initial();ready.board=0;
            var blue=Enumerable.Range(0,rule.Level.blocks.Length).Where(i=>rule.Level.blocks[i].color=="blue").Take(2).ToArray();Check(blue.Length==2,"test fixture");ready.slots[0]=blue[0];ready.slots[1]=blue[1];ready.spent[1]=rule.Level.seals.E[0].hp;
            var visible=rule.Initial();var pending=new bool[28];var eligibility=Enumerable.Repeat(float.PositiveInfinity,28).ToArray();eligibility[blue[0]]=eligibility[blue[1]]=1;
            Check(RuneAutoRules.ReadySlot(rule,ready,visible,pending,eligibility,2)==-1,"unexposed visual layer fired");visible.spent[1]=ready.spent[1];Check(RuneAutoRules.ReadySlot(rule,ready,visible,pending,eligibility,.9f)==-1,"landing delay skipped");pending[blue[0]]=true;
            Check(RuneAutoRules.ReadySlot(rule,ready,visible,pending,eligibility,2)==1,"pending flight fired");pending[blue[0]]=false;Check(RuneAutoRules.ReadySlot(rule,ready,visible,pending,eligibility,2)==0,"left to right priority");
            // Inject the same ready scenario into the authored page and inspect cadence and reservation.
            typeof(RunePlayPage).GetProperty("State").SetValue(p,ready);Set(p,"visual",visible);Set(p,"clock",2f);var liveEligibility=(float[])Field(p,"eligibleAt");Array.Copy(eligibility,liveEligibility,28);Set(p,"nextAutoAt",0f);
            p.AdvancePresentation(0);Check(p.State.relaunch==1&&p.State.slots[0]==-1&&p.State.slots[1]==blue[1],"automatic reservation");Tick(p,.08f);Check(p.State.relaunch==1,"automatic cadence too fast");Tick(p,.01f);Check(p.State.relaunch==2,"second automatic launch");
            p.LoadLevel(0);Tick(p,1);Check(p.State.taps==0&&p.ActiveFlights==0,"reset retained automatic queue");
            File.WriteAllText("Tools/RuneBreak/QA/surface-motion.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="passed",htmlTransitions=transitions,particleValues=samples,maxParticleError=maxError,levelRuns=p.catalog.levels.Length*2,manualBoardInputs=boards,automaticLaunches=relaunches,boundedFragments=190,pool=48}));
            Debug.Log("SURFACE MOTION PASS: "+transitions+" HTML transitions, "+samples+" particle values, "+boards+" board inputs, "+relaunches+" automatic launches.");
        }
        finally{RunePreferences.Audio=audio;RunePreferences.Reduced=reduced;if(hasClears)PlayerPrefs.SetInt("RuneBreak.Clears",clears);else PlayerPrefs.DeleteKey("RuneBreak.Clears");if(hasNext)PlayerPrefs.SetInt("RuneBreak.Next",next);else PlayerPrefs.DeleteKey("RuneBreak.Next");PlayerPrefs.Save();p.LoadLevel(0);}
    }
    public static void Film()
    {
        var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();PopupManager.Clear();pages.ChangeImmediate(UIPageType.PlayPage,0);var p=pages.GetPage<RunePlayPage>(UIPageType.PlayPage);p.CanvasGroup.alpha=1;p.LoadLevel(0);bool audio=RunePreferences.Audio,reduced=RunePreferences.Reduced;RunePreferences.Audio=false;RunePreferences.Reduced=false;
        Directory.CreateDirectory("Temp/SurfaceMotionFilm");
        var camera=p.Canvas.worldCamera;var prior=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(540,960,24);var tex=new Texture2D(540,960,TextureFormat.RGB24,false);bool capturedAuto=false,capturedBreak=false;
        try
        {
            camera.targetTexture=rt;
            for(int frame=0;frame<135;frame++)
            {
                if(frame==5)p.Tap(2,false);if(frame==11)p.Tap(1,false);if(frame==46)p.Tap(3,false);if(frame==52)p.Tap(0,false);
                p.AdvancePresentation(1f/30);Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,540,960),0,0);tex.Apply();var bytes=tex.EncodeToPNG();File.WriteAllBytes("Temp/SurfaceMotionFilm/"+frame.ToString("000")+".png",bytes);
                if(!capturedAuto&&p.State.relaunch>0&&frame>66){File.WriteAllBytes("Tools/RuneBreak/QA/Surface-Automatic-Launch.png",bytes);capturedAuto=true;}
                if(!capturedBreak&&p.effects.ActiveShards>=42){File.WriteAllBytes("Tools/RuneBreak/QA/Surface-Solid-Fragments.png",bytes);capturedBreak=true;}
            }
            Check(capturedAuto&&capturedBreak&&p.State.relaunch==2,"film did not exercise automatic storage and fragments");
        }
        finally{camera.targetTexture=prior;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);RunePreferences.Audio=audio;RunePreferences.Reduced=reduced;p.LoadLevel(0);}
        Debug.Log("SURFACE FILM PASS: two stored blue blocks launched automatically after red gate break.");
    }
    public static void Scheduling(){SchedulingAsync().Forget();}
    static async UniTaskVoid SchedulingAsync()
    {
        var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();PopupManager.Clear();pages.ChangeImmediate(UIPageType.PlayPage,0);var p=pages.GetPage<RunePlayPage>(UIPageType.PlayPage);bool audio=RunePreferences.Audio,reduced=RunePreferences.Reduced;bool enabled=p.enabled;p.enabled=false;RunePreferences.Audio=false;
        try
        {
            foreach(bool motionReduced in new[]{false,true})
            {
                RunePreferences.Reduced=motionReduced;p.LoadLevel(0);
                // Store gold while its matching gate is already visible. It still must land first.
                p.State.spent[0]=2;((RuneState)Field(p,"visual")).spent[0]=2;p.Tap(6,false);
                var flight=p.pool.GetComponentsInChildren<RuneFlight>(true).Single(f=>f.gameObject.activeSelf);p.AdvancePresentation(flight.duration-.001f);Check(p.State.relaunch==0,"launched before landing");p.AdvancePresentation(.0011f);Check(p.State.relaunch==0,"landing delay missing");
                Tick(p,motionReduced?.060f:.115f);Check(p.State.relaunch==0,"landing settle too short");
                p.OpenSettings();await UniTask.WaitUntil(()=>!PopupManager.IsChanging).Timeout(TimeSpan.FromSeconds(5));float time=(float)Field(p,"clock");p.AdvancePresentation(1);p.Tap(7,false);Check((float)Field(p,"clock")==time&&p.State.relaunch==0&&p.State.taps==1,"settings did not pause automatic/input");
                ((RuneSettingsPanel)PopupManager.CurrentPopup).Dismiss();await UniTask.WaitUntil(()=>!p.IsModalBlocked).Timeout(TimeSpan.FromSeconds(5));Tick(p,.01f);Check(p.State.relaunch==1,"automatic launch failed to resume: reduced="+motionReduced+" blocked="+p.IsModalBlocked+" time="+Field(p,"clock")+" eligible="+((float[])Field(p,"eligibleAt"))[6]+" pending="+((bool[])Field(p,"pending"))[6]+" slots="+string.Join(",",p.State.slots));
                // Rapid board taps overlap pending storage and gate changes without double hits.
                p.LoadLevel(0);foreach(int id in new[]{2,1,3,0}){p.Tap(id,false);Tick(p,.018f);}Tick(p,2);
                Check(p.State.relaunch==2&&p.State.spent[1]==4&&p.State.Filled==0,"overlapping arrivals lost or duplicated reservations");
                // Opening an already queued automatic slot before a board tap frees capacity first.
                p.LoadLevel(0);var s=p.State;s.board&=~(1u<<6);s.slots[0]=6;s.spent[0]=2;((RuneState)Field(p,"visual")).spent[0]=2;((float[])Field(p,"eligibleAt"))[6]=0;p.Tap(2,false);
                Check(p.State.relaunch==1&&p.State.slots[0]==2,"automatic priority before board tap");
            }
            File.WriteAllText("Tools/RuneBreak/QA/surface-scheduling.json","{\"status\":\"passed\",\"landingDelay\":[0.12,0.065],\"settingsPause\":true,\"rapidTaps\":true,\"automaticPriority\":true}");Debug.Log("SURFACE SCHEDULING PASS: landing, settings pause, overlapping gate arrivals, auto priority.");
        }
        catch(Exception e){Debug.LogException(e);File.WriteAllText("Tools/RuneBreak/QA/surface-scheduling.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="failed",error=e.ToString()}));}
        finally{PopupManager.Clear();RunePreferences.Audio=audio;RunePreferences.Reduced=reduced;p.LoadLevel(0);p.enabled=enabled;}
    }
}
