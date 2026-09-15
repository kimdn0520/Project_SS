using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;
using ProjectSS.Expedition;
public static class CameraCanvasQA
{
    public static void Execute(){Run().Forget();}
    static async UniTask Shot(PlayPage page,string name)
    {
        await UniTask.WaitForEndOfFrame(page);var t=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes("PrototypeQA/camera-"+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);
    }
    static async UniTaskVoid Run()
    {
        try
        {
            var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();
            var ui=page.popupManager.RenderCamera;
            if(ui==null||ui==page.Canvas.worldCamera||ui.rect!=new Rect(0,0,1,1))throw new Exception("Missing full-screen UI camera");
            var panel=page.GetComponentInChildren<MenuPanelLayer>(true).GetComponent<Canvas>();
            var side=page.transform.Find("SidebarCanvas").GetComponent<Canvas>();
            if(new[]{panel,side}.Any(c=>c.renderMode!=RenderMode.ScreenSpaceCamera||c.worldCamera!=ui))throw new Exception("Panel/sidebar binding");
            page.OpenMenu(1);await Shot(page,"panel");page.OpenVeins();
            await UniTask.WaitForEndOfFrame(page);var popup=(BasePopupHandler)PopupManager.CurrentPopup;
            if(popup.Canvas.renderMode!=RenderMode.ScreenSpaceCamera||popup.Canvas.worldCamera!=ui||popup.Canvas.sortingOrder<=panel.sortingOrder)throw new Exception("Popup camera or sorting");
            await UniTask.Delay(500,ignoreTimeScale:true);await Shot(page,"popup");
            ToastPopup.Show("카메라 연결 확인");await UniTask.WaitForEndOfFrame(page);
            var toast=UnityEngine.Object.FindFirstObjectByType<ToastPopup>().GetComponent<Canvas>();
            if(toast.renderMode!=RenderMode.ScreenSpaceCamera||toast.worldCamera!=ui)throw new Exception("Toast camera binding");
            File.WriteAllText("PrototypeQA/camera-canvases.txt","PASS: cached full-screen UI camera; panel/sidebar/popup/toast ScreenSpaceCamera; popup above panels\n");
            ToastPopup.HideIfActive();PopupManager.Clear();page.OpenMenu(0);
        }
        catch(Exception e){File.WriteAllText("PrototypeQA/camera-canvases.txt","FAIL: "+e);Debug.LogException(e);}
    }
}
