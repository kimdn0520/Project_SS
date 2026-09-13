using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using ProjectSS.Expedition;
public static class FinalPlayCheck
{
 public static object Execute()
 {
  var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();
  if(page==null||page.Model==null)throw new Exception("No initialized PlayPage");
  if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Play"||PageManager.Instance.PageCount!=1)throw new Exception("Unexpected scene/page flow");
  if(page.GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("SidebarSlot_"))!=6)throw new Exception("Sidebar slot count");
  if(page.GetComponentsInChildren<Transform>(true).Any(t=>new[]{"Title","Kicker","MinerName","BurstCharge","Rhythm","Feedback","Loot"}.Contains(t.name)&&t.gameObject.activeInHierarchy))throw new Exception("Removed screen hints remain");
  page.OpenMenu(1);page.OpenHero(0);if(!page.HandleBack()||!page.heroGrid.activeSelf)throw new Exception("Hero detail back navigation");page.HandleBack();
  return new{scene="Play",page=PageManager.Instance.CurrentPageType.ToString(),prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(page.gameObject),sidebarSlots=6,backNavigation="PASS"};
 }
 public static void Resolutions()
 {
  var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();page.OpenMenu(0);
  Capture(1080,2400,new Rect(0,90,1080,2220),"PrototypeQA/phone-safearea.png");
  Capture(1536,2048,new Rect(0,30,1536,1988),"PrototypeQA/tablet-safearea.png");
 }
 public static object Retry()
 {
  var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();var data=page.Model.Data;string backup=JsonUtility.ToJson(data);int losses=0;
  Action<bool> ended=win=>{if(!win)losses++;};
  try
  {
   page.Model.Retreat();data.cleared=99;data.equipment=new[]{0,-1,-1,-1,1,-1,-1,-1,2,-1,-1,-1};data.autoBattle=true;
   page.OnWillEnter(null);page.Model.BattleEnded+=ended;
   for(int i=0;i<4000&&losses<2;i++)page.TickJourney(.1f);
   if(losses!=2||data.cleared!=99)throw new Exception("Auto retry did not repeat same stage after defeat");
   data.autoBattle=false;for(int i=0;i<100;i++)page.TickJourney(.1f);
   if(page.State!=PlayPage.Journey.Waiting)throw new Exception("Retry did not stop after auto disabled");
   return new {defeats=losses,stage="10-10",retry="PASS",autoOff="PASS"};
  }
  finally{page.Model.BattleEnded-=ended;page.Model.Retreat();JsonUtility.FromJsonOverwrite(backup,data);page.OnWillEnter(null);page.SendMessage("Persist");}
 }
 static void Capture(int width,int height,Rect safe,string path)
 {
  var cam=Camera.main;var oldTarget=cam.targetTexture;var oldRect=cam.rect;var oldAspect=cam.aspect;var active=RenderTexture.active;
  var rt=new RenderTexture(width,height,24);var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
  try
  {
   float aspect=720f/1280f;
   if(safe.width/safe.height>aspect){float w=safe.height*aspect;safe.x+=(safe.width-w)*.5f;safe.width=w;}
   else{float h=safe.width/aspect;safe.y+=(safe.height-h)*.5f;safe.height=h;}
   cam.targetTexture=rt;cam.rect=new Rect(safe.x/width,safe.y/height,safe.width/width,safe.height/height);cam.aspect=aspect;
   RenderTexture.active=rt;GL.Clear(true,true,Color.black);Canvas.ForceUpdateCanvases();cam.Render();
   texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());
  }
  finally{cam.targetTexture=oldTarget;cam.rect=oldRect;cam.aspect=oldAspect;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);Canvas.ForceUpdateCanvases();}
 }
 public static void Chest(){ChestAsync().Forget();}
 static async UniTaskVoid ChestAsync()
 {
  var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();string backup=JsonUtility.ToJson(page.Model.Data);
  var game=System.Type.GetType("UnityEditor.GameView, UnityEditor");EditorWindow.GetWindow(game).Focus();page.pausePolicy.ReleasePause("AppFocusLoss");
  try
  {
   page.OpenMenu(0);page.Model.Data.autoMine=false;page.Model.Data.chest=true;page.miningView.SetChest(true);
   await UniTask.Delay(300,cancellationToken:page.destroyCancellationToken);page.Dig();
   await UniTask.Delay(650,cancellationToken:page.destroyCancellationToken);
   Capture(720,1280,new Rect(0,0,720,1280),"PrototypeQA/chest-final.png");
   await UniTask.Delay(650,cancellationToken:page.destroyCancellationToken);
  }
  finally{if(page!=null){page.Model.Retreat();JsonUtility.FromJsonOverwrite(backup,page.Model.Data);page.OnWillEnter(null);page.SendMessage("Persist");}}
 }
}
