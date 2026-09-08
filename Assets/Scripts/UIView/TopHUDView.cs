using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상단 HUD 뷰: 스테이지, 플레이어 체력 게이지, 재화(골드) 표기
/// </summary>
public class TopHUDView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;

    public void SetStage(int stage)
    {
        if (stageText != null)
            stageText.text = $"STAGE {stage}";
    }

    public void SetGold(long gold)
    {
        if (goldText != null)
            goldText.text = $"{gold:N0} G";
    }

    public void SetHp(int cur, int max)
    {
        if (hpSlider != null)
            hpSlider.value = max > 0 ? (float)cur / max : 0f;

        if (hpText != null)
            hpText.text = $"{cur} / {max}";
    }
}
