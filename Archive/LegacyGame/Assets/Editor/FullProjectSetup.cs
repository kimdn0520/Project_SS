using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

public static class FullProjectSetup
{
    [MenuItem("ProjectSS/Setup 2-Scene Architecture")]
    public static void Execute()
    {
        SetupBuildSettings();
        SetupSplashScene();
        SetupPlayScene();
        Debug.Log("<color=green>[FullProjectSetup] 2-Scene & Page-based UI 완벽 구축 완료!</color>");
    }

    public static void SetupBuildSettings()
    {
        string splashPath = "Assets/Scenes/Splash.unity";
        string playPath = "Assets/Scenes/Play.unity";

        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(splashPath, true),
            new EditorBuildSettingsScene(playPath, true)
        };

        EditorBuildSettings.scenes = scenes;
        Debug.Log("[FullProjectSetup] Build Settings 씬 0: Splash, 1: Play 등록 완료.");
    }

    public static void SetupSplashScene()
    {
        string scenePath = "Assets/Scenes/Splash.unity";
        Scene splashScene;

        if (File.Exists(scenePath))
        {
            splashScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }
        else
        {
            splashScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        // 1. 카메라
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
        cam.backgroundColor = new Color(0.06f, 0.08f, 0.11f, 1f);

        // 2. Managers
        GameObject mgrGroup = GameObject.Find("[--- MANAGERS ---]");
        if (mgrGroup == null) mgrGroup = new GameObject("[--- MANAGERS ---]");

        AppManager appMgr = Object.FindAnyObjectByType<AppManager>();
        if (appMgr == null)
        {
            GameObject appObj = new GameObject("AppManager");
            appObj.transform.SetParent(mgrGroup.transform, false);
            appMgr = appObj.AddComponent<AppManager>();
        }

        // 3. EventSystem
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 4. Canvas
        GameObject canvasObj = GameObject.Find("SplashCanvas");
        if (canvasObj == null) canvasObj = new GameObject("SplashCanvas");

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        if (canvas == null) canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 10f;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 1280);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasObj.GetComponent<GraphicRaycaster>() == null)
            canvasObj.AddComponent<GraphicRaycaster>();

        // 5. SafeAreaPanel
        GameObject safePanel = GetOrCreateChild(canvasObj, "SafeAreaPanel");
        RectTransform safeRect = GetOrAddRect(safePanel);
        safeRect.anchorMin = Vector2.zero;
        safeRect.anchorMax = Vector2.one;
        safeRect.offsetMin = Vector2.zero;
        safeRect.offsetMax = Vector2.zero;
        if (safePanel.GetComponent<SafeAreaHelper>() == null)
            safePanel.AddComponent<SafeAreaHelper>();

        // 6. Title Logo & Splash UI
        GameObject titleObj = CreateText("Txt_Title", safePanel.transform, new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(600, 100), "PROJECT SS", 46, TextAlignmentOptions.Center, new Color(0.9f, 0.75f, 0.2f));
        GameObject subTitleObj = CreateText("Txt_SubTitle", safePanel.transform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(600, 50), "2D MOBILE IDLE RPG", 22, TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.8f));

        // Loading Area
        GameObject loadingArea = GetOrCreateChild(safePanel, "LoadingArea");
        RectTransform loadRect = GetOrAddRect(loadingArea);
        loadRect.anchorMin = new Vector2(0.5f, 0.2f);
        loadRect.anchorMax = new Vector2(0.5f, 0.2f);
        loadRect.sizeDelta = new Vector2(500, 100);
        loadRect.anchoredPosition = Vector2.zero;

        // Progress Slider
        GameObject sliderObj = GetOrCreateChild(loadingArea, "ProgressSlider");
        RectTransform sRect = GetOrAddRect(sliderObj);
        sRect.anchorMin = new Vector2(0.5f, 0.5f);
        sRect.anchorMax = new Vector2(0.5f, 0.5f);
        sRect.sizeDelta = new Vector2(460, 24);
        sRect.anchoredPosition = Vector2.zero;

        Slider slider = sliderObj.GetComponent<Slider>();
        if (slider == null) slider = sliderObj.AddComponent<Slider>();

        // Slider Bg
        GameObject sBg = GetOrCreateChild(sliderObj, "Background");
        RectTransform sBgRect = GetOrAddRect(sBg);
        sBgRect.anchorMin = Vector2.zero;
        sBgRect.anchorMax = Vector2.one;
        sBgRect.offsetMin = Vector2.zero;
        sBgRect.offsetMax = Vector2.zero;
        Image bgImg = sBg.GetComponent<Image>();
        if (bgImg == null) bgImg = sBg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        // Fill
        GameObject sFillArea = GetOrCreateChild(sliderObj, "Fill Area");
        RectTransform fillAreaRect = GetOrAddRect(sFillArea);
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        GameObject sFill = GetOrCreateChild(sFillArea, "Fill");
        RectTransform fillRect = GetOrAddRect(sFill);
        fillRect.sizeDelta = Vector2.zero;
        Image fillImg = sFill.GetComponent<Image>();
        if (fillImg == null) fillImg = sFill.AddComponent<Image>();
        fillImg.color = new Color(0.3f, 0.8f, 1f);

        slider.fillRect = fillRect;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;

        // Texts
        GameObject percentTxt = CreateText("Txt_Percent", loadingArea.transform, new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f), new Vector2(200, 30), "0%", 20, TextAlignmentOptions.Center, Color.white);
        GameObject statusTxt = CreateText("Txt_Status", loadingArea.transform, new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(400, 30), "Initializing app...", 18, TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f));

        // SplashScene Script
        SplashScene splashComp = canvasObj.GetComponent<SplashScene>();
        if (splashComp == null) splashComp = canvasObj.AddComponent<SplashScene>();

        SerializedObject splashSo = new SerializedObject(splashComp);
        splashSo.FindProperty("progressSlider").objectReferenceValue = slider;
        splashSo.FindProperty("progressText").objectReferenceValue = percentTxt.GetComponent<TextMeshProUGUI>();
        splashSo.FindProperty("statusText").objectReferenceValue = statusTxt.GetComponent<TextMeshProUGUI>();
        splashSo.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(splashScene, scenePath);
        Debug.Log("[FullProjectSetup] Splash 씬 생성 및 구성 완료.");
    }

    public static void SetupPlayScene()
    {
        PagePrefabBuilder.Execute();
    }

    #region Helper Methods

    private static GameObject GetOrCreateChild(GameObject parent, string name)
    {
        Transform child = parent.transform.Find(name);
        if (child != null) return child.gameObject;
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        return obj;
    }

    private static RectTransform GetOrAddRect(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) rt = obj.AddComponent<RectTransform>();
        return rt;
    }

    private static void SetupTabView(GameObject tabObj, int index, string title, string desc, Color bgColor)
    {
        RectTransform rt = GetOrAddRect(tabObj);
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 0.5f);
        rt.sizeDelta = new Vector2(720, 0);
        rt.anchoredPosition = new Vector2(index * 720, 0);

        Image bg = tabObj.GetComponent<Image>();
        if (bg == null) bg = tabObj.AddComponent<Image>();
        bg.color = bgColor;

        CreateText("Txt_TabTitle", tabObj.transform, new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(500, 60), title, 32, TextAlignmentOptions.Center, Color.white);
        CreateText("Txt_TabDesc", tabObj.transform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(600, 150), desc, 20, TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.85f));
    }

    private static Button CreateTabButton(string name, Transform parent, Vector2 pos, string label)
    {
        GameObject obj = GetOrCreateChild(parent.gameObject, name);
        RectTransform rt = GetOrAddRect(obj);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(180, 80);
        rt.anchoredPosition = pos;

        Image img = obj.GetComponent<Image>();
        if (img == null) img = obj.AddComponent<Image>();
        img.color = new Color(0.18f, 0.2f, 0.26f, 1f);

        Button btn = obj.GetComponent<Button>();
        if (btn == null) btn = obj.AddComponent<Button>();

        GameObject txtObj = CreateText("Text", obj.transform, Vector2.zero, Vector2.one, Vector2.zero, label, 20, TextAlignmentOptions.Center, Color.white);
        RectTransform tr = txtObj.GetComponent<RectTransform>();
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        return btn;
    }

    private static Button CreateActionButton(string name, Transform parent, Vector2 pos, Vector2 size, string label, Color color)
    {
        GameObject obj = GetOrCreateChild(parent.gameObject, name);
        RectTransform rt = GetOrAddRect(obj);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = obj.GetComponent<Image>();
        if (img == null) img = obj.AddComponent<Image>();
        img.color = color;

        Button btn = obj.GetComponent<Button>();
        if (btn == null) btn = obj.AddComponent<Button>();

        GameObject txtObj = CreateText("Text", obj.transform, Vector2.zero, Vector2.one, Vector2.zero, label, 22, TextAlignmentOptions.Center, Color.white);
        RectTransform tr = txtObj.GetComponent<RectTransform>();
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        return btn;
    }

    private static GameObject CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, string text, float fontSize, TextAlignmentOptions align, Color color)
    {
        GameObject obj = GetOrCreateChild(parent.gameObject, name);
        RectTransform rt = GetOrAddRect(obj);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = color;

        return obj;
    }

    private static void AddTabItem(SerializedProperty tabsProp, int index, Button btn, string label)
    {
        tabsProp.InsertArrayElementAtIndex(index);
        SerializedProperty itemProp = tabsProp.GetArrayElementAtIndex(index);
        itemProp.FindPropertyRelative("button").objectReferenceValue = btn;
        itemProp.FindPropertyRelative("iconImage").objectReferenceValue = btn.GetComponent<Image>();
        itemProp.FindPropertyRelative("labelText").objectReferenceValue = btn.GetComponentInChildren<TextMeshProUGUI>();
        itemProp.FindPropertyRelative("rootTransform").objectReferenceValue = btn.transform;
    }

    #endregion
}
