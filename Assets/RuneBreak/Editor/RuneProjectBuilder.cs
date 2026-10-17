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
    public static class RuneProjectBuilder
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
            art.cabinets=Enumerable.Range(0,3).Select(i=>Sprite("cabinet_"+i)).ToArray();
            art.runes=(from c in new[]{"red","blue","gold","green"} from d in new[]{"N","E","S","W","gem"} select Sprite("rune_"+c+"_"+d)).ToArray();
            art.white=Sprite("white");art.slot=Sprite("slot");art.fullSlot=Sprite("slot_full");art.locked=Sprite("lock");art.sparkle=Sprite("sparkle");art.storedRunes=new[]{"red","blue","gold","green"}.Select(c=>Sprite("stored_"+c)).ToArray();
            art.waitSlots=new[]{"red","blue","gold","green"}.Select(c=>Sprite("wait_"+c)).ToArray();art.fullWaitSlots=new[]{"red","blue","gold","green"}.Select(c=>Sprite("wait_"+c+"_full")).ToArray();
            art.gates=JsonUtility.FromJson<RuneGateJson>(File.ReadAllText(Root+"Data/gates.json")).gates;foreach(var g in art.gates)g.sprite=Sprite(g.name);
            art.glyphs=JsonUtility.FromJson<RuneGlyphJson>(File.ReadAllText(Root+"Data/glyphs.json")).glyphs;
            levels=LoadOrCreate<RuneLevelCatalog>(Root+"Data/RuneLevels.asset");levels.levels=JsonUtility.FromJson<RuneLevelJson>(File.ReadAllText(Root+"Data/levels.json")).levels;
            var atlas=LoadOrCreateAtlas(Root+"Art/RuneBreak.spriteatlas");
            var existing=SpriteAtlasExtensions.GetPackables(atlas);if(existing.Length>0)SpriteAtlasExtensions.Remove(atlas,existing);
            SpriteAtlasExtensions.Add(atlas,Directory.GetFiles(Root+"Art","*.png").Where(p=>!Path.GetFileName(p).StartsWith("reference_")).Select(p=>(UnityEngine.Object)AssetDatabase.LoadAssetAtPath<Sprite>(p)).ToArray());
            SpriteAtlasExtensions.SetPackingSettings(atlas,new SpriteAtlasPackingSettings {padding=2,enableRotation=false,enableTightPacking=false});
            SpriteAtlasExtensions.SetTextureSettings(atlas,new SpriteAtlasTextureSettings {filterMode=FilterMode.Point,generateMipMaps=false,sRGB=true,readable=false});
            var platform=SpriteAtlasExtensions.GetPlatformSettings(atlas,"DefaultTexturePlatform");platform.maxTextureSize=4096;platform.textureCompression=TextureImporterCompression.Uncompressed;SpriteAtlasExtensions.SetPlatformSettings(atlas,platform);
            var registry=LoadOrCreate<SpriteAtlasSO>(Root+"Data/RuneAtlasRegistry.asset");var so=new SerializedObject(registry);var atlases=so.FindProperty("atlases");atlases.arraySize=1;atlases.GetArrayElementAtIndex(0).objectReferenceValue=atlas;so.ApplyModifiedPropertiesWithoutUndo();
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"Fonts/RuneKorean.asset");
            if(font==null)
            {
                var source=AssetDatabase.LoadAssetAtPath<Font>(Root+"Fonts/RuneKorean.ttf");font=TMP_FontAsset.CreateFontAsset(source,42,5,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);font.name="RuneKorean";
                AssetDatabase.CreateAsset(font,Root+"Fonts/RuneKorean.asset");AssetDatabase.AddObjectToAsset(font.material,font);foreach(var tex in font.atlasTextures)AssetDatabase.AddObjectToAsset(tex,font);
            }
            string chars=File.ReadAllText(Root+"Data/levels.json")+string.Join("",Directory.GetFiles(Root,"*.cs",SearchOption.AllDirectories).Select(File.ReadAllText));
            font.TryAddCharacters(new string(chars.Distinct().ToArray()),out string missing);EditorUtility.SetDirty(font);EditorUtility.SetDirty(art);EditorUtility.SetDirty(levels);EditorUtility.SetDirty(atlas);
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
            p.stageNumber=Pixel("StageNumber",root,150,66,240,25,"STAGE 01",2,Paper);
            var settings=Button("Settings",root,452,44,58,52,"설정");UnityEventTools.AddPersistentListener(settings.onClick,p.OpenSettings);
            p.cabinet=Rect("Cabinet_Original480x584",root,30,165,480,584);
            p.background=Image("OriginalCabinet",p.cabinet,0,0,480,584,art.cabinets[0],Color.white);
            p.gates=new[]{Image("NorthSeal",p.cabinet,90,24,300,52,art.Gate(0,0,0,0),Color.white),Image("EastSeal",p.cabinet,394,78,60,300,art.Gate(0,1,0,0),Color.white)};
            p.gateFlashes=new[]{Image("NorthImpact",p.cabinet,96,46,288,26,null,Color.clear),Image("EastImpact",p.cabinet,398,84,26,288,null,Color.clear)};
            p.pathImage=Image("PathPreview",p.cabinet,0,0,2,2,null,Color.clear);p.pathRect=(RectTransform)p.pathImage.transform;CenterPivot(p.pathRect);p.pathImage.enabled=false;
            p.highlight=Image("HintHighlight",p.cabinet,0,0,44,44,null,Color.clear);p.highlightRect=(RectTransform)p.highlight.transform;CenterPivot(p.highlightRect);p.highlight.enabled=false;
            p.blocks=new Image[28];p.blockRects=new RectTransform[28];
            for(int i=0;i<28;i++)
            {
                var img=Image("Rune_"+i.ToString("00"),p.cabinet,0,0,48,48,art.runes[0],Color.white);var rt=(RectTransform)img.transform;CenterPivot(rt);
                var button=img.gameObject.AddComponent<Button>();button.targetGraphic=img;button.transition=Selectable.Transition.None;img.raycastTarget=true;
                var tap=img.gameObject.AddComponent<RuneTapTarget>();tap.page=p;tap.index=i;UnityEventTools.AddPersistentListener(button.onClick,tap.Tap);p.blocks[i]=img;p.blockRects[i]=rt;
            }
            p.slots=new RuneSlotView[5];
            for(int i=0;i<5;i++)
            {
                var rt=Rect("RecastSlot_"+(i+1),p.cabinet,66+i*72,477,60,60);var s=rt.gameObject.AddComponent<RuneSlotView>();s.panel=Image("Occupied",rt,0,0,60,60,art.slot,Color.white);s.panel.raycastTarget=true;
                s.button=s.panel.gameObject.AddComponent<Button>();s.button.transition=Selectable.Transition.None;s.button.targetGraphic=s.panel;
                var tap=s.panel.gameObject.AddComponent<RuneTapTarget>();tap.page=p;tap.index=i;tap.slot=true;UnityEventTools.AddPersistentListener(s.button.onClick,tap.Tap);
                s.gem=Image("Gem",rt,30,32,48,48,art.storedRunes[0],Color.white);s.gemRect=(RectTransform)s.gem.transform;CenterPivot(s.gemRect);
                s.label=Pixel("State",rt,0,48,60,9,"TAP",1,Paper);s.locked=Image("WaitLock",rt,43,3,14,18,art.locked,Color.white);
                s.sparkle=Image("ReadySparkle",rt,47,7,5,5,art.sparkle,Color.white);s.loading=Image("ArrivalProgress",rt,9,51,42,2,null,Color.white);s.loadingRect=(RectTransform)s.loading.transform;
                p.slots[i]=s;s.panel.enabled=s.gem.enabled=s.label.enabled=s.locked.enabled=s.sparkle.enabled=s.loading.enabled=false;
            }
            Image("CountBacking",p.cabinet,404,447,37,12,null,RuneArtLibrary.Hex("2d373b"));p.occupancy=Pixel("Capacity",p.cabinet,400,446,37,12,"0/5",1,Paper,TextAnchor.MiddleRight);
            var flightRoot=Rect("BakedFlights",p.cabinet,0,0,480,584);p.pool=flightRoot.gameObject.AddComponent<PoolManager>();Set(p.pool,"persistAcrossScenes",false);Set(p.pool,"useBakedPools",true);
            var instances=new PoolObject[48];
            for(int i=0;i<instances.Length;i++)
            {
                var rt=Rect("Flight_"+i,flightRoot,0,0,480,584);var f=rt.gameObject.AddComponent<RuneFlight>();f.poolName="RuneFlights";f.trails=new RectTransform[6];f.trailImages=new Image[6];
                for(int j=0;j<6;j++){var tr=Image("Trail_"+j,rt,0,0,10,10,null,Color.white);f.trailImages[j]=tr;f.trails[j]=(RectTransform)tr.transform;CenterPivot(f.trails[j]);}
                f.image=Image("Rune",rt,0,0,48,48,art.runes[0],Color.white);f.body=(RectTransform)f.image.transform;CenterPivot(f.body);instances[i]=f;rt.gameObject.SetActive(false);
            }
            var poolSo=new SerializedObject(p.pool);var pools=poolSo.FindProperty("bakedPools");pools.arraySize=1;var pool=pools.GetArrayElementAtIndex(0);pool.FindPropertyRelative("name").stringValue="RuneFlights";var refs=pool.FindPropertyRelative("instances");refs.arraySize=instances.Length;for(int i=0;i<instances.Length;i++)refs.GetArrayElementAtIndex(i).objectReferenceValue=instances[i];poolSo.ApplyModifiedPropertiesWithoutUndo();
            var fxrt=Rect("BoundedParticles650",p.cabinet,0,0,480,584);p.effects=fxrt.gameObject.AddComponent<RuneFxGraphic>();p.effects.art=art;p.effects.raycastTarget=false;
            for(int i=0;i<3;i++){Panel("ReadyBoosterSlot_"+(i+1),root,91+i*124,842,110,84);Pixel("Empty",root,91+i*124,850,110,42,"+",2,Muted);Text("Unplanned",root,91+i*124,897,110,20,"준비 중",11,Muted).alignment=TextAlignmentOptions.Center;}
            ConfigurePins(p,root);
            p.audioSource=p.gameObject.AddComponent<AudioSource>();p.audioSource.playOnAwake=false;p.audioSource.spatialBlend=0;
            p.sounds=new[]{"launch","relaunch","store","hit","break","blocked","loss","win"}.Select(s=>AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/"+s+".wav")).ToArray();
            p.settings=Settings(root);var leave=Button("LeaveLevel",p.settings.transform,64,724,412,46,"홈으로");UnityEventTools.AddPersistentListener(leave.onClick,p.Home);p.result=Result(root,p);
            p.LoadLevel(0);return p;
        }
        private static RuneLobbyPage BuildLobby()
        {
            var pair=Page<RuneLobbyPage>("MainPage");var p=pair.page;var root=pair.content;p.catalog=levels;
            Panel("Profile",root,24,22,52,52);Pixel("ProfileGlyph",root,25,23,50,50,"R",3,Paper);
            Panel("Coins",root,88,28,155,40);Text("Currency",root,100,34,130,28,"코인  0",17,Paper);
            Panel("Lives",root,255,28,157,40);Text("Energy",root,268,34,132,28,"하트  5",17,Paper);
            var settings=Button("Settings",root,428,24,88,48,"설정");UnityEventTools.AddPersistentListener(settings.onClick,p.OpenSettings);
            Panel("ProgressBar",root,24,92,492,46);p.progressLabel=Text("Progress",root,42,100,452,30,"결계의 기록  0 / 6",15,Muted);
            p.tabs=new RuneLobbyTab[5];string[] ids={"shop","leaderboard","home","team","cards"},names={"상점","리더보드","홈","팀","카드"},glyphs={"S","1","H","T","C"};
            for(int i=0;i<5;i++)
            {
                var content=Rect("Tab_"+ids[i],root,24,154,492,700);p.tabs[i]=new RuneLobbyTab{id=ids[i],content=content.gameObject};
                if(i==2)
                {
                    Pixel("Title",content,0,20,492,48,"RUNE BREAK",4,Paper);Text("Subtitle",content,0,76,492,32,"색을 준비하고, 다음 층을 여세요.",17,Muted).alignment=TextAlignmentOptions.Center;
                    var showcase=Image("OriginalPixelBoard",content,46,127,360,438,Sprite("reference_5"),Color.white);
                    for(int j=0;j<3;j++){Panel("FutureEventSlot_"+j,content,417,155+j*94,66,76);Pixel("EventMark",content,418,157+j*94,64,35,new[]{"!","+","?"}[j],2,Paper);Text("EventLabel",content,418,195+j*94,64,24,new[]{"이벤트","보상","소식"}[j],11,Muted).alignment=TextAlignmentOptions.Center;}
                    var play=Button("Play",content,84,585,324,70,"제 1 방  ·  플레이");play.image.color=RuneArtLibrary.Hex("5e7964");p.playLabel=play.GetComponentInChildren<TMP_Text>();p.playLabel.fontSize=25;UnityEventTools.AddPersistentListener(play.onClick,p.Play);
                    Text("ChapterNote",content,0,665,492,26,"CHAMBERS 01 — 06",12,Muted).alignment=TextAlignmentOptions.Center;
                }
                else {Pixel("TabGlyph",content,0,205,492,90,glyphs[i],8,Paper);Text("Title",content,0,320,492,55,names[i],29,Paper).alignment=TextAlignmentOptions.Center;Text("Placeholder",content,35,390,422,80,"준비 중이에요.\n새로운 소식을 기다려 주세요.",18,Muted).alignment=TextAlignmentOptions.Center;}
                var button=Button("Nav_"+ids[i],root,24+i*100,870,92,74,"");p.tabs[i].background=button.image;
                Pixel("Icon",button.transform,0,7,92,32,glyphs[i],3,Paper);p.tabs[i].label=Text("Label",button.transform,0,43,92,25,names[i],13,Muted);p.tabs[i].label.alignment=TextAlignmentOptions.Center;
                UnityEventTools.AddIntPersistentListener(button.onClick,p.SelectTab,i);
            }
            p.settings=Settings(root);p.SelectTab(2);return p;
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
        private static GameObject Settings(Transform root)
        {
            var overlay=Image("SettingsModal",root,0,0,540,960,null,new Color(.04f,.09f,.12f,.9f));overlay.raycastTarget=true;
            var panel=Panel("Panel",overlay.transform,40,230,460,482);var s=overlay.gameObject.AddComponent<RuneSettingsPanel>();
            Text("Title",panel.transform,24,25,412,45,"플레이 설정",25,Paper);
            s.audioToggle=Toggle(panel.transform,"효과음",95);s.reducedToggle=Toggle(panel.transform,"움직임 줄이기",205);s.pathToggle=Toggle(panel.transform,"마우스 경로 미리보기",261);
            Text("VolumeLabel",panel.transform,24,151,110,36,"음량",17,Muted);
            var sliderRoot=Rect("Volume",panel.transform,140,159,285,22);var slider=sliderRoot.gameObject.AddComponent<Slider>();var bg=Image("Track",sliderRoot,0,0,285,22,null,Ink);bg.raycastTarget=true;
            var fill=Image("Fill",sliderRoot,0,0,285,22,null,RuneArtLibrary.Hex("83ae83"));var fillRect=(RectTransform)fill.transform;fillRect.anchorMin=Vector2.zero;fillRect.anchorMax=Vector2.one;fillRect.offsetMin=fillRect.offsetMax=Vector2.zero;fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;slider.fillRect=fillRect;slider.targetGraphic=bg;slider.minValue=0;slider.maxValue=1;slider.value=.45f;s.volume=slider;
            UnityEventTools.AddPersistentListener(s.audioToggle.onValueChanged,s.SetAudio);UnityEventTools.AddPersistentListener(s.reducedToggle.onValueChanged,s.SetReduced);UnityEventTools.AddPersistentListener(s.pathToggle.onValueChanged,s.SetPaths);UnityEventTools.AddPersistentListener(slider.onValueChanged,s.SetVolume);
            Text("Rules",panel.transform,24,324,410,67,"보관은 다섯 칸까지 안전해요.\n겉층 색이 열린 룬만 재발사할 수 있어요.\n시간제한이나 재발사 대기시간은 없어요.",14,Muted);
            var close=Button("Continue",panel.transform,24,411,412,47,"계속 플레이");UnityEventTools.AddPersistentListener(close.onClick,s.Close);overlay.gameObject.SetActive(false);return overlay.gameObject;
        }
        private static Toggle Toggle(Transform parent,string label,float y)
        {
            var rt=Rect(label,parent,24,y,412,38);var t=rt.gameObject.AddComponent<Toggle>();var bg=Image("Box",rt,376,5,30,30,null,Ink);bg.raycastTarget=true;var mark=Image("Check",rt,382,11,18,18,null,RuneArtLibrary.Hex("83ae83"));t.targetGraphic=bg;t.graphic=mark;
            Text("Label",rt,0,2,350,35,label,18,Paper);return t;
        }
        private static GameObject Result(Transform root,RunePlayPage p)
        {
            var overlay=Image("ResultModal",root,0,0,540,960,null,new Color(.04f,.09f,.12f,.9f));overlay.raycastTarget=true;var panel=Panel("Panel",overlay.transform,42,270,456,418);
            Pixel("Banner",panel.transform,20,18,416,32,"RUNE BREAK",3,Paper);
            p.resultTitle=Text("ResultTitle",panel.transform,22,76,412,45,"결계를 돌파했어요!",25,Paper);p.resultTitle.alignment=TextAlignmentOptions.Center;
            p.resultDescription=Text("Description",panel.transform,28,136,400,125,"",17,Muted);p.resultDescription.alignment=TextAlignmentOptions.Center;
            var next=Button("NextOrRetry",panel.transform,26,279,404,48,"다음 방으로");p.resultAction=next.GetComponentInChildren<TMP_Text>();UnityEventTools.AddPersistentListener(next.onClick,p.ResultPrimary);
            var home=Button("Home",panel.transform,26,340,404,48,"홈으로");UnityEventTools.AddPersistentListener(home.onClick,p.Home);p.resultHome=home.gameObject;overlay.gameObject.SetActive(false);return overlay.gameObject;
        }
        private static void BuildSplash()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var camera=Camera();
            var go=new GameObject("SplashCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));var c=go.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=10;
            var cs=go.GetComponent<CanvasScaler>();cs.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;cs.referenceResolution=new Vector2(540,960);cs.matchWidthOrHeight=.5f;
            var root=Rect("Content",go.transform,0,0,540,960);root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
            Pixel("Wordmark",root,0,350,540,60,"RUNE BREAK",5,Paper);Text("Subtitle",root,0,426,540,40,"결계 돌파",22,Muted).alignment=TextAlignmentOptions.Center;
            Image("LoadingTrack",root,100,529,340,8,null,RuneArtLibrary.Hex("253c40"));var splash=go.AddComponent<RuneSplash>();splash.progress=Image("LoadingFill",root,100,529,340,8,art.white,RuneArtLibrary.Hex("83ae83"));splash.progress.type=UnityEngine.UI.Image.Type.Filled;splash.progress.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;splash.progress.fillAmount=0;
            splash.status=Pixel("Status",root,0,555,540,20,"OPENING THE CHAMBER",1,Muted);EditorSceneManager.SaveScene(scene,"Assets/Scenes/Splash.unity");
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
            var rt=Rect(name,parent,x,y,w,h);var t=rt.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=text;t.fontSize=size;t.color=color;t.raycastTarget=false;t.alignment=TextAlignmentOptions.MidlineLeft;t.textWrappingMode=TextWrappingModes.Normal;return t;
        }
        private static RunePixelText Pixel(string name,Transform parent,float x,float y,float w,float h,string text,int size,Color color,TextAnchor align=TextAnchor.MiddleCenter)
        {
            var rt=Rect(name,parent,x,y,w,h);var t=rt.gameObject.AddComponent<RunePixelText>();t.art=art;t.Text=text;t.pixelSize=size;t.color=color;t.alignment=align;t.raycastTarget=false;return t;
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
