using UnityEngine;

/// <summary>
/// 모바일 기기의 노치(Notch) 및 하단 홈 바 영역을 피해 안전 영역(Safe Area)을 자동으로 맞춰주는 헬퍼 컴포넌트.
/// UI 패널의 RectTransform에 부착되어 Screen.safeArea 값을 기반으로 앵커를 보정합니다.
/// 해상도 변경 및 기기 회전 시에도 실시간 대응합니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class SafeAreaHelper : MonoBehaviour
{
    [Header("[Safe Area Options]")]
    [SerializeField] private bool conformX = true; // 좌우 안전 영역 적용 여부
    [SerializeField] private bool conformY = true; // 상하 안전 영역 적용 여부 (노치/홈바)

    private RectTransform rectTransform;
    private Rect lastSafeArea = Rect.zero;
    private Vector2Int lastScreenSize = Vector2Int.zero;
    private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        // 화면 크기, 안전 영역, 기기 방향 변경 감지 시 즉시 갱신
        if (lastSafeArea != Screen.safeArea ||
            lastScreenSize.x != Screen.width ||
            lastScreenSize.y != Screen.height ||
            lastOrientation != Screen.orientation)
        {
            Refresh();
        }
    }

    /// <summary>
    /// Screen.safeArea를 읽어 RectTransform의 앵커를 재계산합니다.
    /// </summary>
    public void Refresh()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        ScreenOrientation orientation = Screen.orientation;

        // 화면 크기가 0인 비정상 상황 예외 방지
        if (screenSize.x <= 0 || screenSize.y <= 0) return;

        ApplySafeArea(safeArea, screenSize);

        lastSafeArea = safeArea;
        lastScreenSize = screenSize;
        lastOrientation = orientation;
    }

    private void ApplySafeArea(Rect area, Vector2Int screenSize)
    {
        Vector2 anchorMin = area.position;
        Vector2 anchorMax = area.position + area.size;

        // 0.0 ~ 1.0 범위로 정규화
        anchorMin.x /= screenSize.x;
        anchorMin.y /= screenSize.y;
        anchorMax.x /= screenSize.x;
        anchorMax.y /= screenSize.y;

        // 선택적 축 적용
        if (!conformX)
        {
            anchorMin.x = 0f;
            anchorMax.x = 1f;
        }

        if (!conformY)
        {
            anchorMin.y = 0f;
            anchorMax.y = 1f;
        }

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        // 오프셋을 0으로 설정하여 안전 영역에 정확히 핏(Fit)되도록 설정
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// 에디터 모드에서도 즉시 미리보기가 가능하도록 지원
    /// </summary>
    private void OnValidate()
    {
        if (rectTransform != null)
        {
            Refresh();
        }
    }
}
