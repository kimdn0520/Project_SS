using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak.Editor
{
    // All hierarchy changes are editor authoring. Existing callbacks and managers stay wired.
    public static class RuneBlockUiAuthoring
    {
        private const string Folder=RuneLobbySkinAuthoring.PunchFolder+"Blocks/";
        private static Sprite Art(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Folder+name+".png");
        private static TMP_FontAsset BodyFont=>RuneEnglishAuthoring.Medium;
        private static TMP_FontAsset TitleFont=>RuneEnglishAuthoring.Bold;
        [MenuItem("Rune Break/Apply Block Crash buttons and popups")]
        public static void Apply()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play mode before authoring.");
            RuneLobbyPunchAuthoring.Apply();
            Edit(RuneProjectBuilder.SettingsPopupPath,root=>ConfigureSettings(root.GetComponent<RuneSettingsPanel>()));
            RuneProjectBuilder.RebuildResultPopup();RuneEnglishAuthoring.Apply();
            AssetDatabase.SaveAssets();Debug.Log("BLOCK CRASH UI: level button, settings and both result states share authored block art.");
        }
        private static void Edit(string path,Action<GameObject> configure)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try{configure(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        public static void ConfigurePlayButton(RuneLobbyPage page)
        {
            var button=page.playLabel.GetComponentInParent<RuneRaisedButton>();
            ConfigureLongButton(button,"gold",372,88,"Lv. 1",38);
            page.playLabel.text="Lv. 1";
            page.playLabel.characterSpacing=.5f;
        }
        public static void ConfigureFrame(Image panel,string title,float width,float height)
        {
            if(RuneSurfaceAuthoring.Available){RuneSurfaceAuthoring.ConfigureFrame(panel,title,width,height);return;}
            if(Art("molded_panel_400")==null)return;
            RemoveEdges(panel.transform);
            panel.sprite=Art("molded_panel_"+(height>440?468:400));panel.type=Image.Type.Simple;panel.color=Color.white;panel.raycastTarget=true;panel.rectTransform.sizeDelta=new Vector2(width,height);
            var plate=MakeImage("TitlePlate",panel.transform);plate.sprite=Art("molded_violet_252_68");plate.type=Image.Type.Simple;plate.raycastTarget=false;Rect(plate.rectTransform,(width-252)/2,-31,252,68);
            RemoveStuds(plate.transform);
            var label=Label("Title",plate.transform,title,title=="BLOCK CRASH"?21:24,title=="BLOCK CRASH");Rect(label.rectTransform,37,5,178,49);
        }
        public static void ConfigureSettings(RuneSettingsPanel popup)
        {
            if(RuneSurfaceAuthoring.Available){RuneSurfaceAuthoring.ConfigureSettings(popup);return;}
            if(Art("molded_panel_400")==null)return;
            popup.transform.Find("Curtain").GetComponent<Image>().color=new Color(.025f,.035f,.085f,.85f);
            ConfigureFrame(popup.panel.GetComponent<Image>(),"Settings",460,400);
            ToggleArt(popup.musicToggle,"music",76);ToggleArt(popup.audioToggle,"sound",170);
            var close=popup.lobbyActions.GetComponent<RuneRaisedButton>();Rect((RectTransform)close.transform,100,286,260,72);ButtonArt(close,"gold",260,72,24);
            Rect((RectTransform)popup.playActions.transform,0,286,460,72);
            var home=popup.playActions.transform.Find("Home").GetComponent<RuneRaisedButton>();Rect((RectTransform)home.transform,28,0,194,72);ButtonArt(home,"violet",194,72,20);
            var resume=popup.playActions.transform.Find("Resume").GetComponent<RuneRaisedButton>();Rect((RectTransform)resume.transform,238,0,194,72);ButtonArt(resume,"gold",194,72,20);
        }
        private static void ToggleArt(Toggle toggle,string iconKind,float y)
        {
            var row=toggle.GetComponent<Image>();row.sprite=Art("popup_row");row.type=Image.Type.Simple;row.color=Color.white;Rect(row.rectTransform,28,y,404,76);
            var box=toggle.transform.Find("Box").GetComponent<Image>();box.sprite=Art("toggle_off");box.type=Image.Type.Simple;box.color=Color.white;box.raycastTarget=false;Rect(box.rectTransform,338,14,48,48);
            var old=box.transform.Find("Check");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var on=MakeImage("OnFace",box.transform);on.sprite=Art("toggle_on");on.type=Image.Type.Simple;on.raycastTarget=false;Rect(on.rectTransform,0,0,48,48);toggle.graphic=on;toggle.targetGraphic=box;toggle.toggleTransition=Toggle.ToggleTransition.None;
            var colors=toggle.colors;colors.normalColor=colors.highlightedColor=Color.white;colors.pressedColor=new Color(.88f,.88f,.95f);colors.fadeDuration=.06f;toggle.colors=colors;
            var label=toggle.transform.Find("Label").GetComponent<TMP_Text>();TextStyle(label,false,22,"eae8ff");label.alignment=TextAlignmentOptions.MidlineLeft;Rect(label.rectTransform,74,17,248,40);
            var t=toggle.transform.Find("AudioIcon");var go=t!=null?t.gameObject:new GameObject("AudioIcon",typeof(RectTransform),typeof(CanvasRenderer),typeof(RuneLobbyIcon));go.transform.SetParent(toggle.transform,false);
            var icon=go.GetComponent<RuneLobbyIcon>();icon.smooth=true;icon.kind=iconKind;icon.color=RuneArtLibrary.Hex("a9bfff");icon.raycastTarget=false;Rect(icon.rectTransform,24,22,32,32);
        }
        private static void ButtonArt(Button button,string skin,float width,float height,float fontSize)
        {
            RemoveEdges(button.transform);
            string variant=skin+"_"+Mathf.RoundToInt(width)+"_"+Mathf.RoundToInt(height-12);
            var body=button.GetComponent<Image>();body.sprite=Art("foundation_"+variant);body.type=Image.Type.Simple;body.color=Color.white;body.raycastTarget=true;
            var labels=button.GetComponentsInChildren<TMP_Text>(true);
            var face=MakeImage("Face",button.transform);face.sprite=Art("molded_"+variant);face.type=Image.Type.Simple;face.raycastTarget=false;Rect(face.rectTransform,0,0,width,height-12);face.transform.SetAsFirstSibling();button.targetGraphic=face;
            if(body.sprite==null||face.sprite==null)throw new InvalidOperationException("Export block UI variant: "+variant);
            var raised=button as RuneRaisedButton;if(raised!=null){raised.face=face.rectTransform;raised.restingPosition=Vector2.zero;raised.pressDepth=5;}
            var colors=button.colors;colors.normalColor=colors.highlightedColor=Color.white;colors.pressedColor=new Color(.88f,.87f,.95f);colors.fadeDuration=.06f;button.colors=colors;
            foreach(var label in labels){label.transform.SetParent(face.transform,false);TextStyle(label,false,fontSize,skin=="gold"?"583305":"f5f0ff");Rect(label.rectTransform,36,2,width-72,height-18);}
            RemoveStuds(face.transform);
        }
        public static Sprite LongButtonSprite(string skin,float width,float height)=>Art("molded_"+skin+"_"+Mathf.RoundToInt(width)+"_"+Mathf.RoundToInt(height));
        public static void ConfigureLongButton(Button button,string skin,float width,float height,string value,float fontSize)
        {
            var sprite=LongButtonSprite(skin,width,height);
            if(sprite==null)throw new InvalidOperationException("Export molded button: "+skin+" "+width+"x"+height);
            var raised=button as RuneRaisedButton;
            if(raised==null)throw new InvalidOperationException("Long buttons require the authored RuneRaisedButton: "+button.name);
            RemoveEdges(button.transform);
            var labels=button.GetComponentsInChildren<TMP_Text>(true);
            var body=button.GetComponent<Image>();body.sprite=null;body.color=Color.clear;body.raycastTarget=true;
            var face=MakeImage("Face",button.transform);face.sprite=sprite;face.type=Image.Type.Simple;face.raycastTarget=false;
            Rect(face.rectTransform,0,0,width,height);
            face.rectTransform.pivot=new Vector2(.5f,.5f);face.rectTransform.anchoredPosition=new Vector2(width*.5f,-height*.5f);face.rectTransform.localScale=Vector3.one;
            face.transform.SetAsFirstSibling();button.targetGraphic=face;button.transition=Selectable.Transition.ColorTint;
            raised.face=face.rectTransform;raised.restingPosition=face.rectTransform.anchoredPosition;raised.pressDepth=0;raised.scalePress=true;raised.pressedScale=.96f;raised.pressDuration=.07f;raised.releaseDuration=.14f;
            var colors=button.colors;colors.normalColor=colors.highlightedColor=colors.selectedColor=Color.white;colors.pressedColor=new Color(.97f,.97f,.97f);colors.fadeDuration=.06f;button.colors=colors;
            foreach(var label in labels)
            {
                label.transform.SetParent(face.transform,false);if(value!=null)label.text=value;
                TextStyle(label,true,fontSize,skin=="green"?"21461a":skin=="violet"?"fff5df":"583305");
                label.fontStyle=FontStyles.Normal;label.alignment=TextAlignmentOptions.MidlineGeoAligned;
                float labelHeight=fontSize+14;Rect(label.rectTransform,36,height*.46f-labelHeight*.5f,width-72,labelHeight);
            }
            RemoveStuds(face.transform);
        }
        private static void RemoveEdges(Transform parent){foreach(string name in new[]{"TopEdge","BottomEdge"}){var edge=parent.Find(name);if(edge!=null)UnityEngine.Object.DestroyImmediate(edge.gameObject);}}
        private static void RemoveStuds(Transform parent){for(int i=0;i<4;i++){var stud=parent.Find("BlockStud_"+i);if(stud!=null)UnityEngine.Object.DestroyImmediate(stud.gameObject);}}
        private static void TextStyle(TMP_Text text,bool rounded,float size,string color)
        {
            text.font=rounded?TitleFont:BodyFont;text.fontSharedMaterial=text.font.material;text.fontSize=size;text.fontStyle=FontStyles.Bold;text.fontWeight=FontWeight.Bold;text.color=RuneArtLibrary.Hex(color);text.enableAutoSizing=false;text.characterSpacing=0;text.margin=Vector4.zero;text.alignment=TextAlignmentOptions.Center;text.textWrappingMode=TextWrappingModes.NoWrap;text.raycastTarget=false;
        }
        private static TMP_Text Label(string name,Transform parent,string value,float size,bool rounded)
        {
            var t=parent.Find(name);var go=t!=null?t.gameObject:new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var text=go.GetComponent<TMP_Text>();text.text=value;TextStyle(text,rounded,size,"fff5d0");return text;
        }
        private static Image MakeImage(string name,Transform parent)
        {
            var t=parent.Find(name);var go=t!=null?t.gameObject:new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.color=Color.white;return image;
        }
        private static void Rect(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    }
}
