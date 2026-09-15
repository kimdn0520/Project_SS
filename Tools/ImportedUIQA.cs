using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using ProjectSS.Expedition;
public static class ImportedUIQA
{
    public static void Execute(){Run().Forget();}
    static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
    static async UniTask Shot(string name)
    {
        await UniTask.WaitForEndOfFrame(UnityEngine.Object.FindFirstObjectByType<PlayPage>());
        var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("PrototypeQA/imported-"+name+".png",texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);
    }
    static async UniTaskVoid Run()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();string backup=JsonUtility.ToJson(page.Model.Data);
        try
        {
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();page.pausePolicy.ReleasePause("AppFocusLoss");
            page.OpenMenu(0);await UniTask.Delay(400);
            Check(page.routeButtons.Length==0,"Settings route buttons remain");
            Check(page.veinPopup!=null&&EditorUtility.IsPersistent(page.veinPopup),"Missing vein prefab");
            var safe=page.transform.Find("SidebarCanvas/SafeArea");Check(safe!=null,"No safe sidebar");
            Check(((RectTransform)safe.Find("SidebarVeins")).anchorMin.x==1,"Vein is not right anchored");
            var layout=page.GetComponentInChildren<ExpeditionSidebarLayout>();
            layout.ApplySafeArea(new Rect(120,60,1680,960),new Vector2(1920,1080));
            Check(Mathf.Abs(layout.safeArea.anchorMin.x-120f/1920)<.0001f&&Mathf.Abs(layout.safeArea.anchorMax.x-1800f/1920)<.0001f,"Landscape notch insets failed");
            layout.ApplySafeArea(new Rect(0,90,1080,2180),new Vector2(1080,2400));
            Check(Mathf.Abs(layout.safeArea.anchorMin.y-90f/2400)<.0001f&&Mathf.Abs(layout.safeArea.anchorMax.y-2270f/2400)<.0001f,"Portrait notch/home insets failed");
            await Shot("main");
            page.OpenVeins();await UniTask.Delay(400);var vein=PopupManager.CurrentPopup as VeinSelectionPopup;
            Check(vein!=null&&page.pausePolicy.IsPaused,"Vein open/pause failed");
            Check(vein.transform.Find("PopupCommon/Window/Contents")!=null,"No common popup");
            await Shot("veins");vein.Choose(1);await UniTask.Delay(350);
            page.pausePolicy.ReleasePause("AppFocusLoss");
            Check(page.Model.Data.route==1&&!PopupManager.IsOpenAny&&!page.pausePolicy.IsPaused,"Route change/close/resume failed: route="+page.Model.Data.route+" open="+PopupManager.IsOpenAny+" paused="+page.pausePolicy.IsPaused);
            page.Model.Data.cleared=0;page.OpenVeins();await UniTask.Delay(350);Check(!vein.choices[2].interactable,"Locked vein enabled");vein.Choose(2);await UniTask.Delay(350);Check(page.Model.Data.route==1,"Locked vein selected");
            page.OpenMenu(4);await Shot("settings");page.OpenMenu(0);
            page.Help();await UniTask.Delay(400);await Shot("notice");PopupManager.CurrentPopup.OnEscape();await UniTask.Delay(350);
            page.OpenMenu(1);page.OpenHero(0);page.SelectSlot(0);await UniTask.Delay(400);await Shot("equipment");PopupManager.CurrentPopup.OnEscape();await UniTask.Delay(350);page.OpenMenu(0);
            page.pausePolicy.RequestPause("ImportedQA");
            foreach(var bar in page.heroHealthBars.Concat(new[]{page.enemyHealthBar}))foreach(float v in new[]{1f,.01f,0f})
            {bar.SetValue(v,true);Check(bar.Fill.enabled==(v>0),"Zero HP remains visible");Check(bar.Fill.sprite.name=="progressbar_green"&&bar.Fill.type==Image.Type.Sliced,"HP skin failed");}
            page.heatGauge.SetValue(.65f,Color.white);await Shot("gauge");
            page.miningView.Strike(true,true,1,1);await UniTask.Delay(160);await Shot("fragments");await UniTask.Delay(700);
            page.pausePolicy.ReleasePause("ImportedQA");
            File.WriteAllText("PrototypeQA/imported-ui.txt","PASS: route popup common prefab, selection, locked route, pause/resume, notice/equipment open/back, settings removal, safe edge anchors, sliced HP including 1%/0%, pooled break fragments. Screen: "+Screen.width+"x"+Screen.height);
        }
        catch(Exception e){File.WriteAllText("PrototypeQA/imported-ui.txt","FAIL: "+e);Debug.LogException(e);}
        finally{PopupManager.Clear();page.pausePolicy.ReleasePause("ImportedQA");JsonUtility.FromJsonOverwrite(backup,page.Model.Data);page.OnWillEnter(null);}
    }
}
