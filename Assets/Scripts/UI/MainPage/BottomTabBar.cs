using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

/// <summary>
/// 캐주얼 모바일 스타일 하단 탭 바 컨트롤러.
/// 선택된 탭은 아이콘이 위로 돌출(Pop-up) 및 확대되며 하단에 라벨 텍스트가 표시되고,
/// 비선택 탭은 아이콘이 중앙에 아담하게 위치하며 텍스트가 숨겨집니다.
/// UniTask 기반의 부드러운 Ease-out 애니메이션을 제공합니다.
/// </summary>
public class BottomTabBar : MonoBehaviour
{
    [System.Serializable]
    public class TabItem
    {
        public string tabName;
        public Button button;
        public RectTransform iconRect;
        public Image iconImage;
        public TextMeshProUGUI labelText;
        public RectTransform labelRect;
        public GameObject divider; // 탭 우측 세로 구분선 (선택사항)
    }

    [Header("[Tabs Configuration]")]
    [SerializeField] private List<TabItem> tabs = new List<TabItem>();

    [Header("[Icon Position & Scale Settings]")]
    [Tooltip("비선택 상태의 아이콘 Y 위치 (중앙 기준)")]
    [SerializeField] private float normalIconPosY = 0f;
    [Tooltip("선택 상태에서 위로 돌출되는 아이콘 Y 위치")]
    [SerializeField] private float selectedIconPosY = 28f;
    [Tooltip("비선택 아이콘 스케일")]
    [SerializeField] private float normalIconScale = 0.95f;
    [Tooltip("선택 아이콘 스케일")]
    [SerializeField] private float selectedIconScale = 1.22f;

    [Header("[Label Settings]")]
    [Tooltip("선택 상태에서 라벨 텍스트의 Y 위치")]
    [SerializeField] private float labelPosY = -26f;

    [Header("[Color Settings]")]
    [SerializeField] private Color normalIconColor = new Color(0.75f, 0.82f, 0.95f, 0.85f);
    [SerializeField] private Color selectedIconColor = Color.white;
    [SerializeField] private Color labelColor = Color.white;

    [Header("[Animation Settings]")]
    [SerializeField] private float animDuration = 0.2f;

    public event Action<int> OnTabButtonClicked;

    private int currentIndex = -1;
    private CancellationTokenSource[] animCts;

    public int CurrentIndex => currentIndex;
    public int TabCount => tabs.Count;

    private void Awake()
    {
        animCts = new CancellationTokenSource[tabs.Count];

        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i;
            if (tabs[i].button != null)
            {
                tabs[i].button.onClick.AddListener(() =>
                {
                    OnTabButtonClicked?.Invoke(index);
                });
            }
        }
    }

    /// <summary>
    /// 지정된 인덱스의 탭을 선택 상태로 전환합니다.
    /// </summary>
    public void SetSelectedTab(int targetIndex)
    {
        SetSelectedTab(targetIndex, false);
    }

    public void SetSelectedTab(int targetIndex, bool instant)
    {
        if (targetIndex < 0 || targetIndex >= tabs.Count) return;
        if (currentIndex == targetIndex && !instant) return;

        currentIndex = targetIndex;

        for (int i = 0; i < tabs.Count; i++)
        {
            bool isSelected = (i == targetIndex);
            if (instant)
            {
                ApplyTabStateInstant(tabs[i], isSelected);
            }
            else
            {
                TriggerTabAnim(i, isSelected);
            }
        }
    }

    private void ApplyTabStateInstant(TabItem item, bool isSelected)
    {
        if (item == null) return;

        // 아이콘 위치 및 스케일
        if (item.iconRect != null)
        {
            item.iconRect.anchoredPosition = new Vector2(0, isSelected ? selectedIconPosY : normalIconPosY);
            item.iconRect.localScale = Vector3.one * (isSelected ? selectedIconScale : normalIconScale);
        }

        // 아이콘 컬러
        if (item.iconImage != null)
        {
            item.iconImage.color = isSelected ? selectedIconColor : normalIconColor;
        }

        // 라벨 텍스트
        if (item.labelText != null)
        {
            item.labelText.gameObject.SetActive(isSelected);
            Color c = labelColor;
            c.a = isSelected ? 1f : 0f;
            item.labelText.color = c;
        }

        if (item.labelRect != null)
        {
            item.labelRect.anchoredPosition = new Vector2(0, labelPosY);
        }
    }

    private void TriggerTabAnim(int index, bool isSelected)
    {
        if (index < 0 || index >= tabs.Count) return;

        animCts[index]?.Cancel();
        animCts[index]?.Dispose();
        animCts[index] = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

        AnimateTabItemAsync(tabs[index], isSelected, animCts[index].Token).Forget();
    }

    private async UniTask AnimateTabItemAsync(TabItem item, bool isSelected, CancellationToken ct)
    {
        RectTransform iconRect = item.iconRect;
        Image iconImg = item.iconImage;
        TextMeshProUGUI label = item.labelText;

        if (isSelected && label != null)
        {
            label.gameObject.SetActive(true);
        }

        Vector2 startIconPos = iconRect != null ? iconRect.anchoredPosition : Vector2.zero;
        Vector2 targetIconPos = new Vector2(0, isSelected ? selectedIconPosY : normalIconPosY);

        Vector3 startScale = iconRect != null ? iconRect.localScale : Vector3.one;
        Vector3 targetScale = Vector3.one * (isSelected ? selectedIconScale : normalIconScale);

        Color startIconCol = iconImg != null ? iconImg.color : Color.white;
        Color targetIconCol = isSelected ? selectedIconColor : normalIconColor;

        float startLabelAlpha = label != null ? label.color.a : (isSelected ? 0f : 1f);
        float targetLabelAlpha = isSelected ? 1f : 0f;

        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            ct.ThrowIfCancellationRequested();
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / animDuration);

            // Ease-Out Back 곡선 느낌으로 살짝 팝업 텐션 부여
            float ease = 1f - Mathf.Pow(1f - progress, 3f);

            if (iconRect != null)
            {
                iconRect.anchoredPosition = Vector2.LerpUnclamped(startIconPos, targetIconPos, ease);
                iconRect.localScale = Vector3.LerpUnclamped(startScale, targetScale, ease);
            }

            if (iconImg != null)
            {
                iconImg.color = Color.Lerp(startIconCol, targetIconCol, progress);
            }

            if (label != null)
            {
                Color c = labelColor;
                c.a = Mathf.Lerp(startLabelAlpha, targetLabelAlpha, progress);
                label.color = c;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        // 최종 값 확정
        if (iconRect != null)
        {
            iconRect.anchoredPosition = targetIconPos;
            iconRect.localScale = targetScale;
        }

        if (iconImg != null)
        {
            iconImg.color = targetIconCol;
        }

        if (label != null)
        {
            Color c = labelColor;
            c.a = targetLabelAlpha;
            label.color = c;
            if (!isSelected)
            {
                label.gameObject.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        if (animCts != null)
        {
            for (int i = 0; i < animCts.Length; i++)
            {
                animCts[i]?.Cancel();
                animCts[i]?.Dispose();
            }
        }
    }
}
