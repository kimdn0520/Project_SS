using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace RuneBreak.Editor
{
    public static partial class RuneProjectBuilder
    {
        private const string Root="Assets/RuneBreak/";
        private static RuneArtLibrary art;
        private static RuneLevelCatalog levels;
        private static TMP_FontAsset font;
        private static readonly Color Ink=RuneArtLibrary.Hex("17282f"),Paper=RuneArtLibrary.Hex("ebd8aa"),Muted=RuneArtLibrary.Hex("9caf9c");
        [MenuItem("Rune Break/Rebuild pages and scenes")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before rebuilding.");
            ImportArt();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cam=Camera();
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            var manager=new GameObject("PageManager").AddComponent<PageManager>();
            Set(manager,"persistAcrossScenes",false);Set(manager,"mainCamera",cam);Set(manager,"pagesRoot",manager.transform);Set(manager,"initialPage",(int)UIPageType.MainPage);
            var play=BuildPlay();var lobby=BuildLobby();
            Directory.CreateDirectory("Assets/Resources/Prefabs");
            var playAsset=PrefabUtility.SaveAsPrefabAsset(play.gameObject,"Assets/Resources/Prefabs/PlayPage.prefab");
            var mainAsset=PrefabUtility.SaveAsPrefabAsset(lobby.gameObject,"Assets/Resources/Prefabs/MainPage.prefab");
            UnityEngine.Object.DestroyImmediate(play.gameObject);UnityEngine.Object.DestroyImmediate(lobby.gameObject);
            play=((GameObject)PrefabUtility.InstantiatePrefab(playAsset)).GetComponent<RunePlayPage>();
            lobby=((GameObject)PrefabUtility.InstantiatePrefab(mainAsset)).GetComponent<RuneLobbyPage>();
            play.pages=manager;lobby.pages=manager;play.SetupRenderCamera(cam);lobby.SetupRenderCamera(cam);
            BuildSettingsPopupPrefab();BuildResultPopupPrefab();ConfigurePopupManager(manager,cam);
            PrefabUtility.RecordPrefabInstancePropertyModifications(play);PrefabUtility.RecordPrefabInstancePropertyModifications(lobby);
            PrefabUtility.RecordPrefabInstancePropertyModifications(play.Canvas);PrefabUtility.RecordPrefabInstancePropertyModifications(lobby.Canvas);
            var so=new SerializedObject(manager);var list=so.FindProperty("scenePages");list.arraySize=2;
            list.GetArrayElementAtIndex(0).FindPropertyRelative("pageType").enumValueIndex=(int)UIPageType.MainPage;list.GetArrayElementAtIndex(0).FindPropertyRelative("instance").objectReferenceValue=lobby;
            list.GetArrayElementAtIndex(1).FindPropertyRelative("pageType").enumValueIndex=(int)UIPageType.PlayPage;list.GetArrayElementAtIndex(1).FindPropertyRelative("instance").objectReferenceValue=play;
            so.ApplyModifiedPropertiesWithoutUndo();
            var sprites=new GameObject("SpriteManager").AddComponent<SpriteManager>();Set(sprites,"persistAcrossScenes",false);Set(sprites,"spriteAtlasData",AssetDatabase.LoadAssetAtPath<SpriteAtlasSO>(Root+"Data/RuneAtlasRegistry.asset"));
            play.gameObject.SetActive(false);lobby.gameObject.SetActive(true);
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/Play.unity");
            BuildSplash();
            AssetDatabase.ImportAsset("Assets/Scenes/Splash.unity",ImportAssetOptions.ForceSynchronousImport);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Splash.unity",true){guid=new GUID(AssetDatabase.AssetPathToGUID("Assets/Scenes/Splash.unity"))},new EditorBuildSettingsScene("Assets/Scenes/Play.unity",true){guid=new GUID(AssetDatabase.AssetPathToGUID("Assets/Scenes/Play.unity"))}};
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;PlayerSettings.defaultScreenWidth=540;PlayerSettings.defaultScreenHeight=960;
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");
            Debug.Log("Rune Break: MainPage + PlayPage prefabs and Splash → Play saved. 48 baked flights; original pixel atlas.");
        }
        private static void ImportArt()
        {
            AssetDatabase.Refresh();
            foreach(string p in Directory.GetFiles(Root+"Art","*.png"))
            {
                var ti=(TextureImporter)AssetImporter.GetAtPath(p);if(ti==null)continue;
                ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=1;ti.filterMode=FilterMode.Point;ti.mipmapEnabled=false;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.alphaIsTransparency=true;
                var settings=new TextureImporterSettings();ti.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;ti.SetTextureSettings(settings);ti.SaveAndReimport();
            }
            art=LoadOrCreate<RuneArtLibrary>(Root+"Data/RuneArt.asset");
            art.cabinets=Enumerable.Range(0,3).Select(i=>Sprite("cabinet_"+i+"_textless")).ToArray();
            art.runes=(from c in new[]{"red","blue","gold","green"} from d in new[]{"N","E","S","W","gem"} select Sprite("rune_"+c+"_"+d)).ToArray();
            art.white=Sprite("white");art.slot=Sprite("slot");art.fullSlot=Sprite("slot_full");art.locked=Sprite("lock");art.sparkle=Sprite("sparkle");art.storedRunes=new[]{"red","blue","gold","green"}.Select(c=>Sprite("stored_"+c)).ToArray();
            art.chainWrap=Sprite("chain_wrap");art.mirrorSlash=Sprite("mirror_slash");art.mirrorBackslash=Sprite("mirror_backslash");art.stackMount=Sprite("stack_mount");
            art.waitSlots=new[]{"red","blue","gold","green"}.Select(c=>Sprite("wait_"+c+"_textless")).ToArray();art.fullWaitSlots=new[]{"red","blue","gold","green"}.Select(c=>Sprite("wait_"+c+"_full_textless")).ToArray();
            art.gates=JsonUtility.FromJson<RuneGateJson>(File.ReadAllText(Root+"Data/gates.json")).gates;foreach(var g in art.gates)g.sprite=Sprite(g.name);
            levels=LoadOrCreate<RuneLevelCatalog>(Root+"Data/RuneLevels.asset");levels.levels=JsonUtility.FromJson<RuneLevelJson>(File.ReadAllText(Root+"Data/levels.json")).levels;
            var atlas=LoadOrCreateAtlas(Root+"Art/RuneBreak.spriteatlas");
            var existing=SpriteAtlasExtensions.GetPackables(atlas);if(existing.Length>0)SpriteAtlasExtensions.Remove(atlas,existing);
            SpriteAtlasExtensions.Add(atlas,Directory.GetFiles(Root+"Art","*.png").Where(p=>!Path.GetFileName(p).StartsWith("reference_")).Select(p=>(UnityEngine.Object)AssetDatabase.LoadAssetAtPath<Sprite>(p)).ToArray());
            SpriteAtlasExtensions.SetPackingSettings(atlas,new SpriteAtlasPackingSettings {padding=2,enableRotation=false,enableTightPacking=false});
            SpriteAtlasExtensions.SetTextureSettings(atlas,new SpriteAtlasTextureSettings {filterMode=FilterMode.Point,generateMipMaps=false,sRGB=true,readable=false});
            var platform=SpriteAtlasExtensions.GetPlatformSettings(atlas,"DefaultTexturePlatform");platform.maxTextureSize=4096;platform.textureCompression=TextureImporterCompression.Uncompressed;SpriteAtlasExtensions.SetPlatformSettings(atlas,platform);
            var registry=LoadOrCreate<SpriteAtlasSO>(Root+"Data/RuneAtlasRegistry.asset");var so=new SerializedObject(registry);var atlases=so.FindProperty("atlases");var punchAtlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(Root+"Art/BlockPunch/BlockPunch.spriteatlas");atlases.arraySize=punchAtlas==null?1:2;atlases.GetArrayElementAtIndex(0).objectReferenceValue=atlas;if(punchAtlas!=null)atlases.GetArrayElementAtIndex(1).objectReferenceValue=punchAtlas;so.ApplyModifiedPropertiesWithoutUndo();
            RuneEnglishAuthoring.EnsureFonts();font=RuneEnglishAuthoring.Medium;
            EditorUtility.SetDirty(art);EditorUtility.SetDirty(levels);EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssets();SpriteAtlasUtility.PackAtlases(new[]{atlas},EditorUserBuildSettings.activeBuildTarget);
        }
        private static T LoadOrCreate<T>(string path) where T:ScriptableObject {var value=AssetDatabase.LoadAssetAtPath<T>(path);if(value==null){value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,path);}return value;}
        private static SpriteAtlas LoadOrCreateAtlas(string path){var v=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);if(v==null){v=new SpriteAtlas();AssetDatabase.CreateAsset(v,path);}return v;}
        private static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/"+name+".png");
        private static Camera Camera(){var go=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));go.tag="MainCamera";var c=go.GetComponent<Camera>();c.orthographic=true;c.orthographicSize=5;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=Ink;c.transform.position=new Vector3(0,0,-20);return c;}
        private static (T page,RectTransform content) Page<T>(string name) where T:SceneBase
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CanvasGroup));var c=go.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.pixelPerfect=true;c.planeDistance=10;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(540,960);scaler.matchWidthOrHeight=.5f;
            var page=go.AddComponent<T>();Set(page,"canvas",c);Set(page,"canvasGroup",go.GetComponent<CanvasGroup>());
            var bg=Image("FullScreenBackdrop",go.transform,0,0,540,960,null,Ink);var br=(RectTransform)bg.transform;br.anchorMin=Vector2.zero;br.anchorMax=Vector2.one;br.offsetMin=br.offsetMax=Vector2.zero;
            var content=Rect("SafeContent",go.transform,0,0,540,960);content.anchorMin=content.anchorMax=new Vector2(.5f,.5f);content.pivot=new Vector2(.5f,.5f);content.anchoredPosition=Vector2.zero;
            var safe=go.AddComponent<RuneSafeLayout>();safe.canvasRect=(RectTransform)go.transform;safe.content=content;
            return(page,content);
        }
        private static RunePlayPage BuildPlay()
        {
            var pair=Page<RunePlayPage>("PlayPage");var p=pair.page;var root=pair.content;p.art=art;p.catalog=levels;
            p.stageNumber=CenteredText("StageNumber",root,150,60,240,36,"STAGE 01",24,Paper);
            p.mechanicHint=Text("MechanicHint",root,42,112,456,42,"",16,Muted);p.mechanicHint.alignment=TextAlignmentOptions.Center;
            var settings=Button("Settings",root,452,44,58,52,"Settings");UnityEventTools.AddPersistentListener(settings.onClick,p.OpenSettings);
            p.cabinet=Rect("Cabinet_Original480x584",root,30,165,480,584);
            p.background=Image("OriginalCabinet",p.cabinet,0,0,480,584,art.cabinets[0],Color.white);
            CenteredText("NorthCompass",p.cabinet,230,27,20,15,"N",12,RuneArtLibrary.Hex("afbfaf"));CenteredText("SouthCompass",p.cabinet,230,415,20,15,"S",12,RuneArtLibrary.Hex("afbfaf"));
            Text("RecastTitle",p.cabinet,47,443,100,20,"RECAST",12,Paper);CenteredText("RecastFooter",p.cabinet,24,563,432,18,"PREPARE / OPEN / BURST",12,RuneArtLibrary.Hex("91a495"));
            p.gates=new[]{Image("NorthSeal",p.cabinet,90,24,300,52,art.Gate(0,0,0,0),Color.white),Image("EastSeal",p.cabinet,394,78,60,300,art.Gate(0,1,0,0),Color.white)};
            p.gateFlashes=new[]{Image("NorthImpact",p.cabinet,96,46,288,26,null,Color.clear),Image("EastImpact",p.cabinet,398,84,26,288,null,Color.clear)};
            var lines=Rect("MechanicConnections",p.cabinet,0,0,480,584);p.mechanicLines=lines.gameObject.AddComponent<RuneMechanicGraphic>();p.mechanicLines.raycastTarget=false;
            p.mirrors=new Image[8];p.mirrorGlints=new Image[8];p.mirrorRects=new RectTransform[8];
            for(int i=0;i<8;i++)
            {
                p.mirrors[i]=Image("FixedMirror_"+i,p.cabinet,0,0,48,48,art.mirrorSlash,Color.white);p.mirrorRects[i]=p.mirrors[i].rectTransform;CenterPivot(p.mirrorRects[i]);
                p.mirrorGlints[i]=Image("MirrorGlint_"+i,p.cabinet,0,0,30,30,art.sparkle,Color.clear);CenterPivot(p.mirrorGlints[i].rectTransform);
            }
            p.chainWraps=new Image[28];p.chainRects=new RectTransform[28];p.stackMounts=new Image[28];p.stackRects=new RectTransform[28];p.stackPreviews=new Image[28];p.previewRects=new RectTransform[28];
            for(int i=0;i<28;i++){p.stackMounts[i]=Image("StackMount_"+i,p.cabinet,0,0,48,48,art.stackMount,Color.white);p.stackRects[i]=p.stackMounts[i].rectTransform;CenterPivot(p.stackRects[i]);}
            p.pathImage=Image("PathPreview",p.cabinet,0,0,2,2,null,Color.clear);p.pathRect=(RectTransform)p.pathImage.transform;CenterPivot(p.pathRect);p.pathImage.enabled=false;
            p.highlight=Image("HintHighlight",p.cabinet,0,0,44,44,null,Color.clear);p.highlightRect=(RectTransform)p.highlight.transform;CenterPivot(p.highlightRect);p.highlight.enabled=false;
            p.blocks=new Image[28];p.blockRects=new RectTransform[28];
            for(int i=0;i<28;i++)
            {
                var img=Image("Rune_"+i.ToString("00"),p.cabinet,0,0,48,48,art.runes[0],Color.white);var rt=(RectTransform)img.transform;CenterPivot(rt);
                var button=img.gameObject.AddComponent<Button>();button.targetGraphic=img;button.transition=Selectable.Transition.None;img.raycastTarget=true;
                var tap=img.gameObject.AddComponent<RuneTapTarget>();tap.page=p;tap.index=i;UnityEventTools.AddPersistentListener(button.onClick,tap.Tap);p.blocks[i]=img;p.blockRects[i]=rt;
            }
            for(int i=0;i<28;i++)
            {
                p.chainWraps[i]=Image("ChainWrap_"+i,p.cabinet,0,0,48,48,art.chainWrap,Color.white);p.chainRects[i]=p.chainWraps[i].rectTransform;CenterPivot(p.chainRects[i]);
                p.stackPreviews[i]=Image("UnderRunePreview_"+i,p.cabinet,0,0,26,26,art.runes[0],Color.white);p.previewRects[i]=p.stackPreviews[i].rectTransform;CenterPivot(p.previewRects[i]);
            }
            p.slots=new RuneSlotView[5];
            for(int i=0;i<5;i++)
            {
                var rt=Rect("RecastSlot_"+(i+1),p.cabinet,66+i*72,477,60,60);var s=rt.gameObject.AddComponent<RuneSlotView>();s.panel=Image("Occupied",rt,0,0,60,60,art.slot,Color.white);s.panel.raycastTarget=true;
                s.button=s.panel.gameObject.AddComponent<Button>();s.button.transition=Selectable.Transition.None;s.button.targetGraphic=s.panel;
                var tap=s.panel.gameObject.AddComponent<RuneTapTarget>();tap.page=p;tap.index=i;tap.slot=true;UnityEventTools.AddPersistentListener(s.button.onClick,tap.Tap);
                s.gem=Image("Gem",rt,30,32,48,48,art.storedRunes[0],Color.white);s.gemRect=(RectTransform)s.gem.transform;CenterPivot(s.gemRect);
                s.label=CenteredText("State",rt,0,45,60,15,"TAP",12,Paper);s.locked=Image("WaitLock",rt,43,3,14,18,art.locked,Color.white);
                s.sparkle=Image("ReadySparkle",rt,47,7,5,5,art.sparkle,Color.white);s.loading=Image("ArrivalProgress",rt,9,51,42,2,null,Color.white);s.loadingRect=(RectTransform)s.loading.transform;
                p.slots[i]=s;s.panel.enabled=s.gem.enabled=s.label.enabled=s.locked.enabled=s.sparkle.enabled=s.loading.enabled=false;
            }
            Image("CountBacking",p.cabinet,404,447,37,12,null,RuneArtLibrary.Hex("2d373b"));p.occupancy=CenteredText("Capacity",p.cabinet,400,444,37,16,"0/5",12,Paper,TextAnchor.MiddleRight);
            var flightRoot=Rect("BakedFlights",p.cabinet,0,0,480,584);p.pool=flightRoot.gameObject.AddComponent<PoolManager>();Set(p.pool,"persistAcrossScenes",false);Set(p.pool,"useBakedPools",true);
            var instances=new PoolObject[48];
            for(int i=0;i<instances.Length;i++)
            {
                var rt=Rect("Flight_"+i,flightRoot,0,0,480,584);var f=rt.gameObject.AddComponent<RuneFlight>();f.poolName="RuneFlights";f.trails=new RectTransform[6];f.trailImages=new Image[6];
                for(int j=0;j<6;j++){var tr=Image("Trail_"+j,rt,0,0,10,10,null,Color.white);f.trailImages[j]=tr;f.trails[j]=(RectTransform)tr.transform;CenterPivot(f.trails[j]);}
                f.image=Image("Rune",rt,0,0,48,48,art.runes[0],Color.white);f.body=(RectTransform)f.image.transform;CenterPivot(f.body);instances[i]=f;rt.gameObject.SetActive(false);
            }
            var poolSo=new SerializedObject(p.pool);var pools=poolSo.FindProperty("bakedPools");pools.arraySize=1;var pool=pools.GetArrayElementAtIndex(0);pool.FindPropertyRelative("name").stringValue="RuneFlights";var refs=pool.FindPropertyRelative("instances");refs.arraySize=instances.Length;for(int i=0;i<instances.Length;i++)refs.GetArrayElementAtIndex(i).objectReferenceValue=instances[i];poolSo.ApplyModifiedPropertiesWithoutUndo();
            var fxrt=Rect("BoundedParticles650",p.cabinet,0,0,480,584);p.effects=fxrt.gameObject.AddComponent<RuneFxGraphic>();p.effects.raycastTarget=false;BuildEffectLabels(p.effects,fxrt);
            for(int i=0;i<3;i++){Panel("ReadyBoosterSlot_"+(i+1),root,91+i*124,842,110,84);CenteredText("Empty",root,91+i*124,850,110,42,"+",24,Muted);Text("Unplanned",root,91+i*124,897,110,20,"Coming Soon",11,Muted).alignment=TextAlignmentOptions.Center;}
            ConfigurePins(p,root);
            p.audioSource=p.gameObject.AddComponent<AudioSource>();p.audioSource.playOnAwake=false;p.audioSource.spatialBlend=0;
            p.sounds=new[]{"launch","relaunch","store","hit","break","blocked","loss","win","chain","reflect","reveal"}.Select(s=>AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/"+s+".wav")).ToArray();

            RunePunchAuthoring.ConfigureIfAvailable(p);p.LoadLevel(0);RuneEnglishAuthoring.StyleTree(p.gameObject);return p;
        }
        private static RuneLobbyPage BuildLobby()
        {
            RuneLobbySkinAuthoring.EnsureSkins();
            var pair=Page<RuneLobbyPage>("MainPage");var p=pair.page;var root=pair.content;p.catalog=levels;
            var scenery=Rect("PixelTealScenery",root,0,0,540,960).gameObject.AddComponent<RuneLobbyScenery>();scenery.raycastTarget=false;
            LobbyPanel("Profile",root,24,24,56,56,"teal");CenteredText("ProfileGlyph",root,25,23,54,50,"R",24,RuneArtLibrary.Hex("fff1af"));
            LobbyPanel("Coins",root,90,26,154,52,"inset");LobbyIcon(root,100,37,24,24,"coin",RuneArtLibrary.Hex("e2b860"));
            CenteredText("Currency",root,132,33,100,32,"0",24,Paper);
            LobbyPanel("Lives",root,254,26,154,52,"inset");LobbyIcon(root,264,37,24,24,"heart",RuneArtLibrary.Hex("d86d75"));
            CenteredText("Energy",root,296,33,100,32,"5",24,Paper);
            var settings=LobbyButton("Settings",root,420,24,96,56,"Settings","teal");UnityEventTools.AddPersistentListener(settings.onClick,p.OpenSettings);
            LobbyPanel("NavigationDock",root,12,832,516,116,"panel");
            p.idleTabSkin=RuneLobbySkinAuthoring.Skin("teal");p.selectedTabSkin=RuneLobbySkinAuthoring.Skin("gold");
            p.tabs=new RuneLobbyTab[5];string[] ids={"shop","leaderboard","home","team","modes"},names={"Shop","Rankings","Home","Team","Modes"};
            for(int i=0;i<5;i++)
            {
                var content=Rect("Tab_"+ids[i],root,24,110,492,728);p.tabs[i]=new RuneLobbyTab{id=ids[i],content=content.gameObject};
                if(i==2)
                {
                    CenteredText("TitleShadow",content,0,28,492,56,"BLOCK CRASH",48,RuneArtLibrary.Hex("513d29"));CenteredText("Title",content,0,22,492,56,"BLOCK CRASH",48,RuneArtLibrary.Hex("fff1af"));
                    Image("TitleRuleLeft",content,138,100,92,2,null,RuneArtLibrary.Hex("516662"));
                    Image("TitleRuleRight",content,262,100,92,2,null,RuneArtLibrary.Hex("516662"));
                    Image("TitleRune",content,242,97,8,8,art.sparkle,RuneArtLibrary.Hex("e2b860"));
                    string[] marks={"flag","gift","scroll","calendar","mail","star"},labels={"Events","Rewards","Missions","Daily","Mail","News"};
                    for(int j=0;j<6;j++){
                        float x=j<3?0:428,y=180+(j%3)*104;
                        var side=LobbyButton((j<3?"LeftShortcut_":"RightShortcut_")+(j%3),content,x,y,64,66,"",j<3?"teal":"blue");
                        LobbyIcon(side.face,0,6,64,42,marks[j],RuneArtLibrary.Hex(j<3?"d4e7ac":"c0e4e5"));
                        Text("Caption",side.face,-4,70,72,22,labels[j],12,Paper).alignment=TextAlignmentOptions.Center;
                        UnityEventTools.AddPersistentListener(side.onClick,p.ShowComingSoon);
                    }
                    var play=LobbyButton("Play",content,84,622,324,88,"Lv.1","gold");p.playLabel=play.GetComponentInChildren<TMP_Text>();p.playLabel.fontSize=36;UnityEventTools.AddPersistentListener(play.onClick,p.Play);
                    Image("LeftRune",play.face,28,34,12,12,art.sparkle,RuneArtLibrary.Hex("fff1af"));
                    Image("RightRune",play.face,284,34,12,12,art.sparkle,RuneArtLibrary.Hex("fff1af"));
                }
                else
                {
                    var panel=LobbyPanel("ComingSoonPanel",content,30,210,432,306,"panel");
                    LobbyIcon(panel.transform,180,35,72,64,ids[i],RuneArtLibrary.Hex("e2b860"),5);
                    CenteredText("Title",panel.transform,20,120,392,48,names[i],36,Paper);
                    CenteredText("Placeholder",panel.transform,20,190,392,64,"Coming soon.\nStay tuned!",24,Muted);
                }
                var button=LobbyButton("Nav_"+ids[i],root,24+i*100,844,92,92,"","teal");p.tabs[i].background=button.image;
                p.tabs[i].icon=LobbyIcon(button.face,0,12,92,36,ids[i],Muted);
                p.tabs[i].label=CenteredText("Label",button.face,0,50,92,26,names[i],12,Paper);
                p.tabs[i].selectedMarker=Image("SelectedMarker",button.face,34,76,24,2,null,RuneArtLibrary.Hex("fff1af")).gameObject;
                UnityEventTools.AddIntPersistentListener(button.onClick,p.SelectTab,i);
            }
            p.comingSoon=Text("ComingSoonNotice",root,115,692,310,30,"Coming soon!",16,Paper);p.comingSoon.alignment=TextAlignmentOptions.Center;p.comingSoon.gameObject.SetActive(false);
            RuneLobbyPunchAuthoring.ConfigureIfAvailable(p);p.SelectTab(2);RuneEnglishAuthoring.StyleTree(p.gameObject);return p;
        }
        private static Image LobbyPanel(string name,Transform parent,float x,float y,float w,float h,string skin)
        {
            var img=Image(name,parent,x,y,w,h,RuneLobbySkinAuthoring.Skin(skin),Color.white);
            img.type=UnityEngine.UI.Image.Type.Sliced;return img;
        }
        private static RuneLobbyIcon LobbyIcon(Transform parent,float x,float y,float w,float h,string kind,Color color,int pixels=3)
        {
            var shadow=Rect("IconShadow",parent,x,y+2,w,h).gameObject.AddComponent<RuneLobbyIcon>();shadow.kind=kind;shadow.pixelSize=pixels;shadow.color=RuneArtLibrary.Hex("101f29");shadow.raycastTarget=false;
            var icon=Rect("Icon",parent,x,y,w,h).gameObject.AddComponent<RuneLobbyIcon>();icon.kind=kind;icon.pixelSize=pixels;icon.color=color;icon.raycastTarget=false;return icon;
        }
        private static RuneRaisedButton LobbyButton(string name,Transform parent,float x,float y,float w,float h,string label,string skin)
        {
            var body=LobbyPanel(name,parent,x,y,w,h,"base");body.raycastTarget=true;
            var face=LobbyPanel("Face",body.transform,0,0,w,h-4,skin);
            var b=body.gameObject.AddComponent<RuneRaisedButton>();b.targetGraphic=face;b.face=face.rectTransform;b.restingPosition=Vector2.zero;
            var colors=b.colors;colors.highlightedColor=Color.white;colors.pressedColor=new Color(.9f,.9f,.9f);colors.fadeDuration=0;b.colors=colors;
            if(!string.IsNullOrEmpty(label))CenteredText("Label",face.transform,12,0,w-24,h-8,label,24,RuneArtLibrary.Hex(skin=="gold"?"513d29":skin=="green"?"293f36":"ebd8aa"));
            return b;
        }
        private static RuneLobbyShowcase BuildLobbyShowcase(Transform parent)
        {
            var rt=Rect("InteractiveRuneShowcase",parent,54,178,480,584);rt.localScale=Vector3.one*.8f;
            var p=rt.gameObject.AddComponent<RuneLobbyShowcase>();p.art=art;p.stage=rt;
            p.gate=Image("LayeredSeal",rt,90,24,300,52,art.Gate(1,0,0,0),Color.white);
            p.runes=new Image[6];p.runeRects=new RectTransform[6];
            for(int i=0;i<6;i++){var b=new RuneBlock{x=i%2==0?1:4,y=2+i/2*2};var point=RuneFlight.Center(b);p.runes[i]=Image("AttractRune_"+i,rt,point.x,point.y,60,60,art.Rune(i<2?"green":"gold","N"),Color.white);p.runeRects[i]=p.runes[i].rectTransform;CenterPivot(p.runeRects[i]);}
            var flightRoot=Rect("BakedLobbyFlights",rt,0,0,480,584);p.pool=flightRoot.gameObject.AddComponent<RuneLobbyFlightPool>();Set(p.pool,"persistAcrossScenes",false);Set(p.pool,"useBakedPools",true);
            var so=new SerializedObject(p.pool);var pools=so.FindProperty("bakedPools");pools.arraySize=1;var entry=pools.GetArrayElementAtIndex(0);entry.FindPropertyRelative("name").stringValue="LobbyFlights";var refs=entry.FindPropertyRelative("instances");refs.arraySize=6;
            for(int i=0;i<6;i++){
                var froot=Rect("LobbyFlight_"+i,flightRoot,0,0,480,584);var f=froot.gameObject.AddComponent<RuneFlight>();f.poolName="LobbyFlights";f.trails=new RectTransform[6];f.trailImages=new Image[6];
                for(int j=0;j<6;j++){var tr=Image("Trail_"+j,froot,0,0,10,10,null,Color.white);f.trailImages[j]=tr;f.trails[j]=tr.rectTransform;CenterPivot(f.trails[j]);}
                f.image=Image("Rune",froot,0,0,48,48,art.runes[0],Color.white);f.body=f.image.rectTransform;CenterPivot(f.body);refs.GetArrayElementAtIndex(i).objectReferenceValue=f;froot.gameObject.SetActive(false);
            }so.ApplyModifiedPropertiesWithoutUndo();
            p.effects=Rect("BoundedLobbyParticles",rt,0,0,480,584).gameObject.AddComponent<RuneFxGraphic>();p.effects.raycastTarget=false;
            return p;
        }
        [MenuItem("Rune Break/Rebuild lobby only")]
        public static void RebuildLobby()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before rebuilding.");
            art=AssetDatabase.LoadAssetAtPath<RuneArtLibrary>(Root+"Data/RuneArt.asset");levels=AssetDatabase.LoadAssetAtPath<RuneLevelCatalog>(Root+"Data/RuneLevels.asset");RuneEnglishAuthoring.EnsureFonts();font=RuneEnglishAuthoring.Medium;
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");var manager=UnityEngine.Object.FindFirstObjectByType<PageManager>();var cam=UnityEngine.Object.FindFirstObjectByType<Camera>();
            var old=UnityEngine.Object.FindFirstObjectByType<RuneLobbyPage>(FindObjectsInactive.Include);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var lobby=BuildLobby();var prefab=PrefabUtility.SaveAsPrefabAsset(lobby.gameObject,"Assets/Resources/Prefabs/MainPage.prefab");UnityEngine.Object.DestroyImmediate(lobby.gameObject);
            lobby=((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<RuneLobbyPage>();lobby.pages=manager;lobby.SetupRenderCamera(cam);PrefabUtility.RecordPrefabInstancePropertyModifications(lobby);PrefabUtility.RecordPrefabInstancePropertyModifications(lobby.Canvas);
            var so=new SerializedObject(manager);var pages=so.FindProperty("scenePages");for(int i=0;i<pages.arraySize;i++){var entry=pages.GetArrayElementAtIndex(i);if(entry.FindPropertyRelative("pageType").enumValueIndex==(int)UIPageType.MainPage)entry.FindPropertyRelative("instance").objectReferenceValue=lobby;}so.ApplyModifiedPropertiesWithoutUndo();
            BuildSettingsPopupPrefab();BuildResultPopupPrefab();ConfigurePopupManager(manager,cam);
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("RUNE LOBBY REBUILT: authored MainPage, existing navigation and floating title/block decoration.");
        }
        public static void ConfigurePins(RunePlayPage p,Transform root)
        {
            // Author each booster as one reusable group so its children move with the safe-area pin.
            var pinList=new System.Collections.Generic.List<RuneSafeLayout.Pin>();
            var stage=(RectTransform)p.stageNumber.transform;pinList.Add(new RuneSafeLayout.Pin{target=stage,position=stage.anchoredPosition,top=true});
            var settings=(RectTransform)root.Find("Settings");pinList.Add(new RuneSafeLayout.Pin{target=settings,position=settings.anchoredPosition,top=true,right=true});
            for(int i=0;i<3;i++)
            {
                var slot=(RectTransform)root.Find("ReadyBoosterSlot_"+(i+1));
                // Builder-authored placeholder labels originally share the parent. Reparent without runtime discovery.
                foreach(var child in root.Cast<Transform>().ToArray())if((child.name=="Empty"||child.name=="Unplanned")&&Mathf.Approximately(((RectTransform)child).anchoredPosition.x,slot.anchoredPosition.x))
                {var r=(RectTransform)child;var pos=r.anchoredPosition-slot.anchoredPosition;r.SetParent(slot,false);r.anchoredPosition=pos;}
                pinList.Add(new RuneSafeLayout.Pin{target=slot,position=slot.anchoredPosition,bottom=true});
            }
            p.GetComponent<RuneSafeLayout>().pins=pinList.ToArray();
        }
        private static void BuildSplash()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var camera=Camera();
            var go=new GameObject("SplashCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));var c=go.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=10;
            var cs=go.GetComponent<CanvasScaler>();cs.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;cs.referenceResolution=new Vector2(540,960);cs.matchWidthOrHeight=.5f;
            var root=Rect("Content",go.transform,0,0,540,960);root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
            CenteredText("Wordmark",root,0,350,540,60,"BLOCK CRASH",48,Paper);Text("Subtitle",root,0,426,540,40,"BREAK THROUGH",22,Muted).alignment=TextAlignmentOptions.Center;
            Image("LoadingTrack",root,100,529,340,8,null,RuneArtLibrary.Hex("253c40"));var splash=go.AddComponent<RuneSplash>();splash.progress=Image("LoadingFill",root,100,529,340,8,art.white,RuneArtLibrary.Hex("83ae83"));splash.progress.type=UnityEngine.UI.Image.Type.Filled;splash.progress.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;splash.progress.fillAmount=0;
            splash.status=CenteredText("Status",root,0,555,540,20,"OPENING THE CHAMBER",12,Muted);EditorSceneManager.SaveScene(scene,"Assets/Scenes/Splash.unity");
        }
        private static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        private static void CenterPivot(RectTransform r)=>r.pivot=new Vector2(.5f,.5f);
        private static Image Image(string name,Transform parent,float x,float y,float w,float h,Sprite sprite,Color color)
        {
            var rt=Rect(name,parent,x,y,w,h);var img=rt.gameObject.AddComponent<Image>();img.sprite=sprite==null?art.white:sprite;img.color=color;img.raycastTarget=false;return img;
        }
        private static Image Panel(string name,Transform parent,float x,float y,float w,float h)
        {
            var img=Image(name,parent,x,y,w,h,null,RuneArtLibrary.Hex("253c40"));Image("TopEdge",img.transform,0,0,w,2,null,RuneArtLibrary.Hex("76877f"));Image("BottomEdge",img.transform,0,h-3,w,3,null,RuneArtLibrary.Hex("101f29"));return img;
        }
        private static TMP_Text Text(string name,Transform parent,float x,float y,float w,float h,string text,float size,Color color)
        {
            var rt=Rect(name,parent,x,y,w,h);var t=rt.gameObject.AddComponent<TextMeshProUGUI>();RuneEnglishAuthoring.Style(t);t.text=text;t.fontSize=size;t.color=color;t.alignment=TextAlignmentOptions.MidlineLeft;t.textWrappingMode=TextWrappingModes.Normal;return t;
        }
        private static TMP_Text CenteredText(string name,Transform parent,float x,float y,float w,float h,string text,int size,Color color,TextAnchor align=TextAnchor.MiddleCenter)
        {
            var t=Text(name,parent,x,y,w,h,text,size,color);t.fontSize=size;t.textWrappingMode=TextWrappingModes.NoWrap;t.alignment=align==TextAnchor.MiddleRight?TextAlignmentOptions.MidlineRight:align==TextAnchor.MiddleLeft?TextAlignmentOptions.MidlineLeft:TextAlignmentOptions.Midline;return t;
        }
        private static void BuildEffectLabels(RuneFxGraphic fx,Transform parent)
        {
            fx.captionLabels=new TMP_Text[24];fx.captionShadows=new TMP_Text[24];fx.captionRects=new RectTransform[24];
            for(int i=0;i<24;i++)
            {
                var rt=Rect("Caption_"+i,parent,0,0,300,36);CenterPivot(rt);fx.captionRects[i]=rt;
                fx.captionShadows[i]=CenteredText("Shadow",rt,1,1,300,36,"",12,Ink);fx.captionLabels[i]=CenteredText("Label",rt,0,0,300,36,"",12,Paper);
                RuneEnglishAuthoring.Style(fx.captionShadows[i]);RuneEnglishAuthoring.Style(fx.captionLabels[i]);
                fx.captionShadows[i].enabled=fx.captionLabels[i].enabled=false;
            }
            fx.chainLabel=CenteredText("ChainLabel",parent,176,20,128,24,"",12,RuneArtLibrary.Hex("f7dda2"));fx.chainLabel.enabled=false;
        }
        private static Button Button(string name,Transform parent,float x,float y,float w,float h,string label)
        {
            var image=Panel(name,parent,x,y,w,h);image.raycastTarget=true;var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;
            var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.15f,1.15f,1.05f);colors.pressedColor=new Color(.7f,.8f,.7f);colors.disabledColor=new Color(.6f,.6f,.6f,.5f);b.colors=colors;
            Text("Label",image.transform,4,2,w-8,h-4,label,16,Paper).alignment=TextAlignmentOptions.Center;return b;
        }
        private static void Set(UnityEngine.Object target,string name,UnityEngine.Object value){var s=new SerializedObject(target);s.FindProperty(name).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        private static void Set(UnityEngine.Object target,string name,bool value){var s=new SerializedObject(target);s.FindProperty(name).boolValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        private static void Set(UnityEngine.Object target,string name,int value){var s=new SerializedObject(target);s.FindProperty(name).intValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    }
}
