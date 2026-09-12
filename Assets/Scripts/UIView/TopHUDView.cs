using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상단 HUD 뷰: 스테이지, 몬스터 체력 게이지, 재화(골드, 보석), 지하 깊이(Depth) 표기
/// </summary>
public class TopHUDView : MonoBehaviour
{
    [Header("[Stage & Status]")]
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("[Currencies & Depth]")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI gemText;
    [SerializeField] private TextMeshProUGUI depthText;

    public void SetStage(int stage)
    {
        if (stageText != null)
            stageText.text = $"스테이지 {stage}";
    }

    public void SetStage(string stageName)
    {
        if (stageText != null)
            stageText.text = $"스테이지 {stageName}";
    }

    public void SetGold(long gold)
    {
        if (goldText != null)
            goldText.text = $"{gold:N0} 골드";
    }

    public void SetGems(int gems)
    {
        if (gemText != null)
            gemText.text = $"{gems:N0} 다이아";
    }

    public void SetDepth(int depth)
    {
        if (depthText != null)
            depthText.text = $"지하 {depth:N0}미터";
    }

    public void SetHpBarVisible(bool visible)
    {
        if (hpSlider != null)
            hpSlider.gameObject.SetActive(visible);
    }

    public void SetHp(int cur, int max)
    {
        if (hpSlider != null)
            hpSlider.value = max > 0 ? (float)cur / max : 0f;

        if (hpText != null)
            hpText.text = $"{cur} / {max}";
    }
}
