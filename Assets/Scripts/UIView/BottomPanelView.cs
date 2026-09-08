using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하단 UI 뷰 (뚜카펫 스타일): 인터랙션 버튼, 탭 메뉴, 전투 상태 로그 출력
/// </summary>
public class BottomPanelView : MonoBehaviour
{
    [Header("[Action Buttons]")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button upgradeButton;

    [Header("[Log & Feedback]")]
    [SerializeField] private TextMeshProUGUI logText;

    public event Action<string, object> OnButtonClicked;

    private void Awake()
    {
        if (attackButton != null)
            attackButton.onClick.AddListener(() => OnButtonClicked?.Invoke("Attack", null));

        if (skillButton != null)
            skillButton.onClick.AddListener(() => OnButtonClicked?.Invoke("Skill1", null));

        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(() => OnButtonClicked?.Invoke("Upgrade", null));
    }

    public void AppendLog(string message)
    {
        if (logText != null)
        {
            logText.text = $"[{DateTime.Now:HH:mm:ss}] {message}";
        }
    }
}
