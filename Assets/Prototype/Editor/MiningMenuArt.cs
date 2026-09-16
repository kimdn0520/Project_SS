using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition.Editor
{
 public static class MiningMenuArt
 {
  const string Icons="Assets/Layer Lab/2D Minimal-IconPack/Icons/256/";
  static RectTransform Rect(GameObject go,Transform parent,float x,float y,float w,float h)
  {go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
  static TMP_Text Text(PlayPage page,Transform parent,string name,string value,float x,float y,float w,float h,float size,Color color)
  {var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));Rect(go,parent,x,y,w,h);var t=go.GetComponent<TextMeshProUGUI>();t.font=page.depthLabel.font;t.fontSize=size;t.text=value;t.color=color;t.raycastTarget=false;return t;}
  public static void Configure(PlayPage page)
  {
   page.menuDestinations=new[]{1,5,2,4};string[] labels={"용사","채굴","가방","설정"};string[] names={"MenuHeroes","MenuMining","MenuBag","MenuSettings"};
   string[] sprites={null,"Gear_Weapons_Pickaxe_01.png","UI_Common_Bag_01_Brown.png","UI_System_Setting_01.png"};
   for(int i=0;i<4;i++)
   {
    var b=page.menuButtons[i].GetComponent<Button>();b.name=names[i];b.GetComponentInChildren<TMP_Text>(true).text=labels[i];b.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddIntPersistentListener(b.onClick,page.OpenMenu,page.menuDestinations[i]);
    if(sprites[i]!=null)page.menuIcons[i].GetComponent<Image>().sprite=UIAssetPaths.LoadSprite(Icons+sprites[i]);
   }
   page.menuPanels[0].transform.Find("Title").GetComponent<TMP_Text>().text="용사";
   if(page.menuPanels.Length>4&&page.menuPanels[4]!=null)UnityEngine.Object.DestroyImmediate(page.menuPanels[4]);
   Array.Resize(ref page.menuPanels,5);
   var panel=UnityEngine.Object.Instantiate(page.menuPanels[2],page.menuPanels[2].transform.parent);panel.name="MiningUpgradesPanel";panel.transform.SetSiblingIndex(page.menuPanels[3].transform.GetSiblingIndex()+1);page.menuPanels[4]=panel;
   panel.transform.Find("Title").GetComponent<TMP_Text>().text="채굴";var old=panel.transform.Find("ComingSoon");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
   for(int i=0;i<2;i++)
   {
    var go=new GameObject(i==0?"PickaxePowerPreview":"MiningSpeedPreview",typeof(RectTransform),typeof(Image));Rect(go,panel.transform,26,250+i*194,668,166);var image=go.GetComponent<Image>();image.sprite=ExpeditionUIArt.Panel();image.type=Image.Type.Sliced;image.color=new Color(.13f,.22f,.26f);image.raycastTarget=false;
    var iconGo=new GameObject("Icon",typeof(RectTransform),typeof(Image));Rect(iconGo,go.transform,22,29,96,96);var icon=iconGo.GetComponent<Image>();icon.sprite=UIAssetPaths.LoadSprite(Icons+"Gear_Weapons_Pickaxe_01.png");icon.preserveAspect=true;icon.raycastTarget=false;
    Text(page,go.transform,"Title",i==0?"곡괭이 강화":"채광 속도",140,25,350,38,27,new Color(1,.86f,.57f));
    Text(page,go.transform,"Description",i==0?"광맥에 주는 타격 피해 증가":"연속 채광의 타격 간격 감소",140,77,430,38,21,new Color(.76f,.84f,.83f));
    var badge=Text(page,go.transform,"ComingSoon","준비 중",530,29,112,32,18,new Color(.65f,.73f,.74f));badge.alignment=TextAlignmentOptions.Right;
   }
   foreach(var menu in page.menuPanels)foreach(var button in menu.GetComponentsInChildren<Button>(true))if(button.name.StartsWith("BackToMine"))button.GetComponentInChildren<TMP_Text>(true).text="갱도로";
   panel.SetActive(false);EditorUtility.SetDirty(page);
  }
 }
}
