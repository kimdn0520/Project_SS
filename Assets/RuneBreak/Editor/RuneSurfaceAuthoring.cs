using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace RuneBreak.Editor
{
    // Authoring only: material images come from the supplied HTML through Aseprite.
    // References are baked into the existing page, popup, pool and atlas registry.
    public static class RuneSurfaceAuthoring
    {
        public const string Folder="Assets/RuneBreak/Art/BlockPunch/Surface/";
        public const string ArtPath="Assets/RuneBreak/Data/RuneSurfaceArt.asset";
        private static readonly string[] Colors={"red","blue","gold","green"};
        public static bool Available=>File.Exists(Folder+"settings.png");
        private static Sprite Art(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Folder+name+".png");
        private static TMP_FontAsset BodyFont=>RuneEnglishAuthoring.Medium;
        private static TMP_FontAsset TitleFont=>RuneEnglishAuthoring.Bold;

        [MenuItem("Rune Break/Apply Ivory Surface reference")]
        public static void Apply()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play mode before authoring.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);RuneEnglishAuthoring.EnsureFonts();
            foreach(var path in Directory.GetFiles(Folder,"*.png"))
            {
                var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;
                ti.spritePixelsPerUnit=300;ti.filterMode=FilterMode.Point;ti.mipmapEnabled=false;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.alphaIsTransparency=true;ti.maxTextureSize=4096;
                var settings=new TextureImporterSettings();ti.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;ti.SetTextureSettings(settings);ti.SaveAndReimport();
            }
            var art=AssetDatabase.LoadAssetAtPath<RuneArtLibrary>(ArtPath);
            if(art==null){art=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<RuneArtLibrary>(RunePunchAuthoring.ArtPath));AssetDatabase.CreateAsset(art,ArtPath);}
            var cabinet=Art("cabinet_lavender")??Art("cabinet_clean")??Art("cabinet_refined")??Art("cabinet_aligned")??Art("cabinet");art.punchStyle=art.surfaceStyle=true;art.cabinets=new[]{cabinet,cabinet,cabinet};
            art.runes=(from c in Colors from d in new[]{"N","E","S","W","gem"} select Art(d=="gem"?"stored_"+c:"rune_"+c+"_"+d)).ToArray();
            art.storedRunes=Colors.Select(c=>Art("stored_"+c)).ToArray();
            art.longGates=(from c in Colors from d in new[]{"N","E"} select Art("gate_studs_"+c+"_"+d)??Art("gate_"+c+"_"+d)).ToArray();
            art.slot=art.fullSlot=Art("slot");EditorUtility.SetDirty(art);
            var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/RuneBreak/Art/BlockPunch/BlockPunch.spriteatlas");
            var existing=atlas.GetPackables();var additions=Directory.GetFiles(Folder,"*.png").Select(p=>(UnityEngine.Object)AssetDatabase.LoadAssetAtPath<Sprite>(p)).Where(p=>!existing.Contains(p)).ToArray();
            if(additions.Length>0)atlas.Add(additions);
            atlas.SetTextureSettings(new SpriteAtlasTextureSettings{filterMode=FilterMode.Point,generateMipMaps=false,sRGB=true});EditorUtility.SetDirty(atlas);
            Edit("Assets/Resources/Prefabs/PlayPage.prefab",root=>Configure(root.GetComponent<RunePlayPage>(),art));
            Edit(RuneProjectBuilder.SettingsPopupPath,root=>ConfigureSettings(root.GetComponent<RuneSettingsPanel>()));
            RuneProjectBuilder.RebuildResultPopup();RuneEnglishAuthoring.Apply();
            AssetDatabase.SaveAssets();SpriteAtlasUtility.PackAtlases(new[]{atlas},EditorUserBuildSettings.activeBuildTarget);
            Debug.Log("SURFACE APPLIED: exact ivory materials, existing TMP fonts, star-free LevelClear, two audio settings, 48 existing flights.");
        }
        private static void Edit(string path,Action<GameObject> configure)
        {var root=PrefabUtility.LoadPrefabContents(path);try{configure(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
        public static void ConfigureIfAvailable(RunePlayPage page)
        {var art=AssetDatabase.LoadAssetAtPath<RuneArtLibrary>(ArtPath);if(art!=null)Configure(page,art);}
        public static void ConfigureBackdrop(RunePlayPage p)
        {
            var backdrop=p.transform.Find("FullScreenBackdrop").GetComponent<Image>();backdrop.sprite=null;backdrop.color=RuneArtLibrary.Hex("111224");backdrop.type=UnityEngine.UI.Image.Type.Simple;
            var old=backdrop.transform.Find("LobbyScenery");var go=old!=null?old.gameObject:new GameObject("LobbyScenery",typeof(RectTransform),typeof(CanvasRenderer),typeof(RuneLobbyScenery));go.transform.SetParent(backdrop.transform,false);
            var scenery=go.GetComponent<RuneLobbyScenery>();scenery.punchStyle=true;scenery.raycastTarget=false;var rect=scenery.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,.5f);rect.offsetMin=rect.offsetMax=Vector2.zero;scenery.SetVerticesDirty();
            p.stageNumber.color=RuneArtLibrary.Hex("fff5da");var caption=p.stageNumber.transform.Find("LevelCaption");if(caption!=null)caption.GetComponent<TMP_Text>().color=RuneArtLibrary.Hex("b4afd7");
        }
        private static void Configure(RunePlayPage p,RuneArtLibrary art)
        {
            p.art=art;p.effects.art=art;p.mechanicLines.surfaceStyle=true;p.cabinetOrigin=new Vector2(10,-108);
            Rect(p.cabinet,10,108,480,652);p.cabinet.localScale=Vector3.one*(13f/12f);Rect(p.background.rectTransform,0,0,480,652);p.background.sprite=art.cabinets[0];
            ConfigureBackdrop(p);
            foreach(var name in new[]{"NorthCompass","SouthCompass","RecastTitle","RecastFooter","CountBacking"}){var t=p.cabinet.Find(name);if(t!=null)t.gameObject.SetActive(false);}
            p.occupancy.gameObject.SetActive(false);
            // The 64.4-unit sprite includes a shadow; taps stay inside its 56-unit cell.
            foreach(var block in p.blocks)block.raycastPadding=new Vector4(4.2f,4.2f,4.2f,4.2f);
            var root=p.cabinet.parent;
            Style(p.stageNumber,true,38,"fff5da");Rect(p.stageNumber.rectTransform,190,47,160,48);
            var caption=Label("LevelCaption",p.stageNumber.transform,"LEVEL",11,true,"b4afd7");Rect(caption.rectTransform,0,-17,160,20);caption.characterSpacing=5;
            ConfigureSettingsButton(root.Find("Settings").GetComponent<Button>(),35);
            int previews=p.gatePreviews.Length/2;
            for(int side=0;side<2;side++)
            {
                Rect(p.gates[side].rectTransform,side==0?64:RuneFlight.SurfaceEastGateLeft-8,side==0?52:92,side==0?352:44,side==0?44:352);
                Rect(p.gateFlashes[side].rectTransform,side==0?72:RuneFlight.SurfaceEastGateLeft,side==0?60:100,side==0?336:28,side==0?28:336);
                Rect(p.gateCounts[side].rectTransform,side==0?218:RuneFlight.SurfaceEastGateCenter-17,side==0?59:251,side==0?44:34,30);Style(p.gateCounts[side],true,22,"fff9ec");
                RuneGateNumberAuthoring.Style(p.gateCounts[side]);
                for(int j=0;j<previews;j++){float d=(j+1)*3.5f;Rect(p.gatePreviews[side*previews+j].rectTransform,side==0?78:448+d,side==0?61-d:106,side==0?324:3.5f,side==0?3.5f:324);}
            }
            for(int i=0;i<p.slots.Length;i++)
            {
                var s=p.slots[i];Rect((RectTransform)s.transform,79+i*66,551,58,58);Rect(s.panel.rectTransform,0,0,58,58);s.panel.sprite=art.slot;
                s.button.interactable=false;s.panel.raycastTarget=false;
                s.gemRect.sizeDelta=Vector2.one*RuneFlight.SurfaceStoredSpriteSize;s.label.gameObject.SetActive(false);Rect(s.loadingRect,9,51,40,2);
            }
            for(int i=0;i<3;i++)
            {
                var holder=root.Find("ReadyBoosterSlot_"+(i+1));foreach(var child in holder.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                Rect((RectTransform)holder,108+i*112,842,100,88);var body=holder.GetComponent<Image>();body.sprite=Art("button_"+new[]{"blue","plum","aqua"}[i]);body.color=Color.white;body.type=UnityEngine.UI.Image.Type.Simple;body.raycastTarget=false;
                Rect(body.rectTransform,108+i*112,842,80,80);
                var plus=Label("Placeholder",holder,"+",30,true,"f2edff");Rect(plus.rectTransform,12,16,56,44);
            }
            p.pool.transform.GetComponent<RectTransform>().sizeDelta=new Vector2(480,652);p.effects.rectTransform.sizeDelta=new Vector2(480,652);p.mechanicLines.rectTransform.sizeDelta=new Vector2(480,652);
            foreach(var flight in p.pool.GetComponentsInChildren<RuneFlight>(true))if(flight.ribbon!=null)flight.ribbon.rectTransform.sizeDelta=new Vector2(480,652);
            RuneProjectBuilder.ConfigurePins(p,root);ConfigurePlayAlignment(p);if(Art("reward_wallet")!=null)RuneRewardAuthoring.ConfigurePlay(p);p.LoadLevel(0);RuneEnglishAuthoring.StyleTree(p.gameObject);
        }
        public static void ConfigurePlayAlignment(RunePlayPage p)
        {
            ConfigureSettingsButton(p.cabinet.parent.Find("Settings").GetComponent<Button>(),35);
            p.highlight.enabled=p.pathImage.enabled=false;
            foreach(var slot in p.slots){slot.gemRect.anchoredPosition=new Vector2(29,-29);slot.gemRect.sizeDelta=Vector2.one*RuneFlight.SurfaceStoredSpriteSize;slot.gemRect.localScale=Vector3.one;slot.gem.sprite=p.art.storedRunes[0];}
        }
        public static void ConfigureSettingsButton(Button button,float y)
        {
            var sprite=Art("settings_button");if(sprite==null)return;
            Rect((RectTransform)button.transform,444,y,62,62);
            foreach(var child in button.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var body=button.GetComponent<Image>();body.sprite=sprite;body.type=UnityEngine.UI.Image.Type.Simple;body.color=Color.white;body.raycastTarget=true;
            button.targetGraphic=body;button.transition=Selectable.Transition.ColorTint;
            if(button is RuneRaisedButton raised){var face=Image("Face",button.transform,sprite);Rect(face.rectTransform,0,0,62,62);body.color=Color.clear;raised.face=face.rectTransform;raised.restingPosition=Vector2.zero;raised.pressDepth=3;button.targetGraphic=face;}
            var colors=button.colors;colors.normalColor=colors.highlightedColor=Color.white;colors.pressedColor=new Color(.88f,.88f,.88f);button.colors=colors;
        }
        public static void ConfigureFrame(Image panel,string title,float width,float height)
        {
            panel.sprite=Art(title=="Settings"?"settings":"result");panel.type=UnityEngine.UI.Image.Type.Simple;panel.color=Color.white;panel.raycastTarget=true;
            foreach(string name in new[]{"TopEdge","BottomEdge","TitlePlate"}){var old=panel.transform.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            var label=Label("SurfaceTitle",panel.transform,title,title=="Settings"?28:32,title!="Settings","69547f");Rect(label.rectTransform,54,28,width-108,54);
            if(title=="Settings"){Rect(label.rectTransform,54,23,width-108,48);label.alignment=TextAlignmentOptions.MidlineGeoAligned;}
        }
        public static void ConfigureSettings(RuneSettingsPanel popup)
        {
            popup.transform.Find("Curtain").GetComponent<Image>().color=new Color(.21f,.18f,.22f,.61f);
            popup.panel.sizeDelta=new Vector2(440,452);ConfigureFrame(popup.panel.GetComponent<Image>(),"Settings",440,452);
            Toggle(popup.musicToggle,"Music",116,"music");Toggle(popup.audioToggle,"Sound Effects",220,"sound");
            var close=popup.lobbyActions.GetComponent<Button>();Rect((RectTransform)close.transform,50,343,340,76);Button(close,"close",340,76,"Close",24);
            popup.playActions.SetActive(false);popup.lobbyActions.SetActive(true);
        }
        private static void Toggle(Toggle toggle,string value,float y,string kind)
        {
            var row=toggle.GetComponent<Image>();row.sprite=Art("row");row.type=UnityEngine.UI.Image.Type.Simple;row.color=Color.white;row.raycastTarget=true;Rect(row.rectTransform,36,y,368,86);
            var box=toggle.transform.Find("Box").GetComponent<Image>();box.sprite=Art("switch_off");box.type=UnityEngine.UI.Image.Type.Simple;box.color=Color.white;Rect(box.rectTransform,261,18,92,46);
            foreach(var child in box.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var on=Image("OnFace",box.transform,Art("switch_on"));Rect(on.rectTransform,0,0,92,46);toggle.graphic=on;toggle.targetGraphic=row;toggle.toggleTransition=UnityEngine.UI.Toggle.ToggleTransition.None;
            var colors=toggle.colors;colors.normalColor=colors.highlightedColor=Color.white;colors.pressedColor=new Color(.95f,.94f,.92f);toggle.colors=colors;
            var label=toggle.transform.Find("Label").GetComponent<TMP_Text>();label.text=value;Style(label,false,22,"595169");label.alignment=TextAlignmentOptions.MidlineLeft;Rect(label.rectTransform,83,20,170,42);
            var old=toggle.transform.Find("AudioIcon");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var tile=Image("AudioIcon",toggle.transform,Art("button_"+(kind=="music"?"plum":"blue")));Rect(tile.rectTransform,13,16,54,54);
            var glyph=new GameObject("Symbol",typeof(RectTransform),typeof(CanvasRenderer),typeof(RuneLobbyIcon));glyph.transform.SetParent(tile.transform,false);var icon=glyph.GetComponent<RuneLobbyIcon>();icon.smooth=true;icon.kind=kind;icon.color=RuneArtLibrary.Hex("fff8ed");icon.raycastTarget=false;Rect((RectTransform)glyph.transform,kind=="music"?11.5f:13,kind=="music"?11.5f:10,28,28);
        }
        public static void ConfigureResult(RuneResultPanel p)
        {
            RuneRewardAuthoring.Configure(p);
        }
        public static void Button(Button button,string sprite,float w,float h,string value,float size)
        {
            if(sprite=="close"||sprite=="next"||sprite=="home"||sprite=="retry")
            {
                RuneBlockUiAuthoring.ConfigureLongButton(button,sprite=="next"?"green":sprite=="home"||sprite=="retry"?"amber":"gold",w,h,value,size);return;
            }
            var labels=button.GetComponentsInChildren<TMP_Text>(true);var body=button.GetComponent<Image>();body.sprite=Art(sprite);body.type=UnityEngine.UI.Image.Type.Simple;body.color=Color.white;body.raycastTarget=true;
            // Baked button sprites include a deep lower bevel and shadow. Center the glyphs
            // on the colored face, rather than on the texture rectangle or font line metrics.
            float centerX=w*.5f-2.5f,centerY=h*(sprite=="close"?.40f:.383f),labelHeight=size+12;
            foreach(var label in labels){label.transform.SetParent(button.transform,false);if(value!=null)label.text=value;Style(label,false,size,"fffbed");label.alignment=TextAlignmentOptions.MidlineGeoAligned;Rect(label.rectTransform,centerX-(w-32)*.5f,centerY-labelHeight*.5f,w-32,labelHeight);}
            foreach(string name in new[]{"TopEdge","BottomEdge","Face"}){var old=button.transform.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            button.targetGraphic=body;button.transition=Selectable.Transition.ColorTint;
            var raised=button as RuneRaisedButton;if(raised!=null){var face=Image("Face",button.transform,Art(sprite));Rect(face.rectTransform,0,0,w,h);foreach(var label in labels)label.transform.SetParent(face.transform,false);body.color=Color.clear;raised.face=face.rectTransform;raised.restingPosition=Vector2.zero;raised.pressDepth=3;button.targetGraphic=face;}
            var colors=button.colors;colors.normalColor=colors.highlightedColor=Color.white;colors.pressedColor=new Color(.88f,.88f,.88f);colors.fadeDuration=.08f;button.colors=colors;
        }
        private static void Style(TMP_Text label,bool rounded,float size,string color)
        {label.font=rounded?TitleFont:BodyFont;label.fontSharedMaterial=label.font.material;label.fontSize=size;label.fontStyle=FontStyles.Normal;label.fontWeight=FontWeight.Regular;label.color=RuneArtLibrary.Hex(color);label.enableAutoSizing=false;label.characterSpacing=0;label.margin=Vector4.zero;label.textWrappingMode=TextWrappingModes.NoWrap;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;}
        private static TMP_Text Label(string name,Transform parent,string value,float size,bool rounded,string color)
        {var t=parent.Find(name);var go=t!=null?t.gameObject:new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var label=go.GetComponent<TMP_Text>();label.text=value;Style(label,rounded,size,color);return label;}
        private static Image Image(string name,Transform parent,Sprite sprite)
        {var t=parent.Find(name);var go=t!=null?t.gameObject:new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.sprite=sprite;image.color=Color.white;image.raycastTarget=false;return image;}
        private static void Rect(RectTransform r,float x,float y,float w,float h)
        {r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    }
}
