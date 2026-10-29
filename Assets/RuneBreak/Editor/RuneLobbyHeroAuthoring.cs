using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak.Editor
{
    public static class RuneLobbyHeroAuthoring
    {
        public const string MaterialPath=RuneEnglishAuthoring.Root+"Fredoka-LobbyTitle.mat";
        [MenuItem("Rune Break/Apply floating lobby title")]
        public static void Apply()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play mode before authoring.");
            const string path="Assets/Resources/Prefabs/MainPage.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try{Configure(root.GetComponent<RuneLobbyPage>());PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();Debug.Log("LOBBY HERO: large floating wordmark and four authored blocks; showcase removed.");
        }
        public static void Configure(RuneLobbyPage page)
        {
            var home=page.tabs[page.homeIndex].content.transform;
            foreach(string name in new[]{"InteractiveRuneShowcase","Title","TitleShadow","TitleRuleLeft","TitleRuleRight","TitleRune","PunchRedDecoration","PunchBlueDecoration","PunchSubtitle","PunchPreviewFrame","PunchPreviewTitle","PunchPreviewHint"})
            {var old=home.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            page.showcase=null;
            var art=AssetDatabase.LoadAssetAtPath<RuneArtLibrary>(RunePunchAuthoring.ArtPath);
            var root=Rect("FloatingBrand",home,0,0,492,570,false);
            var hero=root.GetComponent<RuneLobbyHero>();if(hero==null)hero=root.gameObject.AddComponent<RuneLobbyHero>();hero.page=page;page.hero=hero;
            var title=Rect("TitleGroup",root,246,255,360,208,true);
            // Fredoka's line metrics need taller bounds than its visible capitals.
            // Keep the visible centers 100 units apart while leaving SDF padding.
            Title(title,"BrandBlockTitleShadow","BLOCK",0,-15,360,144,92,"665c7c",false);
            Title(title,"BrandCrashTitleShadow","CRASH",0,85,360,144,96,"99510b",false);
            Title(title,"BrandBlockTitle","BLOCK",0,-20,360,144,92,"fff6df",true);
            Title(title,"BrandCrashTitle","CRASH",0,80,360,144,96,"ffdc28",true);
            var red=Block("RedBlock",root,art.storedRunes[0],111,104,84,-14);
            var blue=Block("BlueBlock",root,art.storedRunes[1],368,96,90,12);
            var gold=Block("GoldBlock",root,art.storedRunes[2],143,435,102,12);
            var green=Block("GreenBlock",root,art.storedRunes[3],348,424,88,-11);
            hero.parts=new[]{
                Part(title,new Vector2(2.5f,5),.6f,7.4f,0),
                Part(red,new Vector2(5,9),2.1f,5.8f,.7f),
                Part(blue,new Vector2(6,11),2.4f,6.7f,2.3f),
                Part(gold,new Vector2(7,12),2.1f,6.2f,3.9f),
                Part(green,new Vector2(5,10),1.8f,5.5f,5.2f)
            };
            hero.ResetPose();root.SetAsFirstSibling();
        }
        private static RuneLobbyHero.FloatingPart Part(RectTransform rect,Vector2 amplitude,float rotation,float period,float phase)
            =>new RuneLobbyHero.FloatingPart{rect=rect,position=rect.anchoredPosition,angle=Mathf.DeltaAngle(0,rect.localEulerAngles.z),amplitude=amplitude,rotation=rotation,period=period,phase=phase};
        private static RectTransform Block(string name,Transform parent,Sprite sprite,float x,float y,float size,float angle)
        {
            var rect=Rect(name,parent,x,y,size,size,true);var image=rect.GetComponent<Image>();if(image==null)image=rect.gameObject.AddComponent<Image>();image.sprite=sprite;image.type=Image.Type.Simple;image.color=Color.white;image.raycastTarget=false;rect.localRotation=Quaternion.Euler(0,0,angle);return rect;
        }
        private static void Title(Transform parent,string name,string value,float x,float y,float w,float h,float size,string color,bool face)
        {
            var rect=Rect(name,parent,x,y,w,h,false);var text=rect.GetComponent<TMP_Text>();if(text==null)text=rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text=value;text.fontSize=size;text.characterSpacing=-1.5f;text.alignment=TextAlignmentOptions.MidlineGeoAligned;text.textWrappingMode=TextWrappingModes.NoWrap;
            StyleTitle(text);text.color=RuneArtLibrary.Hex(color);text.enableVertexGradient=face;text.colorGradientPreset=null;
            if(face){var top=RuneArtLibrary.Hex(name.Contains("Crash")?"ffec69":"fffdf2");var bottom=RuneArtLibrary.Hex(name.Contains("Crash")?"ffc619":"eee0c7");text.colorGradient=new VertexGradient(top,top,bottom,bottom);text.color=Color.white;}
            text.raycastTarget=false;
        }
        public static void StyleTitle(TMP_Text text)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if(material==null)
            {
                material=new Material(RuneEnglishAuthoring.Bold.material){name="Fredoka-LobbyTitle"};
                material.SetFloat("_FaceDilate",.04f);material.SetColor("_OutlineColor",RuneArtLibrary.Hex("17152d"));material.SetFloat("_OutlineWidth",.2f);material.EnableKeyword("OUTLINE_ON");
                AssetDatabase.CreateAsset(material,MaterialPath);
            }
            text.font=RuneEnglishAuthoring.Bold;text.fontSharedMaterial=material;text.fontStyle=FontStyles.Normal;text.fontWeight=FontWeight.Regular;text.enableAutoSizing=false;text.extraPadding=true;text.UpdateMeshPadding();
        }
        private static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h,bool center)
        {
            var old=parent.Find(name);var go=old!=null?old.gameObject:new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=center?new Vector2(.5f,.5f):new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;return rect;
        }
    }
}
