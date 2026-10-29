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

public static class SurfaceLayoutQA
{
    public static void Execute(){Run().Forget();}
    private static async UniTaskVoid Run()
    {
        var pages=UnityEngine.Object.FindFirstObjectByType<PageManager>();var play=pages.GetPage<RunePlayPage>(UIPageType.PlayPage);
        bool walletExists=PlayerPrefs.HasKey(RuneCoinReward.SaveKey);string wallet=PlayerPrefs.GetString(RuneCoinReward.SaveKey,"");
        try
        {
            foreach(var size in new[]{new Vector2Int(540,960),new Vector2Int(540,1200),new Vector2Int(768,1024),new Vector2Int(1280,720)})
            {
                SetSize(size.x,size.y);PopupManager.Clear();pages.ChangeImmediate(UIPageType.PlayPage,0);await UniTask.Delay(350,ignoreTimeScale:true);play.CanvasGroup.alpha=1;
                foreach(var rect in play.GetComponentsInChildren<RectTransform>().Where(t=>t.name=="Settings"||t.name.StartsWith("ReadyBoosterSlot_")||t==play.cabinet))Within(rect,play.Canvas);
                Capture(play.Canvas,$"Surface-Play-{size.x}x{size.y}.png");
                play.OpenSettings();await UniTask.WaitUntil(()=>!PopupManager.IsChanging);await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);Canvas.ForceUpdateCanvases();var popup=(RuneSettingsPanel)PopupManager.CurrentPopup;Within(popup.panel,popup.Canvas);Capture(popup.Canvas,$"Surface-Settings-{size.x}x{size.y}.png");
                popup.Dismiss();await UniTask.WaitUntil(()=>!play.IsModalBlocked);
                // Preview the serialized card without awarding progress.
                PopupManager.Show(RuneResultPanel.PrefabName,new RuneResultData{level=1,won=true,hasNext=true,reward=new RuneCoinReward(0)});await UniTask.WaitUntil(()=>!PopupManager.IsChanging);var result=(RuneResultPanel)PopupManager.CurrentPopup;
                Within(result.walletRect,result.Canvas);Within(result.heroRect,result.Canvas);Within((RectTransform)result.claimButton.transform,result.Canvas);
                Within(result.panel,result.Canvas);Within(result.title.rectTransform,result.Canvas);Within(result.primaryRect,result.Canvas);Within((RectTransform)result.homeButton.transform,result.Canvas);
                var primaryBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(result.transform,result.primaryRect);var homeBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(result.transform,result.homeButton.transform);if(primaryBounds.min.x<homeBounds.max.x)throw new Exception("Result buttons overlap");
                foreach(var text in result.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();if(text.isTextOverflowing)throw new Exception("Text overflow "+text.name);}
                var shade=(RectTransform)result.transform.Find("Curtain");var corners=new Vector3[4];shade.GetWorldCorners(corners);var min=RectTransformUtility.WorldToScreenPoint(result.Canvas.worldCamera,corners[0]);var max=RectTransformUtility.WorldToScreenPoint(result.Canvas.worldCamera,corners[2]);
                if(min.x>1||min.y>1||max.x<Screen.width-1||max.y<Screen.height-1)throw new Exception("Result shade not fullscreen");
                Capture(result.Canvas,$"Surface-Clear-{size.x}x{size.y}.png");result.ClaimReward();for(int i=0;i<100;i++)result.AdvancePresentation(.03f);Within(result.primaryRect,result.Canvas);Within((RectTransform)result.homeButton.transform,result.Canvas);Capture(result.Canvas,$"Reward-Collected-{size.x}x{size.y}.png");result.ChooseHome();await UniTask.WaitUntil(()=>!play.IsModalBlocked);play.LoadLevel(0);
            }
            File.WriteAllText("Tools/RuneBreak/QA/surface-layout.json","{\"status\":\"passed\",\"resolutions\":[\"540x960\",\"540x1200\",\"768x1024\",\"1280x720\"],\"fullscreenShade\":true,\"safeArea\":true,\"noTextOverflow\":true}");Debug.Log("SURFACE LAYOUT PASS: four aspect ratios, safe-area controls and fullscreen result shade.");
        }
        catch(Exception e){File.WriteAllText("Tools/RuneBreak/QA/surface-layout.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="failed",error=e.ToString()}));Debug.LogException(e);}
        finally{PopupManager.Clear();if(walletExists)PlayerPrefs.SetString(RuneCoinReward.SaveKey,wallet);else PlayerPrefs.DeleteKey(RuneCoinReward.SaveKey);PlayerPrefs.Save();SetSize(540,960);pages.ChangeImmediate(UIPageType.PlayPage,0);play.LoadLevel(0);}
    }
    private static void Within(RectTransform rect,Canvas canvas)
    {var corners=new Vector3[4];rect.GetWorldCorners(corners);foreach(var corner in corners){var p=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,corner);if(p.x< -1||p.y< -1||p.x>Screen.width+1||p.y>Screen.height+1)throw new Exception("Clipped "+rect.name+" at "+Screen.width+"x"+Screen.height+" corner "+p+" local scale "+rect.localScale+" canvas "+((RectTransform)canvas.transform).rect.size);}}
    private static void SetSize(int width,int height)
    {
        var asm=typeof(UnityEditor.Editor).Assembly;var type=asm.GetType("UnityEditor.GameViewSizes");var singleton=type.BaseType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
        var group=type.GetMethod("GetGroup").Invoke(singleton,new[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeGroupType"),"Standalone")});int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null),index=-1;
        for(int i=0;i<count;i++){var item=group.GetType().GetMethod("GetGameViewSize").Invoke(group,new object[]{i});if((int)item.GetType().GetProperty("width").GetValue(item)==width&&(int)item.GetType().GetProperty("height").GetValue(item)==height){index=i;break;}}
        if(index<0){var item=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),width,height,$"Surface QA {width}x{height}"});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{item});index=count;}
        var vt=asm.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(vt);vt.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,index);view.Repaint();
    }
    private static void Capture(Canvas canvas,string name)
    {
        Canvas.ForceUpdateCanvases();var camera=canvas.worldCamera;var prior=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(Screen.width,Screen.height,24);var tex=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);tex.Apply();File.WriteAllBytes("Tools/RuneBreak/QA/"+name,tex.EncodeToPNG());}
        finally{camera.targetTexture=prior;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
}
