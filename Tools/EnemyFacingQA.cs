using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ProjectSS.Expedition;
public static class EnemyFacingQA
{
 public static void Execute(){Run().Forget();}
 static async UniTaskVoid Run()
 {
  var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();
  try
  {
   page.pausePolicy.RequestPause("FacingQA");page.OpenMenu(0);
   foreach(var other in page.enemies)other.gameObject.SetActive(false);
   for(int i=0;i<page.enemies.Length;i++)
   {
    var enemy=page.enemies[i];enemy.gameObject.SetActive(true);enemy.transform.position=page.enemyRest[i];enemy.InitializeActor();
    var so=new SerializedObject(enemy);var visual=(Transform)so.FindProperty("motionRoot").objectReferenceValue;var rest=so.FindProperty("restPosition").vector3Value;
    if(visual.localScale.x<=0||so.FindProperty("facingDirection").floatValue!=-1)throw new Exception("Wrong authored facing");
    enemy.SetWalking(true);await UniTask.Delay(160);if(visual.localScale.x<=0)throw new Exception("Walk reversed facing");
    enemy.SetWalking(false);await UniTask.Delay(60);page.enemyHpRoot.gameObject.SetActive(true);page.enemyHpRoot.position=enemy.HpPosition;Shot("enemy-"+i);
    enemy.Attack();await UniTask.Delay(40);if(visual.localPosition.x>=rest.x)throw new Exception("Attack lunges away from heroes");Shot("enemy-"+i+"-attack");await UniTask.Delay(340);
    enemy.Hit(false);await UniTask.Delay(30);if(visual.localPosition.x<=rest.x)throw new Exception("Hit recoil moves toward heroes");await UniTask.Delay(280);
    if(visual.localScale.x<=0)throw new Exception("Combat reversed facing");enemy.gameObject.SetActive(false);
   }
   File.WriteAllText("PrototypeQA/enemy-facing.txt","PASS: mushroom, skeleton and iron golem authored left-facing; walk/attack/hit preserve orientation; attacks move left and recoil moves right.");
  }
  catch(Exception e){File.WriteAllText("PrototypeQA/enemy-facing.txt","FAIL: "+e);Debug.LogException(e);}
  finally{page.pausePolicy.ReleasePause("FacingQA");page.OnWillEnter(null);}
 }
 static void Shot(string name)
 {
  var cam=Camera.main;var old=cam.targetTexture;var rect=cam.rect;var active=RenderTexture.active;var rt=new RenderTexture(720,1280,24);var tex=new Texture2D(720,1280,TextureFormat.RGB24,false);
  try{cam.targetTexture=rt;cam.rect=new Rect(0,0,1,1);Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,720,1280),0,0);tex.Apply();File.WriteAllBytes("PrototypeQA/facing-"+name+".png",tex.EncodeToPNG());}
  finally{cam.targetTexture=old;cam.rect=rect;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
 }
}
