using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using ProjectSS.Expedition;
public static class MenuPolishQA
{
    public static void Execute(){Run().Forget();}
    static void Check(bool b,string reason){if(!b)throw new Exception(reason);}
    static async UniTask Shot(string name)
    {
        await UniTask.Delay(250);await UniTask.WaitForEndOfFrame(UnityEngine.Object.FindFirstObjectByType<PlayPage>());
        var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("PrototypeQA/polish-"+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);
    }
    static async UniTaskVoid Run()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();var timescale=Time.timeScale;
        try
        {
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();page.pausePolicy.ReleasePause("AppFocusLoss");
            page.OpenMenu(0);await Shot("battle");
            page.OpenMenu(1);await Shot("heroes");
            for(int i=0;i<8;i++){var r=(RectTransform)page.heroGrid.transform.Find("HeroCard"+i);Check(r.anchoredPosition.x>=54&&r.anchoredPosition.x+r.sizeDelta.x<=666,"Card outside padding");Check(-r.anchoredPosition.y+r.sizeDelta.y<=1104,"Card outside panel");}
            page.OpenHero(0);await Shot("hero-detail");page.SelectSlot(0);await Shot("equipment");
            Check(PopupManager.CurrentPopup is EquipmentSelectionPopup,"Equipment did not open");PopupManager.CurrentPopup.OnEscape();await UniTask.Delay(300);
            page.OpenMenu(2);await Shot("bag");page.inventoryView.SelectFilter(2);await Shot("materials");page.inventoryView.SelectFilter(0);
            page.OpenMenu(4);await Shot("settings");page.OpenMenu(5);await Shot("mining");
            page.OpenMenu(0);page.OpenVeins();await Shot("veins");PopupManager.CurrentPopup.OnEscape();await UniTask.Delay(300);
            ToastPopup.Show("장비를 변경했습니다");await Shot("toast");
            ToastPopup.Show("새로운 메시지가 이전 메시지를 교체합니다",new Vector2(Screen.width,Screen.height),.3f);
            Time.timeScale=0;await UniTask.Delay(1000,ignoreTimeScale:true);
            var toasts=Resources.FindObjectsOfTypeAll<ToastPopup>().Where(t=>t.gameObject.scene.IsValid()).ToArray();
            Check(toasts.Length==1&&!toasts[0].gameObject.activeSelf,"Toast reuse or unscaled dismissal failed");
            ToastPopup.Show("취소 확인");ToastPopup.HideIfActive();Check(!toasts[0].gameObject.activeSelf,"Toast hide failed");
            Check(toasts[0].GetComponentsInChildren<Graphic>(true).All(g=>!g.raycastTarget),"Toast intercepts input");
            File.WriteAllText("PrototypeQA/menu-polish.txt","PASS: 3-column cards inside panel; menu/popup captures; inventory filters; singleton toast replacement, unscaled dismissal and explicit hide; no toast raycast targets; header expanded with independent outlined text materials. "+Screen.width+"x"+Screen.height);
        }
        catch(Exception e){File.WriteAllText("PrototypeQA/menu-polish.txt","FAIL: "+e);Debug.LogException(e);}
        finally{Time.timeScale=timescale;ToastPopup.HideIfActive();PopupManager.Clear();page.OpenMenu(0);}
    }
}
