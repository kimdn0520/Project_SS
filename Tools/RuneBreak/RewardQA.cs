using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using RuneBreak;
public static class RewardQA
{
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static void Check(bool value,string message){if(!value)throw new Exception("REWARD QA: "+message);}
    static async UniTask Ready(){await UniTask.Yield();await UniTask.WaitUntil(()=>!PopupManager.IsChanging).Timeout(TimeSpan.FromSeconds(10));}
    static async UniTask Closed(){await UniTask.WaitUntil(()=>!PopupManager.Instance.IsBlockingInput).Timeout(TimeSpan.FromSeconds(10));await UniTask.Yield();Check(PopupManager.Instance.LiveInstanceCount==0,"popup cleanup");}
    static void Texts(GameObject root){Canvas.ForceUpdateCanvases();foreach(var t in root.GetComponentsInChildren<TMP_Text>()){t.ForceMeshUpdate();Check(!t.isTextOverflowing,"overflow "+t.name+" "+t.text);Check(t.font.name.StartsWith("Fredoka"),"downloaded font");}}
    public static void Execute(){Run().Forget();}
    static async UniTaskVoid Run()
    {
        var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();var p=pages.GetPage<RunePlayPage>(UIPageType.PlayPage);
        bool enabled=p.enabled,audio=RunePreferences.Audio,reduced=RunePreferences.Reduced;string wallet=PlayerPrefs.GetString(RuneCoinReward.SaveKey,"");bool walletExists=PlayerPrefs.HasKey(RuneCoinReward.SaveKey),hasNext=PlayerPrefs.HasKey("RuneBreak.Next"),hasClears=PlayerPrefs.HasKey("RuneBreak.Clears");int next=PlayerPrefs.GetInt("RuneBreak.Next"),clears=PlayerPrefs.GetInt("RuneBreak.Clears");
        try
        {
            RunePreferences.Audio=false;RunePreferences.Reduced=false;p.enabled=false;PopupManager.Clear();pages.ChangeImmediate(UIPageType.PlayPage,0);await UniTask.Yield();p.CanvasGroup.alpha=1;p.LoadLevel(0);
            Check(p.catalog.levels.All(l=>l.seals.N.Length<=2&&l.seals.E.Length<=2),"two-layer catalog");Check(p.art.cabinets[0].name=="cabinet_lavender","lavender board without gate grooves");Capture(p.Canvas,"Reward-Play");
            Check(p.gates[1].rectTransform.anchoredPosition.x+8==RuneFlight.SurfaceEastGateLeft,"east gate face position");Check(p.gatePreviews[p.gatePreviews.Length/2].rectTransform.anchoredPosition.x+36<451,"next layer inside frame");Check(Mathf.Abs(p.gateCounts[1].rectTransform.anchoredPosition.x+17-RuneFlight.SurfaceEastGateCenter)<.01f,"number centered on inset gate");
            for(int side=0;side<2;side++){int gate=p.gates[side].transform.GetSiblingIndex();Check(p.gatePreviews.Skip(side*p.gatePreviews.Length/2).Take(p.gatePreviews.Length/2).All(x=>x.transform.GetSiblingIndex()<gate),"preview stays behind active gate");Check(p.gateCracks[side].transform.GetSiblingIndex()>gate,"cracks visible above gate");Check(p.gateCounts[side].transform.GetSiblingIndex()>p.gateCracks[side].transform.GetSiblingIndex(),"number visible above cracks");}
            // Damage grows on the real gate and persists until that layer is destroyed.
            int hit=Enumerable.Range(0,p.Rules.Level.blocks.Length).First(i=>p.Rules.Apply(p.State,new RuneMove(RuneMoveKind.Board,i),out _,out var shot,out _)&&!shot.store&&!shot.layerBreak);
            p.Tap(hit,false);for(int i=0;i<65;i++)p.AdvancePresentation(1f/60);Check(p.gateCracks.Sum(c=>c.HitCount)==1,"first-hit crack");Capture(p.Canvas,"Reward-Crack");
            // Empty-side flights hit the real outer frame, in every cardinal direction.
            foreach(string dir in new[]{"N","E","S","W"})
            {
                var f=(RuneFlight)p.pool.RentCached("RuneFlights");var b=new RuneBlock{x=2,y=2,color="blue",dir=dir};f.Begin(p.art,b,new RuneShot{store=true,slot=0},0,false,null,true);
                Check(f.TakeReboundImpact(f.ReboundContactTime+.001f,out var point),"wall impact "+dir);Check(dir=="N"?Mathf.Abs(point.y-45)<.01f:dir=="E"?Mathf.Abs(point.x-451)<.01f:dir=="S"?Mathf.Abs(point.y-486)<.01f:Mathf.Abs(point.x-29)<.01f,"actual frame "+dir);
                Check(!f.TakeReboundImpact(f.ReboundContactTime+.1f,out _),"one impact per flight");p.pool.ReturnCached(f);
            }
            p.LoadLevel(0);var route=RuneAutoRules.Solve(p.Rules,p.State);foreach(var move in route.route)
            {
                p.Tap(move.id,false);for(int frame=0;frame<240;frame++){p.AdvancePresentation(1f/120);if(p.State.phase==RunePhase.Won&&p.ActiveFlights==0)break;}
            }
            while(!(PopupManager.CurrentPopup is RuneResultPanel)){p.AdvancePresentation(1f/60);await UniTask.Yield();}await Ready();var result=(RuneResultPanel)PopupManager.CurrentPopup;
            Check(result.title.text=="Clear!"&&result.claimButton.gameObject.activeSelf&&!result.primaryButton.gameObject.activeSelf&&!result.homeButton.gameObject.activeSelf,"claim-first screen");result.AdvancePresentation(1);Check(!result.hero.enabled&&result.coinPile.pieces.Length==8,"layered coin pile");var poses=result.coinPile.pieces.Select(x=>x.rect.anchoredPosition).ToArray();result.AdvancePresentation(1);Check(result.coinPile.pieces.Select(x=>x.rect.anchoredPosition).SequenceEqual(poses)&&result.heroRect.localScale==Vector3.one,"coin pile must settle without idle breathing");Check(result.coinPile.launchPoints.Select(x=>x.position).Distinct().Count()==7,"separate coin launch origins");Texts(result.gameObject);Capture(result.Canvas,"Reward-Clear");
            // A clear overlay blocks board input but leaves existing particle motion running.
            p.effects.Emit(new Vector2(240,74),"gold",true,false,0);int shards=p.effects.ActiveShards,taps=p.State.taps;float time=(float)typeof(RunePlayPage).GetField("clock",Private).GetValue(p);
            p.Tap(0,false);p.AdvancePresentation(.2f);Check(p.State.taps==taps&&(float)typeof(RunePlayPage).GetField("clock",Private).GetValue(p)>time,"background animation/input split");for(int i=0;i<360;i++)p.AdvancePresentation(1f/60);Check(shards>0&&p.effects.ActiveShards==0,"fragments finish behind clear");
            int before=RuneCoinReward.Balance;result.ClaimReward();result.ClaimReward();Check(RuneCoinReward.Balance==before+p.clearRewardCoins,"single credit");Check(!result.primaryButton.gameObject.activeSelf&&!result.homeButton.gameObject.activeSelf,"no navigation before collection");result.ChoosePrimary();result.OnEscape();Check(PopupManager.CurrentPopup==result,"claim cannot be skipped");
            for(int i=0;i<16;i++)result.AdvancePresentation(.04f);Capture(result.Canvas,"Reward-Collecting");
            for(int i=0;i<80;i++)result.AdvancePresentation(.025f);Check(result.RewardReady&&result.primaryButton.gameObject.activeSelf&&result.homeButton.gameObject.activeSelf,"navigation after collection");Check(result.coinInstances.All(c=>!c.gameObject.activeSelf),"coins returned to baked pool");Check(result.balanceLabel.text==RuneCoinReward.Balance.ToString(),"animated balance matches saved balance");Texts(result.gameObject);Capture(result.Canvas,"Reward-NextHome");
            result.ChoosePrimary();await Closed();Check(p.LevelIndex==1,"Next navigation");
            var receipt=new RuneCoinReward(40);int old=RuneCoinReward.Balance;Check(receipt.Claim()&&!receipt.Claim()&&RuneCoinReward.Balance==old+40,"receipt idempotence");
            object action=null;PopupManager.Show(RuneResultPanel.PrefabName,new RuneResultData{won=true,level=2,reward=receipt},x=>action=x);await Ready();result=(RuneResultPanel)PopupManager.CurrentPopup;Check(result.RewardReady&&!result.claimButton.gameObject.activeSelf,"same receipt reopening");result.ChooseHome();await Closed();Check(action is RuneResultAction a&&a==RuneResultAction.Home,"Home callback");
            PopupManager.Show(RuneResultPanel.PrefabName,new RuneResultData{won=false,level=2});await Ready();result=(RuneResultPanel)PopupManager.CurrentPopup;Check(!result.hero.gameObject.activeSelf&&!result.claimButton.gameObject.activeSelf,"failure has no reward");Texts(result.gameObject);Capture(result.Canvas,"Reward-Failure");result.ChoosePrimary();await Closed();
            RunePreferences.Reduced=true;PopupManager.Show(RuneResultPanel.PrefabName,new RuneResultData{won=true,reward=new RuneCoinReward(0)});await Ready();result=(RuneResultPanel)PopupManager.CurrentPopup;result.ClaimReward();for(int i=0;i<40;i++)result.AdvancePresentation(.02f);Check(result.RewardReady&&result.coinInstances.All(c=>!c.gameObject.activeSelf),"reduced coin collection");result.OnEscape();await Closed();RunePreferences.Reduced=false;
            p.LoadLevel(0);p.OpenSettings();await Ready();time=(float)typeof(RunePlayPage).GetField("clock",Private).GetValue(p);p.AdvancePresentation(.2f);Check(Mathf.Approximately(time,(float)typeof(RunePlayPage).GetField("clock",Private).GetValue(p)),"settings still pauses");((RuneSettingsPanel)PopupManager.CurrentPopup).Dismiss();await Closed();
            File.WriteAllText("Tools/RuneBreak/QA/reward-runtime.json","{\"status\":\"passed\",\"twoLayers\":true,\"cracks\":true,\"fourFrameRebounds\":true,\"clearParticlesContinue\":true,\"settingsPauses\":true,\"claimOnce\":true,\"pooledCoinCleanup\":true,\"navigationAfterCollection\":true,\"nextHome\":true,\"noTextOverflow\":true}");Debug.Log("REWARD QA PASS");
        }
        catch(Exception e){File.WriteAllText("Tools/RuneBreak/QA/reward-runtime.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="failed",error=e.ToString()}));Debug.LogException(e);}
        finally{PopupManager.Clear();if(walletExists)PlayerPrefs.SetString(RuneCoinReward.SaveKey,wallet);else PlayerPrefs.DeleteKey(RuneCoinReward.SaveKey);if(hasNext)PlayerPrefs.SetInt("RuneBreak.Next",next);else PlayerPrefs.DeleteKey("RuneBreak.Next");if(hasClears)PlayerPrefs.SetInt("RuneBreak.Clears",clears);else PlayerPrefs.DeleteKey("RuneBreak.Clears");PlayerPrefs.Save();RunePreferences.Audio=audio;RunePreferences.Reduced=reduced;p.enabled=enabled;pages.ChangeImmediate(UIPageType.PlayPage,0);p.LoadLevel(0);}
    }
    public static void Preview(){var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();pages.ChangeImmediate(UIPageType.PlayPage,0);PopupManager.Show(RuneResultPanel.PrefabName,new RuneResultData{won=true,level=1,reward=new RuneCoinReward(40)});}
    public static void Film(){FilmAsync().Forget();}
    static async UniTaskVoid FilmAsync()
    {
        var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();var p=pages.GetPage<RunePlayPage>(UIPageType.PlayPage);bool enabled=p.enabled,audio=RunePreferences.Audio,reduced=RunePreferences.Reduced;
        string wallet=PlayerPrefs.GetString(RuneCoinReward.SaveKey,"");bool walletExists=PlayerPrefs.HasKey(RuneCoinReward.SaveKey),hasNext=PlayerPrefs.HasKey("RuneBreak.Next"),hasClears=PlayerPrefs.HasKey("RuneBreak.Clears");int next=PlayerPrefs.GetInt("RuneBreak.Next"),clears=PlayerPrefs.GetInt("RuneBreak.Clears");
        try
        {
            Directory.CreateDirectory("Temp/RewardMotion");PopupManager.Clear();pages.ChangeImmediate(UIPageType.PlayPage,0);p.CanvasGroup.alpha=1;p.enabled=false;RunePreferences.Audio=false;RunePreferences.Reduced=false;p.LoadLevel(0);int frame=0;
            int id=Enumerable.Range(0,p.Rules.Level.blocks.Length).First(i=>p.Rules.Level.blocks[i].dir=="W"&&p.Rules.IsFree(p.State,i));p.Tap(id,false);
            for(int i=0;i<36;i++){p.AdvancePresentation(1f/30);Capture(p.Canvas,(frame++).ToString("D4"),true);await UniTask.Yield();}
            p.LoadLevel(0);var route=RuneAutoRules.Solve(p.Rules,p.State);foreach(var move in route.route.Take(route.route.Count-1)){p.Tap(move.id,false);for(int i=0;i<300;i++)p.AdvancePresentation(1f/120);}p.Tap(route.route.Last().id,false);int popupFrames=0;
            for(int i=0;i<180;i++)
            {
                p.AdvancePresentation(1f/30);
                if(PopupManager.CurrentPopup is RuneResultPanel result&&!PopupManager.IsChanging){result.enabled=false;if(++popupFrames==48)result.ClaimReward();result.AdvancePresentation(1f/30);}
                Capture(p.Canvas,(frame++).ToString("D4"),true);await UniTask.Yield();
            }
            File.WriteAllText("Temp/RewardMotion/done.txt",frame.ToString());Debug.Log("REWARD FILM READY: "+frame+" frames");
        }
        finally{PopupManager.Clear();if(walletExists)PlayerPrefs.SetString(RuneCoinReward.SaveKey,wallet);else PlayerPrefs.DeleteKey(RuneCoinReward.SaveKey);if(hasNext)PlayerPrefs.SetInt("RuneBreak.Next",next);else PlayerPrefs.DeleteKey("RuneBreak.Next");if(hasClears)PlayerPrefs.SetInt("RuneBreak.Clears",clears);else PlayerPrefs.DeleteKey("RuneBreak.Clears");PlayerPrefs.Save();RunePreferences.Audio=audio;RunePreferences.Reduced=reduced;p.enabled=enabled;pages.ChangeImmediate(UIPageType.PlayPage,0);p.LoadLevel(0);}
    }
    static void Capture(Canvas canvas,string name,bool temporary=false)
    {
        Canvas.ForceUpdateCanvases();var cam=canvas.worldCamera;var prior=cam.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(Screen.width,Screen.height,24);var tex=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
        try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);tex.Apply();File.WriteAllBytes((temporary?"Temp/RewardMotion/":"Tools/RuneBreak/QA/")+name+".png",tex.EncodeToPNG());}
        finally{cam.targetTexture=prior;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
}
