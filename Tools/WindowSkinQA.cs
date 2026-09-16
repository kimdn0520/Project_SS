using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ProjectSS.Expedition;
public static class WindowSkinQA
{
    public static void Execute(){Run().Forget();}
    static void Check(bool b,string message){if(!b)throw new Exception(message);}
    static async UniTask Shot(string name)
    {await UniTask.Delay(300);await UniTask.WaitForEndOfFrame(UnityEngine.Object.FindFirstObjectByType<PlayPage>());var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("PrototypeQA/window-"+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
    static async UniTaskVoid Run()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();
        try
        {
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();page.pausePolicy.ReleasePause("AppFocusLoss");
            page.OpenMenu(0);await Shot("hud");page.OpenMenu(1);await Shot("heroes");
            var layer=page.GetComponentInChildren<MenuPanelLayer>(true);Check(layer.GetComponent<Canvas>().sortingOrder==500&&layer.curtain.activeSelf,"Panel layer/curtain missing");
            var hits=new List<RaycastResult>();var data=new PointerEventData(EventSystem.current){position=new Vector2(Screen.width*.1f,Screen.height*.035f)};EventSystem.current.RaycastAll(data,hits);
            Check(hits.Count>0&&hits[0].gameObject.name=="Curtain","Bottom navigation not blocked by curtain: "+string.Join(",",hits.Select(h=>h.gameObject.name)));
            layer.Close();await UniTask.WaitForEndOfFrame(page);Check(page.ActiveTab==0&&!layer.curtain.activeSelf,"Curtain close failed: tab="+page.ActiveTab+", curtain="+layer.curtain.activeSelf+", popup="+PopupManager.IsOpenAny);
            page.OpenMenu(2);await Shot("bag");page.OpenMenu(1);page.OpenHero(0);page.SelectSlot(0);await Shot("equipment");
            var popup=(EquipmentSelectionPopup)PopupManager.CurrentPopup;Check(popup.Canvas.sortingOrder>500,"Nested popup behind menu");
            var close=(RectTransform)popup.transform.Find("CommonPopup/Window/Close");Check(close.anchoredPosition==new Vector2(648,-215),"Designer close position changed");
            popup.OnClickClose();await UniTask.Delay(300);Check(!PopupManager.IsOpenAny&&page.ActiveTab==1,"Nested close did not return to panel");
            page.OpenMenu(0);page.Help();await Shot("notice");PopupManager.CurrentPopup.OnEscape();await UniTask.Delay(300);
            File.WriteAllText("PrototypeQA/window-skin.txt","PASS: menu curtain and order 500, bottom nav raycast blocked, curtain close, equipment above menu, designer Common close position preserved, notice/close. "+Screen.width+"x"+Screen.height);
        }
        catch(Exception e){File.WriteAllText("PrototypeQA/window-skin.txt","FAIL: "+e);Debug.LogException(e);}
        finally{PopupManager.Clear();page.OpenMenu(0);}
    }
    public static string EditorCheck()
    {
        Check(!EditorApplication.isPlaying,"Stop play first");
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();var sky=page.GetComponentInChildren<BattleSkyExtension>(true);
        Check(!sky.sky.enabled||sky.sky.rectTransform.anchorMin.y>.5f,"Sky extension remains a center square");
        return "PASS: editor sky is hidden or anchored to top band, no center square";
    }
}
