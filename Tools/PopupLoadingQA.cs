using System;using System.IO;using System.Linq;using Cysharp.Threading.Tasks;using UnityEditor;using UnityEngine;using ProjectSS.Expedition;
public static class PopupLoadingQA {
 public static void Execute(){Run().Forget();}
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static async UniTask Ready(string name)=>await UniTask.WaitUntil(()=>PopupManager.CurrentPopup?.PopupName==name&&!PopupManager.IsChanging).Timeout(TimeSpan.FromSeconds(5));
 static async UniTaskVoid Run(){var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();var manager=PopupManager.Instance;
 try{
 PopupManager.Clear();page.pausePolicy.RequestPause("PopupQA");
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/PlayPage.prefab");
 var dependencies=AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(prefab),true);
 Check(!dependencies.Any(p=>p.StartsWith("Assets/Resources/Prefabs/Popups/")&&p.EndsWith(".prefab")),"PlayPage retains popup prefabs");
 Check(new SerializedObject(manager).FindProperty("popupPrefabs")==null,"Preload list still serialized");
 Check(manager.LiveInstanceCount==0,"No preload instances");
 var content=new ExpeditionNoticePopup.Content{pausePolicy=page.pausePolicy,title="테스트",body="지연 로딩 확인",action="확인"};
 var first=PopupManager.ShowAsync<bool>("ExpeditionNoticePopup",content);var duplicate=PopupManager.ShowAsync<bool>("ExpeditionNoticePopup",content);
 await Ready("ExpeditionNoticePopup");Check(PopupManager.OpenedCount==1&&manager.LiveInstanceCount==1,"Duplicate load spawned twice");
 var queued=PopupManager.QueueAsync<bool>("DismantleConfirmationPopup",content);Check(manager.QueuedCount==1&&manager.LiveInstanceCount==1,"Queued popup instantiated early");
 ((ExpeditionNoticePopup)PopupManager.CurrentPopup).Accept();Check(await first&&await duplicate,"Duplicate callers must share result");
 await Ready("DismantleConfirmationPopup");Check(manager.LiveInstanceCount==1&&manager.QueuedCount==0,"Queue loads when shown");
 ((ExpeditionNoticePopup)PopupManager.CurrentPopup).Accept();Check(await queued,"Queued result");await UniTask.Yield();Check(manager.LiveInstanceCount==0&&!PopupManager.IsOpenAny,"Close releases all instance references");
 var pending=PopupManager.ShowAsync<bool>("ItemDetailsPopup",new ItemDetailsPopup.Selection{page=page,index=0,uid=page.Model.EquippedUid(0,0)});PopupManager.Clear();Check(!await pending,"Clear cancels pending show");await UniTask.Delay(200,ignoreTimeScale:true);Check(manager.LiveInstanceCount==0&&!PopupManager.IsOpenAny,"Cleared load must not reopen");
 var reopened=PopupManager.ShowAsync<bool>("ExpeditionNoticePopup",content);await Ready("ExpeditionNoticePopup");var nested=PopupManager.ShowAsync<bool>("DismantleConfirmationPopup",content);await Ready("DismantleConfirmationPopup");Check(PopupManager.OpenedCount==2,"Nested stack preserved");PopupManager.CurrentPopup.OnEscape();Check(!await nested,"X cancel result");await Ready("ExpeditionNoticePopup");PopupManager.CurrentPopup.OnEscape();Check(!await reopened,"Parent cancel result");
 await PopupManager.ReleaseUnusedAssetsAsync();Check(manager.LiveInstanceCount==0,"No retained instances after trim");
 File.WriteAllText("PrototypeQA/popup-loading.txt","PASS: no serialized popup dependencies, on-demand load, duplicate-open deduplication, lazy queue, close/reopen, clear during load, nested results and idle asset collection.");
 }catch(Exception e){File.WriteAllText("PrototypeQA/popup-loading.txt","FAIL: "+e);Debug.LogException(e);}finally{PopupManager.Clear();page.pausePolicy.ReleasePause("PopupQA");}}
}
