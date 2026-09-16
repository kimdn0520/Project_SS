using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition.Editor
{
 public static class WarmUIArt
 {
  static Sprite frame,button;
  static Material frameMaterial,buttonMaterial;
  static void Rect(RectTransform r,float x,float y,float w,float h)
  {r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
  static Material Material(string path,float extent,float radius,bool dark)
  {
   var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(m==null){m=new Material(Shader.Find("ProjectSS/UI/WindowFrame"));AssetDatabase.CreateAsset(m,path);}
   m.SetFloat("_HalfExtent",extent);m.SetFloat("_CornerRadius",radius);m.SetFloat("_UseDarkKey",dark?1:0);EditorUtility.SetDirty(m);return m;
  }
  static void Import()
  {
   const string fp="Assets/Textures/UI/Common/Frames/WarmWindow-v3.png",bp="Assets/Textures/UI/Common/Buttons/JadeAction-v3.png";
   foreach(var path in new[]{fp,bp})
   {
    var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spritePixelsPerUnit=100;
    imp.maxTextureSize=path==bp?256:4096;imp.mipmapEnabled=false;imp.alphaIsTransparency=true;imp.textureCompression=TextureImporterCompression.Uncompressed;
    imp.filterMode=FilterMode.Bilinear;imp.npotScale=TextureImporterNPOTScale.None;
    if(path==fp){imp.spriteImportMode=SpriteImportMode.Single;imp.spriteBorder=new Vector4(180,180,180,180);}
    else{imp.spriteImportMode=SpriteImportMode.Multiple;imp.spritesheet=new[]{new SpriteMetaData{name="Action",rect=new Rect(41,137,2090,481),alignment=0,pivot=new Vector2(.5f,.5f),border=new Vector4(190,100,190,100)}};}
    imp.SaveAndReimport();
   }
   frame=AssetDatabase.LoadAssetAtPath<Sprite>(fp);button=AssetDatabase.LoadAllAssetsAtPath(bp).OfType<Sprite>().First();
   frameMaterial=Material("Assets/Prototype/Art/WarmWindowUI.mat",588,123,false);
   buttonMaterial=Material("Assets/Prototype/Art/JadeActionUI.mat",0,0,true);buttonMaterial.SetFloat("_UseDarkKey",2);
  }
  static void Frame(Image im)
  {im.sprite=frame;im.material=frameMaterial;im.color=Color.white;im.type=Image.Type.Sliced;im.pixelsPerUnitMultiplier=5;if(im.GetComponent<AtlasLocalUV>()==null)im.gameObject.AddComponent<AtlasLocalUV>();}
  static void Style(Button b)
  {
   var im=b.GetComponent<Image>();im.sprite=button;im.material=buttonMaterial;im.color=Color.white;im.type=Image.Type.Sliced;im.pixelsPerUnitMultiplier=8f;
   if(im.GetComponent<AtlasLocalUV>()==null)im.gameObject.AddComponent<AtlasLocalUV>();
   var surface=b.transform.Find("Surface");if(surface!=null)surface.gameObject.SetActive(false);
   b.transition=Selectable.Transition.ColorTint;var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.97f,.85f);colors.pressedColor=new Color(.73f,.79f,.73f);colors.selectedColor=Color.white;colors.disabledColor=new Color(.55f,.61f,.58f);colors.fadeDuration=.08f;b.colors=colors;
   foreach(var text in b.GetComponentsInChildren<TMP_Text>(true))
   {
    var r=text.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);r.offsetMin=new Vector2(14,8);r.offsetMax=new Vector2(-14,-5);
    text.alignment=TextAlignmentOptions.Center;text.color=new Color(1,.96f,.82f);text.fontStyle=FontStyles.Bold;text.fontSize=24;text.enableAutoSizing=true;text.fontSizeMin=16;text.fontSizeMax=24;text.margin=Vector4.zero;text.raycastTarget=false;
   }
  }
  static Button Create(Transform parent,string name,string caption,TMP_FontAsset font,float x,float y,float width,float height)
  {
   var t=parent.Find(name);
   if(t==null){t=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button)).transform;t.SetParent(parent,false);}
   Rect((RectTransform)t,x,y,width,height);var b=t.GetComponent<Button>();b.targetGraphic=t.GetComponent<Image>();
   var text=t.GetComponentInChildren<TMP_Text>(true);
   if(text==null){var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(t,false);text=go.GetComponent<TMP_Text>();}
   text.font=font;text.text=caption;Style(b);return b;
  }
  public static void Configure(PlayPage page)
  {
   Import();
   foreach(var panel in page.menuPanels)
   {
    foreach(var im in panel.GetComponentsInChildren<Image>(true))if(im.name=="Backdrop"||im.name=="BagBackground")Frame(im);
    foreach(var b in panel.GetComponentsInChildren<Button>(true))if(new[]{"Sound","Help","Reset"}.Contains(b.name)||b.name.StartsWith("BackToHeroes"))Style(b);
   }
   AlignHeroBackButtons(page);
   // Mining upgrades are still previews; expose the shared button style without implying a working purchase.
   foreach(var preview in page.menuPanels[4].GetComponentsInChildren<Image>(true).Where(i=>i.name.EndsWith("Preview")))
   {
    var old=preview.transform.Find("ComingSoon");if(old!=null)old.gameObject.SetActive(false);
    var title=preview.transform.Find("Title")?.GetComponent<TMP_Text>();if(title!=null)Rect(title.rectTransform,136,18,440,38);
    var desc=preview.transform.Find("Description")?.GetComponent<TMP_Text>();if(desc!=null){Rect(desc.rectTransform,136,61,440,36);desc.fontSize=18;}
    var b=Create(preview.transform,"UpgradeAction","준비 중",page.depthLabel.font,424,104,166,54);b.interactable=false;
   }
   foreach(var name in new[]{"PopupCommon","ExpeditionNotice","EquipmentSelection","VeinSelection"})
   {
    var path="Assets/Resources/Prefabs/Popups/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
    try
    {
     var window=root.transform.Find(name=="PopupCommon"?"Window":"PopupCommon/Window");Frame(window.Find("bg").GetComponent<Image>());
     foreach(var b in root.GetComponentsInChildren<Button>(true))if(b.name=="Accept"||b.name=="Unequip")Style(b);
     var vein=root.GetComponent<VeinSelectionPopup>();if(vein!=null)Veins(vein,page.depthLabel.font);
     PrefabUtility.SaveAsPrefabAsset(root,path);
    }
    finally{PrefabUtility.UnloadPrefabContents(root);}
   }
   var template=new GameObject("ActionButton",typeof(RectTransform),typeof(Image),typeof(Button));
   var holder=new GameObject("Temporary",typeof(RectTransform));template.transform.SetParent(holder.transform,false);
   Create(holder.transform,"ActionButton","확인",page.depthLabel.font,0,0,180,64);
   PrefabUtility.SaveAsPrefabAsset(template,"Assets/Resources/Prefabs/UI/ActionButton.prefab");Object.DestroyImmediate(holder);
  }
  public static void AlignHeroBackButtons(PlayPage page)
  {
   foreach(var panel in page.heroEquipmentPanels)
    foreach(var b in panel.GetComponentsInChildren<Button>(true).Where(b=>b.name.StartsWith("BackToHeroes")))
    {
     Rect((RectTransform)b.transform,54,190,180,56);
     var label=b.GetComponentInChildren<TMP_Text>(true);
     if(label!=null){label.fontSize=22;label.fontSizeMax=22;label.alignment=TextAlignmentOptions.Center;}
    }
  }
  static void Veins(VeinSelectionPopup popup,TMP_FontAsset font)
  {
   popup.choiceLabels=new TMP_Text[3];popup.conditions=new TMP_Text[3];
   for(int i=0;i<3;i++)
   {
    var row=popup.labels[i].transform.parent;
    var old=row.GetComponent<Button>();if(old!=null){old.onClick=new Button.ButtonClickedEvent();old.enabled=false;}
    row.GetComponent<Image>().raycastTarget=false;
    Rect(popup.labels[i].rectTransform,114,14,420,38);popup.labels[i].fontStyle=FontStyles.Bold;popup.labels[i].fontSize=25;
    var condition=row.Find("Condition");
    if(condition==null){condition=new GameObject("Condition",typeof(RectTransform),typeof(TextMeshProUGUI)).transform;condition.SetParent(row,false);}
    var text=condition.GetComponent<TMP_Text>();text.font=font;text.fontSize=18;text.color=new Color(.36f,.34f,.27f);text.raycastTarget=false;Rect(text.rectTransform,114,61,250,44);popup.conditions[i]=text;
    var b=Create(row,"SelectAction","선택",font,390,68,162,54);b.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddIntPersistentListener(b.onClick,popup.Choose,i);
    popup.choices[i]=b;popup.choiceLabels[i]=b.GetComponentInChildren<TMP_Text>();
   }
  }
 }
}
