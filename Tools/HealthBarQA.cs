using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using ProjectSS.Expedition;
public static class HealthBarQA
{
 public static void Execute(){Run().Forget();}
 static void Check(bool b,string s){if(!b)throw new Exception(s);}
 static async UniTaskVoid Run()
 {
  var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();
  try
  {
   EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();page.pausePolicy.ReleasePause("AppFocusLoss");page.OpenMenu(0);await UniTask.Delay(2500);page.pausePolicy.RequestPause("HealthQA");page.OnWillLeave();await UniTask.Yield();
   var bars=page.heroHealthBars.Concat(new[]{page.enemyHealthBar}).ToArray();page.enemyHpRoot.gameObject.SetActive(true);
   foreach(float value in new[]{1f,.75f,.5f,.1f,.01f,0f})
   {
    foreach(var bar in bars)
    {
     bar.SetValue(value,true);var so=new SerializedObject(bar);var reveal=(RectTransform)so.FindProperty("reveal").objectReferenceValue;var width=so.FindProperty("fullWidth").floatValue;
     Check(bar.Fill.type==Image.Type.Sliced&&bar.Fill.sprite.border.x>0,"Fill is not sliced");Check(Mathf.Abs(reveal.rect.width-width*value)<.01f,"Inaccurate HP width");Check(bar.Fill.rectTransform.rect.width>=8&&Mathf.Abs(bar.Fill.rectTransform.rect.height-10)<.01f,"Caps were compressed");Check(bar.Fill.enabled==(value>0),"Zero HP leaves a colored dot");
    }
    Shot("hp-"+Mathf.RoundToInt(value*100));
   }
   await UniTask.Delay(300);
   foreach(var bar in bars){bar.SetValue(1,true);bar.SetValue(.3f);}
   await UniTask.Delay(50);Check(bars.All(b=>b.Value>.3f&&b.Value<1),"HP transition not smooth");await UniTask.Delay(170);Check(bars.All(b=>Mathf.Abs(b.Value-.3f)<.01f),"HP transition did not settle");Shot("hp-animated");
   foreach(var bar in bars)bar.SetValue(.65f,true);
   var divider=page.Canvas.transform.Find("BattleDivider");var shine=(RectTransform)divider.Find("MovingShine");bool crossed=false;
   Check(divider.GetComponentsInChildren<Graphic>().All(g=>!g.raycastTarget),"Divider intercepts input");
   for(int i=0;i<115;i++){await UniTask.Delay(50);if(!crossed&&shine.anchoredPosition.x>250&&shine.anchoredPosition.x<470){crossed=true;Shot("divider-shine");}}
   Check(crossed,"Gold shine did not sweep across the divider");divider.gameObject.SetActive(false);Check(shine.anchoredPosition.x<0,"Shine did not reset when hidden");divider.gameObject.SetActive(true);
   File.WriteAllText("PrototypeQA/health-divider.txt","PASS: all four HP bars at 100/75/50/10/1/0 percent; sliced caps and accurate masked widths; zero hides fill; tween settles. Gold divider shine crosses center, resets on disable and does not block raycasts.");
  }
  catch(Exception e){File.WriteAllText("PrototypeQA/health-divider.txt","FAIL: "+e);Debug.LogException(e);}
  finally{page.pausePolicy.ReleasePause("HealthQA");page.Refresh();page.OnDidEnter();}
 }
 static void Shot(string name)
 {
  var cam=Camera.main;var old=cam.targetTexture;var rect=cam.rect;var active=RenderTexture.active;var rt=new RenderTexture(720,1280,24);var tex=new Texture2D(720,1280,TextureFormat.RGB24,false);
  try{cam.targetTexture=rt;cam.rect=new Rect(0,0,1,1);Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,720,1280),0,0);tex.Apply();File.WriteAllBytes("PrototypeQA/"+name+".png",tex.EncodeToPNG());}
  finally{cam.targetTexture=old;cam.rect=rect;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
 }
}
