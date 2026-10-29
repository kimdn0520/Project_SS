using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RuneBreak;
public static class ButtonCoinsQA
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("BUTTON COINS QA: "+message);}
    static async UniTask Wait(float seconds)=>await UniTask.Delay(TimeSpan.FromSeconds(seconds),ignoreTimeScale:true);
    public static void Execute()=>Run().Forget();
    static async UniTaskVoid Run()
    {
        var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();var lobby=pages.GetPage<RuneLobbyPage>(UIPageType.MainPage);
        bool reduced=RunePreferences.Reduced;float timeScale=Time.timeScale;bool hadWallet=PlayerPrefs.HasKey(RuneCoinReward.SaveKey);string wallet=PlayerPrefs.GetString(RuneCoinReward.SaveKey,"");
        try
        {
            RunePreferences.Reduced=false;PopupManager.Clear();pages.ChangeImmediate(UIPageType.MainPage,0);await Wait(.3f);lobby.CanvasGroup.alpha=1;
            var button=lobby.playLabel.GetComponentInParent<RuneRaisedButton>();Check(button.onClick.GetPersistentEventCount()>0,"level callback retained");
            await Interaction(button);
            Capture(lobby.Canvas,"ButtonCoins-Lobby");
            lobby.OpenSettings();await UniTask.WaitUntil(()=>!PopupManager.IsChanging);var settings=(RuneSettingsPanel)PopupManager.CurrentPopup;
            var close=settings.lobbyActions.GetComponent<RuneRaisedButton>();await Interaction(close);Check(((Image)close.targetGraphic).sprite.name=="molded_gold_340_76","molded Close");
            Capture(settings.Canvas,"ButtonCoins-Settings");close.onClick.Invoke();await UniTask.WaitUntil(()=>!PopupManager.IsChanging);await UniTask.Yield();Check(PopupManager.Instance.LiveInstanceCount==0,"Close destroys settings");
            PopupManager.Show(RuneResultPanel.PrefabName,new RuneResultData{level=1,won=true,hasNext=true,reward=new RuneCoinReward(0)});await UniTask.WaitUntil(()=>!PopupManager.IsChanging);var result=(RuneResultPanel)PopupManager.CurrentPopup;await Wait(.8f);
            await Interaction((RuneRaisedButton)result.claimButton);Capture(result.Canvas,"ButtonCoins-Claim");
            Check(result.coinInstances.Length==12&&result.coinInstances.All(c=>c.image.sprite.name=="lobby_coin"),"shared coin in existing 12 instance pool");
            Check(result.coinPile.pieces.Length==8&&result.coinPile.launchPoints.Length==7,"layered pile and seven origins");
            result.claimButton.onClick.Invoke();Check(result.IsCollecting&&!result.primaryButton.gameObject.activeSelf,"navigation waits for coins");
            for(int i=0;i<100;i++)result.AdvancePresentation(.03f);Check(result.RewardReady&&result.coinInstances.All(c=>!c.gameObject.activeSelf),"all coins returned");
            await Interaction((RuneRaisedButton)result.primaryButton);await Interaction((RuneRaisedButton)result.homeButton);Capture(result.Canvas,"ButtonCoins-Collected");
            result.ChooseHome();await UniTask.WaitUntil(()=>!PopupManager.IsChanging);await UniTask.Yield();Check(PopupManager.Instance.LiveInstanceCount==0,"result cleanup");
            File.WriteAllText("Tools/RuneBreak/QA/button-coins.json","{\"status\":\"passed\",\"singleFace\":true,\"centeredScale\":0.96,\"pointerCancel\":true,\"disableReset\":true,\"unscaledTime\":true,\"reducedMotion\":true,\"coinPoolReturned\":12,\"callbacksPreserved\":true}");
            Debug.Log("BUTTON COINS PASS: single faces; press/release/cancel/disable/paused time; pool and callbacks.");
        }
        catch(Exception e){File.WriteAllText("Tools/RuneBreak/QA/button-coins.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="failed",error=e.ToString()}));Debug.LogException(e);}
        finally{PopupManager.Clear();RunePreferences.Reduced=reduced;Time.timeScale=timeScale;if(hadWallet)PlayerPrefs.SetString(RuneCoinReward.SaveKey,wallet);else PlayerPrefs.DeleteKey(RuneCoinReward.SaveKey);PlayerPrefs.Save();pages.ChangeImmediate(UIPageType.MainPage,0);}
    }
    static async UniTask Interaction(RuneRaisedButton b)
    {
        Check(b.scalePress&&b.face!=null&&b.pressDepth==0,"cached scale interaction: "+b.name);Check(b.GetComponent<Image>().color.a==0&&b.GetComponent<Image>().sprite==null,"no foundation: "+b.name);
        Check(b.face.pivot==new Vector2(.5f,.5f),"center pivot");
        Vector2 root=((RectTransform)b.transform).sizeDelta,position=b.face.anchoredPosition;var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
        Time.timeScale=0;b.OnPointerEnter(pointer);b.OnPointerDown(pointer);await Wait(.12f);Check(Mathf.Abs(b.face.localScale.x-.96f)<.001f,"pressed at time scale zero: "+b.name);Check(b.face.anchoredPosition==position&&((RectTransform)b.transform).sizeDelta==root,"stable center and hit area");
        b.OnPointerUp(pointer);await Wait(.2f);Check(b.face.localScale==Vector3.one,"release reset");
        b.OnPointerDown(pointer);await Wait(.1f);b.OnPointerExit(pointer);await Wait(.2f);Check(b.face.localScale==Vector3.one,"drag off cancels");b.OnPointerUp(pointer);
        b.OnPointerEnter(pointer);b.OnPointerDown(pointer);await Wait(.1f);b.interactable=false;Check(b.face.localScale==Vector3.one,"disabled resets immediately");b.interactable=true;b.OnPointerUp(pointer);b.OnPointerExit(pointer);
        RunePreferences.Reduced=true;b.OnPointerEnter(pointer);b.OnPointerDown(pointer);Check(Mathf.Abs(b.face.localScale.x-.96f)<.001f,"reduced immediate press");b.OnPointerUp(pointer);Check(b.face.localScale==Vector3.one,"reduced immediate release");b.OnPointerExit(pointer);RunePreferences.Reduced=false;Time.timeScale=1;
    }
    static void Capture(Canvas canvas,string name)
    {
        Canvas.ForceUpdateCanvases();var camera=canvas.worldCamera;var prior=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(Screen.width,Screen.height,24);var tex=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);tex.Apply();File.WriteAllBytes("Tools/RuneBreak/QA/"+name+".png",tex.EncodeToPNG());}
        finally{camera.targetTexture=prior;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
}
