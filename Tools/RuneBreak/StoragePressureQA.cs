using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RuneBreak;

public static class StoragePressureQA
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Field(object o,string name)=>o.GetType().GetField(name,Private).GetValue(o);
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,Private).SetValue(o,value);
    static void Check(bool ok,string message){if(!ok)throw new Exception("STORAGE PRESSURE: "+message);}
    static void Tick(RunePlayPage p,float time,float dt=1f/60)
    {
        while(time>0){float step=Mathf.Min(time,dt);Set(p,"finishAt",float.PositiveInfinity);p.AdvancePresentation(step);time-=step;ValidatePresentation(p);}
    }
    static void ValidatePresentation(RunePlayPage p)
    {
        var flights=(List<RuneFlight>)Field(p,"flights");var visible=(RuneState)Field(p,"storageView");float clock=(float)Field(p,"clock");
        Check(flights.All(f=>!float.IsInfinity(f.start)&&!float.IsNaN(f.start)),"finite departure times");
        Check(p.State.Filled<=5,"logical storage capacity");
        for(int slot=0;slot<5;slot++)
        {
            int id=visible.slots[slot];Check(id<0||flights.All(f=>!f.shot.relaunch||f.shot.id!=id||clock<f.start),"departed block duplicated in slot");
            foreach(var f in flights.Where(f=>f.shot.relaunch&&f.shot.slot==slot))
            {
                Check(f.gameObject.activeSelf==(clock>=f.start),"future departure rendering");
                var incoming=flights.FirstOrDefault(x=>x.shot.store&&x.shot.id==f.shot.id);
                if(incoming!=null)Check(f.start>=incoming.start+incoming.duration+(RunePreferences.Reduced?.065f:.12f)-.0001f,"launch before its arrival");
            }
        }
        foreach(var group in flights.Where(f=>f.shot.store).GroupBy(f=>f.shot.slot))
        {
            var arrivals=group.OrderBy(f=>f.start+f.duration).ToArray();
            for(int i=1;i<arrivals.Length;i++)
            {
                var departure=flights.FirstOrDefault(f=>f.shot.relaunch&&f.shot.id==arrivals[i-1].shot.id);
                if(departure!=null)Check(arrivals[i].start+arrivals[i].duration>=departure.start+.0349f,"two arrivals share occupied slot");
            }
        }
    }
    static void Fixture(RunePlayPage p){p.LoadLevel(4);foreach(int id in new[]{15,14,13,12}){p.Tap(id,false);Tick(p,1.5f);}Check(p.State.Filled==4&&p.State.spent[0]==0,"reference screenshot fixture");}
    public static void Execute(){Run().Forget();}
    public static void Film(){FilmAsync().Forget();}
    static async UniTaskVoid FilmAsync()
    {
        var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();var p=pages.GetPage<RunePlayPage>(UIPageType.PlayPage);bool enabled=p.enabled,audio=RunePreferences.Audio,reduced=RunePreferences.Reduced;int oldLevel=p.LevelIndex;
        var camera=p.Canvas.worldCamera;var prior=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(540,960,24);var tex=new Texture2D(540,960,TextureFormat.RGB24,false);
        try
        {
            Directory.CreateDirectory("Temp/StoragePressureFilm");PopupManager.Clear();pages.ChangeImmediate(UIPageType.PlayPage,4);p.CanvasGroup.alpha=1;p.enabled=false;RunePreferences.Audio=false;RunePreferences.Reduced=false;Fixture(p);camera.targetTexture=rt;
            for(int frame=0;frame<180;frame++)
            {
                if(frame>=12&&frame<=18&&frame%2==0)p.Tap(11-(frame-12)/2,false);
                Tick(p,1f/30);Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,540,960),0,0);tex.Apply();var bytes=tex.EncodeToPNG();File.WriteAllBytes("Temp/StoragePressureFilm/"+frame.ToString("D4")+".png",bytes);
                if(frame==0||frame==179)File.WriteAllBytes("Tools/RuneBreak/QA/StoragePressure-"+(frame==0?"Before":"After")+".png",bytes);await UniTask.Yield();
            }
            Check(p.State.phase==RunePhase.Playing&&p.State.Filled==0&&p.State.spent[0]==8,"film failed to drain storage");File.WriteAllText("Temp/StoragePressureFilm/done.txt","180");Debug.Log("STORAGE PRESSURE FILM READY");
        }
        finally{camera.targetTexture=prior;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);RunePreferences.Audio=audio;RunePreferences.Reduced=reduced;p.LoadLevel(oldLevel);p.enabled=enabled;}
    }
    static async UniTaskVoid Run()
    {
        var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();var p=pages.GetPage<RunePlayPage>(UIPageType.PlayPage);
        bool enabled=p.enabled,audio=RunePreferences.Audio,reduced=RunePreferences.Reduced;int oldLevel=p.LevelIndex;
        var snapshots=new List<object>();
        try
        {
            PopupManager.Clear();pages.ChangeImmediate(UIPageType.PlayPage,4);p.CanvasGroup.alpha=1;p.enabled=false;RunePreferences.Audio=false;
            foreach(bool smallMotion in new[]{false,true})foreach(float interval in new[]{0f,.018f,.07f,.25f})foreach(float dt in new[]{1f/120,1f/30})
            {
                RunePreferences.Reduced=smallMotion;Fixture(p);var oracle=p.State.Copy();
                foreach(int id in new[]{11,10,9,8})
                {
                    Check(RuneAutoRules.Step(p.Rules,oracle,id,out oracle),"HTML automatic move");
                    p.Tap(id,false);Check(p.State.phase==RunePhase.Playing,"rapid green tap caused loss");int taps=p.State.taps;p.Tap(id,false);Check(p.State.taps==taps,"duplicate tap accepted");Tick(p,interval,dt);
                }
                Tick(p,7,dt);Check(p.State.board==oracle.board&&p.State.slots.SequenceEqual(oracle.slots)&&p.State.spent.SequenceEqual(oracle.spent),"settled state differs from HTML automatic rules");
                Check(p.State.spent[0]==8&&p.State.Filled==0&&p.ActiveFlights==0,"green and gold cascade did not finish");
                snapshots.Add(new{reduced=smallMotion,interval,dt,phase=p.State.phase.ToString(),relaunch=p.State.relaunch});
            }
            // Capacity remains meaningful: no matching stored color means a genuine overflow still loses.
            p.LoadLevel(4);var dead=p.State.Copy();dead.slots=new[]{12,13,14,15,4};foreach(int id in dead.slots)dead.board&=~(1u<<id);typeof(RunePlayPage).GetProperty("State").SetValue(p,dead);Set(p,"visual",dead.Copy());
            Check(RuneAutoRules.ReadySlot(p.Rules,p.State)<0,"dead fixture unexpectedly ready");p.Tap(11,false);Check(p.State.phase==RunePhase.Lost,"genuine overflow was ignored");
            // Pause must stop scheduled departures, and reset must return even inactive reserved flights.
            RunePreferences.Reduced=false;Fixture(p);foreach(int id in new[]{11,10,9,8})p.Tap(id,false);p.OpenSettings();await UniTask.WaitUntil(()=>!PopupManager.IsChanging).Timeout(TimeSpan.FromSeconds(5));float before=(float)Field(p,"clock");p.AdvancePresentation(2);Check((float)Field(p,"clock")==before,"settings advanced queued departure");
            ((RuneSettingsPanel)PopupManager.CurrentPopup).Dismiss();await UniTask.WaitUntil(()=>!p.IsModalBlocked).Timeout(TimeSpan.FromSeconds(5));Tick(p,7);Check(p.State.Filled==0,"queue failed after settings");
            Fixture(p);foreach(int id in new[]{11,10,9,8})p.Tap(id,false);p.LoadLevel(4);Check(p.ActiveFlights==0&&p.State.taps==0,"reset retained reservations");var rented=new List<PoolObject>();for(int i=0;i<48;i++){var item=p.pool.RentCached("RuneFlights");Check(item!=null,"pooled flight leak");rented.Add(item);}foreach(var item in rented)p.pool.ReturnCached(item);
            File.WriteAllText("Tools/RuneBreak/QA/storage-pressure.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="passed",cases=snapshots.Count,results=snapshots,htmlSettledState=true,trueOverflowStillLoses=true,settingsPause=true,poolCleanup=true}));Debug.Log("STORAGE PRESSURE PASS: "+snapshots.Count+" rapid-tap cases, HTML state, real overflow, pause and pool cleanup.");
        }
        catch(Exception e){File.WriteAllText("Tools/RuneBreak/QA/storage-pressure.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="failed",error=e.ToString()}));Debug.LogException(e);}
        finally{PopupManager.Clear();RunePreferences.Audio=audio;RunePreferences.Reduced=reduced;p.LoadLevel(oldLevel);p.enabled=enabled;}
    }
}
