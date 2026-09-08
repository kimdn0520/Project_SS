using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

public static class PagePrefabBuilder
{
    private const string PREFAB_DIR = "Assets/Resources/Prefabs";
    private const string MAIN_PAGE_PREFAB_PATH = "Assets/Resources/Prefabs/MainPage.prefab";
    private const string PLAY_PAGE_PREFAB_PATH = "Assets/Resources/Prefabs/PlayPage.prefab";

    [MenuItem("ProjectSS/Build Page Prefabs")]
    public static void Execute()
    {
        if (!Directory.Exists(PREFAB_DIR))
        {
            Directory.CreateDirectory(PREFAB_DIR);
        }

        Camera mainCam = GetOrCreateMainCamera();

        // 1. MainPage 프리팹 생성 (SF 에셋 적용)
        GameObject mainPagePrefab = CreateMainPagePrefab(mainCam);

        // 2. PlayPage 프리팹 생성 (UI_Canvas와 Game_Root가 병렬인 일반 Transform 루트)
        GameObject playPagePrefab = CreatePlayPagePrefab(mainCam);

        // 3. Play 씬 리팩토링 (빈 껍데기 + PageManager에 프리팹 연결)
        SetupCleanPlayScene(mainPagePrefab, playPagePrefab, mainCam);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=cyan>[PagePrefabBuilder] Page 프리팹 캡슐화 및 Space GUI 테마 적용 완료!</color>");
    }

    private static Camera GetOrCreateMainCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        cam.orthographic = true;
        cam.orthographicSize = 6.4f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
        return cam;
    }

    #region MainPage Prefab Builder

    private static GameObject CreateMainPagePrefab(Camera mainCam)
    {
        GameObject rootObj = new GameObject("MainPage");
        MainPageView mainPageView = rootObj.AddComponent<MainPageView>();

        GameObject canvasObj = new GameObject("UI_Canvas");
        canvasObj.transform.SetParent(rootObj.transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = mainCam;
        canvas.planeDistance = 10f;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 1280);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();

        Sprite spaceBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Background_Images/large/home-background-large.png");
        GameObject bgObj = new GameObject("Space_Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImage = bgObj.AddComponent<Image>();
        if (spaceBgSprite != null)
        {
            bgImage.sprite = spaceBgSprite;
            bgImage.type = Image.Type.Simple;
            bgImage.preserveAspect = false;
        }
        else
        {
            bgImage.color = new Color(0.06f, 0.07f, 0.12f, 1f);
        }

        // 2-1. TabSwiper (컨텐츠 스와이프 영역, Canvas 직속 자식)
        GameObject contentArea = new GameObject("TabSwiper");
        contentArea.transform.SetParent(canvasObj.transform, false);
        RectTransform caRect = contentArea.AddComponent<RectTransform>();
        caRect.anchorMin = new Vector2(0, 0);
        caRect.anchorMax = new Vector2(1, 1);
        caRect.offsetMin = new Vector2(0, 130);
        caRect.offsetMax = new Vector2(0, -110);

        Image caImg = contentArea.AddComponent<Image>();
        caImg.color = new Color(0, 0, 0, 0.005f);

        GameObject content = new GameObject("Content");
        content.transform.SetParent(contentArea.transform, false);
        RectTransform cRect = content.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 0);
        cRect.anchorMax = new Vector2(0, 1);
        cRect.pivot = new Vector2(0, 0.5f);
        cRect.sizeDelta = new Vector2(720 * 3, 0);
        cRect.anchoredPosition = new Vector2(-720, 0);

        SwipeTabController swipeCtrl = contentArea.AddComponent<SwipeTabController>();
        SerializedObject swipeSo = new SerializedObject(swipeCtrl);
        swipeSo.FindProperty("contentRect").objectReferenceValue = cRect;
        swipeSo.FindProperty("tabWidth").floatValue = 720f;
        swipeSo.FindProperty("totalTabs").intValue = 3;
        swipeSo.FindProperty("initialTabIndex").intValue = 1;
        swipeSo.ApplyModifiedProperties();

        Sprite shopContainerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Containers/Large/shop-container-large.png");
        CreateTabContent(content.transform, 0, "SHOP", "SPACE BLACK MARKET", "Buy Space Fighters, Shields and Laser Upgrades!", shopContainerSprite);

        Sprite homeContainerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Containers/Large/inventory-menu-container-large.png");
        GameObject homeTab = CreateTabContent(content.transform, 1, "MISSION LOBBY", "SECTOR 7: DEEP SPACE PATROL", "Hostile Alien Fleets Detected Ahead! Deploy your fighter to defend the galaxy.", homeContainerSprite);

        Sprite startBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/large-blue-sparkle-large.png");
        if (startBtnSprite == null)
            startBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/large-blue-large.png");

        Button btnStart = CreateSFActionButton("Btn_StartGame", homeTab.transform, new Vector2(0, -180), new Vector2(340, 90), "START MISSION", startBtnSprite, new Color(0.2f, 0.9f, 1f));

        Sprite rankContainerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Containers/Large/victory-defeat-container-large.png");
        CreateTabContent(content.transform, 2, "RANKING", "GALACTIC LEADERBOARD", "1. Commander Shepard (Wave 99) | 2. StarLord (Wave 85) | 3. Nova (Wave 72)", rankContainerSprite);

        // 2-2. SafeArea (상단 재화 바 및 하단 탭 바 컨테이너)
        GameObject safePanel = new GameObject("SafeArea");
        safePanel.transform.SetParent(canvasObj.transform, false);
        RectTransform safeRect = safePanel.AddComponent<RectTransform>();
        safeRect.anchorMin = Vector2.zero;
        safeRect.anchorMax = Vector2.one;
        safeRect.sizeDelta = Vector2.zero;
        safePanel.AddComponent<SafeAreaHelper>();

        Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Containers/Large/homepage-icon-container-large.png");
        Sprite coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Icons/coin-128.png");
        Sprite gemSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Icons/gem-128.png");

        GameObject topArea = new GameObject("TopArea");
        topArea.transform.SetParent(safePanel.transform, false);
        RectTransform topRect = topArea.AddComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1);
        topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot = new Vector2(0.5f, 1);
        topRect.sizeDelta = new Vector2(0, 110);
        topRect.anchoredPosition = Vector2.zero;

        Image topBg = topArea.AddComponent<Image>();
        if (panelSprite != null) topBg.sprite = panelSprite;
        topBg.color = new Color(0.1f, 0.14f, 0.22f, 0.92f);

        CreateResourceBadge(topArea.transform, new Vector2(-150, -55), coinSprite, "12,500 G", Color.yellow);
        CreateResourceBadge(topArea.transform, new Vector2(150, -55), gemSprite, "350 GEM", new Color(0.3f, 0.85f, 1f));

        // 2-3. BarBG (하단 바 배경 - 로열 블루 + 상단 골드 라인 + 구분선)
        GameObject barBgObj = new GameObject("BarBG");
        barBgObj.transform.SetParent(safePanel.transform, false);
        RectTransform barBgRect = barBgObj.AddComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0, 0);
        barBgRect.anchorMax = new Vector2(1, 0);
        barBgRect.pivot = new Vector2(0.5f, 0);
        barBgRect.sizeDelta = new Vector2(0, 130);
        barBgRect.anchoredPosition = Vector2.zero;

        Image barBgImg = barBgObj.AddComponent<Image>();
        barBgImg.color = new Color(0.04f, 0.20f, 0.48f, 1f); // 로열 블루

        // 상단 골드 테두리 라인
        GameObject goldLine = new GameObject("TopGoldLine");
        goldLine.transform.SetParent(barBgObj.transform, false);
        RectTransform goldRect = goldLine.AddComponent<RectTransform>();
        goldRect.anchorMin = new Vector2(0, 1);
        goldRect.anchorMax = new Vector2(1, 1);
        goldRect.pivot = new Vector2(0.5f, 1);
        goldRect.sizeDelta = new Vector2(0, 4);
        goldRect.anchoredPosition = Vector2.zero;
        Image goldImg = goldLine.AddComponent<Image>();
        goldImg.color = new Color(1f, 0.74f, 0.05f, 1f); // 황금색

        // 세로 구분선 (Dividers)
        CreateDivider("Divider_1", barBgObj.transform, -120f);
        CreateDivider("Divider_2", barBgObj.transform, 120f);

        // 2-4. TabButtonParent (3개 탭 버튼 컨테이너 - RectTransform & HorizontalLayoutGroup)
        GameObject tabParentObj = new GameObject("TabButtonParent");
        tabParentObj.transform.SetParent(safePanel.transform, false);
        RectTransform tabParentRect = tabParentObj.AddComponent<RectTransform>();
        tabParentRect.anchorMin = new Vector2(0, 0);
        tabParentRect.anchorMax = new Vector2(1, 0);
        tabParentRect.pivot = new Vector2(0.5f, 0);
        tabParentRect.anchoredPosition = new Vector2(0, -50);
        tabParentRect.sizeDelta = new Vector2(0, 200);

        HorizontalLayoutGroup hlg = tabParentObj.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.spacing = 0;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = false;
        hlg.childScaleWidth = false;
        hlg.childScaleHeight = false;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        BottomTabBar bottomTabBar = tabParentObj.AddComponent<BottomTabBar>();

        Sprite shopIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/TabIcon_Shop.png");
        Sprite homeIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/TabIcon_Home.png");
        Sprite rankIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/TabIcon_Leaderboard.png");

        Button btnShop = CreateTabButton("TabButtonShop", tabParentObj.transform, "SHOP", shopIcon);
        Button btnHome = CreateTabButton("TabButtonHome", tabParentObj.transform, "HOME", homeIcon);
        Button btnRank = CreateTabButton("TabButtonLeaderboard", tabParentObj.transform, "LEADERBOARD", rankIcon);

        SerializedObject btbSo = new SerializedObject(bottomTabBar);
        SerializedProperty tabsProp = btbSo.FindProperty("tabs");
        tabsProp.ClearArray();
        AddTabToBar(tabsProp, 0, btnShop, "SHOP");
        AddTabToBar(tabsProp, 1, btnHome, "HOME");
        AddTabToBar(tabsProp, 2, btnRank, "LEADERBOARD");
        btbSo.ApplyModifiedProperties();

        SerializedObject mpSo = new SerializedObject(mainPageView);
        mpSo.FindProperty("canvas").objectReferenceValue = canvas;
        mpSo.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        mpSo.FindProperty("swipeTabController").objectReferenceValue = swipeCtrl;
        mpSo.FindProperty("bottomTabBar").objectReferenceValue = bottomTabBar;
        mpSo.FindProperty("startGameButton").objectReferenceValue = btnStart;
        mpSo.ApplyModifiedProperties();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rootObj, MAIN_PAGE_PREFAB_PATH);
        Object.DestroyImmediate(rootObj);
        return prefab;
    }

    #endregion

    #region PlayPage Prefab Builder (UI_Canvas and Game_Root Siblings)

    private static GameObject CreatePlayPagePrefab(Camera mainCam)
    {
        // 1. 최상위 루트: 절대 Canvas가 아닌 일반 GameObject (Transform)
        GameObject rootObj = new GameObject("PlayPage");
        PlayPageView playPageView = rootObj.AddComponent<PlayPageView>();

        // 2. 자식 1: UI_Canvas (Canvas, Screen Space - Camera)
        GameObject uiCanvasObj = new GameObject("UI_Canvas");
        uiCanvasObj.transform.SetParent(rootObj.transform, false);

        Canvas canvas = uiCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = mainCam;
        canvas.planeDistance = 10f;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = uiCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 1280);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        uiCanvasObj.AddComponent<GraphicRaycaster>();
        CanvasGroup canvasGroup = uiCanvasObj.AddComponent<CanvasGroup>();

        GameObject safePanel = new GameObject("SafeAreaPanel");
        safePanel.transform.SetParent(uiCanvasObj.transform, false);
        RectTransform safeRect = safePanel.AddComponent<RectTransform>();
        safeRect.anchorMin = Vector2.zero;
        safeRect.anchorMax = Vector2.one;
        safeRect.sizeDelta = Vector2.zero;
        safePanel.AddComponent<SafeAreaHelper>();

        Sprite containerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Containers/Large/homepage-icon-container-large.png");

        GameObject topHudObj = new GameObject("TopHUD");
        topHudObj.transform.SetParent(safePanel.transform, false);
        RectTransform thRect = topHudObj.AddComponent<RectTransform>();
        thRect.anchorMin = new Vector2(0, 1);
        thRect.anchorMax = new Vector2(1, 1);
        thRect.pivot = new Vector2(0.5f, 1);
        thRect.sizeDelta = new Vector2(0, 160);
        thRect.anchoredPosition = Vector2.zero;

        Image thBg = topHudObj.AddComponent<Image>();
        if (containerSprite != null) thBg.sprite = containerSprite;
        thBg.color = new Color(0.08f, 0.12f, 0.18f, 0.95f);

        TopHUDView topHudView = topHudObj.AddComponent<TopHUDView>();
        GameObject txtStage = CreateTMP("Txt_Stage", topHudObj.transform, new Vector2(0.5f, 0.75f), new Vector2(300, 35), "SECTOR 1", 26, TextAlignmentOptions.Center, new Color(0.3f, 0.85f, 1f));
        GameObject txtGold = CreateTMP("Txt_Gold", topHudObj.transform, new Vector2(0.5f, 0.45f), new Vector2(300, 35), "0 G", 24, TextAlignmentOptions.Center, Color.yellow);
        GameObject hpSliderObj = CreateHPSlider(topHudObj.transform);
        TextMeshProUGUI hpTxt = hpSliderObj.transform.Find("Txt_Hp").GetComponent<TextMeshProUGUI>();

        SerializedObject thSo = new SerializedObject(topHudView);
        thSo.FindProperty("stageText").objectReferenceValue = txtStage.GetComponent<TextMeshProUGUI>();
        thSo.FindProperty("goldText").objectReferenceValue = txtGold.GetComponent<TextMeshProUGUI>();
        thSo.FindProperty("hpSlider").objectReferenceValue = hpSliderObj.GetComponent<Slider>();
        thSo.FindProperty("hpText").objectReferenceValue = hpTxt;
        thSo.ApplyModifiedProperties();

        Sprite redBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/short-pink-large.png");
        Button btnLobby = CreateSFActionButton("Btn_Lobby", topHudObj.transform, new Vector2(280, -40), new Vector2(120, 50), "LOBBY", redBtnSprite, Color.white);

        GameObject bottomPanelObj = new GameObject("BottomPanel");
        bottomPanelObj.transform.SetParent(safePanel.transform, false);
        RectTransform bpRect = bottomPanelObj.AddComponent<RectTransform>();
        bpRect.anchorMin = new Vector2(0, 0);
        bpRect.anchorMax = new Vector2(1, 0);
        bpRect.pivot = new Vector2(0.5f, 0);
        bpRect.sizeDelta = new Vector2(0, 320);
        bpRect.anchoredPosition = Vector2.zero;

        Image bpBg = bottomPanelObj.AddComponent<Image>();
        if (containerSprite != null) bpBg.sprite = containerSprite;
        bpBg.color = new Color(0.08f, 0.1f, 0.16f, 0.98f);

        BottomPanelView bottomView = bottomPanelObj.AddComponent<BottomPanelView>();
        Sprite blueBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/large-blue-large.png");
        Sprite purpleBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/large-purple-large.png");
        Sprite yellowBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/large-yellow-large.png");

        Button btnAtk = CreateSFActionButton("Btn_Attack", bottomPanelObj.transform, new Vector2(-210, 40), new Vector2(180, 80), "ATTACK (+Gold)", blueBtnSprite, Color.white);
        Button btnSkill = CreateSFActionButton("Btn_Skill", bottomPanelObj.transform, new Vector2(0, 40), new Vector2(180, 80), "LASER (Area)", purpleBtnSprite, Color.white);
        Button btnUpg = CreateSFActionButton("Btn_Upgrade", bottomPanelObj.transform, new Vector2(210, 40), new Vector2(180, 80), "UPGRADE (100 G)", yellowBtnSprite, Color.white);

        GameObject logTxtObj = CreateTMP("Txt_BattleLog", bottomPanelObj.transform, new Vector2(0.5f, 0.85f), new Vector2(660, 40), "Space combat system ready.", 18, TextAlignmentOptions.Center, new Color(0.7f, 0.85f, 1f));

        SerializedObject bpSo = new SerializedObject(bottomView);
        bpSo.FindProperty("attackButton").objectReferenceValue = btnAtk;
        bpSo.FindProperty("skillButton").objectReferenceValue = btnSkill;
        bpSo.FindProperty("upgradeButton").objectReferenceValue = btnUpg;
        bpSo.FindProperty("logText").objectReferenceValue = logTxtObj.GetComponent<TextMeshProUGUI>();
        bpSo.ApplyModifiedProperties();

        // 3. 자식 2: Game_Root (일반 Transform, Sibling! Canvas 밖 순수 2D World Space!)
        GameObject gameRootObj = new GameObject("Game_Root");
        gameRootObj.transform.SetParent(rootObj.transform, false);
        gameRootObj.transform.position = Vector3.zero;

        GameRootController gameRootCtrl = gameRootObj.AddComponent<GameRootController>();

        GameObject envObj = new GameObject("Environment");
        envObj.transform.SetParent(gameRootObj.transform, false);

        GameObject stageContentObj = new GameObject("StageContent");
        stageContentObj.transform.SetParent(gameRootObj.transform, false);
        IdleRpgGameContent idleRpg = stageContentObj.AddComponent<IdleRpgGameContent>();

        GameObject playerObj = new GameObject("Player_Fighter");
        playerObj.transform.SetParent(stageContentObj.transform, false);
        playerObj.transform.localPosition = new Vector3(-1.2f, 1.2f, 0f);
        playerObj.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
        SpriteRenderer pSr = playerObj.AddComponent<SpriteRenderer>();
        pSr.sortingOrder = 2;

        Sprite dishSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Icons/dish-128.png");
        if (dishSprite != null) pSr.sprite = dishSprite;
        else pSr.color = new Color(0.3f, 0.85f, 1f);

        GameObject monsterObj = new GameObject("Alien_Hostile");
        monsterObj.transform.SetParent(stageContentObj.transform, false);
        monsterObj.transform.localPosition = new Vector3(1.2f, 1.2f, 0f);
        monsterObj.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
        SpriteRenderer mSr = monsterObj.AddComponent<SpriteRenderer>();
        mSr.sortingOrder = 2;

        Sprite alienSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Icons/alien-128.png");
        if (alienSprite != null) mSr.sprite = alienSprite;
        else mSr.color = new Color(1f, 0.4f, 0.4f);

        SerializedObject idleSo = new SerializedObject(idleRpg);
        idleSo.FindProperty("playerTransform").objectReferenceValue = playerObj.transform;
        idleSo.FindProperty("playerRenderer").objectReferenceValue = pSr;
        idleSo.FindProperty("monsterTransform").objectReferenceValue = monsterObj.transform;
        idleSo.FindProperty("monsterRenderer").objectReferenceValue = mSr;
        idleSo.ApplyModifiedProperties();

        GameObject vfxObj = new GameObject("VFXRoot");
        vfxObj.transform.SetParent(gameRootObj.transform, false);

        SerializedObject grcSo = new SerializedObject(gameRootCtrl);
        grcSo.FindProperty("contentModuleMono").objectReferenceValue = idleRpg;
        grcSo.FindProperty("environmentRoot").objectReferenceValue = envObj.transform;
        grcSo.FindProperty("entityRoot").objectReferenceValue = stageContentObj.transform;
        grcSo.FindProperty("vfxRoot").objectReferenceValue = vfxObj.transform;
        grcSo.ApplyModifiedProperties();

        SerializedObject ppSo = new SerializedObject(playPageView);
        ppSo.FindProperty("canvas").objectReferenceValue = canvas;
        ppSo.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        ppSo.FindProperty("uiCanvas").objectReferenceValue = canvas;
        ppSo.FindProperty("gameRoot").objectReferenceValue = gameRootCtrl;
        ppSo.FindProperty("topHUDView").objectReferenceValue = topHudView;
        ppSo.FindProperty("bottomPanelView").objectReferenceValue = bottomView;
        ppSo.FindProperty("lobbyButton").objectReferenceValue = btnLobby;
        ppSo.ApplyModifiedProperties();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rootObj, PLAY_PAGE_PREFAB_PATH);
        Object.DestroyImmediate(rootObj);
        return prefab;
    }

    #endregion

    #region Scene Setup

    private static void SetupCleanPlayScene(GameObject mainPagePrefab, GameObject playPagePrefab, Camera mainCam)
    {
        string scenePath = "Assets/Scenes/Play.unity";
        Scene playScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // 불필요한 MainGameScene 제거 (옥상옥 껍데기 완전 삭제)
        GameObject oldMainScene = GameObject.Find("MainGameScene");
        if (oldMainScene != null)
        {
            Object.DestroyImmediate(oldMainScene);
        }

        // PageManager (씬 루트 레벨 싱글톤)
        PageManager pageManager = Object.FindAnyObjectByType<PageManager>();
        GameObject pageMgrObj;
        if (pageManager == null)
        {
            pageMgrObj = new GameObject("PageManager");
            pageManager = pageMgrObj.AddComponent<PageManager>();
        }
        else
        {
            pageMgrObj = pageManager.gameObject;
            pageMgrObj.name = "PageManager";
        }

        // PagesRoot
        Transform pagesRootT = pageMgrObj.transform.Find("PagesRoot");
        GameObject pagesRoot = pagesRootT != null ? pagesRootT.gameObject : new GameObject("PagesRoot");
        pagesRoot.transform.SetParent(pageMgrObj.transform, false);

        // PopupRoot
        Transform popupRootT = pageMgrObj.transform.Find("PopupRoot");
        GameObject popupRoot = popupRootT != null ? popupRootT.gameObject : new GameObject("PopupRoot");
        popupRoot.transform.SetParent(pageMgrObj.transform, false);

        SerializedObject pmSo = new SerializedObject(pageManager);
        pmSo.FindProperty("mainPagePrefab").objectReferenceValue = mainPagePrefab.GetComponent<SceneBase>();
        pmSo.FindProperty("playPagePrefab").objectReferenceValue = playPagePrefab.GetComponent<SceneBase>();
        pmSo.FindProperty("mainCamera").objectReferenceValue = mainCam;
        pmSo.FindProperty("pagesRoot").objectReferenceValue = pagesRoot.transform;
        pmSo.FindProperty("popupRoot").objectReferenceValue = popupRoot.transform;
        pmSo.FindProperty("initialPage").enumValueIndex = (int)UIPageType.MainPage;
        pmSo.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(playScene, scenePath);
        Debug.Log("[PagePrefabBuilder] Play.unity 씬이 MainGameScene 없이 클린한 PageManager 단일 구조로 리팩토링되었습니다.");
    }

    #endregion

    #region UI Helper Methods

    private static void CreateResourceBadge(Transform parent, Vector2 pos, Sprite icon, string amount, Color textColor)
    {
        GameObject badge = new GameObject("Badge");
        badge.transform.SetParent(parent, false);
        RectTransform rt = badge.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(180, 45);
        rt.anchoredPosition = pos;

        if (icon != null)
        {
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(badge.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 0.5f);
            iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0, 0.5f);
            iconRt.sizeDelta = new Vector2(36, 36);
            iconRt.anchoredPosition = new Vector2(6, 0);

            Image img = iconObj.AddComponent<Image>();
            img.sprite = icon;
            img.preserveAspect = true;
        }

        CreateTMP("Txt_Amount", badge.transform, new Vector2(0.6f, 0.5f), new Vector2(120, 35), amount, 20, TextAlignmentOptions.Left, textColor);
    }

    private static Button CreateSFNavButton(string name, Transform parent, Vector2 pos, string label, Sprite btnSprite)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(140, 80);
        rt.anchoredPosition = pos;

        Image img = obj.AddComponent<Image>();
        if (btnSprite != null) img.sprite = btnSprite;
        img.color = Color.white;

        Button btn = obj.AddComponent<Button>();
        CreateTMP("Text", obj.transform, Vector2.zero, Vector2.one, Vector2.zero, label, 20, TextAlignmentOptions.Center, Color.white);
        return btn;
    }

    private static Button CreateSFActionButton(string name, Transform parent, Vector2 pos, Vector2 size, string label, Sprite btnSprite, Color tint)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = obj.AddComponent<Image>();
        if (btnSprite != null) img.sprite = btnSprite;
        img.color = tint;

        Button btn = obj.AddComponent<Button>();
        CreateTMP("Text", obj.transform, Vector2.zero, Vector2.one, Vector2.zero, label, 22, TextAlignmentOptions.Center, Color.white);
        return btn;
    }

    private static GameObject CreateTabContent(Transform parent, int index, string header, string title, string desc, Sprite containerSprite)
    {
        GameObject tabObj = new GameObject($"Tab_{index}_{header}");
        tabObj.transform.SetParent(parent, false);
        RectTransform rt = tabObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 0.5f);
        rt.sizeDelta = new Vector2(720, 0);
        rt.anchoredPosition = new Vector2(index * 720, 0);

        GameObject containerObj = new GameObject("Container");
        containerObj.transform.SetParent(tabObj.transform, false);
        RectTransform cRt = containerObj.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.5f, 0.5f);
        cRt.anchorMax = new Vector2(0.5f, 0.5f);
        cRt.sizeDelta = new Vector2(640, 720);
        cRt.anchoredPosition = new Vector2(0, 20);

        Image cImg = containerObj.AddComponent<Image>();
        if (containerSprite != null)
        {
            cImg.sprite = containerSprite;
            cImg.type = Image.Type.Sliced;
        }
        cImg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

        CreateTMP("Txt_Header", containerObj.transform, new Vector2(0.5f, 0.85f), new Vector2(500, 40), header, 24, TextAlignmentOptions.Center, new Color(0.4f, 0.85f, 1f));
        CreateTMP("Txt_Title", containerObj.transform, new Vector2(0.5f, 0.72f), new Vector2(560, 50), title, 28, TextAlignmentOptions.Center, Color.white);
        CreateTMP("Txt_Desc", containerObj.transform, new Vector2(0.5f, 0.45f), new Vector2(560, 160), desc, 20, TextAlignmentOptions.Center, new Color(0.75f, 0.8f, 0.9f));

        return tabObj;
    }

    private static GameObject CreateHPSlider(Transform parent)
    {
        GameObject sliderObj = new GameObject("HpSlider");
        sliderObj.transform.SetParent(parent, false);
        RectTransform rt = sliderObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.15f);
        rt.anchorMax = new Vector2(0.5f, 0.15f);
        rt.sizeDelta = new Vector2(400, 24);
        rt.anchoredPosition = Vector2.zero;

        Slider slider = sliderObj.AddComponent<Slider>();

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero;
        faRt.anchorMax = Vector2.one;
        faRt.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fRt = fill.AddComponent<RectTransform>();
        fRt.sizeDelta = Vector2.zero;
        Image fImg = fill.AddComponent<Image>();
        fImg.color = new Color(0.2f, 0.85f, 0.4f);

        slider.fillRect = fRt;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 1;

        CreateTMP("Txt_Hp", sliderObj.transform, Vector2.zero, Vector2.one, Vector2.zero, "100 / 100", 18, TextAlignmentOptions.Center, Color.white);
        return sliderObj;
    }

    private static GameObject CreateTMP(string name, Transform parent, Vector2 anchor, Vector2 size, string text, float fontSize, TextAlignmentOptions align, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = color;
        return obj;
    }

    private static GameObject CreateDivider(string name, Transform parent, float xPos)
    {
        GameObject div = new GameObject(name);
        div.transform.SetParent(parent, false);
        RectTransform rt = div.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(2, 70);
        rt.anchoredPosition = new Vector2(xPos, 0);

        Image img = div.AddComponent<Image>();
        img.color = new Color(0.15f, 0.35f, 0.65f, 0.4f);
        img.raycastTarget = false;
        return div;
    }

    private static Button CreateTabButton(string name, Transform parent, string label, Sprite iconSprite)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 200);

        // 클릭 감지용 투명 배경
        Image clickTarget = btnObj.AddComponent<Image>();
        clickTarget.color = Color.clear;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = clickTarget;

        // 1. Icon (기본 중앙 위치, 선택 시 위로 돌출 및 확대)
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform, false);
        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = new Vector2(68, 68);
        iconRt.anchoredPosition = Vector2.zero;

        Image iconImg = iconObj.AddComponent<Image>();
        if (iconSprite != null)
        {
            iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;
        }
        iconImg.raycastTarget = false;

        // 2. Label (아이콘 바로 아래 위치, 선택 시에만 표시)
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        RectTransform labelRt = labelObj.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 0.5f);
        labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        labelRt.pivot = new Vector2(0.5f, 0.5f);
        labelRt.sizeDelta = new Vector2(160, 26);
        labelRt.anchoredPosition = new Vector2(0, -26);

        TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 17;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        labelObj.SetActive(false);

        return btn;
    }

    private static void AddTabToBar(SerializedProperty tabsProp, int index, Button btn, string tabName)
    {
        tabsProp.InsertArrayElementAtIndex(index);
        SerializedProperty itemProp = tabsProp.GetArrayElementAtIndex(index);
        itemProp.FindPropertyRelative("tabName").stringValue = tabName;
        itemProp.FindPropertyRelative("button").objectReferenceValue = btn;

        Transform iconT = btn.transform.Find("Icon");
        if (iconT != null)
        {
            itemProp.FindPropertyRelative("iconRect").objectReferenceValue = iconT.GetComponent<RectTransform>();
            itemProp.FindPropertyRelative("iconImage").objectReferenceValue = iconT.GetComponent<Image>();
        }

        Transform labelT = btn.transform.Find("Label");
        if (labelT != null)
        {
            itemProp.FindPropertyRelative("labelText").objectReferenceValue = labelT.GetComponent<TextMeshProUGUI>();
            itemProp.FindPropertyRelative("labelRect").objectReferenceValue = labelT.GetComponent<RectTransform>();
        }
    }
    
    private static GameObject CreateTMP(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, string text, float fontSize, TextAlignmentOptions align, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = color;
        return obj;
    }

    #endregion
}