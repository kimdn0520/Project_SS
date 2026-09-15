using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class PagePrefabBuilder
{
    private const string PREFAB_DIR = "Assets/Resources/Prefabs";
    private const string MAIN_PAGE_PREFAB_PATH = "Assets/Resources/Prefabs/MainPage.prefab";
    private const string PLAY_PAGE_PREFAB_PATH = "Assets/Resources/Prefabs/PlayPage.prefab";

    [MenuItem("ProjectSS/Build Page Prefabs")]
    public static void Execute()
    {
        // Keep legacy setup entry points on the new direct-to-Play architecture.
        ProjectSS.Expedition.Editor.ExpeditionBuilder.Build();
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
        cam.backgroundColor = new Color(0.42f, 0.76f, 0.98f, 1f);
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

    #region PlayPage Prefab Builder (v2: UI_Canvas and Game_Root Siblings with WorldAreaLayoutBinder)

    private static GameObject CreatePlayPagePrefab(Camera mainCam)
    {
        // 1. 최상위 루트: 일반 GameObject (Transform)
        GameObject rootObj = new GameObject("PlayPage");
        PlayPageView playPageView = rootObj.AddComponent<PlayPageView>();

        // 2. 자식 1: UI_Canvas (Screen Space - Camera, planeDistance = 1)
        GameObject uiCanvasObj = new GameObject("UI_Canvas");
        uiCanvasObj.transform.SetParent(rootObj.transform, false);

        Canvas canvas = uiCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = mainCam;
        canvas.planeDistance = 1.0f;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = uiCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.0f; // Width 기준 고정

        uiCanvasObj.AddComponent<GraphicRaycaster>();
        CanvasGroup canvasGroup = uiCanvasObj.AddComponent<CanvasGroup>();

        // 2-1. SafeAreaRoot
        GameObject safeAreaObj = new GameObject("SafeAreaRoot");
        safeAreaObj.transform.SetParent(uiCanvasObj.transform, false);
        RectTransform safeRect = safeAreaObj.AddComponent<RectTransform>();
        safeRect.anchorMin = Vector2.zero;
        safeRect.anchorMax = Vector2.one;
        safeRect.sizeDelta = Vector2.zero;
        safeAreaObj.AddComponent<SafeAreaHelper>();

        Sprite containerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Containers/Large/homepage-icon-container-large.png");

        // 2-2. TopHudRegion (높이 112)
        GameObject topHudObj = new GameObject("TopHudRegion");
        topHudObj.transform.SetParent(safeAreaObj.transform, false);
        RectTransform thRect = topHudObj.AddComponent<RectTransform>();
        thRect.anchorMin = new Vector2(0, 1);
        thRect.anchorMax = new Vector2(1, 1);
        thRect.pivot = new Vector2(0.5f, 1);
        thRect.sizeDelta = new Vector2(0, 112);
        thRect.anchoredPosition = Vector2.zero;

        Image thBg = topHudObj.AddComponent<Image>();
        if (containerSprite != null) thBg.sprite = containerSprite;
        thBg.color = new Color(0.04f, 0.06f, 0.10f, 0.70f);

        TopHUDView topHudView = topHudObj.AddComponent<TopHUDView>();
        GameObject txtStage = CreateTMP("Txt_Stage", topHudObj.transform, new Vector2(0.5f, 1f), new Vector2(400, 32), "스테이지 1-1", 24, TextAlignmentOptions.Center, new Color(0.3f, 0.9f, 1f));
        txtStage.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -22);

        GameObject hpSliderObj = CreateHPSlider(topHudObj.transform);
        hpSliderObj.SetActive(false); // 보스/적 체력은 전투 영역 BattleHud로 분리
        TextMeshProUGUI hpTxt = hpSliderObj.transform.Find("Txt_Hp")?.GetComponent<TextMeshProUGUI>();

        // 재화 및 심도 표시 (3열 정렬: 골드, 다이아, 심도 - Y = -75)
        GameObject txtGold = CreateTMP("Txt_Gold", topHudObj.transform, new Vector2(0.5f, 1f), new Vector2(240, 30), "0 골드", 20, TextAlignmentOptions.Center, Color.yellow);
        txtGold.GetComponent<RectTransform>().anchoredPosition = new Vector2(-260, -75);

        GameObject txtGem = CreateTMP("Txt_Gem", topHudObj.transform, new Vector2(0.5f, 1f), new Vector2(240, 30), "10 다이아", 20, TextAlignmentOptions.Center, Color.cyan);
        txtGem.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -75);

        GameObject txtDepth = CreateTMP("Txt_Depth", topHudObj.transform, new Vector2(0.5f, 1f), new Vector2(240, 30), "지하 1미터", 20, TextAlignmentOptions.Center, new Color(1f, 0.65f, 0.2f));
        txtDepth.GetComponent<RectTransform>().anchoredPosition = new Vector2(260, -75);

        SerializedObject thSo = new SerializedObject(topHudView);
        thSo.FindProperty("stageText").objectReferenceValue = txtStage.GetComponent<TextMeshProUGUI>();
        thSo.FindProperty("hpSlider").objectReferenceValue = hpSliderObj.GetComponent<Slider>();
        if (hpTxt != null) thSo.FindProperty("hpText").objectReferenceValue = hpTxt;
        thSo.FindProperty("goldText").objectReferenceValue = txtGold.GetComponent<TextMeshProUGUI>();
        thSo.FindProperty("gemText").objectReferenceValue = txtGem.GetComponent<TextMeshProUGUI>();
        thSo.FindProperty("depthText").objectReferenceValue = txtDepth.GetComponent<TextMeshProUGUI>();
        thSo.ApplyModifiedProperties();

        // 2-3. GameplayRegion (TopHUD와 ActionRegion을 뺀 중간 영역)
        GameObject gameplayRegionObj = new GameObject("GameplayRegion");
        gameplayRegionObj.transform.SetParent(safeAreaObj.transform, false);
        RectTransform gpRect = gameplayRegionObj.AddComponent<RectTransform>();
        gpRect.anchorMin = new Vector2(0, 0);
        gpRect.anchorMax = new Vector2(1, 1);
        gpRect.offsetMin = new Vector2(0, 320);
        gpRect.offsetMax = new Vector2(0, -112);

        // 2-3-1. BattleRegion (상단 40%, 0.60 ~ 1.0)
        GameObject battleRegionObj = new GameObject("BattleRegion");
        battleRegionObj.transform.SetParent(gameplayRegionObj.transform, false);
        RectTransform brRect = battleRegionObj.AddComponent<RectTransform>();
        brRect.anchorMin = new Vector2(0, 0.60f);
        brRect.anchorMax = new Vector2(1, 1f);
        brRect.offsetMin = Vector2.zero;
        brRect.offsetMax = Vector2.zero;

        // BattleHud (적 체력바 게이지)
        GameObject battleHudObj = new GameObject("BattleHud");
        battleHudObj.transform.SetParent(battleRegionObj.transform, false);
        RectTransform bhRt = battleHudObj.AddComponent<RectTransform>();
        bhRt.anchorMin = new Vector2(0.5f, 1f);
        bhRt.anchorMax = new Vector2(0.5f, 1f);
        bhRt.pivot = new Vector2(0.5f, 1f);
        bhRt.anchoredPosition = new Vector2(0, -20);
        bhRt.sizeDelta = new Vector2(360, 32);

        GameObject battleHpSliderObj = CreateHPSlider(battleHudObj.transform);
        battleHpSliderObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        battleHpSliderObj.GetComponent<RectTransform>().sizeDelta = new Vector2(360, 24);

        // 2-3-2. MiningRegion (하단 60%, 0.0 ~ 0.60)
        GameObject miningRegionObj = new GameObject("MiningRegion");
        miningRegionObj.transform.SetParent(gameplayRegionObj.transform, false);
        RectTransform mrRect = miningRegionObj.AddComponent<RectTransform>();
        mrRect.anchorMin = new Vector2(0, 0f);
        mrRect.anchorMax = new Vector2(1, 0.60f);
        mrRect.offsetMin = Vector2.zero;
        mrRect.offsetMax = Vector2.zero;

        // 사이드바 컨트롤러 (MiningRegion 안쪽에 완벽 격리)
        SidebarController sidebarCtrl = safeAreaObj.AddComponent<SidebarController>();

        // Left Sidebar (화면 좌측 - 퀘스트, 우편, 업적)
        GameObject leftSidebarObj = new GameObject("SidebarLeft");
        leftSidebarObj.transform.SetParent(miningRegionObj.transform, false);
        RectTransform leftRt = leftSidebarObj.AddComponent<RectTransform>();
        leftRt.anchorMin = new Vector2(0, 1f);
        leftRt.anchorMax = new Vector2(0, 1f);
        leftRt.pivot = new Vector2(0, 1f);
        leftRt.anchoredPosition = new Vector2(20, -40);
        leftRt.sizeDelta = new Vector2(96, 320);

        VerticalLayoutGroup leftVlg = leftSidebarObj.AddComponent<VerticalLayoutGroup>();
        leftVlg.childAlignment = TextAnchor.UpperCenter;
        leftVlg.spacing = 16;
        leftVlg.childControlWidth = false;
        leftVlg.childControlHeight = false;

        Button btnQuest = CreateSidebarIconButton("Btn_Quest", leftSidebarObj.transform, "Assets/Layer Lab/2D Minimal-IconPack/Icons/128/UI_Rewards_Badge_04.png");
        Button btnMail = CreateSidebarIconButton("Btn_Mail", leftSidebarObj.transform, "Assets/Layer Lab/2D Minimal-IconPack/Icons/128/UI_Common_Inbox_01.png");
        Button btnAchieve = CreateSidebarIconButton("Btn_Achievement", leftSidebarObj.transform, "Assets/Layer Lab/2D Minimal-IconPack/Icons/128/UI_ETC_Target_01.png");

        // Right Sidebar (화면 우측 - 랭킹, 던전, 상점)
        GameObject rightSidebarObj = new GameObject("SidebarRight");
        rightSidebarObj.transform.SetParent(miningRegionObj.transform, false);
        RectTransform rightRt = rightSidebarObj.AddComponent<RectTransform>();
        rightRt.anchorMin = new Vector2(1, 1f);
        rightRt.anchorMax = new Vector2(1, 1f);
        rightRt.pivot = new Vector2(1, 1f);
        rightRt.anchoredPosition = new Vector2(-20, -40);
        rightRt.sizeDelta = new Vector2(96, 320);

        VerticalLayoutGroup rightVlg = rightSidebarObj.AddComponent<VerticalLayoutGroup>();
        rightVlg.childAlignment = TextAnchor.UpperCenter;
        rightVlg.spacing = 16;
        rightVlg.childControlWidth = false;
        rightVlg.childControlHeight = false;

        Button btnRank = CreateSidebarIconButton("Btn_Rank", rightSidebarObj.transform, "Assets/Layer Lab/2D Minimal-IconPack/Icons/128/UI_Social_Ranking_01_Yellow.png");
        Button btnDungeon = CreateSidebarIconButton("Btn_Dungeon", rightSidebarObj.transform, "Assets/Layer Lab/2D Minimal-IconPack/Icons/128/UI_Battle_Skull_01.png");
        Button btnShop = CreateSidebarIconButton("Btn_Shop", rightSidebarObj.transform, "Assets/Layer Lab/2D Minimal-IconPack/Icons/128/UI_Store_CoinShop_01.png");

        SerializedObject sbSo = new SerializedObject(sidebarCtrl);
        sbSo.FindProperty("questButton").objectReferenceValue = btnQuest;
        sbSo.FindProperty("mailButton").objectReferenceValue = btnMail;
        sbSo.FindProperty("achievementButton").objectReferenceValue = btnAchieve;
        sbSo.FindProperty("rankButton").objectReferenceValue = btnRank;
        sbSo.FindProperty("dungeonButton").objectReferenceValue = btnDungeon;
        sbSo.FindProperty("shopButton").objectReferenceValue = btnShop;
        sbSo.ApplyModifiedProperties();

        // 2-4. ActionRegion (하단 320 높이)
        GameObject actionRegionObj = new GameObject("ActionRegion");
        actionRegionObj.transform.SetParent(safeAreaObj.transform, false);
        RectTransform arRect = actionRegionObj.AddComponent<RectTransform>();
        arRect.anchorMin = new Vector2(0, 0);
        arRect.anchorMax = new Vector2(1, 0);
        arRect.pivot = new Vector2(0.5f, 0);
        arRect.sizeDelta = new Vector2(0, 320);
        arRect.anchoredPosition = Vector2.zero;

        BottomPanelView bottomView = actionRegionObj.AddComponent<BottomPanelView>();

        // 하단 바 배경 (BottomNavBackground: 높이 140)
        GameObject navBgObj = new GameObject("BottomNavBackground");
        navBgObj.transform.SetParent(actionRegionObj.transform, false);
        RectTransform nbRt = navBgObj.AddComponent<RectTransform>();
        nbRt.anchorMin = new Vector2(0, 0);
        nbRt.anchorMax = new Vector2(1, 0);
        nbRt.pivot = new Vector2(0.5f, 0);
        nbRt.sizeDelta = new Vector2(0, 140);
        nbRt.anchoredPosition = Vector2.zero;

        Image nbImg = navBgObj.AddComponent<Image>();
        if (containerSprite != null) nbImg.sprite = containerSprite;
        nbImg.color = new Color(0.08f, 0.10f, 0.16f, 0.95f);

        // 상단 금색 라인
        GameObject goldLineObj = new GameObject("TopGoldLine");
        goldLineObj.transform.SetParent(navBgObj.transform, false);
        RectTransform glRt = goldLineObj.AddComponent<RectTransform>();
        glRt.anchorMin = new Vector2(0, 1);
        glRt.anchorMax = new Vector2(1, 1);
        glRt.pivot = new Vector2(0.5f, 1);
        glRt.sizeDelta = new Vector2(0, 3);
        glRt.anchoredPosition = Vector2.zero;
        Image glImg = goldLineObj.AddComponent<Image>();
        glImg.color = new Color(0.95f, 0.78f, 0.20f, 0.95f);

        // 좌측 메뉴 그룹 (LeftMenuGroup, 너비 370, 높이 140)
        GameObject leftMenuObj = new GameObject("LeftMenuGroup");
        leftMenuObj.transform.SetParent(actionRegionObj.transform, false);
        RectTransform lmRt = leftMenuObj.AddComponent<RectTransform>();
        lmRt.anchorMin = new Vector2(0, 0);
        lmRt.anchorMax = new Vector2(0, 0);
        lmRt.pivot = new Vector2(0, 0);
        lmRt.anchoredPosition = new Vector2(0, 0);
        lmRt.sizeDelta = new Vector2(370, 140);

        HorizontalLayoutGroup lmHlg = leftMenuObj.AddComponent<HorizontalLayoutGroup>();
        lmHlg.childAlignment = TextAnchor.MiddleCenter;
        lmHlg.spacing = 20;
        lmHlg.childControlWidth = false;
        lmHlg.childControlHeight = false;

        Sprite navBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/short-blue-large.png");
        if (navBtnSprite == null)
            navBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/short-yellow-large.png");

        Button btnHero = CreateSFNavButton("Btn_HeroManage", leftMenuObj.transform, Vector2.zero, "용사 관리", navBtnSprite);
        Button btnChallenge = CreateSFNavButton("Btn_Challenge", leftMenuObj.transform, Vector2.zero, "도전", navBtnSprite);

        // 우측 메뉴 그룹 (RightMenuGroup, 너비 370, 높이 140)
        GameObject rightMenuObj = new GameObject("RightMenuGroup");
        rightMenuObj.transform.SetParent(actionRegionObj.transform, false);
        RectTransform rmRt = rightMenuObj.AddComponent<RectTransform>();
        rmRt.anchorMin = new Vector2(1, 0);
        rmRt.anchorMax = new Vector2(1, 0);
        rmRt.pivot = new Vector2(1, 0);
        rmRt.anchoredPosition = new Vector2(0, 0);
        rmRt.sizeDelta = new Vector2(370, 140);

        HorizontalLayoutGroup rmHlg = rightMenuObj.AddComponent<HorizontalLayoutGroup>();
        rmHlg.childAlignment = TextAnchor.MiddleCenter;
        rmHlg.spacing = 20;
        rmHlg.childControlWidth = false;
        rmHlg.childControlHeight = false;

        Button btnInventory = CreateSFNavButton("Btn_Inventory", rightMenuObj.transform, Vector2.zero, "가방", navBtnSprite);
        Button btnSettings = CreateSFNavButton("Btn_Settings", rightMenuObj.transform, Vector2.zero, "설정", navBtnSprite);

        // 중앙 거대 원형 DIG 버튼 (DigButtonRoot: 지름 288, anchoredPosition=(0, 168))
        GameObject digBtnObj = new GameObject("DigButtonRoot");
        digBtnObj.transform.SetParent(actionRegionObj.transform, false);
        RectTransform digRt = digBtnObj.AddComponent<RectTransform>();
        digRt.anchorMin = new Vector2(0.5f, 0f);
        digRt.anchorMax = new Vector2(0.5f, 0f);
        digRt.pivot = new Vector2(0.5f, 0.5f);
        digRt.sizeDelta = new Vector2(288, 288);
        digRt.anchoredPosition = new Vector2(0, 168);

        // Base Visual (외곽 테두리 고정)
        GameObject baseVisObj = new GameObject("ButtonBaseVisual");
        baseVisObj.transform.SetParent(digBtnObj.transform, false);
        RectTransform bvRt = baseVisObj.AddComponent<RectTransform>();
        bvRt.anchorMin = Vector2.zero;
        bvRt.anchorMax = Vector2.one;
        bvRt.sizeDelta = Vector2.zero;
        Image baseImg = baseVisObj.AddComponent<Image>();
        Sprite digBaseSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Dig_Button_Base.png");
        if (digBaseSp != null) baseImg.sprite = digBaseSp;
        baseImg.color = Color.white;
        baseImg.raycastTarget = false;

        // PressableFace (눌리는 볼록한 전면 원형 버튼)
        GameObject faceObj = new GameObject("PressableFace");
        faceObj.transform.SetParent(digBtnObj.transform, false);
        RectTransform faceRt = faceObj.AddComponent<RectTransform>();
        faceRt.anchorMin = Vector2.zero;
        faceRt.anchorMax = Vector2.one;
        faceRt.sizeDelta = new Vector2(-24, -24);
        faceRt.anchoredPosition = Vector2.zero;
        Image faceImg = faceObj.AddComponent<Image>();
        Sprite digFaceSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Dig_Button_Face.png");
        if (digFaceSp != null) faceImg.sprite = digFaceSp;
        faceImg.color = Color.white;
        faceImg.raycastTarget = true;

        // ProgressRing (원형 채굴 게이지)
        GameObject ringObj = new GameObject("ProgressRing");
        ringObj.transform.SetParent(faceObj.transform, false);
        RectTransform ringRt = ringObj.AddComponent<RectTransform>();
        ringRt.anchorMin = Vector2.zero;
        ringRt.anchorMax = Vector2.one;
        ringRt.sizeDelta = Vector2.zero;
        Image ringImg = ringObj.AddComponent<Image>();
        Sprite ringSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Dig_Progress_Ring.png");
        if (ringSp != null) ringImg.sprite = ringSp;
        ringImg.type = Image.Type.Filled;
        ringImg.fillMethod = Image.FillMethod.Radial360;
        ringImg.fillOrigin = (int)Image.Origin360.Top;
        ringImg.fillAmount = 0f;
        ringImg.raycastTarget = false;

        // 중앙 곡괭이 아이콘
        GameObject pickIconObj = new GameObject("Pickaxe_Icon");
        pickIconObj.transform.SetParent(faceObj.transform, false);
        RectTransform piRt = pickIconObj.AddComponent<RectTransform>();
        piRt.anchorMin = new Vector2(0.5f, 0.5f);
        piRt.anchorMax = new Vector2(0.5f, 0.5f);
        piRt.sizeDelta = new Vector2(96, 96);
        piRt.anchoredPosition = new Vector2(0, 16);
        Image piImg = pickIconObj.AddComponent<Image>();
        Sprite pickSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Game/Gear_Weapons_Pickaxe_01.png");
        if (pickSp != null) piImg.sprite = pickSp;
        piImg.preserveAspect = true;
        piImg.raycastTarget = false;

        // DIG 텍스트
        GameObject digTxtObj = CreateTMP("Txt_DigLabel", faceObj.transform, new Vector2(0.5f, 0.22f), new Vector2(160, 32), "DIG", 26, TextAlignmentOptions.Center, new Color(0.20f, 0.12f, 0.02f));
        digTxtObj.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // HoldDigButton 부착 (원형 레이캐스트 필터 및 탭/홀드 계약)
        HoldDigButton holdDig = digBtnObj.AddComponent<HoldDigButton>();
        SerializedObject hdSo = new SerializedObject(holdDig);
        hdSo.FindProperty("rectTransform").objectReferenceValue = digRt;
        hdSo.FindProperty("pressableFace").objectReferenceValue = faceObj.transform;
        hdSo.FindProperty("progressRing").objectReferenceValue = ringImg;
        hdSo.ApplyModifiedProperties();

        SerializedObject bpSo = new SerializedObject(bottomView);
        bpSo.FindProperty("holdDigButton").objectReferenceValue = holdDig;
        bpSo.FindProperty("btnHeroManage").objectReferenceValue = btnHero;
        bpSo.FindProperty("btnChallenge").objectReferenceValue = btnChallenge;
        bpSo.FindProperty("btnInventory").objectReferenceValue = btnInventory;
        bpSo.FindProperty("btnSettings").objectReferenceValue = btnSettings;
        bpSo.ApplyModifiedProperties();

        // 2-5. ToastLayer
        GameObject toastObj = new GameObject("ToastLayer");
        toastObj.transform.SetParent(safeAreaObj.transform, false);
        RectTransform toastRt = toastObj.AddComponent<RectTransform>();
        toastRt.anchorMin = Vector2.zero;
        toastRt.anchorMax = Vector2.one;
        toastRt.sizeDelta = Vector2.zero;

        // 3. ModalLayer (Canvas 직계 최상위 sibling)
        GameObject modalLayerObj = new GameObject("ModalLayer");
        modalLayerObj.transform.SetParent(uiCanvasObj.transform, false);
        RectTransform mlRt = modalLayerObj.AddComponent<RectTransform>();
        mlRt.anchorMin = Vector2.zero;
        mlRt.anchorMax = Vector2.one;
        mlRt.sizeDelta = Vector2.zero;

        // 4. 자식 2: Game_Root (순수 2D World Space)
        GameObject gameRootObj = new GameObject("Game_Root");
        gameRootObj.transform.SetParent(rootObj.transform, false);
        gameRootObj.transform.position = Vector3.zero;

        GameRootController gameRootCtrl = gameRootObj.AddComponent<GameRootController>();
        DiggerGameContent diggerGame = gameRootObj.AddComponent<DiggerGameContent>();

        Sprite whiteSquare = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/WhiteSquare.png");

        // 4-1. BattleAreaRoot (상단 40% 전투 월드)
        GameObject battleAreaObj = new GameObject("BattleAreaRoot");
        battleAreaObj.transform.SetParent(gameRootObj.transform, false);
        battleAreaObj.transform.localPosition = new Vector3(0f, 2.76f, 0f);

        // BattleAreaMask (SpriteMask)
        GameObject bMaskObj = new GameObject("BattleAreaMask");
        bMaskObj.transform.SetParent(battleAreaObj.transform, false);
        bMaskObj.transform.localScale = new Vector3(12.0f, 6.0f, 1f);
        SpriteMask bMask = bMaskObj.AddComponent<SpriteMask>();
        if (whiteSquare != null) bMask.sprite = whiteSquare;

        GameObject bVisualObj = new GameObject("BattleVisualRoot");
        bVisualObj.transform.SetParent(battleAreaObj.transform, false);

        // BackgroundRoot & ParallaxScroller
        GameObject bgRootObj = new GameObject("BackgroundRoot");
        bgRootObj.transform.SetParent(bVisualObj.transform, false);
        ParallaxScroller scroller = bgRootObj.AddComponent<ParallaxScroller>();

        Sprite[] mapSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Layer Lab/2D Maps - Simple Sidescroll/Extenstions/Simple Sidescroll1/Sprite/map1.png").OfType<Sprite>().ToArray();
        var sDict = mapSprites.ToDictionary(s => s.name, s => s);
        const float WrapWidth = 14.0f;

        if (sDict.TryGetValue("map01_12", out Sprite sprSky))
        {
            GameObject skyA = CreateSpriteTile("Sky_TileA", bgRootObj.transform, sprSky, new Vector3(0f, 1.8f, 0f), Vector3.one, -12, SpriteDrawMode.Sliced, new Vector2(14.5f, 4.5f));
            GameObject skyB = CreateSpriteTile("Sky_TileB", bgRootObj.transform, sprSky, new Vector3(WrapWidth, 1.8f, 0f), Vector3.one, -12, SpriteDrawMode.Sliced, new Vector2(14.5f, 4.5f));
            SetMaskInteraction(skyA); SetMaskInteraction(skyB);
            scroller.AddLayer("Sky", 0.05f, new List<Transform> { skyA.transform, skyB.transform }, WrapWidth);
        }

        if (sDict.TryGetValue("map01_11", out Sprite sprForest))
        {
            GameObject mtnA = new GameObject("Mtn_A");
            mtnA.transform.SetParent(bgRootObj.transform, false);
            mtnA.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            var f1 = CreateSpriteTile("Forest_1", mtnA.transform, sprForest, new Vector3(-3.2f, 0f, 0f), new Vector3(0.75f, 0.75f, 1f), -10);
            var f2 = CreateSpriteTile("Forest_2", mtnA.transform, sprForest, new Vector3(2.5f, 0f, 0f), new Vector3(0.75f, 0.75f, 1f), -10);
            SetMaskInteraction(f1); SetMaskInteraction(f2);

            GameObject mtnB = new GameObject("Mtn_B");
            mtnB.transform.SetParent(bgRootObj.transform, false);
            mtnB.transform.localPosition = new Vector3(WrapWidth, 1.4f, 0f);
            var f3 = CreateSpriteTile("Forest_1", mtnB.transform, sprForest, new Vector3(-3.2f, 0f, 0f), new Vector3(0.75f, 0.75f, 1f), -10);
            var f4 = CreateSpriteTile("Forest_2", mtnB.transform, sprForest, new Vector3(2.5f, 0f, 0f), new Vector3(0.75f, 0.75f, 1f), -10);
            SetMaskInteraction(f3); SetMaskInteraction(f4);

            scroller.AddLayer("Forest", 0.20f, new List<Transform> { mtnA.transform, mtnB.transform }, WrapWidth);
        }

        // 지면 레이어 (map01_01)
        if (sDict.TryGetValue("map01_01", out Sprite sprGround))
        {
            GameObject gA = CreateSpriteTile("Ground_A", bgRootObj.transform, sprGround, new Vector3(0f, -0.9f, 0f), Vector3.one, -2, SpriteDrawMode.Sliced, new Vector2(14.5f, 2.2f));
            GameObject gB = CreateSpriteTile("Ground_B", bgRootObj.transform, sprGround, new Vector3(WrapWidth, -0.9f, 0f), Vector3.one, -2, SpriteDrawMode.Sliced, new Vector2(14.5f, 2.2f));
            SetMaskInteraction(gA); SetMaskInteraction(gB);
            scroller.AddLayer("Ground", 1.0f, new List<Transform> { gA.transform, gB.transform }, WrapWidth);
        }

        // CombatRoot
        GameObject combatRootObj = new GameObject("CombatRoot");
        combatRootObj.transform.SetParent(bVisualObj.transform, false);

        BattleController battleCtrl = combatRootObj.AddComponent<BattleController>();

        // ActiveHero 생성 (SampleCharacter 공식 프리팹 인스턴스)
        GameObject charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Layer Lab/2D Maps - Simple Sidescroll/Common/SampleCharacter/Prefab/SampleCharacter.prefab");
        GameObject heroObj;
        if (charPrefab != null)
        {
            heroObj = PrefabUtility.InstantiatePrefab(charPrefab, combatRootObj.transform) as GameObject;
            heroObj.name = "ActiveHero";
        }
        else
        {
            heroObj = new GameObject("ActiveHero");
            heroObj.transform.SetParent(combatRootObj.transform, false);
        }
        heroObj.transform.localPosition = new Vector3(-2.2f, -0.45f, 0f);
        heroObj.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        // SampleCharacterMover 컴포넌트 제거 (자동 이동 방지)
        var heroMover = heroObj.GetComponent<LayerLab.SampleCharacterMover>();
        if (heroMover != null) Object.DestroyImmediate(heroMover);

        // Sword 활성화, 다른 무기들 비활성화
        Transform rightHand = heroObj.transform.Find("Character_20260617_193302/HandRight");
        Transform swordTrans = null;
        if (rightHand != null)
        {
            for (int i = 0; i < rightHand.childCount; i++)
            {
                var child = rightHand.GetChild(i);
                if (child.name == "Sword")
                {
                    child.gameObject.SetActive(true);
                    swordTrans = child;
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        ActiveHeroActor heroActor = heroObj.AddComponent<ActiveHeroActor>();
        SerializedObject ahaSo = new SerializedObject(heroActor);
        ahaSo.FindProperty("animator").objectReferenceValue = heroObj.GetComponentInChildren<Animator>();
        ahaSo.FindProperty("visualRoot").objectReferenceValue = heroObj.transform;
        if (swordTrans != null) ahaSo.FindProperty("weaponTransform").objectReferenceValue = swordTrans;
        ahaSo.ApplyModifiedProperties();

        SetMaskInteractionRecursive(heroObj);

        // EnemyMonster 생성 (Skeleton_Soldier 프리팹 인스턴스)
        GameObject skelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Layer Lab/2D Minimal-EnemyMonster/EnemyMonster 2/Prefabs/Skeleton/Skeleton_Soldier.prefab");
        GameObject enemyObj;
        if (skelPrefab != null)
        {
            enemyObj = PrefabUtility.InstantiatePrefab(skelPrefab, combatRootObj.transform) as GameObject;
            enemyObj.name = "EnemyMonster";
        }
        else
        {
            enemyObj = new GameObject("EnemyMonster");
            enemyObj.transform.SetParent(combatRootObj.transform, false);
        }
        enemyObj.transform.localPosition = new Vector3(3.5f, -0.45f, 0f);
        enemyObj.transform.localScale = new Vector3(-0.85f, 0.85f, 1f); // 좌측 응시
        EnemyMonsterActor enemyActor = enemyObj.AddComponent<EnemyMonsterActor>();
        SetMaskInteractionRecursive(enemyObj);

        // ProjectileRoot & HitVFX
        GameObject projRootObj = new GameObject("ProjectileRoot");
        projRootObj.transform.SetParent(bVisualObj.transform, false);

        GameObject hitVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Layer Lab/Cartoon Casual VFX Pack/Prefabs/Hit/Hit1_Yellow.prefab");

        SerializedObject bcSo = new SerializedObject(battleCtrl);
        bcSo.FindProperty("activeHero").objectReferenceValue = heroActor;
        bcSo.FindProperty("enemyMonster").objectReferenceValue = enemyActor;
        bcSo.FindProperty("parallaxScroller").objectReferenceValue = scroller;
        bcSo.FindProperty("battleVisualRoot").objectReferenceValue = bVisualObj.transform;
        bcSo.FindProperty("projectileRoot").objectReferenceValue = projRootObj.transform;
        if (hitVfxPrefab != null) bcSo.FindProperty("hitVfxPrefab").objectReferenceValue = hitVfxPrefab;
        bcSo.ApplyModifiedProperties();

        // 4-2. MiningAreaRoot (하단 60% 채굴 월드)
        GameObject miningAreaObj = new GameObject("MiningAreaRoot");
        miningAreaObj.transform.SetParent(gameRootObj.transform, false);
        miningAreaObj.transform.localPosition = new Vector3(0f, -1.52f, 0f);

        // MiningAreaMask (SpriteMask) - 하단까지 넉넉하게 확장
        GameObject mMaskObj = new GameObject("MiningAreaMask");
        mMaskObj.transform.SetParent(miningAreaObj.transform, false);
        mMaskObj.transform.localScale = new Vector3(12.0f, 14.0f, 1f);
        SpriteMask mMask = mMaskObj.AddComponent<SpriteMask>();
        if (whiteSquare != null) mMask.sprite = whiteSquare;

        GameObject mVisualObj = new GameObject("MiningVisualRoot");
        mVisualObj.transform.SetParent(miningAreaObj.transform, false);

        // CaveBackdropRoot (동굴/지하 단면 배경 - 하단 확장 영역까지 완전 커버)
        GameObject caveBgObj = new GameObject("CaveBackdropRoot");
        caveBgObj.transform.SetParent(mVisualObj.transform, false);
        SpriteRenderer caveSr = caveBgObj.AddComponent<SpriteRenderer>();
        Sprite caveSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Game/Cave_Backdrop.png");
        if (caveSp != null) caveSr.sprite = caveSp;
        caveSr.drawMode = SpriteDrawMode.Sliced;
        caveSr.size = new Vector2(16f, 16f);
        caveSr.sortingOrder = -15;
        SetMaskInteraction(caveBgObj);

        // RetiredHeroRoot (은퇴용사 땅만파 - 화면 중앙 X=0 배치)
        GameObject diggerObj;
        if (charPrefab != null)
        {
            diggerObj = PrefabUtility.InstantiatePrefab(charPrefab, mVisualObj.transform) as GameObject;
            diggerObj.name = "RetiredHeroRoot";
        }
        else
        {
            diggerObj = new GameObject("RetiredHeroRoot");
            diggerObj.transform.SetParent(mVisualObj.transform, false);
        }
        diggerObj.transform.localPosition = new Vector3(-0.40f, 0.38f, 0f);
        diggerObj.transform.localScale = new Vector3(1.0f, 1.0f, 1f);

        // SampleCharacterMover 컴포넌트 제거
        var diggerMover = diggerObj.GetComponent<LayerLab.SampleCharacterMover>();
        if (diggerMover != null) Object.DestroyImmediate(diggerMover);

        // 은퇴용사는 UniTask 기반 정밀 스윙 모션을 사용하므로 기본 런 애니메이터 비활성화
        Animator dAnim = diggerObj.GetComponentInChildren<Animator>();
        if (dAnim != null) dAnim.enabled = false;

        // 은퇴용사 비주얼 커스텀:
        // 1) 수염(Beard) 활성화 및 헤어/수염 백발(노장 은퇴용사 분위기)
        Transform beardTrans = diggerObj.transform.Find("Character_20260617_193302/Body/Head/Beard");
        if (beardTrans != null)
        {
            beardTrans.gameObject.SetActive(true);
            var bSr = beardTrans.GetComponent<SpriteRenderer>();
            if (bSr != null) bSr.color = new Color(0.88f, 0.88f, 0.92f, 1f); // 연회색 백발 수염
        }
        Transform hairTrans = diggerObj.transform.Find("Character_20260617_193302/Body/Head/Hair");
        if (hairTrans != null)
        {
            var hSr = hairTrans.GetComponent<SpriteRenderer>();
            if (hSr != null) hSr.color = new Color(0.85f, 0.85f, 0.90f, 1f); // 연회색 백발 헤어
        }

        // 발 밑 그림자(Shadow)는 부자연스러우므로 완전히 제거
        Transform shadowTrans = diggerObj.transform.Find("Character_20260617_193302/Shadow");
        if (shadowTrans != null)
        {
            Object.DestroyImmediate(shadowTrans.gameObject);
        }

        // 2) HandRight 하위 기본 무기들 전부 비활성화
        Transform dRightHand = diggerObj.transform.Find("Character_20260617_193302/HandRight");
        if (dRightHand != null)
        {
            for (int i = 0; i < dRightHand.childCount; i++)
            {
                dRightHand.GetChild(i).gameObject.SetActive(false);
            }
        }

        // 3) HandLeft 하위 방패 및 서브아이템 비활성화
        Transform dLeftHand = diggerObj.transform.Find("Character_20260617_193302/HandLeft");
        if (dLeftHand != null)
        {
            for (int i = 0; i < dLeftHand.childCount; i++)
            {
                dLeftHand.GetChild(i).gameObject.SetActive(false);
            }
        }

        // 도구 소켓 (ToolSocket) - 용사 오른손 위치에 배치 및 자루 피벗 보정
        GameObject toolSockObj = new GameObject("ToolSocket");
        toolSockObj.transform.SetParent(diggerObj.transform, false);
        toolSockObj.transform.localPosition = new Vector3(0.20f, 0.05f, 0f);
        toolSockObj.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

        // pickaxe-256_0 스프라이트 로드
        Sprite pickaxeSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Space_Exploration_GUI_Kit/Icons/pickaxe-256.png")
            .OfType<Sprite>()
            .FirstOrDefault(s => s.name == "pickaxe-256_0");
        if (pickaxeSprite == null)
            pickaxeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Game/Gear_Weapons_Pickaxe_01.png");

        GameObject toolVisObj = new GameObject("ToolVisual");
        toolVisObj.transform.SetParent(toolSockObj.transform, false);
        toolVisObj.transform.localPosition = new Vector3(0.38f, 0.38f, 0f); // 손잡이가 손에 오도록 오프셋
        SpriteRenderer toolSr = toolVisObj.AddComponent<SpriteRenderer>();
        toolSr.sortingOrder = 15;
        if (pickaxeSprite != null) toolSr.sprite = pickaxeSprite;

        // DigImpactAnchor (타격점: 발 앞쪽 블록 상단면 중앙)
        GameObject impactAnchorObj = new GameObject("DigImpactAnchor");
        impactAnchorObj.transform.SetParent(diggerObj.transform, false);
        impactAnchorObj.transform.localPosition = new Vector3(0.40f, -0.35f, 0f);

        Transform charVisTrans = diggerObj.transform.Find("Character_20260617_193302");
        if (charVisTrans == null) charVisTrans = diggerObj.transform;

        RetiredHeroActor retiredHero = diggerObj.AddComponent<RetiredHeroActor>();
        SerializedObject rhSo = new SerializedObject(retiredHero);
        rhSo.FindProperty("characterVisual").objectReferenceValue = charVisTrans;
        rhSo.FindProperty("toolSocket").objectReferenceValue = toolSockObj.transform;
        rhSo.FindProperty("toolRenderer").objectReferenceValue = toolSr;
        rhSo.FindProperty("digImpactAnchor").objectReferenceValue = impactAnchorObj.transform;
        rhSo.FindProperty("pickaxeSprite").objectReferenceValue = pickaxeSprite;
        rhSo.FindProperty("shovelSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Game/Gear_Weapons_Shovel_01.png");
        rhSo.ApplyModifiedProperties();

        SetMaskInteractionRecursive(diggerObj);

        // DigSiteRoot (세로 1열 채굴 블록들: 용사 발 바로 아래부터 수직 기둥)
        GameObject digSiteObj = new GameObject("DigSiteRoot");
        digSiteObj.transform.SetParent(mVisualObj.transform, false);
        digSiteObj.transform.localPosition = new Vector3(0f, -0.80f, 0f);

        MiningController miningCtrl = mVisualObj.AddComponent<MiningController>();

        // 세로 1열 블록 5개 생성 (오직 네모난 돌/흙 블록만 사용!)
        float stepY = 1.65f;
        GameObject b0 = CreateMiningNodeObject("Block_0", digSiteObj.transform, new Vector3(0f, 0f, 0f), 0);
        GameObject b1 = CreateMiningNodeObject("Block_1", digSiteObj.transform, new Vector3(0f, -stepY, 0f), 1);
        GameObject b2 = CreateMiningNodeObject("Block_2", digSiteObj.transform, new Vector3(0f, -stepY * 2f, 0f), 0);
        GameObject b3 = CreateMiningNodeObject("Block_3", digSiteObj.transform, new Vector3(0f, -stepY * 3f, 0f), 1);
        GameObject b4 = CreateMiningNodeObject("Block_4", digSiteObj.transform, new Vector3(0f, -stepY * 4f, 0f), 0);

        GameObject chunksObj = new GameObject("ExcavationChunksRoot");
        chunksObj.transform.SetParent(mVisualObj.transform, false);

        SerializedObject mcSo = new SerializedObject(miningCtrl);
        mcSo.FindProperty("retiredHero").objectReferenceValue = retiredHero;
        mcSo.FindProperty("digSiteRoot").objectReferenceValue = digSiteObj.transform;
        mcSo.FindProperty("excavationChunksRoot").objectReferenceValue = chunksObj.transform;
        mcSo.FindProperty("blockStepY").floatValue = stepY;
        mcSo.FindProperty("scrollDuration").floatValue = 0.22f;
        if (hitVfxPrefab != null) mcSo.FindProperty("miningVfxPrefab").objectReferenceValue = hitVfxPrefab;
        var nodeViewsProp = mcSo.FindProperty("activeNodeViews");
        nodeViewsProp.ClearArray();
        nodeViewsProp.InsertArrayElementAtIndex(0);
        nodeViewsProp.GetArrayElementAtIndex(0).objectReferenceValue = b0.GetComponent<MiningNodeView>();
        nodeViewsProp.InsertArrayElementAtIndex(1);
        nodeViewsProp.GetArrayElementAtIndex(1).objectReferenceValue = b1.GetComponent<MiningNodeView>();
        nodeViewsProp.InsertArrayElementAtIndex(2);
        nodeViewsProp.GetArrayElementAtIndex(2).objectReferenceValue = b2.GetComponent<MiningNodeView>();
        nodeViewsProp.InsertArrayElementAtIndex(3);
        nodeViewsProp.GetArrayElementAtIndex(3).objectReferenceValue = b3.GetComponent<MiningNodeView>();
        nodeViewsProp.InsertArrayElementAtIndex(4);
        nodeViewsProp.GetArrayElementAtIndex(4).objectReferenceValue = b4.GetComponent<MiningNodeView>();
        mcSo.ApplyModifiedProperties();

        // DiggerGameContent 직렬화 설정
        SerializedObject dgcSo = new SerializedObject(diggerGame);
        dgcSo.FindProperty("battleController").objectReferenceValue = battleCtrl;
        dgcSo.FindProperty("miningController").objectReferenceValue = miningCtrl;
        dgcSo.ApplyModifiedProperties();

        SerializedObject grSo = new SerializedObject(gameRootCtrl);
        grSo.FindProperty("contentModuleMono").objectReferenceValue = diggerGame;
        grSo.FindProperty("environmentRoot").objectReferenceValue = bgRootObj.transform;
        grSo.FindProperty("entityRoot").objectReferenceValue = combatRootObj.transform;
        grSo.FindProperty("vfxRoot").objectReferenceValue = projRootObj.transform;
        grSo.ApplyModifiedProperties();

        // 5. WorldAreaLayoutBinder 부착 및 참조 연결!
        WorldAreaLayoutBinder layoutBinder = rootObj.AddComponent<WorldAreaLayoutBinder>();
        SerializedObject lbSo = new SerializedObject(layoutBinder);
        lbSo.FindProperty("mainCamera").objectReferenceValue = mainCam;
        lbSo.FindProperty("targetCanvas").objectReferenceValue = canvas;
        lbSo.FindProperty("safeAreaRoot").objectReferenceValue = safeRect;
        lbSo.FindProperty("topHudRegion").objectReferenceValue = thRect;
        lbSo.FindProperty("actionRegion").objectReferenceValue = arRect;
        lbSo.FindProperty("gameplayRegion").objectReferenceValue = gpRect;
        lbSo.FindProperty("battleRegion").objectReferenceValue = brRect;
        lbSo.FindProperty("miningRegion").objectReferenceValue = mrRect;
        lbSo.FindProperty("battleAreaRoot").objectReferenceValue = battleAreaObj.transform;
        lbSo.FindProperty("miningAreaRoot").objectReferenceValue = miningAreaObj.transform;
        lbSo.FindProperty("battleAreaMask").objectReferenceValue = bMask;
        lbSo.FindProperty("miningAreaMask").objectReferenceValue = mMask;
        lbSo.FindProperty("miningBottomExtension").floatValue = 180f;
        lbSo.ApplyModifiedProperties();

        // 6. PlayPageView 직렬화 바인딩
        SerializedObject ppvSo = new SerializedObject(playPageView);
        ppvSo.FindProperty("uiCanvas").objectReferenceValue = canvas;
        ppvSo.FindProperty("gameRoot").objectReferenceValue = gameRootCtrl;
        ppvSo.FindProperty("layoutBinder").objectReferenceValue = layoutBinder;
        ppvSo.FindProperty("topHUDView").objectReferenceValue = topHudView;
        ppvSo.FindProperty("bottomPanelView").objectReferenceValue = bottomView;
        ppvSo.FindProperty("sidebarController").objectReferenceValue = sidebarCtrl;
        ppvSo.ApplyModifiedProperties();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rootObj, PLAY_PAGE_PREFAB_PATH);
        Object.DestroyImmediate(rootObj);
        return prefab;
    }

    private static GameObject CreateMiningNodeObject(string name, Transform parent, Vector3 localPos, int typeIndex)
    {
        GameObject nodeObj = new GameObject(name);
        nodeObj.transform.SetParent(parent, false);
        nodeObj.transform.localPosition = localPos;
        nodeObj.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

        GameObject visObj = new GameObject("Visual");
        visObj.transform.SetParent(nodeObj.transform, false);

        SpriteRenderer blockSr = visObj.AddComponent<SpriteRenderer>();
        blockSr.sortingOrder = 2;
        SetMaskInteraction(visObj);

        GameObject crackObj = new GameObject("CrackOverlay");
        crackObj.transform.SetParent(visObj.transform, false);
        SpriteRenderer crackSr = crackObj.AddComponent<SpriteRenderer>();
        crackSr.sortingOrder = 3;
        SetMaskInteraction(crackObj);
        crackObj.SetActive(false);

        MiningNodeView view = nodeObj.AddComponent<MiningNodeView>();
        Sprite soilSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Game/Block_Soil.png");
        Sprite rockSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Game/Block_Rock.png");
        Sprite oreSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Game/Material_Ore_01.png");
        Sprite crack1Sp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Game/Block_Crack_1.png");
        Sprite crack2Sp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Game/Block_Crack_2.png");

        blockSr.sprite = (typeIndex == 1) ? soilSp : rockSp;

        SerializedObject mnvSo = new SerializedObject(view);
        mnvSo.FindProperty("blockRenderer").objectReferenceValue = blockSr;
        mnvSo.FindProperty("crackRenderer").objectReferenceValue = crackSr;
        mnvSo.FindProperty("visualRoot").objectReferenceValue = visObj.transform;
        mnvSo.FindProperty("soilSprite").objectReferenceValue = soilSp;
        mnvSo.FindProperty("rockSprite").objectReferenceValue = rockSp;
        mnvSo.FindProperty("oreSprite").objectReferenceValue = rockSp; // 광석도 네모 블록 사용
        mnvSo.FindProperty("crack1Sprite").objectReferenceValue = crack1Sp;
        mnvSo.FindProperty("crack2Sprite").objectReferenceValue = crack2Sp;
        mnvSo.ApplyModifiedProperties();

        return nodeObj;
    }

    private static void SetMaskInteraction(GameObject go)
    {
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }
    }

    private static void SetMaskInteractionRecursive(GameObject root)
    {
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }
    }

    #endregion

    #region Scene Setup

    private static void SetupCleanPlayScene(GameObject mainPagePrefab, GameObject playPagePrefab)
    {
        string scenePath = "Assets/Scenes/Play.unity";
        Scene playScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // 불필요한 MainGameScene 제거 (옥상옥 껍데기 완전 삭제)
        GameObject oldMainScene = GameObject.Find("MainGameScene");
        if (oldMainScene != null)
        {
            Object.DestroyImmediate(oldMainScene);
        }

        // 메인 카메라 찾기 및 설정
        Camera sceneCam = Camera.main;
        if (sceneCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            sceneCam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        sceneCam.clearFlags = CameraClearFlags.SolidColor;
        sceneCam.backgroundColor = new Color(0.10f, 0.12f, 0.18f, 1f);
        sceneCam.orthographic = true;
        sceneCam.orthographicSize = 6.4f;
        sceneCam.nearClipPlane = 0.1f;
        sceneCam.farClipPlane = 100f;
        sceneCam.rect = new Rect(0, 0, 1, 1);
        sceneCam.targetDisplay = 0;
        sceneCam.transform.position = new Vector3(0, 0, -10f);
        EditorUtility.SetDirty(sceneCam);

        RenderSettings.skybox = null;

        // [--- MANAGERS ---]
        GameObject mgrs = GameObject.Find("[--- MANAGERS ---]");
        if (mgrs == null) mgrs = new GameObject("[--- MANAGERS ---]");

        // 1. SessionPausePolicy
        SessionPausePolicy pausePolicy = mgrs.GetComponent<SessionPausePolicy>();
        if (pausePolicy == null) pausePolicy = mgrs.AddComponent<SessionPausePolicy>();

        // 2. PlayerInventoryModel
        PlayerInventoryModel invModel = mgrs.GetComponent<PlayerInventoryModel>();
        if (invModel == null) invModel = mgrs.AddComponent<PlayerInventoryModel>();

        // 3. HeroStatsModel
        HeroStatsModel heroStats = mgrs.GetComponent<HeroStatsModel>();
        if (heroStats == null) heroStats = mgrs.AddComponent<HeroStatsModel>();

        // 4. SpriteManager
        SpriteManager spriteMgr = mgrs.GetComponent<SpriteManager>();
        if (spriteMgr == null) spriteMgr = mgrs.AddComponent<SpriteManager>();

        // SpriteManager에 핵심 텍스처들 사전 등록
        List<Sprite> coreSprites = new List<Sprite>();
        string[] spritePaths = Directory.GetFiles("Assets/Textures", "*.png", SearchOption.AllDirectories);
        foreach (var spPath in spritePaths)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(spPath.Replace("\\", "/"));
            if (s != null && !coreSprites.Contains(s))
            {
                coreSprites.Add(s);
            }
        }

        ProjectSS.Expedition.Editor.EquipmentAtlasArt.BindFolder(spriteMgr,"Assets/Textures","LegacyUI");

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

        // PlayPage 프리팹 인스턴스를 PagesRoot 하위에 씬 객체로 배치 (WYSIWYG 씬 뷰 편집 완벽 지원)
        Transform existingPlayPage = pagesRoot.transform.Find("PlayPage");
        GameObject playPageSceneObj;
        if (existingPlayPage != null)
        {
            playPageSceneObj = existingPlayPage.gameObject;
        }
        else
        {
            playPageSceneObj = PrefabUtility.InstantiatePrefab(playPagePrefab, pagesRoot.transform) as GameObject;
            playPageSceneObj.name = "PlayPage";
        }

        // 씬 내 PlayPage의 Canvas에 worldCamera를 Main Camera로 연결하여 씬 뷰에서 1:1 완벽 정렬
        PlayPageView playPageView = playPageSceneObj.GetComponent<PlayPageView>();
        if (playPageView != null)
        {
            playPageView.SetupRenderCamera(sceneCam);
        }

        SerializedObject pmSo = new SerializedObject(pageManager);
        pmSo.FindProperty("mainPagePrefab").objectReferenceValue = mainPagePrefab.GetComponent<SceneBase>();
        pmSo.FindProperty("playPagePrefab").objectReferenceValue = playPagePrefab.GetComponent<SceneBase>();
        pmSo.FindProperty("mainCamera").objectReferenceValue = sceneCam;
        pmSo.FindProperty("pagesRoot").objectReferenceValue = pagesRoot.transform;
        pmSo.FindProperty("popupRoot").objectReferenceValue = popupRoot.transform;
        pmSo.FindProperty("initialPage").enumValueIndex = (int)UIPageType.PlayPage;
        pmSo.ApplyModifiedProperties();
        EditorUtility.SetDirty(pageManager);

        EditorSceneManager.MarkSceneDirty(playScene);
        EditorSceneManager.SaveScene(playScene, scenePath);
        Debug.Log("<color=cyan>[PagePrefabBuilder] Play.unity 씬 및 v2 아키텍처 매니저 셋업 완료!</color>");
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
        rt.sizeDelta = new Vector2(130, 56);
        rt.anchoredPosition = pos;

        Image img = obj.AddComponent<Image>();
        if (btnSprite != null)
        {
            img.sprite = btnSprite;
            img.type = Image.Type.Sliced;
        }
        img.color = Color.white;

        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;

        GameObject txtObj = CreateTMP("Text", obj.transform, Vector2.zero, Vector2.one, Vector2.zero, label, 16, TextAlignmentOptions.Center, Color.white);
        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.fontStyle = FontStyles.Bold;
        }

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

    private static Button CreateSidebarIconButton(string name, Transform parent, string iconPath)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(56, 56);

        // 원형/사각 베이스 배경
        Image bgImg = btnObj.AddComponent<Image>();
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/round-yellow-large.png");
        if (bgSprite == null)
            bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/short-pink-large.png");
        
        if (bgSprite != null) bgImg.sprite = bgSprite;
        bgImg.color = new Color(0.15f, 0.2f, 0.3f, 0.95f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bgImg;

        // 내부 아이콘
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform, false);
        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = new Vector2(38, 38);
        iconRt.anchoredPosition = Vector2.zero;

        Image iconImg = iconObj.AddComponent<Image>();
        Sprite iconSp = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        if (iconSp != null)
        {
            iconImg.sprite = iconSp;
            iconImg.preserveAspect = true;
        }
        iconImg.raycastTarget = false;

        return btn;
    }

    private static Button CreateBottomNavButton(string name, Transform parent, Vector2 pos, string label, string iconPath)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(105, 110);

        Image clickTarget = btnObj.AddComponent<Image>();
        clickTarget.color = Color.clear;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = clickTarget;

        // 아이콘 베이스
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform, false);
        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = new Vector2(50, 50);
        iconRt.anchoredPosition = new Vector2(0, 14);

        Image iconImg = iconObj.AddComponent<Image>();
        Sprite iconSp = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        if (iconSp != null)
        {
            iconImg.sprite = iconSp;
            iconImg.preserveAspect = true;
        }
        iconImg.raycastTarget = false;

        // 텍스트 라벨
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        RectTransform labelRt = labelObj.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 0.5f);
        labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        labelRt.sizeDelta = new Vector2(110, 30);
        labelRt.anchoredPosition = new Vector2(0, -30);

        TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 15;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.85f, 0.9f, 1f);
        tmp.raycastTarget = false;

        return btn;
    }

    private static void SetAllSortingOrders(GameObject obj, int order)
    {
        if (obj == null) return;
        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers)
        {
            r.sortingOrder = order;
        }
    }

    private static GameObject CreateSpriteTile(string name, Transform parent, Sprite sprite, Vector3 localPos, Vector3 localScale, int sortingOrder, SpriteDrawMode drawMode = SpriteDrawMode.Simple, Vector2 size = default)
    {
        GameObject tileObj = new GameObject(name);
        tileObj.transform.SetParent(parent, false);
        tileObj.transform.localPosition = localPos;
        tileObj.transform.localScale = localScale;

        SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        sr.drawMode = drawMode;
        if (size != default)
        {
            sr.size = size;
        }
        return tileObj;
    }

    #endregion
}
