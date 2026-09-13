using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectSS.Expedition.Editor
{
    /// <summary>All object creation and component discovery happens here, once, in edit mode.</summary>
    public static class ExpeditionBuilder
    {
        public const string ScenePath = "Assets/Scenes/Play.unity";
        private const string Maker = "Assets/Layer Lab/2D Minimal-CharacterMaker/";
        private const string Pack = Maker + "Extenstions/Parts Pack Base/";
        private const string Icons = "Assets/Layer Lab/2D Minimal-IconPack/Icons/256/";
        private static TMP_FontAsset font;
        private static Sprite round;
        private static Material spriteMaterial;
        private static Material miningMaterial;
        private static readonly Color Ink = Hex("101D23"), Panel = Hex("1C2D34"), White = Hex("F3F0E5"), Gold = Hex("F5C16B"), Muted = Hex("98ADB0"), Teal = Hex("77D9C2");
        private static Sprite[] registered;

        [MenuItem("ProjectSS/Prototype/Build Expedition Prototype")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before building.");
            var previous = SceneManager.GetActiveScene();
            if (previous.isDirty && !string.IsNullOrEmpty(previous.path)) EditorSceneManager.SaveScene(previous);
            Directory.CreateDirectory("Assets/Prototype/Art");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ExpeditionPrototype";
            font = LoadFont();
            round = ExpeditionUIArt.Panel();
            spriteMaterial = new Material(Shader.Find("ProjectSS/ClippedSprite"));
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/Art/UnlitSprite.mat");
            if (mat == null) AssetDatabase.CreateAsset(spriteMaterial, "Assets/Prototype/Art/UnlitSprite.mat");
            else { UnityEngine.Object.DestroyImmediate(spriteMaterial); spriteMaterial = mat; }
            spriteMaterial.shader = Shader.Find("ProjectSS/ClippedSprite");
            spriteMaterial.SetVector("_WorldClipRect", new Vector4(-3.6f, 2.4f, 3.6f, 5.4f));
            EditorUtility.SetDirty(spriteMaterial);
            miningMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/Art/MiningClipped.mat");
            if (miningMaterial == null) { miningMaterial = new Material(spriteMaterial); AssetDatabase.CreateAsset(miningMaterial, "Assets/Prototype/Art/MiningClipped.mat"); }
            miningMaterial.SetVector("_WorldClipRect", new Vector4(-3.6f, -2.38f, 3.6f, 0.8f));
            EditorUtility.SetDirty(miningMaterial);

            var cam = New("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera"; cam.transform.position = new Vector3(0, 0, -10);
            cam.orthographic = true; cam.orthographicSize = 6.4f;
            cam.backgroundColor = Ink; cam.clearFlags = CameraClearFlags.SolidColor;
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 100;
            cam.gameObject.AddComponent<AudioListener>();
            var viewport = cam.gameObject.AddComponent<ExpeditionViewport>(); Set(viewport, "renderCamera", cam);
            var events = New("EventSystem"); events.AddComponent<EventSystem>(); events.AddComponent<StandaloneInputModule>();

            var pageManager = New("PageManager").AddComponent<PageManager>();
            var popupManager = New("PopupManager").AddComponent<PopupManager>();
            var pool = New("PoolManager").AddComponent<PoolManager>();
            var sprites = New("SpriteManager").AddComponent<SpriteManager>();
            var pause = New("SessionPausePolicy").AddComponent<SessionPausePolicy>();

            var root = New("PlayPage");
            var page = root.AddComponent<PlayPage>();
            var world = New("Game_Root", root.transform).transform;
            var ui = MakeCanvas("UI_Canvas", root.transform, cam, 100);
            var group = ui.gameObject.AddComponent<CanvasGroup>();
            Set(page, "canvas", ui); Set(page, "canvasGroup", group);
            page.sprites = sprites; page.pool = pool; page.pausePolicy = pause;
            page.catalog = MakeCatalog();
            SetArray(sprites, "registeredSprites", registered);
            Set(pageManager, "mainCamera", cam);
            Set(pageManager, "pagesRoot", root.transform);
            var pm = new SerializedObject(pageManager);
            pm.FindProperty("initialPage").enumValueIndex = (int)UIPageType.PlayPage;
            var scenePages = pm.FindProperty("scenePages"); scenePages.arraySize = 1;
            scenePages.GetArrayElementAtIndex(0).FindPropertyRelative("pageType").enumValueIndex = (int)UIPageType.PlayPage;
            scenePages.GetArrayElementAtIndex(0).FindPropertyRelative("instance").objectReferenceValue = page;
            pm.ApplyModifiedPropertiesWithoutUndo();

            BuildWorld(page, world);
            BuildUI(page, ui.transform);
            page.notice = MakeNotice(cam, pause);
            VerticalPlayLayout.Configure(page);
            page.notice.gameObject.SetActive(false);
            Set(popupManager, "popupRoot", New("PopupRoot", root.transform).transform);
            BuildPool(page, pool, world);
            // All services and popup references are part of the page prefab; the camera is injected.
            foreach (var service in new MonoBehaviour[] { sprites, pool, pause, popupManager })
            {
                service.transform.SetParent(root.transform, true);
                var config = new SerializedObject(service);
                var persist = config.FindProperty("persistAcrossScenes"); if (persist != null) persist.boolValue = false;
                config.ApplyModifiedPropertiesWithoutUndo();
            }
            var pmConfig = new SerializedObject(pageManager); pmConfig.FindProperty("persistAcrossScenes").boolValue=false;pmConfig.ApplyModifiedPropertiesWithoutUndo();
            page.notice.transform.SetParent(root.transform, true);
            const string prefabPath="Assets/Resources/Prefabs/PlayPage.prefab";
            AssetDatabase.DeleteAsset("Assets/Prototype/ExpeditionPrototype.unity");
            PrefabUtility.SaveAsPrefabAssetAndConnect(root,prefabPath,InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Splash.unity",true),new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root;
            Debug.Log("Expedition prototype built: " + ScenePath);
        }

        [MenuItem("ProjectSS/Prototype/Open Expedition Prototype")]
        public static void Open()
        {
            if (EditorApplication.isPlaying) return;
            if (!File.Exists(ScenePath)) Build();
            else EditorSceneManager.OpenScene(ScenePath);
        }

        private static ExpeditionCatalog MakeCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ExpeditionCatalog>("Assets/Prototype/ExpeditionCatalog.asset");
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<ExpeditionCatalog>(); AssetDatabase.CreateAsset(catalog, "Assets/Prototype/ExpeditionCatalog.asset"); }
            catalog.gear = new[]
            {
                Gear("낡은 검", "전사의 기본 무기", "Sword", "FA_WP_Main_Sword_019_WoodSilver", 0, 11, 1.25f),
                Gear("정찰 단검", "빠른 연속 공격", "Sword", "FA_WP_Main_Sword_005_GrayWhite", 1, 7, 0.72f),
                Gear("견습 완드", "마법 피해", "Wand", "FA_WP_Main_Wand_003_BlueWood", 2, 10, 1.6f),
                Gear("철기사의 검", "전사 · 안정적인 강한 일격", "Sword", "FA_WP_Main_Sword_009_GraySilver", 0, 26, 1.2f, GearEffect.Normal, 9),
                Gear("파쇄 망치", "전사 · 3초간 갑옷 파괴", "Blunt", "FA_WP_Main_Blunt_004_Gray", 0, 34, 1.5f, GearEffect.BreakArmor, 15, 4),
                Gear("서리 지팡이", "마법사 · 적 공격 속도 감소", "Staff", "FA_WP_Main_Staff_003_WoodSkyblue", 2, 23, 1.6f, GearEffect.Frost, 6, 12),
                Gear("흡혈 단검", "도적 · 타격마다 체력 +5", "Sword", "FA_WP_Main_Sword_008_Purple", 1, 15, 0.72f, GearEffect.Leech, 12, 4, 3, 1),
                Gear("화염 지팡이", "마법사 · 초당 화상 피해 12", "Staff", "FA_WP_Main_Staff_004_WoodPink", 2, 31, 1.6f, GearEffect.Burn, 8, 10, 4, 1),
                Gear("수호 갑옷", "공용 · 최대 체력 +100", "Chest", "FA_Chest_001_Silver", -1, 0, 0, GearEffect.Armor, 18, 6),
                Gear("탐험 투구", "공용 · 최대 체력 +35", "Helmet", "FA_Helmet_001_Ivory", -1, 0, 0, GearEffect.Normal, 9, 3),
                new GearDefinition {title="서리 부적",description="공용 · 공격 +5 / 체력 +15",hero=-1,equipSlot=3,slot="Accessory",spriteKey="Economy_Gem_03_Blue",icon=SpriteAt(Icons+"Economy_Gem_03_Blue.png"),damage=5,health=15,iron=8,crystal=8},
            };
            catalog.gear[8].equipSlot=2;catalog.gear[8].health=100;
            catalog.gear[9].equipSlot=1;catalog.gear[9].health=35;
            for(int i=3;i<8;i++){catalog.gear[i].effect=GearEffect.Normal;catalog.gear[i].description="기본 공격 "+catalog.gear[i].damage+" · "+catalog.gear[i].interval+"초 간격";catalog.gear[i].unlock=0;}
            registered = catalog.gear.Select(g => g.slot=="Accessory"?g.icon:SpriteAt(PartPath(g.slot,g.spriteKey))).Distinct().ToArray();
            Directory.CreateDirectory("Assets/Resources/Weapon/Prefabs");
            for(int i=0;i<catalog.gear.Length;i++)
            {
                var g=catalog.gear[i];var obj=New(g.spriteKey);var item=obj.AddComponent<WeaponItem>();item.catalogId=i;
                item.visual=New("Visual",obj.transform).AddComponent<SpriteRenderer>();item.visual.sprite=g.icon;
                item.visual.sharedMaterial=miningMaterial;item.visual.sortingOrder=75;
                item.visual.transform.localScale=Vector3.one*.54f/Mathf.Max(g.icon.bounds.size.x,g.icon.bounds.size.y);
                item.label=New("Label",obj.transform).AddComponent<TextMeshPro>();item.label.font=font;item.label.fontSize=2.5f;
                item.label.alignment=TextAlignmentOptions.Center;item.label.rectTransform.sizeDelta=new Vector2(3,.5f);item.label.GetComponent<MeshRenderer>().sortingOrder=80;
                var prefab=PrefabUtility.SaveAsPrefabAsset(obj,"Assets/Resources/Weapon/Prefabs/"+g.spriteKey+".prefab");
                g.prefab=prefab.GetComponent<WeaponItem>();UnityEngine.Object.DestroyImmediate(obj);
            }
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static GearDefinition Gear(string title, string detail, string slot, string key, int hero, float dmg, float interval, GearEffect effect = GearEffect.Normal, int iron = 0, int crystal = 0, int relic = 0, int unlock = 0)
        {
            return new GearDefinition { title = title, description = detail, slot = slot, spriteKey = key, hero = hero, damage = dmg, interval = interval, effect = effect, iron = iron, crystal = crystal, relic = relic, unlock = unlock, icon = SpriteAt(Pack + "Thumbnail/" + slot + "/" + key + ".png") };
        }
        private static string PartPath(string slot, string key) => Pack + "Parts/" + (slot == "Chest" || slot == "Helmet" ? slot : "HandRight/" + slot) + "/" + key + ".png";

        private static void BuildWorld(PlayPage page, Transform parent)
        {
            var gameRoot=parent;
            parent=New("BattleWorld",gameRoot).transform;
            string atlas = "Assets/Layer Lab/2D Maps - Simple Sidescroll/Extenstions/Simple Sidescroll1/Sprite/map1.png";
            WorldSprite("Sky", parent, SpriteAt(atlas, "map01_12"), 360, 300, 764, 270, -20, Hex("CADCCA"));
            WorldSprite("Forest", parent, SpriteAt(atlas, "map01_09"), 360, 400, 800, 210, -18, Hex("7AA69C"));
            WorldSprite("Ground", parent, SpriteAt(atlas, "map01_04"), 360, 500, 760, 190, -15, Hex("88AD8B"));
            WorldSprite("Ruins", parent, SpriteAt(atlas, "map01_06"), 619, 410, 128, 100, -12, Hex("A5C2B5"));
            page.heroes = new ExpeditionActor[3];
            for (int i = 0; i < 3; i++) page.heroes[i] = MakeHero(parent, "Hero_" + i, 335 - i * 112, 508, i, 1.6f);
            page.enemies = new ExpeditionActor[3];
            string[] names = { "Walker/Walker_Mushroom", "Skeleton/Skeleton_Warrior", "Golem/Golem_Iron" };
            for (int i = 0; i < 3; i++)
            {
                var wrapper = New("Enemy_" + i, parent);
                wrapper.transform.position = WorldPoint(570, 508);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Layer Lab/2D Minimal-EnemyMonster/EnemyMonster 2/Prefabs/" + names[i] + ".prefab");
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, wrapper.transform);
                PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                PrepareRenderers(visual, 25);
                FitActor(visual, i == 2 ? 1.8f : 1.45f, true);
                var actor = wrapper.AddComponent<ExpeditionActor>();
                ConfigureActor(actor, visual);
                page.enemies[i] = actor;
                wrapper.SetActive(i == 0);
            }
            var scrollParts=new System.Collections.Generic.List<Transform>();
            foreach(var sr in parent.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.sortingOrder<0).ToArray())
            {
                var tile=New(sr.name+"_Scroll",parent).transform;tile.position=Vector3.zero;sr.transform.SetParent(tile,true);
                var copy=UnityEngine.Object.Instantiate(tile.gameObject,parent).transform;copy.localPosition=Vector3.right*7.6f;
                scrollParts.Add(tile);scrollParts.Add(copy);
            }
            page.scrolling=scrollParts.ToArray();parent.position=Vector3.up*1.3f;
            page.enemyRest=page.enemies.Select(e=>e.transform.position).ToArray();
            page.miningWorld = New("MiningWorld", gameRoot).transform;
            WorldSprite("CaveBackdrop", page.miningWorld, round, 360, 905, 714, 264, -12, Hex("1E3339"));
            page.miner = MakeHero(page.miningWorld, "RetiredHero_Thor", 198, 972, 3, 1.65f);
            page.miner.transform.localPosition += Vector3.down * .14f;
            page.blocks = new SpriteRenderer[3]; page.deposits = new SpriteRenderer[3];
            page.depositSprites = new[] { SpriteAt(Icons + "Material_Ore_03_Silver.png"), SpriteAt(Icons + "Economy_Gem_03_Blue.png"), SpriteAt(Icons + "Economy_Gem_03_Yellow.png") };
            for (int i = 0; i < 3; i++)
            {
                page.blocks[i] = WorldSprite("VeinRock_" + i, page.miningWorld, SpriteAt("Assets/Rounded Blocks/stone.png"), 372 + i * 173, 895, 166, 178, 10 - i, i == 0 ? Color.white : i == 1 ? Hex("859CA0") : Hex("506A73"));
                page.deposits[i] = WorldSprite("EmbeddedOre_" + i, page.blocks[i].transform, page.depositSprites[0], 372 + i * 173, 885, i == 0 ? 87 : 62, i == 0 ? 87 : 62, 15 - i, i == 0 ? Color.white : Hex("869D9D"));
            }
            page.miningView = page.miningWorld.gameObject.AddComponent<ExpeditionMiningView>();
            Set(page.miningView, "shakeRoot", page.miningWorld); Set(page.miningView, "pool", page.pool);
            SetArray(page.miningView, "rocks", page.blocks); SetArray(page.miningView, "deposits", page.deposits); SetArray(page.miningView, "oreSprites", page.depositSprites);
            var flash = WorldSprite("ImpactFlash", page.miningWorld, round, 304, 899, 18, 12, 35, Color.clear);
            Set(page.miningView, "impactFlash", flash);
            var mv = new SerializedObject(page.miningView);
            var positions = mv.FindProperty("rockPositions"); var scales = mv.FindProperty("rockScales"); positions.arraySize = scales.arraySize = 3;
            for (int i = 0; i < 3; i++) { positions.GetArrayElementAtIndex(i).vector3Value = page.blocks[i].transform.localPosition; scales.GetArrayElementAtIndex(i).vector3Value = page.blocks[i].transform.localScale; }
            mv.ApplyModifiedPropertiesWithoutUndo();
            var source = page.miningWorld.gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0;
            Set(page.miningView, "audioSource", source);
            SetArray(page.miningView, "hitSounds", new[] {
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Prototype/Art/Audio/MiningPick_Foley_02_Ready.wav") });
            Set(page.miningView, "breakSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Prototype/Art/Audio/MiningBreak_Foley_01_Ready.wav"));
            var crackLines = new LineRenderer[4];
            var crackHighlights = new LineRenderer[4];
            Vector3[][] paths = {
                new[] { new Vector3(-0.5f,0.77f), new Vector3(-0.23f,0.35f), new Vector3(-0.4f,0.06f), new Vector3(-0.12f,-0.25f) },
                new[] { new Vector3(0.62f,0.44f), new Vector3(0.3f,0.19f), new Vector3(0.41f,-0.12f), new Vector3(0.04f,-0.39f) },
                new[] { new Vector3(-0.12f,-0.25f), new Vector3(0.02f,-0.46f), new Vector3(-0.21f,-0.65f), new Vector3(-0.09f,-0.83f) },
                new[] { new Vector3(-0.73f,-0.48f), new Vector3(-0.4f,-0.3f), new Vector3(-0.12f,-0.25f), new Vector3(0.04f,-0.39f), new Vector3(0.38f,-0.5f), new Vector3(0.72f,-0.7f) }
            };
            for (int i = 0; i < 4; i++)
            {
                var line = New("Fracture_" + i, page.miningWorld).AddComponent<LineRenderer>();
                line.transform.position = page.blocks[0].transform.position;
                line.useWorldSpace = false; line.positionCount = paths[i].Length; line.SetPositions(paths[i]);
                line.startWidth = 0.065f; line.endWidth = 0.035f; line.numCapVertices = 2; line.numCornerVertices = 2; line.sortingOrder = 31;
                line.startColor = line.endColor = Hex("101820"); line.sharedMaterial = miningMaterial; line.enabled = false;
                crackLines[i] = line;
                var edge = New("FractureEdge_" + i, page.miningWorld).AddComponent<LineRenderer>();
                edge.transform.position = line.transform.position + new Vector3(0.018f, -0.015f);
                edge.useWorldSpace = false; edge.positionCount = paths[i].Length; edge.SetPositions(paths[i]);
                edge.startWidth = 0.10f; edge.endWidth = 0.065f; edge.numCapVertices = edge.numCornerVertices = 2;
                edge.sortingOrder = 30; edge.startColor = edge.endColor = Hex("E0C796");
                edge.sharedMaterial = miningMaterial; edge.enabled = false; crackHighlights[i] = edge;
                // Bake world-sized strokes under the vein: impact movement and replacement stay aligned.
                line.transform.SetParent(page.blocks[0].transform, true);
                edge.transform.SetParent(page.blocks[0].transform, true);
            }
            var chest=WorldSprite("TreasureChest",page.blocks[0].transform,SpriteAt("Assets/Textures/Game/Item_Chest_01_Wood.png"),372,895,142,142,25,Color.white);
            Set(page.miningView,"chestVisual",chest);chest.gameObject.SetActive(false);
            var opening=New("ChestOpening",page.miningWorld);opening.transform.position=chest.transform.position;
            var parts=ChestParts();float scale=1.42f/5.12f;
            var body=New("ChestBody",opening.transform).AddComponent<SpriteRenderer>();body.sprite=parts[0];body.sharedMaterial=miningMaterial;body.sortingOrder=29;body.transform.localScale=Vector3.one*scale;body.transform.localPosition=new Vector3(0,-.385f);
            var lid=New("ChestLid",opening.transform).AddComponent<SpriteRenderer>();lid.sprite=parts[1];lid.sharedMaterial=miningMaterial;lid.sortingOrder=30;lid.transform.localScale=Vector3.one*scale;lid.transform.localPosition=new Vector3(0,-.058f);
            var glow=New("ChestGlow",opening.transform).AddComponent<SpriteRenderer>();glow.sprite=ExpeditionUIArt.Circle();glow.sharedMaterial=miningMaterial;glow.sortingOrder=28;glow.transform.localScale=new Vector3(1.2f/glow.sprite.bounds.size.x,.6f/glow.sprite.bounds.size.y,1);
            opening.transform.SetParent(page.blocks[0].transform,true);opening.SetActive(false);
            Set(page.miningView,"chestOpenRoot",opening);Set(page.miningView,"chestLid",lid.transform);Set(page.miningView,"chestGlow",glow);
            var chestSo=new SerializedObject(page.miningView);chestSo.FindProperty("lidRest").vector3Value=lid.transform.localPosition;chestSo.ApplyModifiedPropertiesWithoutUndo();
            foreach(var deposit in page.deposits)deposit.enabled=false;
            page.miningWorld.position=Vector3.up*1.5f;
            SetArray(page.miningView, "cracks", crackLines);
            SetArray(page.miningView, "crackHighlights", crackHighlights);
            foreach (var sr in page.miningWorld.GetComponentsInChildren<SpriteRenderer>(true)) sr.sharedMaterial = miningMaterial;
        }

        private static ExpeditionActor MakeHero(Transform parent, string name, float x, float y, int variant, float height)
        {
            var wrapper = New(name, parent); wrapper.transform.position = WorldPoint(x, y);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Maker + "Common/Prefabs/Character.prefab");
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, wrapper.transform);
            PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var renderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
            string[] slots = { "Sword", "Axe", "Blunt", "Wand", "Staff", "Spear", "Bow", "Crossbow", "Shield", "Sub_Item", "Arrow", "Bolt" };
            foreach (var sr in renderers)
            {
                if (slots.Contains(sr.name)) sr.gameObject.SetActive(false);
                if (sr.name == "Hair") { sr.sprite = SpriteAt(Pack + "Character/Hair/Body_Hair_00" + (variant + 1) + ".png"); sr.gameObject.SetActive(true); }
                if (sr.name == "Chest") sr.sprite = SpriteAt(Pack + "Parts/Chest/" + new[] { "FA_Chest_003_GrayWhite", "FA_Chest_002_Dark", "FA_Chest_002_Purple", "FA_Chest_004_Brown" }[variant] + ".png");
                if (sr.name == "Beard") { sr.gameObject.SetActive(variant == 3); if (variant == 3) sr.sprite = SpriteAt(Pack + "Character/Beard/Body_Beard_004.png"); }
                if (sr.name == "Helmet") sr.gameObject.SetActive(false);
                if (sr.name == "Hair" || sr.name == "Hair_Helmet" || sr.name == "Beard") sr.color = Hex(new[] { "A77245", "536E7A", "B5A2E5", "ADB7AF" }[variant]);
                if (sr.name == "Head" || sr.name == "Body") sr.color = Hex("F3CEAA");
            }
            string weaponSlot = variant == 2 ? "Wand" : variant == 3 ? "Blunt" : "Sword";
            string weapon = variant == 2 ? "FA_WP_Main_Wand_003_BlueWood" : variant == 3 ? "FA_WP_Main_Blunt_001_Wood" : variant == 1 ? "FA_WP_Main_Sword_005_GrayWhite" : "FA_WP_Main_Sword_019_WoodSilver";
            var weaponSr = renderers.First(s => s.name == weaponSlot);
            weaponSr.sprite = SpriteAt(PartPath(weaponSlot, weapon)); weaponSr.gameObject.SetActive(true);
            PrepareRenderers(visual, 20);
            FitActor(visual, height, false);
            var actor = wrapper.AddComponent<ExpeditionActor>();
            ConfigureActor(actor, visual);
            var so = new SerializedObject(actor);
            var array = so.FindProperty("weaponSlots"); string[] activeSlots = { "Sword", "Blunt", "Wand", "Staff" }; array.arraySize = activeSlots.Length;
            for (int i = 0; i < activeSlots.Length; i++)
            {
                array.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue = activeSlots[i];
                array.GetArrayElementAtIndex(i).FindPropertyRelative("renderer").objectReferenceValue = renderers.First(s => s.name == activeSlots[i]);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Set(actor, "chest", renderers.First(s => s.name == "Chest")); Set(actor, "helmet", renderers.First(s => s.name == "Helmet"));
            Set(actor, "defaultChest", renderers.First(s => s.name == "Chest").sprite);
            Set(actor, "armorChest", SpriteAt(Pack + "Parts/Chest/FA_Chest_001_Silver.png"));
            Set(actor, "armorHelmet", FirstSprite(Pack + "Parts/Helmet", "FA_Helmet_001"));
            if (variant == 3)
            {
                var animator = visual.GetComponentInChildren<Animator>(true);
                if (animator != null) animator.enabled = false;
                var tool = renderers.First(s => s.name == "Blunt");
                var hand = New("MiningGrip", visual.transform).transform;
                hand.position = WorldPoint(219, 899);
                hand.rotation = Quaternion.identity;
                var parentScale = visual.transform.lossyScale;
                hand.localScale = new Vector3(1 / parentScale.x, 1 / parentScale.y, 1 / parentScale.z);
                tool.transform.SetParent(hand, false);
                tool.sprite = SpriteAt("Assets/Space_Exploration_GUI_Kit/Icons/pickaxe-256.png");
                float toolScale = 1.15f / tool.sprite.bounds.size.x;
                tool.transform.localScale = Vector3.one * toolScale;
                tool.transform.localRotation = Quaternion.identity;
                // Grip at source pixel (65,64), near the lower third of the handle.
                var gripPixel = new Vector2(65, 64) - tool.sprite.rect.position;
                var gripOffset = (gripPixel - tool.sprite.pivot) / tool.sprite.pixelsPerUnit;
                tool.transform.localPosition = -(Vector3)gripOffset * toolScale;
                Set(actor, "miningHand", hand);
                var miningSo = new SerializedObject(actor); miningSo.FindProperty("miningHandRest").quaternionValue = hand.localRotation; miningSo.ApplyModifiedPropertiesWithoutUndo();
            }
            return actor;
        }

        private static void ConfigureActor(ExpeditionActor actor, GameObject visual)
        {
            var animator=visual.GetComponentInChildren<Animator>(true);
            Set(actor, "animator", animator);
            if(animator!=null && animator.runtimeAnimatorController!=null)
            {
                var walk=animator.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name.IndexOf("Walk",StringComparison.OrdinalIgnoreCase)>=0);
                var config=new SerializedObject(actor);config.FindProperty("walkState").stringValue=walk!=null?walk.name:"Run";config.ApplyModifiedPropertiesWithoutUndo();
            }
            Set(actor, "motionRoot", visual.transform);
            var so = new SerializedObject(actor); so.FindProperty("restPosition").vector3Value = visual.transform.localPosition; so.ApplyModifiedPropertiesWithoutUndo();
            SetArray(actor, "tintedRenderers", visual.GetComponentsInChildren<SpriteRenderer>(true));
            var renderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
            var visible = renderers.Where(r => r.sprite != null && r.enabled && r.gameObject.activeInHierarchy).ToArray();
            if (visible.Length > 0)
            {
                Bounds bounds = visible[0].bounds;
                foreach (var sr in visible) bounds.Encapsulate(sr.bounds);
                var anchor = New("HPAnchor", visual.transform).transform;
                anchor.position = new Vector3(bounds.center.x, bounds.max.y + 0.1f, 0);
                Set(actor, "hpAnchor", anchor);
            }
            so = new SerializedObject(actor);
            var colors = so.FindProperty("baseColors"); colors.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++) colors.GetArrayElementAtIndex(i).colorValue = renderers[i].color;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void PrepareRenderers(GameObject visual, int order)
        {
            foreach (var mono in visual.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mono != null && mono.GetType().Name != "AnimationEventReceiver") UnityEngine.Object.DestroyImmediate(mono);
            }
            var sorting = visual.GetComponent<SortingGroup>();
            if (sorting == null) sorting = visual.AddComponent<SortingGroup>();
            sorting.sortingOrder = order;
            foreach (var sr in visual.GetComponentsInChildren<SpriteRenderer>(true)) sr.sharedMaterial = spriteMaterial;
        }
        private static void FitActor(GameObject visual, float height, bool flip)
        {
            visual.transform.localPosition = Vector3.zero; visual.transform.localScale = Vector3.one;
            var anim = visual.GetComponentInChildren<Animator>(true);
            if (anim != null && anim.runtimeAnimatorController != null)
            {
                var clip = anim.runtimeAnimatorController.animationClips.FirstOrDefault(a => a.name.Contains("Idle"));
                if (clip != null) clip.SampleAnimation(anim.gameObject, 0);
            }
            var renderers = visual.GetComponentsInChildren<SpriteRenderer>().Where(r => r.sprite != null && r.enabled).ToArray();
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            foreach (var sr in renderers) bounds.Encapsulate(sr.bounds);
            float scale = height / Mathf.Max(0.1f, bounds.size.y);
            float dx = bounds.center.x - visual.transform.position.x, dy = bounds.min.y - visual.transform.position.y;
            visual.transform.localScale = new Vector3(flip ? -scale : scale, scale, scale);
            visual.transform.localPosition = new Vector3((flip ? dx : -dx) * scale, -dy * scale, 0);
        }

        private static void BuildUI(PlayPage page, Transform ui)
        {
            Box(ui,"TopHUD",0,0,720,100,Ink);
            page.stageLabel=Text(ui,"Stage","1-1 · 초원 전선",24,12,450,34,25,White,true);
            page.resources=Text(ui,"Resources","철 0   결정 0   파편 0",24,58,660,30,18,Gold);
            var toggle=New("AutoBattle",ui,true).AddComponent<Toggle>();Rect((RectTransform)toggle.transform,500,16,200,32);
            var checkBack=Box(toggle.transform,"CheckBack",0,0,30,30,Panel);checkBack.raycastTarget=true;
            var check=Box(toggle.transform,"Check",6,6,18,18,Teal);toggle.targetGraphic=checkBack;toggle.graphic=check;
            Text(toggle.transform,"Label","자동 원정",40,0,154,32,19,White);
            UnityEventTools.AddPersistentListener(toggle.onValueChanged,page.SetAutoBattle);page.autoBattleToggle=toggle;
            Region(ui,"BattleHUD",100,300,out var bh);
            page.enemyStatus=Text(bh,"BattleStatus","이동 중",24,104,430,25,15,White);
            page.heroHpBars=new Image[3];page.heroHpRoots=new RectTransform[3];
            for(int i=0;i<3;i++)page.heroHpBars[i]=OverheadBar(bh,"HeroHP_"+i,page.heroes[i].HpPosition,66,Teal,out page.heroHpRoots[i]);
            page.enemyBar=OverheadBar(bh,"EnemyHP",page.enemies[0].HpPosition,110,Hex("EA8F77"),out page.enemyHpRoot);
            Box(ui,"BattleDivider",0,400,720,3,Teal);
            page.minePanel=New("MinePanel",ui,true);Stretch((RectTransform)page.minePanel.transform);var m=page.minePanel.transform;
            page.depthLabel=Text(m,"Depth","갱도 0m",160,418,400,42,29,White,true,TextAlignmentOptions.Center);
            page.routeButtons=Array.Empty<Button>();page.routeLabels=Array.Empty<TMP_Text>();page.routePanels=Array.Empty<Image>();
            for(int side=0;side<2;side++)for(int row=0;row<3;row++)
                Box(m,"SidebarSlot_"+side+"_"+row,side==0?18:626,421+row*99,76,76,Panel);

            // A fixed pedestal and a separate oval face leave the four navigation buttons exposed.
            var circle=ExpeditionUIArt.Circle();
            var shadow=Icon(m,"DigShadow",circle,215,1017,290,180);shadow.color=Hex("080F14");shadow.preserveAspect=false;
            Box(m,"BurstGaugeFrame",529,978,22,150,Hex("294047"));
            page.heatBar=Box(m,"BurstGauge",533,982,14,142,Gold);
            page.heatBar.type=Image.Type.Filled;page.heatBar.fillMethod=Image.FillMethod.Vertical;page.heatBar.fillOrigin=0;page.heatBar.fillAmount=0;
            var baseImage=Icon(m,"DigBase",circle,217,988,286,180);baseImage.color=Hex("754A27");baseImage.preserveAspect=false;
            var face=Icon(m,"Dig",circle,230,970,260,166);face.color=Hex("F6BE52");face.preserveAspect=false;face.raycastTarget=true;
            var dig=face.gameObject.AddComponent<Button>();dig.targetGraphic=face;dig.transition=Selectable.Transition.None;page.digButton=dig;
            var faceRect=face.rectTransform;faceRect.pivot=new Vector2(.5f,.5f);faceRect.anchoredPosition+=new Vector2(130,-83);
            page.digLabel=Text(face.transform,"DigLabel","DIG",0,40,260,80,48,Hex("34271C"),true,TextAlignmentOptions.Center);

            page.holdDig=dig.gameObject.AddComponent<HoldDigButton>();Set(page.holdDig,"rectTransform",faceRect);Set(page.holdDig,"pressableFace",faceRect);Set(page.holdDig,"pausePolicy",page.pausePolicy);Set(page.holdDig,"selectable",dig);
            var hold=new SerializedObject(page.holdDig);hold.FindProperty("circularHitArea").boolValue=true;hold.FindProperty("repeatInterval").floatValue=.035f;hold.FindProperty("initialHoldDelay").floatValue=.13f;hold.FindProperty("pressOffset").vector3Value=new Vector3(0,-15,0);hold.ApplyModifiedPropertiesWithoutUndo();
            DigButtonArt.Configure(page);
            var navigation=New("BottomNavigation",ui,true);Stretch((RectTransform)navigation.transform);
            Box(navigation.transform,"NavigationDeck",0,1210,720,70,Hex("202B3A"));
            string[] menus={"용사 관리","가방","도전","설정"};float[] menuX={16,119,506,609};
            string[] menuAssets={"Gear_Helmet_01.png","UI_Common_Bag_01_Brown.png","UI_Rewards_Trophy_01_Gold.png","UI_System_Setting_01.png"};
            page.menuButtons=new Image[4];page.menuIcons=new Transform[4];page.menuIconRest=new Vector3[4];
            for(int i=0;i<4;i++)
            {
                var b=Button(navigation.transform,"Menu"+(i+1),menus[i],menuX[i],1194,95,68,Hex("2B354C"),null);
                var label=b.GetComponentInChildren<TMP_Text>();label.fontSize=18;Rect((RectTransform)label.transform,0,28,95,36);
                var icon=Icon(b.transform,"MenuIcon",SpriteAt(Icons+menuAssets[i]),5,-47,85,85);
                page.menuButtons[i]=(Image)b.targetGraphic;page.menuIcons[i]=icon.transform;page.menuIconRest[i]=icon.transform.localPosition;
                UnityEventTools.AddIntPersistentListener(b.onClick,page.OpenMenu,i+1);
            }
            page.menuPanels=new GameObject[4];
            for(int i=0;i<4;i++)
            {
                var panel=New("MenuPanel"+(i+1),ui,true);Stretch((RectTransform)panel.transform);page.menuPanels[i]=panel;
                Box(panel.transform,"Backdrop",0,100,720,1073,Ink).raycastTarget=true;
                Text(panel.transform,"Title",menus[i],26,119,500,48,31,White,true);
                var close=Button(panel.transform,"BackToMine"+i,"채굴로",566,118,128,48,Panel,null);UnityEventTools.AddIntPersistentListener(close.onClick,page.OpenMenu,0);
            }
            var h=page.menuPanels[0].transform;
            page.heroGrid=New("HeroCollection",h,true);Stretch((RectTransform)page.heroGrid.transform);
            Text(page.heroGrid.transform,"CollectionHint","보유 용사 3명  ·  카드를 눌러 장비 관리",26,181,668,30,18,Teal);
            string[] heroNames={"로웬","린","미라"};string[] jobs={"전사","도적","마법사"};
            var portraits=new Sprite[3];for(int i=0;i<3;i++)portraits[i]=BakePortrait(page.heroes[i],i);
            page.menuIcons[0].GetComponent<Image>().sprite=portraits[0];
            for(int i=0;i<8;i++)
            {
                float x=20+(i%4)*174,y=242+(i/4)*248;
                var card=Button(page.heroGrid.transform,"HeroCard"+i,"",x,y,158,218,i<3?Hex("6C508B"):Hex("27333F"),null);
                if(i<3)
                {
                    UnityEventTools.AddIntPersistentListener(card.onClick,page.OpenHero,i);
                    Box(card.transform,"PortraitWell",6,6,146,158,Hex("A783BC"));
                    Icon(card.transform,"Portrait",portraits[i],8,14,142,145);
                    Text(card.transform,"Role",jobs[i],10,8,132,26,16,White,true);
                    Text(card.transform,"HeroName",heroNames[i],6,167,146,32,22,White,true,TextAlignmentOptions.Center);
                    Box(page.heroGrid.transform,"ActiveStrip"+i,x,y+224,158,17,Hex("38676B"));
                    Text(page.heroGrid.transform,"ActiveLabel"+i,"출전 중",x,y+220,158,26,13,White,false,TextAlignmentOptions.Center);
                }
                else
                {
                    card.interactable=false;
                    Text(card.transform,"Unknown","?",0,38,158,100,54,Muted,true,TextAlignmentOptions.Center);
                    Text(card.transform,"Locked","미발견",0,164,158,35,18,Muted,false,TextAlignmentOptions.Center);
                }
            }
            page.heroEquipmentPanels=new GameObject[3];page.heroDetails=new TMP_Text[3];page.slotLabels=new TMP_Text[12];page.slotIcons=new Image[12];
            for(int hero=0;hero<3;hero++)
            {
                var panel=New("HeroEquipment"+hero,h,true);Stretch((RectTransform)panel.transform);page.heroEquipmentPanels[hero]=panel;
                Button(panel.transform,"BackToHeroes"+hero,"용사 목록",26,180,162,40,Panel,page.ShowHeroGrid).GetComponentInChildren<TMP_Text>().fontSize=18;
                Icon(panel.transform,"Portrait",portraits[hero],259,243,202,234);
                page.heroDetails[hero]=Text(panel.transform,"HeroDetails"+hero,"",170,482,380,83,26,White,true,TextAlignmentOptions.Center);
                for(int slot=0;slot<4;slot++)
                {
                    int index=hero*4+slot;float x=70+(slot%2)*300,y=605+(slot/2)*240;
                    var button=Button(panel.transform,"Slot"+index,"",x,y,280,217,Panel,null);UnityEventTools.AddIntPersistentListener(button.onClick,page.SelectSlot,index);
                    page.slotIcons[index]=Icon(button.transform,"Item",null,96,12,88,107);
                    page.slotLabels[index]=button.GetComponentInChildren<TMP_Text>();Rect((RectTransform)page.slotLabels[index].transform,8,132,264,77);page.slotLabels[index].fontSize=21;
                }
                panel.SetActive(false);
            }
            var bag=page.menuPanels[1].transform;
            page.routeButtons=new Button[3];page.routeLabels=new TMP_Text[3];page.routePanels=new Image[3];
            for(int i=0;i<3;i++)
            {
                var b=Button(bag,"Route"+i,new[]{"철 광맥","서리 광맥","유적 광맥"}[i],140+i*188,272,178,36,Panel,null);
                b.GetComponentInChildren<TMP_Text>().fontSize=16;UnityEventTools.AddIntPersistentListener(b.onClick,page.SelectRoute,i);
                page.routeButtons[i]=b;page.routeLabels[i]=b.GetComponentInChildren<TMP_Text>();page.routePanels[i]=(Image)b.targetGraphic;
            }
            Text(page.menuPanels[2].transform,"ComingSoon","새로운 도전을 준비하고 있습니다\n\n지금은 광맥을 개척하고\n원정대의 장비를 모아보세요",70,390,580,230,27,White,false,TextAlignmentOptions.Center);
            var settings=page.menuPanels[3].transform;
            var sound=Button(settings,"Sound","소리 ON",80,280,560,70,Panel,page.ToggleSound);page.soundLabel=sound.GetComponentInChildren<TMP_Text>();
            Button(settings,"Help","플레이 안내",80,376,560,70,Panel,page.Help);
            Button(settings,"Reset","진행 초기화",80,472,560,70,Panel,page.ResetProgress);
            Text(settings,"SaveHint","진행 상황은 이 기기에 자동 저장됩니다",80,592,560,60,20,Muted,false,TextAlignmentOptions.Center);
            foreach(var panel in page.menuPanels)panel.SetActive(false);
            navigation.transform.SetAsLastSibling();
        }
        static int catalogCount(PlayPage page)=>page.catalog.gear.Length;


        private static Sprite[] ChestParts()
        {
            const string path="Assets/Prototype/Art/ChestParts.asset";
            var parts=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(p=>p.rect.y).ToArray();if(parts.Length==2)return parts;
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Layer Lab/2D Minimal-IconPack/Icons/512/Item_Chest_01_Wood.png");
            var body=Sprite.Create(texture,new Rect(0,0,512,235),new Vector2(.5f,.5f),100);body.name="0_Body";
            var lid=Sprite.Create(texture,new Rect(0,235,512,277),new Vector2(.5f,0),100);lid.name="1_Lid";
            AssetDatabase.CreateAsset(body,path);AssetDatabase.AddObjectToAsset(lid,path);return new[]{body,lid};
        }
        private static Sprite BakePortrait(ExpeditionActor source, int index)
        {
            Directory.CreateDirectory("Assets/Prototype/Art/Portraits");
            string path="Assets/Prototype/Art/Portraits/Hero_"+index+".png";
            var clone=UnityEngine.Object.Instantiate(source.gameObject);clone.SetActive(true);clone.transform.position=new Vector3(1000,0,0);
            var material=new Material(Shader.Find("Sprites/Default"));
            foreach(var t in clone.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
            var renderers=clone.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled&&r.sprite!=null).ToArray();
            var bounds=renderers[0].bounds;foreach(var r in renderers){r.sharedMaterial=material;bounds.Encapsulate(r.bounds);}
            var go=New("PortraitBakeCamera");var cam=go.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=bounds.size.y*.57f;
            cam.transform.position=new Vector3(bounds.center.x,bounds.center.y,-10);cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.clear;
            var rt=new RenderTexture(256,320,24,RenderTextureFormat.ARGB32);cam.targetTexture=rt;cam.Render();
            var prior=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(256,320,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,256,320),0,0);tex.Apply();
            File.WriteAllBytes(path,tex.EncodeToPNG());RenderTexture.active=prior;cam.targetTexture=null;
            UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(clone);UnityEngine.Object.DestroyImmediate(material);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static ExpeditionNotice MakeNotice(Camera cam, SessionPausePolicy pause)
        {
            var canvas = MakeCanvas("ExpeditionNotice", null, cam, 1000);
            var popup = canvas.gameObject.AddComponent<ExpeditionNotice>();
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            Set(popup, "canvas", canvas); Set(popup, "canvasGroup", group); Set(popup, "pausePolicy", pause);
            Box(canvas.transform, "Dim", 0, 0, 720, 1280, new Color(0.025f, 0.065f, 0.075f, 0.92f)).raycastTarget = true;
            Box(canvas.transform, "Card", 36, 254, 648, 753, Panel);
            Text(canvas.transform, "NoticeKicker", "안내", 66, 282, 520, 26, 16, Teal);
            var title = Text(canvas.transform, "NoticeTitle", "", 66, 331, 582, 92, 32, Gold, true);
            var body = Text(canvas.transform, "NoticeBody", "", 66, 432, 580, 420, 22, White);
            var accept = Button(canvas.transform, "Accept", "계속 탐사하기", 66, 874, 588, 64, Hex("B87539"), popup.Accept);
            var close = Button(canvas.transform, "Close", "닫기", 554, 277, 98, 39, Hex("30434A"), popup.OnClickClose);
            Set(popup, "titleText", title); Set(popup, "bodyText", body); Set(popup, "actionText", accept.GetComponentInChildren<TMP_Text>());
            return popup;
        }

        private static void BuildPool(PlayPage page, PoolManager pool, Transform world)
        {
            var root = New("PrewarmedEffects", world).transform;
            page.effects = new ExpeditionFx[64];
            for (int i = 0; i < page.effects.Length; i++)
            {
                var go = New("Effect_" + i, root);
                var fx = go.AddComponent<ExpeditionFx>(); page.effects[i] = fx;
                var label = New("Text", go.transform).AddComponent<TextMeshPro>();
                label.font = font; label.fontSize = 2.7f; label.alignment = TextAlignmentOptions.Center;
                label.rectTransform.sizeDelta = new Vector2(2.3f, 0.4f);
                label.GetComponent<MeshRenderer>().sortingOrder = 80;
                var spark = New("Spark", go.transform).AddComponent<SpriteRenderer>();
                spark.sprite = SpriteAt(Icons + "Economy_Gem_03_Yellow.png"); spark.sharedMaterial = spriteMaterial; spark.sortingOrder = 70;
                Set(fx, "label", label); Set(fx, "spark", spark); Set(fx, "pool", pool);
                Set(fx, "defaultSpark", spark.sprite);
                Set(fx, "battleMaterial", spriteMaterial); Set(fx, "miningMaterial", miningMaterial);
                go.SetActive(false);
            }
            var so = new SerializedObject(pool); so.FindProperty("useBakedPools").boolValue = true;
            var arr = so.FindProperty("bakedPools"); arr.arraySize = 1+page.catalog.gear.Length;
            var item = arr.GetArrayElementAtIndex(0); item.FindPropertyRelative("name").stringValue = "ExpeditionFx";
            var refs = item.FindPropertyRelative("instances"); refs.arraySize = page.effects.Length;
            for (int i = 0; i < page.effects.Length; i++) refs.GetArrayElementAtIndex(i).objectReferenceValue = page.effects[i];
            page.itemEffects=new WeaponItem[page.catalog.gear.Length*2];
            for(int id=0;id<page.catalog.gear.Length;id++)
            {
                var entry=arr.GetArrayElementAtIndex(id+1);entry.FindPropertyRelative("name").stringValue="Weapon_"+id;
                var instances=entry.FindPropertyRelative("instances");instances.arraySize=2;
                for(int n=0;n<2;n++)
                {
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(page.catalog.gear[id].prefab.gameObject,root);
                    var cached=go.GetComponent<WeaponItem>();cached.pool=pool;cached.poolName="Weapon_"+id;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(cached);
                    page.itemEffects[id*2+n]=cached;instances.GetArrayElementAtIndex(n).objectReferenceValue=cached;go.SetActive(false);
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_FontAsset LoadFont()
        {
            const string path = "Assets/Prototype/Art/ExpeditionKorean.asset";
            var result = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (result != null && result.material != null && result.atlasTextures.Length > 0 && result.atlasTextures[0] != null) return result;
            if (result != null) AssetDatabase.DeleteAsset(path);
            result = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/MalgunGothic.ttf"), 48, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            result.name = "ExpeditionKorean";
            var material = result.material;
            var textures = result.atlasTextures;
            AssetDatabase.CreateAsset(result, path);
            AssetDatabase.AddObjectToAsset(material, result);
            foreach (var tex in textures) AssetDatabase.AddObjectToAsset(tex, result);
            result.material = material;
            result.atlasTextures = textures;
            EditorUtility.SetDirty(result);
            AssetDatabase.SaveAssets();
            return result;
        }
        private static Canvas MakeCanvas(string name, Transform parent, Camera cam, int order)
        {
            var go = New(name, parent, true); var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 10; canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            go.AddComponent<GraphicRaycaster>(); return canvas;
        }
        private static GameObject New(string name, Transform parent = null, bool rect = false)
        {
            var go = rect ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }
        private static void Rect(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
        }
        private static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        private static Image Box(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var img = New(name, parent, true).AddComponent<Image>(); Rect(img.rectTransform, x, y, w, h);
            img.sprite = round; img.type = Image.Type.Sliced; img.color = color; img.raycastTarget = false; return img;
        }
        private static Image Icon(Transform parent, string name, Sprite sprite, float x, float y, float w, float h)
        {
            var img = New(name, parent, true).AddComponent<Image>(); Rect(img.rectTransform, x, y, w, h);
            img.sprite = sprite; img.preserveAspect = true; img.raycastTarget = false; return img;
        }
        private static TMP_Text Text(Transform parent, string name, string value, float x, float y, float w, float h, float size, Color color, bool bold = false, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
        {
            var text = New(name, parent, true).AddComponent<TextMeshProUGUI>(); Rect(text.rectTransform, x, y, w, h);
            text.font = font; text.text = value; text.fontSize = size; text.color = color;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal; text.alignment = alignment;
            text.raycastTarget = false; text.overflowMode = TextOverflowModes.Truncate; text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }
        private static Button Button(Transform parent, string name, string value, float x, float y, float w, float h, Color color, UnityAction action)
        {
            var image = Box(parent, name, x, y, w, h, color); image.raycastTarget = true;
            var b = image.gameObject.AddComponent<Button>(); b.targetGraphic = image;
            var colors = b.colors; colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f); colors.pressedColor = new Color(0.75f, 0.85f, 0.85f); colors.disabledColor = new Color(0.6f, 0.6f, 0.6f); b.colors = colors;
            var label = Text(image.transform, name + "Label", value, 5, 0, w - 10, h, 21, White, true, TextAlignmentOptions.Center);
            if (action != null) UnityEventTools.AddPersistentListener(b.onClick, action);
            return b;
        }
        private static Image Bar(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            Box(parent, name + "Track", x, y, w, h, Hex("32434A"));
            var fill = Box(parent, name, x, y, w, h, color); fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0; return fill;
        }
        private static Image OverheadBar(Transform parent, string name, Vector3 position, float width, Color color, out RectTransform root)
        {
            var frame = Box(parent, name, 0, 0, width, 12, Hex("15272D")); root = frame.rectTransform;
            root.pivot = new Vector2(0.5f, 0.5f); root.position = position;
            var fill = Box(root, "Fill", 2, 2, width - 4, 8, color);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
            return fill;
        }
        private static GameObject Region(Transform parent, string name, float y, float height, out Transform content)
        {
            var region = New(name, parent, true); Rect((RectTransform)region.transform, 0, y, 720, height);
            region.AddComponent<RectMask2D>();
            content = New("Content", region.transform, true).transform;
            Rect((RectTransform)content, 0, -y, 720, 1280);
            return region;
        }
        private static Vector3 WorldPoint(float x, float y) => new Vector3((x - 360) / 100, (640 - y) / 100, 0);
        private static SpriteRenderer WorldSprite(string name, Transform parent, Sprite sprite, float x, float y, float w, float h, int order, Color color)
        {
            var sr = New(name, parent).AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.color = color; sr.sortingOrder = order; sr.sharedMaterial = spriteMaterial;
            sr.transform.position = WorldPoint(x, y);
            sr.transform.localScale = new Vector3(w / 100 / sprite.bounds.size.x, h / 100 / sprite.bounds.size.y, 1);
            if (sprite == round) { sr.drawMode = SpriteDrawMode.Sliced; sr.size = new Vector2(w / 100, h / 100); sr.transform.localScale = Vector3.one; }
            // Child decoration should use world size, independent of its parent's rock scale.
            if (parent.lossyScale != Vector3.one) sr.transform.localScale = new Vector3(sr.transform.localScale.x / parent.lossyScale.x, sr.transform.localScale.y / parent.lossyScale.y, 1);
            return sr;
        }
        private static Sprite SpriteAt(string path, string name = null)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            var result = name == null ? sprites.FirstOrDefault() : sprites.FirstOrDefault(s => s.name == name);
            if (result == null) throw new InvalidOperationException("Missing sprite: " + path + " / " + name);
            return result;
        }
        private static Sprite FirstSprite(string folder, string prefix)
        {
            string file = Directory.GetFiles(folder, prefix + "*.png").OrderBy(p => p).First();
            return SpriteAt(file.Replace('\\', '/'));
        }
        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }
        private static void Set(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var so = new SerializedObject(target); so.FindProperty(property).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetArray<T>(UnityEngine.Object target, string property, T[] values) where T : UnityEngine.Object
        {
            var so = new SerializedObject(target); var array = so.FindProperty(property); array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
