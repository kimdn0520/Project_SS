using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition.Editor
{
    public static class BattleDividerArt
    {
        static Color Hex(string s){ColorUtility.TryParseHtmlString("#"+s,out var c);return c;}
        static void Stripe(Transform root,string name,float y,float h,string hex,Sprite sprite)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(root,false);var im=go.GetComponent<Image>();im.sprite=sprite;im.type=Image.Type.Sliced;im.pixelsPerUnitMultiplier=16;im.color=Hex(hex);im.raycastTarget=false;
            var r=im.rectTransform;r.anchorMin=new Vector2(0,1);r.anchorMax=new Vector2(1,1);r.pivot=new Vector2(.5f,1);r.anchoredPosition=new Vector2(0,-y);r.sizeDelta=new Vector2(0,h);
        }
        public static void Configure(PlayPage page)
        {
            var old=page.Canvas.transform.Find("BattleDivider");var parent=old.parent;int index=old.GetSiblingIndex();Object.DestroyImmediate(old.gameObject);
            var go=new GameObject("BattleDivider",typeof(RectTransform),typeof(RectMask2D),typeof(BattleDividerShimmer));var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.sizeDelta=new Vector2(720,7);
            var sprite=ExpeditionUIArt.Panel();Stripe(rect,"DarkEdge",0,7,"231C17",sprite);Stripe(rect,"GoldBody",1,5,"CD9138",sprite);Stripe(rect,"UpperHighlight",1,1.4f,"FFE7A0",sprite);Stripe(rect,"LowerBronze",5,1,"785025",sprite);
            var light=new GameObject("MovingShine",typeof(RectTransform),typeof(CanvasRenderer),typeof(DividerShineGraphic));light.transform.SetParent(rect,false);var shine=light.GetComponent<DividerShineGraphic>();shine.color=new Color(1,.97f,.80f,.8f);shine.raycastTarget=false;
            var r=(RectTransform)light.transform;r.anchorMin=r.anchorMax=new Vector2(0,.5f);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(-100,0);r.sizeDelta=new Vector2(150,5);
            var so=new SerializedObject(go.GetComponent<BattleDividerShimmer>());so.FindProperty("shine").objectReferenceValue=r;so.ApplyModifiedPropertiesWithoutUndo();
            Directory.CreateDirectory("Assets/Resources/Prefabs/UI");var prefab=PrefabUtility.SaveAsPrefabAsset(go,"Assets/Resources/Prefabs/UI/BattleDivider.prefab");Object.DestroyImmediate(go);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);((RectTransform)instance.transform).anchoredPosition=new Vector2(0,-398);instance.transform.SetSiblingIndex(index);
        }
    }
}
