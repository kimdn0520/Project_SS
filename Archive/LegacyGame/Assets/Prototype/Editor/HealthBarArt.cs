using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition.Editor
{
    public static class HealthBarArt
    {
        static Color Hex(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
        static void Ref(Object o,string name,Object value){var so=new SerializedObject(o);so.FindProperty(name).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        static Image Layer(Transform parent,string name,Sprite sprite,string color,float inset)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.sprite=sprite;image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=4;image.color=Hex(color);image.raycastTarget=false;
            var r=image.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*inset;r.offsetMax=-Vector2.one*inset;return image;
        }
        public static void Configure(PlayPage page)
        {
            const string path="Assets/Resources/Prefabs/UI/OverheadHealthBar.prefab";Directory.CreateDirectory("Assets/Resources/Prefabs/UI");
            var go=new GameObject("OverheadHealthBar",typeof(RectTransform),typeof(OverheadHealthBar));var root=(RectTransform)go.transform;root.sizeDelta=new Vector2(100,16);
            var sprite=ExpeditionUIArt.Panel();Layer(root,"Outline",sprite,"111C25",0);Layer(root,"Rim",sprite,"617783",1);Layer(root,"Track",sprite,"23333D",2);
            var mask=new GameObject("Reveal",typeof(RectTransform),typeof(RectMask2D));mask.transform.SetParent(root,false);var reveal=(RectTransform)mask.transform;reveal.anchorMin=reveal.anchorMax=new Vector2(0,.5f);reveal.pivot=new Vector2(0,.5f);reveal.anchoredPosition=new Vector2(3,0);reveal.sizeDelta=new Vector2(94,10);
            var fill=Layer(reveal,"Fill",sprite,"68D7AE",0);var r=fill.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,.5f);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(94,10);
            var bar=go.GetComponent<OverheadHealthBar>();Ref(bar,"reveal",reveal);Ref(bar,"fill",fill);
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,path);Object.DestroyImmediate(go);
            page.heroHealthBars=new OverheadHealthBar[3];
            for(int i=0;i<3;i++){page.heroHealthBars[i]=Replace(page.heroHpRoots[i],prefab,74,"68D7AE");page.heroHpRoots[i]=(RectTransform)page.heroHealthBars[i].transform;page.heroHpBars[i]=page.heroHealthBars[i].Fill;}
            page.enemyHealthBar=Replace(page.enemyHpRoot,prefab,112,"F17E72");page.enemyHpRoot=(RectTransform)page.enemyHealthBar.transform;page.enemyBar=page.enemyHealthBar.Fill;
        }
        static OverheadHealthBar Replace(RectTransform old,GameObject prefab,float width,string color)
        {
            var parent=old.parent;var position=old.position;var name=old.name;int index=old.GetSiblingIndex();Object.DestroyImmediate(old.gameObject);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);instance.name=name;var rect=(RectTransform)instance.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(width,16);rect.position=position;rect.SetSiblingIndex(index);
            var bar=instance.GetComponent<OverheadHealthBar>();var so=new SerializedObject(bar);so.FindProperty("fullWidth").floatValue=width-6;so.ApplyModifiedPropertiesWithoutUndo();bar.Fill.color=Hex(color);bar.SetValue(1,true);return bar;
        }
    }
}
