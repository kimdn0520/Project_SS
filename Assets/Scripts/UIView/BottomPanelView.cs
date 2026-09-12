using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하단 컨트롤 뷰 (뚜까펫 스타일):
/// 중앙에 거대한 Hold 채굴 버튼 돌출 + 좌우 4개 메뉴 탭(현역용사, 은퇴용사, 도전, 설정)
/// </summary>
public class BottomPanelView : MonoBehaviour
{
    [Header("[Center Dig Button (Dducca Pet Style)]")]
    [SerializeField] private HoldDigButton holdDigButton;

    [Header("[Bottom 4 Navigation Menus]")]
    [SerializeField] private Button btnHeroManage;      // 용사 관리
    [SerializeField] private Button btnChallenge;       // 도전
    [SerializeField] private Button btnInventory;       // 가방
    [SerializeField] private Button btnSettings;        // 설정

    [Header("[Log & Feedback]")]
    [SerializeField] private TextMeshProUGUI logText;

    public event Action<string, object> OnButtonClicked;

    private void Awake()
    {
        if (holdDigButton != null)
        {
            holdDigButton.OnDig += HandleDig;
        }

        BindMenuButton(btnHeroManage, "HeroManage", "용사 관리", () => PopupManager.Show("HeroManagementPopup"));
        BindMenuButton(btnChallenge, "Challenge", "도전", () => AppendLog("도전 모드는 현재 준비 중입니다."));
        BindMenuButton(btnInventory, "Inventory", "가방", () => PopupManager.Show("InventoryPopup"));
        BindMenuButton(btnSettings, "Settings", "설정", () => PopupManager.Show("SettingsPopup"));
    }

    private void BindMenuButton(Button btn, string cmd, string label, Action customAction = null)
    {
        if (btn != null)
        {
            btn.onClick.AddListener(() =>
            {
                customAction?.Invoke();
                OnButtonClicked?.Invoke(cmd, null);
            });
        }
    }

    private void HandleDig()
    {
        OnButtonClicked?.Invoke("Mine", null);
    }

    public void AppendLog(string message)
    {
        if (logText != null)
        {
            logText.text = message;
        }
    }

    private void OnDestroy()
    {
        if (holdDigButton != null)
        {
            holdDigButton.OnDig -= HandleDig;
        }
    }
}
