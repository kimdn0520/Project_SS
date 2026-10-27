using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak.Editor
{
    public static class RuneLobbyPunchAuthoring
    {
        private static TMP_FontAsset Korean=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/MalgunGothic SDF.asset");
        private static TMP_FontAsset Rounded=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Arial Rounded Bold SDF.asset");
        [MenuItem("Rune Break/Apply Block Punch to MainPage")]
        public static void Apply()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play mode before authoring.");
            RuneLobbySkinAuthoring.EnsurePunchSkins();
            const string path="Assets/Resources/Prefabs/MainPage.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try{Configure(root.GetComponent<RuneLobbyPage>());PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            var live=UnityEngine.Object.FindFirstObjectByType<RuneLobbyPage>(FindObjectsInactive.Include);
            if(live!=null&&live.gameObject.scene.path=="Assets/Scenes/Play.unity")
            {
                // The older scene pinned the CTA label to y=0. The new two-line face
                // owns this layout in the prefab; retain all manager/camera bindings.
                var position=new SerializedObject(live.playLabel.rectTransform).FindProperty("m_AnchoredPosition");
                if(position.prefabOverride)PrefabUtility.RevertPropertyOverride(position,InteractionMode.AutomatedAction);
                var backdrop=new SerializedObject(live.transform.Find("FullScreenBackdrop").GetComponent<Image>()).FindProperty("m_Color");
                if(backdrop.prefabOverride)PrefabUtility.RevertPropertyOverride(backdrop,InteractionMode.AutomatedAction);
                EditorSceneManager.SaveScene(live.gameObject.scene);
            }
            AssetDatabase.SaveAssets();Debug.Log("BLOCK PUNCH LOBBY: authored glossy surfaces, shared block art, live seal preview, existing navigation/actions preserved.");
        }
        public static void ConfigureIfAvailable(RuneLobbyPage page)
        {
            if(AssetDatabase.LoadAssetAtPath<RuneArtLibrary>(RunePunchAuthoring.ArtPath)==null)return;
            RuneLobbySkinAuthoring.EnsurePunchSkins();Configure(page);
        }
        private static void Font(TMP_Text t,TMP_FontAsset font,float size,Color color)
        {
            t.font=font;t.fontSharedMaterial=font.material;t.fontSize=size;t.fontStyle=FontStyles.Normal;t.fontWeight=FontWeight.Regular;t.color=color;t.enableAutoSizing=false;t.margin=Vector4.zero;t.raycastTarget=false;
        }
        private static void Configure(RuneLobbyPage page)
        {
            var art=AssetDatabase.LoadAssetAtPath<RuneArtLibrary>(RunePunchAuthoring.ArtPath);var root=page.transform.Find("SafeContent");var home=page.tabs[page.homeIndex].content.transform;
            page.punchStyle=true;page.idleTabSkin=RuneLobbySkinAuthoring.PunchSkin("inset");page.selectedTabSkin=RuneLobbySkinAuthoring.PunchSkin("violet");
            page.activeTabText=Color.white;page.idleTabText=RuneArtLibrary.Hex("b5b2d0");page.activeTabIcon=RuneArtLibrary.Hex("ffe68a");page.idleTabIcon=RuneArtLibrary.Hex("aaa8cb");
            page.transform.Find("FullScreenBackdrop").GetComponent<Image>().color=RuneArtLibrary.Hex("111224");
            var scenery=page.GetComponentInChildren<RuneLobbyScenery>(true);scenery.transform.SetParent(page.transform,false);scenery.transform.SetSiblingIndex(1);var sr=scenery.rectTransform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.pivot=new Vector2(.5f,.5f);sr.offsetMin=sr.offsetMax=Vector2.zero;scenery.punchStyle=true;scenery.SetVerticesDirty();
            // Restyle authored surfaces, including existing placeholder/demo content.
            foreach(var img in root.GetComponentsInChildren<Image>(true))
            {
                if(img.type!=Image.Type.Sliced||img.sprite==null)continue;
                string old=img.sprite.name;string skin=old.Contains("gold")?"gold":old.Contains("blue")?"blue":old.Contains("green")?"green":old.Contains("base")?"base":old.Contains("inset")?"inset":old.Contains("panel")?"panel":"violet";
                img.sprite=RuneLobbySkinAuthoring.PunchSkin(skin);img.color=Color.white;img.pixelsPerUnitMultiplier=1;
            }
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))Font(text,Korean,text.fontSize,RuneArtLibrary.Hex("eeeafd"));
            foreach(var icon in root.GetComponentsInChildren<RuneLobbyIcon>(true)){icon.smooth=true;icon.color=RuneArtLibrary.Hex(icon.name=="IconShadow"?"13152f":icon.kind=="coin"?"ffd952":icon.kind=="heart"?"ff7499":"d4cfff");icon.SetVerticesDirty();}
            foreach(var icon in root.GetComponentsInChildren<RuneLobbyIcon>(true).Where(i=>i.transform.parent==root&&(i.kind=="coin"||i.kind=="heart")).ToArray())UnityEngine.Object.DestroyImmediate(icon.gameObject);
            CurrencyIcon(root,"CoinArtwork","coin",95);CurrencyIcon(root,"HeartArtwork","heart",259);
            foreach(var button in root.GetComponentsInChildren<RuneRaisedButton>(true))
            {
                button.pressDepth=3;var c=button.colors;c.normalColor=c.highlightedColor=Color.white;c.pressedColor=new Color(.87f,.87f,.94f);c.fadeDuration=.06f;button.colors=c;
            }
            Font(root.Find("ProfileGlyph").GetComponent<TMP_Text>(),Rounded,25,Color.white);
            var currency=root.Find("Currency").GetComponent<TMP_Text>();Font(currency,Korean,18,RuneArtLibrary.Hex("fff1ae"));
            var energy=root.Find("Energy").GetComponent<TMP_Text>();Font(energy,Korean,18,RuneArtLibrary.Hex("ffd1df"));
            var title=home.Find("Title").GetComponent<TMP_Text>();title.text="BLOCK <color=#FFD84D>CRASH</color>";Font(title,Rounded,48,Color.white);Rect(title.rectTransform,0,10,492,60);
            var shadow=home.Find("TitleShadow").GetComponent<TMP_Text>();shadow.text="BLOCK CRASH";Font(shadow,Rounded,48,RuneArtLibrary.Hex("080c22"));Rect(shadow.rectTransform,0,14,492,60);
            foreach(string name in new[]{"TitleRuleLeft","TitleRuleRight","TitleRune"}){var t=home.Find(name);if(t!=null)t.gameObject.SetActive(false);}
            foreach(string name in new[]{"PunchSubtitle","PunchPreviewFrame","PunchPreviewTitle","PunchPreviewHint"})Remove(home,name);
            Deco("PunchRedDecoration",home,39,113,49,art.Rune("red","E"),-12);Deco("PunchBlueDecoration",home,454,111,44,art.Rune("blue","W"),13);
            var preview=page.showcase;preview.art=art;preview.effects.art=art;preview.flightScale=1.9f;
            Rect(preview.stage,49.2f,127,480,584);preview.stage.localScale=Vector3.one*.82f;
            Rect(preview.gate.rectTransform,88,38,304,42);preview.gate.sprite=art.longGates[RuneArtLibrary.ColorIndex("green")*2];
            preview.layerPreviews=new Image[2];
            for(int i=0;i<2;i++){var strip=Panel("PunchNextLayer_"+i,preview.stage,102,43-i*4,276,3,"blue");strip.type=Image.Type.Simple;strip.sprite=art.white;strip.color=art.MidTone(i==0?"blue":"red");strip.transform.SetSiblingIndex(0);preview.layerPreviews[i]=strip;}
            preview.gateCount=Label("PunchGateCount",preview.stage,218,43,44,32,"2",22,"fff7df",true);
            for(int i=0;i<6;i++)
            {
                float x=120+(i%2==0?1:4)*48,y=108+(1+i/2*2)*48;Remove(preview.stage,"PunchCell_"+i);
                var rt=preview.runeRects[i];rt.sizeDelta=new Vector2(82,82);rt.anchoredPosition=new Vector2(x,-y);preview.runes[i].sprite=art.Rune(i<2?"green":i<4?"blue":"red","N");
            }
            Remove(preview.stage,"TouchToBurst");
            foreach(var graphic in preview.stage.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
            for(int i=0;i<6;i++)
            {
                string name=(i<3?"LeftShortcut_":"RightShortcut_")+(i%3);var t=home.Find(name);if(t==null)continue;
                var button=t.GetComponent<RuneRaisedButton>();Rect((RectTransform)t,i<3?0:436,188+i%3*108,56,62);Rect(button.face,0,0,56,58);
                button.image.sprite=RuneLobbySkinAuthoring.PunchSkin(i<3?"violet":"blue");
                foreach(var icon in button.face.GetComponentsInChildren<RuneLobbyIcon>())Rect(icon.rectTransform,9,9+(icon.name=="IconShadow"?2:0),38,36);
                var caption=button.face.Find("Caption").GetComponent<TMP_Text>();Rect(caption.rectTransform,-8,64,72,22);Font(caption,Korean,12,RuneArtLibrary.Hex("c8c3e5"));
            }
            var play=home.Find("Play").GetComponent<RuneRaisedButton>();Rect((RectTransform)play.transform,60,600,372,100);
            Remove(play.face,"PunchStage");RuneBlockUiAuthoring.ConfigurePlayButton(page);
            var left=play.face.Find("LeftRune");var right=play.face.Find("RightRune");if(left!=null)left.gameObject.SetActive(false);if(right!=null)right.gameObject.SetActive(false);
            foreach(var tab in page.tabs)
            {
                var button=tab.background.GetComponentInParent<RuneRaisedButton>();button.GetComponent<Image>().sprite=RuneLobbySkinAuthoring.PunchSkin("base");
                Font(tab.label,Korean,12,Color.white);tab.icon.smooth=true;
                tab.selectedMarker.GetComponent<Image>().color=RuneArtLibrary.Hex("ffe68a");
            }
            var settings=root.Find("Settings").GetComponent<RuneRaisedButton>();Font(settings.GetComponentInChildren<TMP_Text>(),Korean,18,Color.white);
            Font(page.comingSoon,Korean,16,RuneArtLibrary.Hex("fff1ae"));Rect(page.comingSoon.rectTransform,115,673,310,28);
            var pins=root.Cast<Transform>().OfType<RectTransform>().Where(t=>t.name!="PixelTealScenery"&&(t.anchoredPosition.y>-100||t.name=="NavigationDock"||t.name.StartsWith("Nav_"))).Select(t=>new RuneSafeLayout.Pin{target=t,position=t.anchoredPosition,top=t.anchoredPosition.y>-100,bottom=t.anchoredPosition.y<=-100}).ToList();
            pins.Add(new RuneSafeLayout.Pin{target=(RectTransform)play.transform,position=((RectTransform)play.transform).anchoredPosition,bottom=true});page.GetComponent<RuneSafeLayout>().pins=pins.ToArray();
            page.SelectTab(page.homeIndex);
        }
        private static void CurrencyIcon(Transform root,string name,string kind,float x)
        {
            var image=Panel(name,root,x,31,36,36,"panel");image.type=Image.Type.Simple;image.preserveAspect=true;image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(RuneLobbySkinAuthoring.PunchFolder+"Icons/lobby_"+kind+".png");
            if(image.sprite==null)throw new InvalidOperationException("Export lobby currency icons before authoring: "+kind);
        }
        private static void Remove(Transform parent,string name){var child=parent.Find(name);if(child!=null)UnityEngine.Object.DestroyImmediate(child.gameObject);}
        private static void Rect(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        private static Image Panel(string name,Transform parent,float x,float y,float w,float h,string skin)
        {
            var t=parent.Find(name);var go=t!=null?t.gameObject:new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.sprite=RuneLobbySkinAuthoring.PunchSkin(skin);image.type=Image.Type.Sliced;image.color=Color.white;image.raycastTarget=false;Rect(image.rectTransform,x,y,w,h);return image;
        }
        private static TMP_Text Label(string name,Transform parent,float x,float y,float w,float h,string text,float size,string color,bool rounded=false)
        {
            var t=parent.Find(name);var go=t!=null?t.gameObject:new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var label=go.GetComponent<TMP_Text>();Font(label,rounded?Rounded:Korean,size,RuneArtLibrary.Hex(color));label.text=text;label.alignment=TextAlignmentOptions.Center;label.textWrappingMode=TextWrappingModes.NoWrap;Rect(label.rectTransform,x,y,w,h);return label;
        }
        private static void Deco(string name,Transform parent,float x,float y,float size,Sprite sprite,float rotation)
        {
            var image=Panel(name,parent,x,y,size,size,"panel");image.sprite=sprite;image.type=Image.Type.Simple;image.rectTransform.pivot=new Vector2(.5f,.5f);image.rectTransform.localRotation=Quaternion.Euler(0,0,rotation);
        }
    }
}
