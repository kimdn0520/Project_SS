using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition.Editor
{
 public static class DigFeedbackArt
 {
  static RectTransform Rect(GameObject go,Transform parent,float y,float width,float height)
  {go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(0,y);r.sizeDelta=new Vector2(width,height);return r;}
  public static void Configure(PlayPage page)
  {
   var root=page.digButton.transform;var old=root.Find("CapContactShadow");if(old!=null)Object.DestroyImmediate(old.gameObject);old=root.Find("BreakImpactRing");if(old!=null)Object.DestroyImmediate(old.gameObject);
   var shadowGo=new GameObject("CapContactShadow",typeof(RectTransform),typeof(Image));Rect(shadowGo,root,-23,260,91);shadowGo.transform.SetAsFirstSibling();var shadow=shadowGo.GetComponent<Image>();shadow.sprite=ExpeditionUIArt.Circle();shadow.color=new Color(0,0,0,.28f);shadow.raycastTarget=false;
   var ringGo=new GameObject("BreakImpactRing",typeof(RectTransform),typeof(CanvasRenderer),typeof(ButtonImpactRing));Rect(ringGo,root,-9,280,127);var ring=ringGo.GetComponent<ButtonImpactRing>();ring.color=new Color(1,.83f,.36f,0);ring.raycastTarget=false;
   var so=new SerializedObject(page.holdDig);so.FindProperty("capShadow").objectReferenceValue=shadow;so.FindProperty("impactRing").objectReferenceValue=ring;so.FindProperty("pickaxeIcon").objectReferenceValue=root.Find("PressableFace/PickaxeIcon");
   so.FindProperty("pressOffset").vector3Value=new Vector3(0,-18,0);so.FindProperty("pressedScale").vector3Value=new Vector3(.99f,.98f,1);so.ApplyModifiedPropertiesWithoutUndo();
   ConfigureDecal(page);
  }
  public static void ConfigureDecal(PlayPage page)
  {
   PolishedMiningArt.ConfigureDomedCap(page);
   var icon=page.digButton.transform.Find("PressableFace/PickaxeIcon");
   if(icon!=null)Object.DestroyImmediate(icon.gameObject);
   var so=new SerializedObject(page.holdDig);
   so.FindProperty("pickaxeIcon").objectReferenceValue=null;
   so.ApplyModifiedPropertiesWithoutUndo();
  }
 }
}
