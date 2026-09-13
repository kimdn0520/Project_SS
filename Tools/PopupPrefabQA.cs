using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;
using ProjectSS.Expedition;
public static class PopupPrefabQA
{
    public static void Execute(){Run().Forget();}
    static void Check(bool b,string reason){if(!b)throw new Exception(reason);}
    static async UniTaskVoid Run()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();var backup=JsonUtility.ToJson(page.Model.Data);
        try
        {
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();page.pausePolicy.ReleasePause("AppFocusLoss");
            Check(EditorUtility.IsPersistent(page.notice)&&EditorUtility.IsPersistent(page.equipmentPopup),"Page references are not prefab assets");
            Check(page.GetComponentsInChildren<BasePopupHandler>(true).Length==0,"Popups were instantiated before first use");
            page.OpenMenu(1);page.OpenHero(0);page.SelectSlot(0);await UniTask.Delay(350);
            var equipment=(EquipmentSelectionPopup)PopupManager.CurrentPopup;
            Check(equipment!=page.equipmentPopup&&equipment.Canvas.worldCamera==page.Canvas.worldCamera,"Prefab/camera binding failed");
            Check(equipment.Canvas.renderMode==RenderMode.ScreenSpaceCamera&&page.pausePolicy.IsPaused,"Canvas mode/pause failed");
            Shot("popup-equipment");equipment.OnEscape();await UniTask.Delay(300);Check(!PopupManager.IsOpenAny&&!page.pausePolicy.IsPaused,"Back/pause release failed");
            int item=Enumerable.Range(0,page.catalog.gear.Length).First(i=>page.catalog.gear[i].equipSlot==2&&(page.catalog.gear[i].hero<0||page.catalog.gear[i].hero==0));page.Model.Data.inventory[item]++;
            page.SelectSlot(2);await UniTask.Delay(300);Check(ReferenceEquals(equipment,PopupManager.CurrentPopup),"Popup cache not reused");
            equipment.rows[item].button.onClick.Invoke();await UniTask.Delay(300);Check(page.Model.Equipped(0,2)==item&&!PopupManager.IsOpenAny,"Equip failed");
            page.SelectSlot(2);await UniTask.Delay(300);equipment.unequip.onClick.Invoke();await UniTask.Delay(300);Check(page.Model.Equipped(0,2)==-1,"Unequip failed");
            page.Help();await UniTask.Delay(300);var notice=(ExpeditionNotice)PopupManager.CurrentPopup;
            Check(notice.Canvas.worldCamera==page.Canvas.worldCamera,"Notice camera missing");Shot("popup-notice");notice.Accept();await UniTask.Delay(300);
            var answer=PopupManager.ShowAsync<bool>(page.notice.PopupName,new ExpeditionNotice.Content{pausePolicy=page.pausePolicy,title="진행 초기화",body="현재 Play 진행과 획득 장비를 초기화할까요?",action="초기화",confirmation=true});await UniTask.Delay(300);Shot("popup-confirmation");notice.OnEscape();Check(!await answer,"Cancel returned true");
            Check(!page.pausePolicy.IsPaused&&page.GetComponentsInChildren<BasePopupHandler>(true).Length==2,"Popup reuse/pause final state failed");
            File.WriteAllText("PrototypeQA/popup-prefabs.txt","PASS: separate prefab assets; lazy creation; cached reuse; ScreenSpaceCamera injection; equipment filters/equip/unequip; notice; cancellation result; pause release; two cached instances.");Debug.Log("POPUP PREFAB QA PASS");
        }
        catch(Exception e){File.WriteAllText("PrototypeQA/popup-prefabs.txt","FAIL: "+e);Debug.LogException(e);}
        finally{PopupManager.Clear();JsonUtility.FromJsonOverwrite(backup,page.Model.Data);page.OnWillEnter(null);PlayerPrefs.SetString("ProjectSS.Play.v3",backup);PlayerPrefs.Save();}
    }
    static void Shot(string name)
    {
        var camera=Camera.main;var old=camera.targetTexture;var rect=camera.rect;var active=RenderTexture.active;
        var rt=new RenderTexture(720,1280,24);var texture=new Texture2D(720,1280,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,720,1280),0,0);texture.Apply();File.WriteAllBytes("PrototypeQA/"+name+".png",texture.EncodeToPNG());}
        finally{camera.targetTexture=old;camera.rect=rect;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
}
