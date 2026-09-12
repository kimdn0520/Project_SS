using System;
using UnityEngine;

/// <summary>
/// 단일 카메라(MainCamera) 및 Screen Space - Camera Canvas 환경에서
/// UI의 BattleRegion(40%) / MiningRegion(60%) 영역을 순수 2D World 영역과 정밀 바인딩하는 컴포넌트.
/// Safe Area, Compact 모드(예외 규칙), OrthographicSize 프레이밍, 월드 Bounds 및 마스크(SpriteMask) 스케일을 총괄합니다.
/// </summary>
[ExecuteAlways]
public class WorldAreaLayoutBinder : MonoBehaviour
{
    [Header("[Camera Framing]")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float screenWorldWidth = 12.0f; // 가로 기준 월드 폭 12유닛
    [SerializeField] private float gameplayPlaneZ = 0.0f;    // 게임플레이 2D 평면 z좌표

    [Header("[Canvas & UI Regions]")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform safeAreaRoot;
    [SerializeField] private RectTransform topHudRegion;
    [SerializeField] private RectTransform actionRegion;
    [SerializeField] private RectTransform gameplayRegion;
    [SerializeField] private RectTransform battleRegion;
    [SerializeField] private RectTransform miningRegion;

    [Header("[World Roots & Masks]")]
    [SerializeField] private Transform battleAreaRoot;
    [SerializeField] private Transform miningAreaRoot;
    [SerializeField] private SpriteMask battleAreaMask;
    [SerializeField] private SpriteMask miningAreaMask;

    [Header("[Layout Parameters]")]
    [SerializeField] private float defaultTopHudH = 112f;
    [SerializeField] private float defaultActionH = 320f;
    [SerializeField] private float compactTopHudH = 96f;
    [SerializeField] private float compactActionH = 280f;
    [SerializeField] private float minMiningH = 520f;
    [SerializeField] private float minBattleH = 240f;
    [SerializeField] private float baseBattleRatio = 0.40f;
    [SerializeField] private float miningBottomExtension = 180f;

    // 런타임 계산 결과 프로퍼티
    public bool IsCompact { get; private set; }
    public float EffectiveBattleRatio { get; private set; } = 0.40f;
    public Bounds BattleWorldBounds { get; private set; }
    public Bounds MiningWorldBounds { get; private set; }

    public event Action OnLayoutChanged;

    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private Rect lastSafeArea = Rect.zero;
    private bool isDirty = true;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        SetDirty();
    }

    private void OnEnable()
    {
        SetDirty();
    }

    private void Update()
    {
        CheckResolutionAndSafeArea();
        if (isDirty)
        {
            ApplyLayout();
        }
    }

    public void SetDirty()
    {
        isDirty = true;
    }

    private void CheckResolutionAndSafeArea()
    {
        int curW = Screen.width;
        int curH = Screen.height;
        Rect curSafe = Screen.safeArea;

        if (curW != lastScreenWidth || curH != lastScreenHeight || curSafe != lastSafeArea)
        {
            lastScreenWidth = curW;
            lastScreenHeight = curH;
            lastSafeArea = curSafe;
            isDirty = true;
        }
    }

    public void ForceUpdateLayout()
    {
        isDirty = true;
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        isDirty = false;

        if (mainCamera == null || targetCanvas == null || safeAreaRoot == null)
        {
            return;
        }

        // 1. 단일 카메라 프레이밍 (가로 기준 orthographicSize 설정)
        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float targetOrtho = screenWorldWidth / (2.0f * aspect);
        if (Mathf.Abs(mainCamera.orthographicSize - targetOrtho) > 0.001f)
        {
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = targetOrtho;
        }

        // 2. Safe Area 정규화 앵커 적용
        ApplySafeArea();

        // 3. 40:60 세로 분할 및 Compact 모드 적용
        ApplyRegionAnchors();

        // 4. 월드 좌표 bounds 및 마스크/AreaRoot 매핑
        ApplyWorldBoundsAndMasks();

        OnLayoutChanged?.Invoke();
    }

    private void ApplySafeArea()
    {
        Rect safe = Screen.safeArea;
        Vector2 anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        Vector2 anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);

        safeAreaRoot.anchorMin = anchorMin;
        safeAreaRoot.anchorMax = anchorMax;
        safeAreaRoot.offsetMin = Vector2.zero;
        safeAreaRoot.offsetMax = Vector2.zero;
    }

    private void ApplyRegionAnchors()
    {
        float totalH = safeAreaRoot.rect.height;
        if (totalH <= 0) return;

        float topH = defaultTopHudH;
        float actH = defaultActionH;
        float gameplayH = totalH - topH - actH;
        float battleH = gameplayH * baseBattleRatio;
        float miningH = gameplayH - battleH;

        bool compact = false;

        // 1차 Compact 검사
        if (miningH < minMiningH || battleH < minBattleH)
        {
            compact = true;
            topH = compactTopHudH;
            actH = compactActionH;
            gameplayH = totalH - topH - actH;
            battleH = gameplayH * baseBattleRatio;
            miningH = gameplayH - battleH;
        }

        // 2차 예외: 그래도 채굴 높이가 부족할 경우 채굴 최소 높이 강제 보장
        float ratio = baseBattleRatio;
        if (miningH < minMiningH)
        {
            miningH = minMiningH;
            battleH = gameplayH - minMiningH;
            if (battleH < minBattleH)
            {
                Debug.LogWarning($"[WorldAreaLayoutBinder] 지원 최소 화면 높이 미달: totalH={totalH}, gameplayH={gameplayH}");
            }
            ratio = Mathf.Clamp01(battleH / Mathf.Max(1f, gameplayH));
        }

        IsCompact = compact;
        EffectiveBattleRatio = ratio;

        // TopHudRegion 배치
        if (topHudRegion != null)
        {
            topHudRegion.anchorMin = new Vector2(0f, 1f);
            topHudRegion.anchorMax = new Vector2(1f, 1f);
            topHudRegion.pivot = new Vector2(0.5f, 1f);
            topHudRegion.anchoredPosition = Vector2.zero;
            topHudRegion.sizeDelta = new Vector2(0f, topH);
        }

        // ActionRegion 배치
        if (actionRegion != null)
        {
            actionRegion.anchorMin = new Vector2(0f, 0f);
            actionRegion.anchorMax = new Vector2(1f, 0f);
            actionRegion.pivot = new Vector2(0.5f, 0f);
            actionRegion.anchoredPosition = Vector2.zero;
            actionRegion.sizeDelta = new Vector2(0f, actH);
        }

        // GameplayRegion 배치
        if (gameplayRegion != null)
        {
            gameplayRegion.anchorMin = new Vector2(0f, 0f);
            gameplayRegion.anchorMax = new Vector2(1f, 1f);
            gameplayRegion.pivot = new Vector2(0.5f, 0.5f);
            gameplayRegion.offsetMin = new Vector2(0f, actH);
            gameplayRegion.offsetMax = new Vector2(0f, -topH);
        }

        // BattleRegion & MiningRegion (splitY 경계 공유)
        float splitY = 1.0f - EffectiveBattleRatio;

        if (battleRegion != null)
        {
            battleRegion.anchorMin = new Vector2(0f, splitY);
            battleRegion.anchorMax = new Vector2(1f, 1f);
            battleRegion.offsetMin = Vector2.zero;
            battleRegion.offsetMax = Vector2.zero;
        }

        if (miningRegion != null)
        {
            miningRegion.anchorMin = new Vector2(0f, 0f);
            miningRegion.anchorMax = new Vector2(1f, splitY);
            miningRegion.offsetMin = new Vector2(0f, -miningBottomExtension);
            miningRegion.offsetMax = Vector2.zero;
        }
    }

    private void ApplyWorldBoundsAndMasks()
    {
        Camera cam = targetCanvas.worldCamera != null ? targetCanvas.worldCamera : mainCamera;
        if (cam == null) return;

        // 1. BattleRegion World Bounds 계산
        if (battleRegion != null)
        {
            Bounds bBounds = CalculateWorldBoundsFromRect(battleRegion, cam);
            BattleWorldBounds = bBounds;

            if (battleAreaRoot != null)
            {
                battleAreaRoot.position = new Vector3(bBounds.center.x, bBounds.center.y, gameplayPlaneZ);
            }

            if (battleAreaMask != null)
            {
                battleAreaMask.transform.position = new Vector3(bBounds.center.x, bBounds.center.y, gameplayPlaneZ);
                battleAreaMask.transform.localScale = new Vector3(bBounds.size.x, bBounds.size.y, 1f);
            }
        }

        // 2. MiningRegion World Bounds 계산
        if (miningRegion != null)
        {
            Bounds mBounds = CalculateWorldBoundsFromRect(miningRegion, cam);
            MiningWorldBounds = mBounds;

            if (miningAreaRoot != null)
            {
                miningAreaRoot.position = new Vector3(mBounds.center.x, mBounds.center.y, gameplayPlaneZ);
            }

            if (miningAreaMask != null)
            {
                miningAreaMask.transform.position = new Vector3(mBounds.center.x, mBounds.center.y, gameplayPlaneZ);
                miningAreaMask.transform.localScale = new Vector3(mBounds.size.x, mBounds.size.y, 1f);
            }
        }
    }

    /// <summary>
    /// UI RectTransform의 코너들을 Screen Space를 거쳐 게임플레이 평면(z=gameplayPlaneZ)의 월드 좌표 Bounds로 투영
    /// </summary>
    public Bounds CalculateWorldBoundsFromRect(RectTransform rt, Camera uiCam)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        Plane gamePlane = new Plane(Vector3.forward, new Vector3(0, 0, gameplayPlaneZ));

        Vector3 minWorld = new Vector3(float.MaxValue, float.MaxValue, gameplayPlaneZ);
        Vector3 maxWorld = new Vector3(float.MinValue, float.MinValue, gameplayPlaneZ);

        for (int i = 0; i < 4; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, corners[i]);
            Ray ray = mainCamera.ScreenPointToRay(screenPoint);

            if (gamePlane.Raycast(ray, out float enter))
            {
                Vector3 worldPoint = ray.GetPoint(enter);
                minWorld = Vector3.Min(minWorld, worldPoint);
                maxWorld = Vector3.Max(maxWorld, worldPoint);
            }
        }

        Vector3 size = maxWorld - minWorld;
        Vector3 center = (minWorld + maxWorld) * 0.5f;
        return new Bounds(center, size);
    }
}
