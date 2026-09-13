using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using ProjectSS.Expedition;
public static class DigFeelQA
{
 public static void Execute(){Run().Forget();}
 static void Check(bool b,string why){if(!b)throw new Exception(why);}
 static async UniTaskVoid Run()
 {
  var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();var backup=JsonUtility.ToJson(page.Model.Data);
  try
  {
   EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();page.pausePolicy.ReleasePause("AppFocusLoss");page.OpenMenu(0);page.holdDig.HardCancel();page.Model.Data.chest=false;page.miningView.SetChest(false);
   var root=page.digButton.transform;var face=root.Find("PressableFace");var icon=face.Find("PickaxeIcon");var ring=root.Find("BreakImpactRing").GetComponent<Graphic>();var pedestal=root.parent.Find("DigBase");var rest=face.localPosition;var baseRest=pedestal.localPosition;var inputRest=root.localPosition;var iconRest=icon!=null?icon.localRotation:Quaternion.identity;
   var pointer=new PointerEventData(EventSystem.current){pointerId=83,button=PointerEventData.InputButton.Left};int before=page.Model.Data.excavations;Shot("idle");
   ExecuteEvents.Execute(root.gameObject,pointer,ExecuteEvents.pointerDownHandler);
   float min=rest.y,max=float.MinValue,peakRing=0;bool shotPress=false,shotBreak=false;int reversals=0;float previous=face.localPosition.y,direction=0;
   for(int i=0;i<260;i++)
   {
    await UniTask.Delay(20);Check(root.localPosition==inputRest&&pedestal.localPosition==baseRest&&root.localScale==Vector3.one,"Input/base moved");
    Check(icon==null||Quaternion.Angle(icon.localRotation,iconRest)<.01f,"Decal rotated independently of cap");
    float y=face.localPosition.y;min=Mathf.Min(min,y);max=Mathf.Max(max,y);peakRing=Mathf.Max(peakRing,ring.color.a);
    float next=Mathf.Sign(y-previous);if(Mathf.Abs(y-previous)>.15f){if(direction!=0&&next!=direction)reversals++;direction=next;}previous=y;
    if(!shotPress&&y<rest.y-22){shotPress=true;Shot("contact");}
    if(!shotBreak&&ring.color.a>.3f){shotBreak=true;Shot("break");}
   }
   ExecuteEvents.Execute(root.gameObject,pointer,ExecuteEvents.pointerUpHandler);await UniTask.Delay(350);
   Check(!page.holdDig.IsPressed&&Vector3.Distance(face.localPosition,rest)<.01f&&face.localScale==Vector3.one&&(icon==null||Quaternion.Angle(icon.localRotation,iconRest)<.1f),"Release not restored");
   Check(page.Model.Data.excavations>before&&reversals>=8&&min<rest.y-23&&peakRing>.3f,"Missing mining feedback");Shot("released");
   ExecuteEvents.Execute(root.gameObject,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(root.gameObject,pointer,ExecuteEvents.pointerExitHandler);await UniTask.Delay(250);Check(!page.holdDig.IsPressed&&Vector3.Distance(face.localPosition,rest)<.01f,"Exit not restored");
   ExecuteEvents.Execute(root.gameObject,pointer,ExecuteEvents.pointerDownHandler);page.pausePolicy.RequestPause("DigFeelQA");Check(!page.holdDig.IsPressed&&face.localPosition==rest&&ring.color.a==0,"Pause hard cancel failed");page.pausePolicy.ReleasePause("DigFeelQA");
   File.WriteAllText("PrototypeQA/dig-feel.txt",$"PASS: actual hold mined {page.Model.Data.excavations-before} veins; {reversals} cap reversals; deepest depression {rest.y-min:F1}px; gold ring peak {peakRing:F2}; input and base stay fixed; release/exit/pause restore cap, icon and effect.");
  }
  catch(Exception e){File.WriteAllText("PrototypeQA/dig-feel.txt","FAIL: "+e);Debug.LogException(e);}
  finally{page.holdDig.HardCancel();page.pausePolicy.ReleasePause("DigFeelQA");JsonUtility.FromJsonOverwrite(backup,page.Model.Data);page.OpenMenu(2);page.OnWillEnter(null);PlayerPrefs.SetString("ProjectSS.Play.v3",backup);PlayerPrefs.Save();}
 }
 static void Shot(string name)
 {
  var cam=Camera.main;var old=cam.targetTexture;var rect=cam.rect;var active=RenderTexture.active;var rt=new RenderTexture(720,1280,24);var tex=new Texture2D(720,1280,TextureFormat.RGB24,false);
  try{cam.targetTexture=rt;cam.rect=new Rect(0,0,1,1);Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,720,1280),0,0);tex.Apply();File.WriteAllBytes("PrototypeQA/dig-feel-"+name+".png",tex.EncodeToPNG());}
  finally{cam.targetTexture=old;cam.rect=rect;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
 }
}

