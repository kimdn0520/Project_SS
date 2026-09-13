using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using ProjectSS.Expedition;
public static class BlockyVeinQA
{
 public static void Execute(){Run().Forget();}
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 static async UniTaskVoid Run()
 {
  var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();var backup=JsonUtility.ToJson(page.Model.Data);
  try
  {
   EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();page.pausePolicy.ReleasePause("AppFocusLoss");page.OpenMenu(0);page.Model.Data.autoBattle=false;page.Model.Data.chest=false;page.miningView.SetChest(false);page.holdDig.HardCancel();
   var so=new SerializedObject(page.miningView);var bank=so.FindProperty("damageSprites");Check(bank.arraySize==5,"Missing damage frames");
   int objectCount=page.miningWorld.GetComponentsInChildren<Transform>(true).Length;
   Shot("idle");
   foreach(string name in new[]{"SidebarStore","SidebarFirstCharge"})
   {
    var root=page.minePanel.transform.Find(name);var button=root.GetComponent<Button>();var visual=root.Find("Visual");var pointer=new PointerEventData(EventSystem.current){pointerId=63,button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(page.Canvas.worldCamera,visual.position)};
    var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Check(hits.Count>0&&hits[0].gameObject==root.gameObject,"Shortcut not raycastable: "+name);
    Check(button.onClick.GetPersistentEventCount()==0,"Shortcut has a business action");
    ExecuteEvents.Execute(root.gameObject,pointer,ExecuteEvents.pointerDownHandler);await UniTask.Delay(110);Check(visual.localScale.x<.95f,"No press feedback");Shot(name+"-pressed");ExecuteEvents.Execute(root.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(root.gameObject,pointer,ExecuteEvents.pointerClickHandler);await UniTask.Delay(180);
    Check(Mathf.Abs(visual.localScale.x-1)<.01f&&page.ActiveTab==0&&!PopupManager.IsOpenAny,"Shortcut click changed screen or stayed pressed");
   }
   for(int stage=1;stage<=4;stage++)
   {
    page.miningView.Strike(stage==4,false,0,stage*.25f-.01f);Shot("fracture-"+stage);Check(page.blocks.Any(r=>r.sprite==(Sprite)bank.GetArrayElementAtIndex(stage).objectReferenceValue),"Damage sprite missing");await UniTask.Delay(500);
   }
   for(int cycle=0;cycle<24;cycle++)
   {
    page.miningView.Descend();
    for(int frame=0;frame<12;frame++){await UniTask.Delay(40);Check(page.blocks.Where(r=>r.enabled).Min(r=>r.bounds.min.y)<-6.4f,"Bottom vein coverage missing");}
    if(cycle==7||cycle==18)Shot("scroll-"+cycle);
   }
   Check(page.miningWorld.GetComponentsInChildren<Transform>(true).Length==objectCount,"Map allocated new objects");
   page.Model.Data.chest=true;page.miningView.SetChest(true);Shot("chest-closed");int before=page.Model.Data.excavations;
   page.pausePolicy.ReleasePause("AppFocusLoss");page.Dig();Check(page.ChestOpening,"Chest did not open");
   await UniTask.Delay(260);Shot("chest-hinge");await UniTask.Delay(390);Shot("chest-inside");await UniTask.Delay(430);Shot("chest-reward");await UniTask.Delay(1500);
   Check(!page.ChestOpening&&page.Model.Data.excavations==before+1,"Chest reward was missing or duplicated");
   File.WriteAllText("PrototypeQA/blocky-art.txt","PASS: two prefab shortcuts raycast + press/release, no click actions or popups. Five fracture frames. 24 scrolling descents, bottom coverage and fixed map object count. Cartoon chest hinge/inside/reward, exactly one reward.");Debug.Log("BLOCKY VEIN QA PASS");
  }
  catch(Exception e){File.WriteAllText("PrototypeQA/blocky-art.txt","FAIL: "+e);Debug.LogException(e);}
  finally{page.holdDig.HardCancel();JsonUtility.FromJsonOverwrite(backup,page.Model.Data);page.OpenMenu(2);page.OnWillEnter(null);PlayerPrefs.SetString("ProjectSS.Play.v3",backup);PlayerPrefs.Save();Shot("final");}
 }
 static void Shot(string name)
 {
  var cam=Camera.main;var old=cam.targetTexture;var rect=cam.rect;var active=RenderTexture.active;var rt=new RenderTexture(720,1280,24);var tex=new Texture2D(720,1280,TextureFormat.RGB24,false);
  try{cam.targetTexture=rt;cam.rect=new Rect(0,0,1,1);Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,720,1280),0,0);tex.Apply();File.WriteAllBytes("PrototypeQA/blocky-"+name+".png",tex.EncodeToPNG());}
  finally{cam.targetTexture=old;cam.rect=rect;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
 }
}
