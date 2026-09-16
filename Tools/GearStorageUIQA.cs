using System;using System.IO;using System.Linq;using System.Reflection;using Cysharp.Threading.Tasks;using UnityEditor;using UnityEngine;using ProjectSS.Expedition;
public static class GearStorageUIQA {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static async UniTask Wait()=>await UniTask.WaitUntil(()=>!PopupManager.IsChanging).Timeout(TimeSpan.FromSeconds(5));
 static async UniTask Shot(PlayPage p,string name){await UniTask.Delay(150,ignoreTimeScale:true);await UniTask.WaitForEndOfFrame(p);var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("PrototypeQA/gear-"+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
 public static void Execute(){Run().Forget();}
 static async UniTaskVoid Run(){var p=UnityEngine.Object.FindFirstObjectByType<PlayPage>();var old=p.Model;var catalog=p.catalog;var field=typeof(PlayPage).GetField("<Model>k__BackingField",BindingFlags.NonPublic|BindingFlags.Instance);const string key="ProjectSS.Play.v3";bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);ExpeditionCatalog temp=null;
 try{EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();p.pausePolicy.RequestPause("GearQA");
 temp=UnityEngine.Object.Instantiate(catalog);temp.gear[3].randomOptions=new[]{new GearRandomOption{stat=GearOptionStat.Attack,unit=GearOptionUnit.Flat,min=1,max=5,weight=1}};temp.gear[3].maxRandomOptions=1;
 var m=new ExpeditionModel(temp,ExpeditionSave.Fresh(temp.gear.Length),1);m.TryAddGear(3);m.TryAddGear(3);field.SetValue(p,m);p.catalog=temp;p.OpenMenu(2);p.inventoryView.SelectFilter(0);p.Refresh();
 Check(p.inventoryView.capacityLabel.text.Contains("5/100"),"Capacity label");
 var bag=p.inventoryView;Check(bag.FilteredInstanceIds.Count==5,"Individual rows, including duplicates");
 string equipped=m.EquippedUid(0,0);Check(bag.TryGetInstanceRow(equipped,out var worn)&&worn.count.text=="장비 중"&&!worn.dismantle.gameObject.activeSelf,"Equipped badge and no dismantle");
 string selected=m.Copies(3).Last().uid;Check(bag.TryGetInstanceRow(selected,out var row),"Exact copy row");await Shot(p,"storage");
 row.button.onClick.Invoke();await UniTask.WaitUntil(()=>PopupManager.CurrentPopup is ItemDetailsPopup&&!PopupManager.IsChanging).Timeout(TimeSpan.FromSeconds(5));
 var popup=(ItemDetailsPopup)PopupManager.CurrentPopup;Check(popup.SelectedUid==selected,"Exact copy details");
 var window=popup.transform.Find("PopupCommon/Window");Check(window.Find("Contents").GetComponentsInChildren<UnityEngine.UI.Button>(true).Length==0,"Details contains no action buttons");
 var bg=(RectTransform)window.Find("bg");var close=(RectTransform)window.Find("Close");var chrome=window.GetComponent<CommonPopupChrome>();chrome.Apply();var frameTop=bg.TransformPoint(new Vector3(bg.rect.xMax,bg.rect.yMax,0));var closeTop=close.TransformPoint(new Vector3(close.rect.xMax,close.rect.yMax,0));Check(Vector3.Distance(frameTop,closeTop)<2,"X follows common frame corner");await Shot(p,"instance");
 popup.OnEscape();await Wait();
 row.dismantle.onClick.Invoke();await UniTask.WaitUntil(()=>PopupManager.CurrentPopup is ExpeditionNotice&&!PopupManager.IsChanging).Timeout(TimeSpan.FromSeconds(5));var confirm=(ExpeditionNotice)PopupManager.CurrentPopup;var frame=confirm.transform.Find("PopupCommon/Window/bg") as RectTransform;Check(frame.rect.width==500&&frame.rect.height==280,"Compact confirmation size");Check(confirm.PopupName=="DismantleConfirmation","Dedicated confirmation variant");await Shot(p,"confirm");PopupManager.CurrentPopup.OnEscape();await Wait();await UniTask.Yield();Check(m.Instance(selected)!=null,"Cancel preserves item");
 row.dismantle.onClick.Invoke();await UniTask.WaitUntil(()=>PopupManager.CurrentPopup is ExpeditionNotice&&!PopupManager.IsChanging).Timeout(TimeSpan.FromSeconds(5));((ExpeditionNotice)PopupManager.CurrentPopup).Accept();await Wait();await UniTask.Delay(100,ignoreTimeScale:true);
 Check(m.Instance(selected)==null&&m.Data.inventory[3]==1&&bag.FilteredInstanceIds.Count==4,"Only requested copy removed");Check(!m.Dismantle(equipped),"Equipped model protection");
 while(m.StorageUsed(0)<100)m.TryAddGear(3);bag.Refresh();int created=bag.CreatedCellCount;
 for(int n=0;n<=20;n++){bag.scroll.verticalNormalizedPosition=1-n/20f;bag.scroll.onValueChanged.Invoke(bag.scroll.normalizedPosition);await UniTask.Yield();}
 Check(bag.CreatedCellCount==created,"Recycled cells remain bounded");string last=bag.FilteredInstanceIds.Last();Check(bag.TryGetInstanceRow(last,out var recycled),"Last instance bound");
 recycled.button.onClick.Invoke();await UniTask.WaitUntil(()=>PopupManager.CurrentPopup is ItemDetailsPopup&&!PopupManager.IsChanging).Timeout(TimeSpan.FromSeconds(5));Check(((ItemDetailsPopup)PopupManager.CurrentPopup).SelectedUid==last,"Recycled click points to correct instance");PopupManager.CurrentPopup.OnEscape();await Wait();
 Check(JsonUtility.FromJson<ExpeditionSave>(PlayerPrefs.GetString(key)).IsValid(temp.gear.Length),"Persisted save valid");
 File.WriteAllText("PrototypeQA/gear-storage-ui.txt","PASS: individual duplicate rows, equipped badge/protection, read-only exact-instance details, aligned X, cell dismantle cancel/confirm, save, 100 items with "+created+" pooled cells and recycled UID clicks.");
 }catch(Exception e){File.WriteAllText("PrototypeQA/gear-storage-ui.txt","FAIL: "+e);Debug.LogException(e);}finally{PopupManager.Clear();field.SetValue(p,old);p.catalog=catalog;if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();p.pausePolicy.ReleasePause("GearQA");p.OpenMenu(2);p.Refresh();typeof(PlayPage).GetMethod("ApplyEquipment",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,null);if(temp!=null)UnityEngine.Object.Destroy(temp);}}
}


