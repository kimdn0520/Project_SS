using System;using System.Linq;using UnityEditor;using UnityEditor.U2D;using UnityEngine;using UnityEngine.UI;using UnityEngine.U2D;using TMPro;using ProjectSS.Expedition;
public static class ApplyGearStorageUI {
 static void Rect(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
 public static string Execute(){
 const string target="Assets/Textures/UI/Icons/Navigation/Dismantle.png";
 if(AssetDatabase.LoadMainAssetAtPath(target)==null)AssetDatabase.CopyAsset("Assets/Space_Exploration_GUI_Kit/Picto_Icons/Dark_Purple/trash-64.png",target);
 var importer=(TextureImporter)AssetImporter.GetAtPath(target);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.maxTextureSize=64;importer.mipmapEnabled=false;importer.SaveAndReimport();var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(target);
 const string path="Assets/Resources/Prefabs/Popups/ItemDetails.prefab";var root=PrefabUtility.LoadPrefabContents(path);
 try{var popup=root.GetComponent<ItemDetailsPopup>();var window=root.transform.Find("PopupCommon/Window");
 foreach(var b in window.Find("Contents").GetComponentsInChildren<Button>(true))UnityEngine.Object.DestroyImmediate(b.gameObject);
 Rect(popup.stats.transform.parent as RectTransform,92,495,532,425);Rect(popup.ownership.rectTransform,92,938,532,32);popup.ownership.alignment=TextAlignmentOptions.Left;
 window.GetComponent<CommonPopupChrome>()?.Apply();
 PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
 const string pagePath="Assets/Resources/Prefabs/PlayPage.prefab";root=PrefabUtility.LoadPrefabContents(pagePath);
 try{var page=root.GetComponent<PlayPage>();foreach(var row in page.inventoryView.gearRows.Concat(page.inventoryView.materialRows)){
 float width=row.root.rect.width;Rect(row.title.rectTransform,120,16,width-225,34);Rect(row.detail.rectTransform,120,55,width-208,52);
 Rect(row.count.rectTransform,width-85,18,75,28);row.count.fontSize=17;row.count.enableAutoSizing=false;row.count.alignment=TextAlignmentOptions.Center;row.count.color=new Color(.12f,.4f,.32f);row.count.raycastTarget=false;
 var child=row.root.Find("Dismantle");var go=child!=null?child.gameObject:new GameObject("Dismantle",typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(row.root,false);Rect((RectTransform)go.transform,width-66,63,44,44);
 // 44px touch area, 26px artwork.
 var hit=go.GetComponent<Image>();hit.color=new Color(1,1,1,0);hit.raycastTarget=true;var button=go.GetComponent<Button>();button.targetGraphic=hit;button.onClick=new Button.ButtonClickedEvent();
 var art=go.transform.Find("Icon");if(art==null){art=new GameObject("Icon",typeof(RectTransform),typeof(Image)).transform;art.SetParent(go.transform,false);}Rect((RectTransform)art,9,9,26,26);var image=art.GetComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;row.dismantle=button;
 }PrefabUtility.SaveAsPrefabAsset(root,pagePath);}finally{PrefabUtility.UnloadPrefabContents(root);}
 SpriteAtlasUtility.PackAtlases(new[]{AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/SpriteAtlas/UIIcons.spriteatlasv2")},EditorUserBuildSettings.activeBuildTarget);AssetDatabase.SaveAssets();return "Individual rows, small dismantle icon, read-only item details and aligned X saved.";
 }}
