using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RuneBreak;

// A visual capture only: opening a reward preview never claims or saves coins.
public static class CoinMaterialPreview
{
    public static void Execute()=>CapturePages().Forget();
    static async UniTaskVoid CapturePages()
    {
        var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();
        try
        {
            PopupManager.Clear();pages.ChangeImmediate(UIPageType.MainPage,0);
            await UniTask.Delay(400,ignoreTimeScale:true);var lobby=pages.GetPage<RuneLobbyPage>(UIPageType.MainPage);lobby.CanvasGroup.alpha=1;Capture(lobby.Canvas,"YellowCoin-Lobby");
            pages.ChangeImmediate(UIPageType.PlayPage,0);await UniTask.Delay(400,ignoreTimeScale:true);
            PopupManager.Show(RuneResultPanel.PrefabName,new RuneResultData{level=1,won=true,hasNext=true,reward=new RuneCoinReward(40)});
            await UniTask.WaitUntil(()=>!PopupManager.IsChanging);await UniTask.Delay(850,ignoreTimeScale:true);
            var popup=(RuneResultPanel)PopupManager.CurrentPopup;Capture(popup.Canvas,"YellowCoin-Reward");
            File.WriteAllText("Tools/RuneBreak/QA/yellow-coin-preview.json","{\"status\":\"captured\",\"rewardClaimed\":false}");
        }
        catch(Exception e){Debug.LogException(e);File.WriteAllText("Tools/RuneBreak/QA/yellow-coin-preview.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="failed",error=e.ToString()}));}
        finally{PopupManager.Clear();pages.ChangeImmediate(UIPageType.MainPage,0);}
    }
    static void Capture(Canvas canvas,string name)
    {
        Canvas.ForceUpdateCanvases();var camera=canvas.worldCamera;var prior=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(Screen.width,Screen.height,24);var tex=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);tex.Apply();File.WriteAllBytes("Tools/RuneBreak/QA/"+name+".png",tex.EncodeToPNG());}
        finally{camera.targetTexture=prior;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
}
