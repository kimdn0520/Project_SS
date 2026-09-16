using System;using System.Linq;using UnityEditor;using UnityEngine;using UnityEngine.UI;using TMPro;using ProjectSS.Expedition;
public static class ApplyCompactConfirmation {
 static void Rect(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
 public static string Execute(){
 const string common="Assets/Resources/Prefabs/Popups/CommonPopup.prefab";var root=PrefabUtility.LoadPrefabContents(common);
 try{var window=root.transform.Find("Window");var chrome=window.GetComponent<CommonPopupChrome>()??window.gameObject.AddComponent<CommonPopupChrome>();chrome.frame=(RectTransform)window.Find("bg");chrome.close=(RectTransform)window.Find("Close");chrome.Apply();PrefabUtility.SaveAsPrefabAsset(root,common);}finally{PrefabUtility.UnloadPrefabContents(root);}
 foreach(var name in new[]{"ExpeditionNoticePopup","ItemDetailsPopup","EquipmentSelectionPopup","VeinSelectionPopup"}){
 string path="Assets/Resources/Prefabs/Popups/"+name+".prefab";root=PrefabUtility.LoadPrefabContents(path);
 try{var chrome=root.GetComponentInChildren<CommonPopupChrome>(true);if(chrome==null)throw new Exception("Missing inherited CommonPopup chrome: "+name);
 var so=new SerializedObject(chrome.close);foreach(var prop in new[]{"m_AnchorMin","m_AnchorMax","m_Pivot","m_AnchoredPosition","m_SizeDelta"}){var p=so.FindProperty(prop);if(p.prefabOverride)PrefabUtility.RevertPropertyOverride(p,InteractionMode.AutomatedAction);}chrome.Apply();PrefabUtility.SaveAsPrefabAsset(root,path);
 }finally{PrefabUtility.UnloadPrefabContents(root);}}
 const string target="Assets/Resources/Prefabs/Popups/DismantleConfirmationPopup.prefab";
 root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Popups/ExpeditionNoticePopup.prefab"));
 try{root.name="DismantleConfirmationPopup";var notice=root.GetComponent<ExpeditionNoticePopup>();var so=new SerializedObject(notice);so.FindProperty("popupName").stringValue="DismantleConfirmationPopup";
 var title=(TMP_Text)so.FindProperty("titleText").objectReferenceValue;var body=(TMP_Text)so.FindProperty("bodyText").objectReferenceValue;var action=(TMP_Text)so.FindProperty("actionText").objectReferenceValue;so.ApplyModifiedPropertiesWithoutUndo();
 var window=root.transform.Find("CommonPopup/Window");Rect((RectTransform)window.Find("bg"),110,500,500,280);window.Find("Title/TitlePlate").gameObject.SetActive(false);
 Rect(title.rectTransform,140,535,390,40);title.fontSize=27;title.enableAutoSizing=false;title.alignment=TextAlignmentOptions.Left;
 Rect(body.rectTransform,140,592,440,66);body.fontSize=22;body.enableAutoSizing=true;body.fontSizeMin=18;body.fontSizeMax=22;body.alignment=TextAlignmentOptions.Center;
 Rect((RectTransform)action.GetComponentInParent<Button>(true).transform,260,691,200,56);action.fontSize=23;
 window.GetComponent<CommonPopupChrome>().Apply();PrefabUtility.SaveAsPrefabAsset(root,target);
 }finally{UnityEngine.Object.DestroyImmediate(root);}
 AssetDatabase.SaveAssets();return "Compact 500x280 confirmation variant saved; close alignment inherited from CommonPopup.";
 }}

