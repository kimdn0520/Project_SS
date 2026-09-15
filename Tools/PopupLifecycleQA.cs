using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using ProjectSS.Expedition;
public static class PopupLifecycleQA
{
 public static void Execute(){Run().Forget();}
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static async UniTask Settle(){await UniTask.Delay(350,ignoreTimeScale:true);await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);}
 static async UniTaskVoid Run()
 {
  EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();
  var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();int route=page.Model.Data.route;
  var so=new SerializedObject(page.popupManager);var root=(Transform)so.FindProperty("popupRoot").objectReferenceValue;
  try
  {
   PopupManager.Clear();page.OpenMenu(0);page.pausePolicy.ReleasePause("AppFocusLoss");await Settle();
   page.OpenVeins();await Settle();var first=(VeinSelectionPopup)PopupManager.CurrentPopup;
   Check(first.transform.parent==root&&root.name=="PopupRoot","Incorrect popup parent");int id=first.GetInstanceID();
   int target=route==0?1:0;first.Choose(target);await Settle();
   Check(PopupManager.CurrentPopup==(IPopupHandler)first&&first.IsOpen,"Selection closed popup");
   Check(page.Model.Data.route==target&&!first.choices[target].interactable&&first.choiceLabels[target].text=="선택됨","Selection state not refreshed");
   first.Choose(target);Check(first.IsOpen,"Selected route click closed popup");
   await PopupManager.CloseAsync();await Settle();Check(first==null&&root.childCount==0,"Closed popup retained");
   page.OpenVeins();await Settle();var second=(VeinSelectionPopup)PopupManager.CurrentPopup;
   Check(second.GetInstanceID()!=id,"Popup reused instead of instantiated");PopupManager.CloseImmediate();await Settle();Check(second==null&&root.childCount==0,"Immediate close retained popup");
   PopupManager.Queue("VeinSelection",page);await Settle();var queuedFirst=(VeinSelectionPopup)PopupManager.CurrentPopup;
   PopupManager.Queue("VeinSelection",page);await PopupManager.CloseAsync();await Settle();
   Check(queuedFirst==null&&PopupManager.CurrentPopup is VeinSelectionPopup,"Repeated queued popup lost");
   PopupManager.Clear();await Settle();Check(root.childCount==0&&!PopupManager.IsOpenAny,"Clear retained popup");
   page.OpenMenu(1);page.OpenHero(0);page.SelectSlot(0);await Settle();
   Check(PopupManager.CurrentPopup is EquipmentSelectionPopup,"Equipment popup did not open");
   page.Help();await Settle();Check(PopupManager.OpenedCount==2,"Nested popup did not open");
   await PopupManager.CloseAllAsync();await Settle();Check(root.childCount==0,"CloseAll retained popup");
   File.WriteAllText("PrototypeQA/popup-lifecycle.txt","PASS: PopupRoot parent; selection stays open and refreshes; selected route no-op; close destroys; reopen creates new instance; immediate close, repeated same-name queue, Clear and CloseAll leave no children.");
  }
  catch(Exception e){File.WriteAllText("PrototypeQA/popup-lifecycle.txt","FAIL: "+e);Debug.LogException(e);}
  finally{PopupManager.Clear();page.ChangeVein(route);page.OpenMenu(0);}
 }
}
