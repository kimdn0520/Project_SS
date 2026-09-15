using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using ProjectSS.Expedition;
public static class PopupTransitionQA
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static void Execute(){Run().Forget();}
    static async UniTaskVoid Run()
    {
        var results=new List<string>();
        try
        {
            var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();PopupManager.Clear();
            for(int pass=0;pass<3;pass++)
            {
                if(pass==1)page.OpenVeins();
                else SidebarNotice.Show(page,"두더지 지원품","버튼 등장 상태 확인");
                var popup=(BasePopupHandler)PopupManager.CurrentPopup;
                Require(popup!=null,"popup created");
                var buttons=popup.GetComponentsInChildren<Button>(true);
                var enabled=buttons.First(b=>b.interactable&&b.gameObject.activeInHierarchy);
                var blocker=popup.Canvas.transform.Find("TransitionInputBlocker");
                Require(blocker!=null&&blocker.gameObject.activeSelf,"transition shield active");
                await UniTask.WaitForEndOfFrame(page);
                Canvas.ForceUpdateCanvases();
                var center=enabled.GetComponent<RectTransform>().TransformPoint(enabled.GetComponent<RectTransform>().rect.center);
                var camera=popup.Canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:popup.Canvas.worldCamera;
                var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,center)};
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
                Require(hits.Count>0&&hits[0].gameObject==blocker.gameObject,"transition shield intercepts button touches: "+string.Join(",",hits.Select(h=>h.gameObject.name))+" button="+enabled.name+" point="+pointer.position+" canvas="+popup.Canvas.name+" mode="+popup.Canvas.renderMode);
                int frames=0;float deadline=Time.realtimeSinceStartup+10;
                while(PopupManager.IsChanging&&Time.realtimeSinceStartup<deadline)
                {
                    foreach(var button in buttons.Where(b=>b.gameObject.activeInHierarchy))
                        Require(button.IsInteractable()==button.interactable,"button keeps its authored enabled/disabled state during animation");
                    frames++;await UniTask.Yield();
                }
                Require(!PopupManager.IsChanging&&!blocker.gameObject.activeSelf,"input unlocked after animation");
                Require(enabled.IsInteractable(),"button usable after opening");
                results.Add("PASS: opening "+pass+"; stable button states across "+frames+" frames; touches shielded then restored");
                popup.Close();Require(blocker.gameObject.activeSelf,"closing transition shield active");
                while(PopupManager.IsChanging&&Time.realtimeSinceStartup<deadline)await UniTask.Yield();
                Require(!PopupManager.IsOpenAny,"popup closes normally");
            }
        }
        catch(Exception e){results.Add("FAIL: "+e);Debug.LogException(e);}
        finally{PopupManager.Clear();File.WriteAllLines("PrototypeQA/popup-transition.txt",results);}
    }
}
