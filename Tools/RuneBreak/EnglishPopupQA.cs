using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RuneBreak;

public static class EnglishPopupQA
{
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static void Check(bool ok,string message){if(!ok)throw new Exception("ENGLISH POPUP: "+message);}
    static void Tick(RunePlayPage p,float duration){int n=Mathf.CeilToInt(duration*120);for(int i=0;i<n;i++)p.AdvancePresentation(duration/n);}
    static async UniTask Ready(){await UniTask.Yield();await UniTask.WaitUntil(()=>!PopupManager.IsChanging).Timeout(TimeSpan.FromSeconds(10));}
    static async UniTask Closed(){await UniTask.WaitUntil(()=>!PopupManager.Instance.IsBlockingInput).Timeout(TimeSpan.FromSeconds(10));await UniTask.Yield();Check(PopupManager.Instance.LiveInstanceCount==0,"popup instance was not destroyed");}
    static void Claim(RuneResultPanel p){p.ClaimReward();for(int i=0;i<150;i++)p.AdvancePresentation(.02f);}
    static void Texts(GameObject root,bool layout=true)
    {
        Canvas.ForceUpdateCanvases();foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))
        {
            Check(!t.text.Any(c=>c>=0xAC00&&c<=0xD7A3),"non-English label: "+t.name+" "+t.text);Check(t.font!=null&&t.font.name.StartsWith("Fredoka"),"wrong font: "+t.name);
            if(!layout||!t.isActiveAndEnabled||string.IsNullOrEmpty(t.text))continue;t.ForceMeshUpdate();Check(!t.isTextOverflowing,"text overflow: "+t.name+" "+t.text);
            foreach(var c in t.textInfo.characterInfo.Take(t.textInfo.characterCount))if(c.isVisible)Check(c.textElement!=null,"missing glyph");
        }
    }
    public static void Execute(){Run().Forget();}
    static async UniTaskVoid Run()
    {
        var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();var p=pages.GetPage<RunePlayPage>(UIPageType.PlayPage);var lobby=pages.GetPage<RuneLobbyPage>(UIPageType.MainPage);
        bool hasWallet=PlayerPrefs.HasKey(RuneCoinReward.SaveKey);string wallet=PlayerPrefs.GetString(RuneCoinReward.SaveKey,"");
        bool enabled=p.enabled,audio=RunePreferences.Audio,reduced=RunePreferences.Reduced,hasNext=PlayerPrefs.HasKey("RuneBreak.Next"),hasClears=PlayerPrefs.HasKey("RuneBreak.Clears");int next=PlayerPrefs.GetInt("RuneBreak.Next"),clears=PlayerPrefs.GetInt("RuneBreak.Clears");
        try
        {
            RunePreferences.Audio=false;RunePreferences.Reduced=false;p.enabled=false;PopupManager.Clear();
            foreach(string path in new[]{"Assets/Resources/Prefabs/MainPage.prefab","Assets/Resources/Prefabs/PlayPage.prefab","Assets/Resources/Prefabs/Popups/SettingsPopup.prefab","Assets/Resources/Prefabs/Popups/ResultPopup.prefab"})
            {var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);Texts(prefab,false);Check(!prefab.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="ResultModal"),"embedded result remains");}
            var registry=new SerializedObject(PopupManager.Instance).FindProperty("popupPrefabs");Check(Enumerable.Range(0,registry.arraySize).Any(i=>registry.GetArrayElementAtIndex(i).objectReferenceValue is RuneResultPanel),"result prefab not registered");
            foreach(bool inLobby in new[]{true,false})
            {
                pages.ChangeImmediate(inLobby?UIPageType.MainPage:UIPageType.PlayPage,0);await UniTask.Yield();
                if(inLobby){lobby.CanvasGroup.alpha=1;Texts(lobby.gameObject);Capture(lobby.Canvas,"English-Lobby");lobby.OpenSettings();}else{p.CanvasGroup.alpha=1;Texts(p.gameObject);Capture(p.Canvas,"English-Play");p.OpenSettings();}
                await Ready();var settings=(RuneSettingsPanel)PopupManager.CurrentPopup;Texts(settings.gameObject);Capture(settings.Canvas,inLobby?"English-Settings-Lobby":"English-Settings-Play");
                bool oldMusic=RunePreferences.Music;settings.musicToggle.isOn=!oldMusic;Check(RunePreferences.Music==!oldMusic,"music toggle");settings.musicToggle.isOn=oldMusic;
                settings.audioToggle.isOn=true;Check(RunePreferences.Audio,"sound toggle");settings.audioToggle.isOn=false;settings.Dismiss();await Closed();
            }
            // Finish an actual level using only board taps and automatic storage launches.
            p.LoadLevel(0);var route=RuneAutoRules.Solve(p.Rules,p.State);foreach(var move in route.route){p.Tap(move.id,false);Tick(p,2.5f);}Tick(p,1);await Ready();
            var result=(RuneResultPanel)PopupManager.CurrentPopup;Check(p.State.phase==RunePhase.Won&&result!=null,"real win popup");Check(result.title.text=="Clear!"&&result.claimButton.gameObject.activeSelf,"clear content");Claim(result);
            Check(result.AnimationTarget==result.panel&&result.Canvas.worldCamera!=null,"cached popup dependencies");Check(!result.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="ResultBlock"||t.name=="Banner"),"star or emblem remains");
            Texts(result.gameObject);Capture(result.Canvas,"English-LevelClear");int taps=p.State.taps;p.Tap(0,false);Check(p.State.taps==taps,"popup input blocking");
            result.primaryButton.onClick.Invoke();result.primaryButton.onClick.Invoke();await Closed();Check(p.LevelIndex==1&&p.State.phase==RunePhase.Playing,"next level or duplicate click");
            // Independent prefab handles last-level, failure and demo retry without page UI references.
            foreach(var test in new[]{new RuneResultData{level=6,won=true,hasNext=false},new RuneResultData{level=2,won=false},new RuneResultData{level=7,won=false,demo=true}})
            {
                object action=null;PopupManager.Show(RuneResultPanel.PrefabName,test,v=>action=v);await Ready();result=(RuneResultPanel)PopupManager.CurrentPopup;Texts(result.gameObject);
                if(test.won)Claim(result);else Check(!result.homeButton.gameObject.activeSelf&&Mathf.Approximately(result.primaryRect.anchoredPosition.x,165),"single action layout");Capture(result.Canvas,test.won?"English-FinalClear":test.demo?"English-DemoRetry":"English-Failure");result.ChoosePrimary();await Closed();
                Check(action is RuneResultAction&&((RuneResultAction)action)==(test.won?RuneResultAction.Next:test.demo?RuneResultAction.Retry:RuneResultAction.Home),"result action");
            }
            // Result Home returns to the existing lobby. Back has the same destination.
            foreach(bool back in new[]{false,true})
            {
                pages.ChangeImmediate(UIPageType.PlayPage,0);p.State.phase=RunePhase.Won;typeof(RunePlayPage).GetMethod("ShowResult",Private).Invoke(p,null);await Ready();result=(RuneResultPanel)PopupManager.CurrentPopup;
                Claim(result);if(back)PopupManager.HandleBackInput();else result.homeButton.onClick.Invoke();await Closed();await UniTask.WaitUntil(()=>!pages.IsChanging).Timeout(TimeSpan.FromSeconds(10));Check(pages.CurrentPageType==UIPageType.MainPage&&lobby.gameObject.activeInHierarchy,"home/back navigation");
            }
            File.WriteAllText("Tools/RuneBreak/QA/english-popup.json","{\"status\":\"passed\",\"standaloneResultPrefab\":true,\"registeredPopupManager\":true,\"englishLabels\":true,\"fredoka\":true,\"settingsContexts\":2,\"realLevelClear\":true,\"nextHomeRetryBack\":true,\"instanceCleanup\":true}");Debug.Log("ENGLISH POPUP PASS");
        }
        catch(Exception e){Debug.LogException(e);File.WriteAllText("Tools/RuneBreak/QA/english-popup.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="failed",error=e.ToString()}));}
        finally
        {
            PopupManager.Clear();if(hasWallet)PlayerPrefs.SetString(RuneCoinReward.SaveKey,wallet);else PlayerPrefs.DeleteKey(RuneCoinReward.SaveKey);RunePreferences.Audio=audio;RunePreferences.Reduced=reduced;if(hasNext)PlayerPrefs.SetInt("RuneBreak.Next",next);else PlayerPrefs.DeleteKey("RuneBreak.Next");if(hasClears)PlayerPrefs.SetInt("RuneBreak.Clears",clears);else PlayerPrefs.DeleteKey("RuneBreak.Clears");PlayerPrefs.Save();p.enabled=enabled;pages.ChangeImmediate(UIPageType.MainPage);
        }
    }
    static void Capture(Canvas canvas,string name)
    {
        Canvas.ForceUpdateCanvases();var camera=canvas.worldCamera;var prior=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(Screen.width,Screen.height,24);var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();File.WriteAllBytes("Tools/RuneBreak/QA/"+name+".png",texture.EncodeToPNG());}
        finally{camera.targetTexture=prior;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
}
