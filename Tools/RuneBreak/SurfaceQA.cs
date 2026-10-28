using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RuneBreak;

public static class SurfaceQA
{
    private static void Check(bool ok,string message){if(!ok)throw new Exception("SURFACE QA: "+message);}
    public static void Execute(){Run().Forget();}
    private static async UniTaskVoid Run()
    {
        var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();var play=pages.GetPage<RunePlayPage>(UIPageType.PlayPage);var lobby=pages.GetPage<RuneLobbyPage>(UIPageType.MainPage);
        bool hasWallet=PlayerPrefs.HasKey(RuneCoinReward.SaveKey);string wallet=PlayerPrefs.GetString(RuneCoinReward.SaveKey,"");
        bool music=RunePreferences.Music,sfx=RunePreferences.Audio;bool hasNext=PlayerPrefs.HasKey("RuneBreak.Next"),hasClears=PlayerPrefs.HasKey("RuneBreak.Clears");int next=PlayerPrefs.GetInt("RuneBreak.Next",0),clears=PlayerPrefs.GetInt("RuneBreak.Clears",0);
        try
        {
            PopupManager.Clear();pages.ChangeImmediate(UIPageType.PlayPage,0);await UniTask.Delay(300,ignoreTimeScale:true);play.CanvasGroup.alpha=1;
            Check(play.art.surfaceStyle,"surface art not assigned");Check(play.pool.GetComponentsInChildren<RuneFlight>(true).Length==48,"pool changed");CheckText(play.gameObject);Capture(play.Canvas,"Surface-Play.png");
            // Verify a real UI click, immediate storage reservation and the new landing coordinates.
            int id=Enumerable.Range(0,play.Rules.Level.blocks.Length).First(i=>play.Rules.Apply(play.State,new RuneMove(RuneMoveKind.Board,i),out _,out var shot,out _)&&shot.store);
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(play.Canvas.worldCamera,play.blockRects[id].position),button=PointerEventData.InputButton.Left};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Check(hits.Count>0&&hits[0].gameObject==play.blocks[id].gameObject,"touch target alignment");
            var outside=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(play.Canvas.worldCamera,play.blockRects[id].TransformPoint(new Vector3(30,0,0)))};var edgeHits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(outside,edgeHits);Check(!edgeHits.Any(h=>h.gameObject==play.blocks[id].gameObject),"sprite shadow overlaps neighboring touch cell");
            ExecuteEvents.Execute(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);Check(play.State.taps==1&&play.State.slots.Contains(id),"tap reservation");
            var flight=play.pool.GetComponentsInChildren<RuneFlight>(true).Single(f=>f.gameObject.activeSelf);Check(Vector2.Distance(flight.to,RuneFlight.Slot(flight.shot.slot,true)+new Vector2(0,RuneFlight.SurfaceStorageFlightOffset))<.01f,"new storage target");
            play.AdvancePresentation(.14f);Capture(play.Canvas,"Surface-Flight.png");play.AdvancePresentation(2);Capture(play.Canvas,"Surface-Stored.png");Check(play.ActiveFlights==0,"flight returned");play.LoadLevel(0);
            foreach(bool isLobby in new[]{false,true})
            {
                pages.ChangeImmediate(isLobby?UIPageType.MainPage:UIPageType.PlayPage,0);await UniTask.Delay(200,ignoreTimeScale:true);
                if(isLobby)lobby.OpenSettings();else play.OpenSettings();await UniTask.WaitUntil(()=>!PopupManager.IsChanging);
                var popup=(RuneSettingsPanel)PopupManager.CurrentPopup;Check(popup!=null,"settings open");Check(popup.lobbyActions.activeSelf&&!popup.playActions.activeSelf,"same Close action in both contexts");
                CheckText(popup.gameObject);Check(popup.GetComponentsInChildren<TMP_Text>().Select(t=>t.text).OrderBy(x=>x).SequenceEqual(new[]{"Settings","Music","Sound Effects","Close"}.OrderBy(x=>x)),"settings content");
                Capture(popup.Canvas,isLobby?"Surface-Settings-Lobby.png":"Surface-Settings-Play.png");
                popup.musicToggle.isOn=!music;popup.audioToggle.isOn=!sfx;Check(RunePreferences.Music==!music&&RunePreferences.Audio==!sfx,"audio callbacks");
                popup.musicToggle.isOn=music;popup.audioToggle.isOn=sfx;
                if(!isLobby){int taps=play.State.taps;play.Tap(id,false);Check(play.State.taps==taps,"modal input blocked");}
                popup.lobbyActions.GetComponent<Button>().onClick.Invoke();await UniTask.WaitUntil(()=>!PopupManager.Instance.IsBlockingInput);await UniTask.Yield();Check(PopupManager.Instance.LiveInstanceCount==0,"close and destroy");
            }
            pages.ChangeImmediate(UIPageType.PlayPage,0);await UniTask.Delay(200,ignoreTimeScale:true);play.CanvasGroup.alpha=1;
            foreach(var phase in new[]{RunePhase.Won,RunePhase.Lost})
            {
                play.LoadLevel(0);play.State.phase=phase;typeof(RunePlayPage).GetMethod("ShowResult",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(play,null);
                await UniTask.WaitUntil(()=>!PopupManager.IsChanging);var result=(RuneResultPanel)PopupManager.CurrentPopup;Check(result.title.text==(phase==RunePhase.Won?"Clear!":"Try Again"),"title");CheckText(result.gameObject);
                foreach(var b in result.GetComponentsInChildren<Button>())Check(b.onClick.GetPersistentEventCount()==1,"result callback");
                Capture(result.Canvas,phase==RunePhase.Won?"Surface-LevelClear.png":"Surface-Loss.png");
                if(phase==RunePhase.Won){result.ClaimReward();for(int i=0;i<150;i++)result.AdvancePresentation(.02f);}result.primaryButton.onClick.Invoke();await UniTask.WaitUntil(()=>!play.IsModalBlocked&&!pages.IsChanging);if(phase==RunePhase.Won)Check(play.LevelIndex==1,"next level callback");
            }
            File.WriteAllText("Tools/RuneBreak/QA/surface-runtime.json","{\"status\":\"passed\",\"fonts\":\"official Fredoka Medium, SemiBold and Bold\",\"pool\":48,\"settingsContexts\":2,\"title\":\"Clear!\",\"star\":false,\"tapAndStorageReservation\":true,\"audio\":true,\"nextLevel\":true}");Debug.Log("SURFACE QA PASS");
        }
        catch(Exception e){File.WriteAllText("Tools/RuneBreak/QA/surface-runtime.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="failed",error=e.ToString()}));Debug.LogException(e);}
        finally
        {
            PopupManager.Clear();RunePreferences.Music=music;RunePreferences.Audio=sfx;
            if(hasWallet)PlayerPrefs.SetString(RuneCoinReward.SaveKey,wallet);else PlayerPrefs.DeleteKey(RuneCoinReward.SaveKey);
            if(hasNext)PlayerPrefs.SetInt("RuneBreak.Next",next);else PlayerPrefs.DeleteKey("RuneBreak.Next");if(hasClears)PlayerPrefs.SetInt("RuneBreak.Clears",clears);else PlayerPrefs.DeleteKey("RuneBreak.Clears");PlayerPrefs.Save();
            pages.ChangeImmediate(UIPageType.PlayPage,0);play.LoadLevel(0);
        }
    }
    private static void CheckText(GameObject root){Canvas.ForceUpdateCanvases();foreach(var label in root.GetComponentsInChildren<TMP_Text>()){label.ForceMeshUpdate();Check(!label.isTextOverflowing,"overflow "+label.name+" "+label.text);}}
    private static void Capture(Canvas canvas,string name)
    {
        Canvas.ForceUpdateCanvases();var camera=canvas.worldCamera;var prior=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(Screen.width,Screen.height,24);var tex=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);tex.Apply();File.WriteAllBytes("Tools/RuneBreak/QA/"+name,tex.EncodeToPNG());}
        finally{camera.targetTexture=prior;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
}
