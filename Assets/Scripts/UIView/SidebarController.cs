using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 방치형 게임 양 사이드바(Left/Right Sidebars) 컨트롤러.
/// SafeArea 안쪽 좌우 여백에 수직으로 아이콘 버튼들을 정렬하고 유저 인터랙션을 처리합니다.
/// </summary>
public class SidebarController : MonoBehaviour
{
    [Header("[Left Sidebar Icons (Quests, Mail, Events)]")]
    [SerializeField] private Button questButton;
    [SerializeField] private Button mailButton;
    [SerializeField] private Button achievementButton;
    [SerializeField] private Button giftButton;

    [Header("[Right Sidebar Icons (Rank, Dungeon, Shop, Setting)]")]
    [SerializeField] private Button rankButton;
    [SerializeField] private Button dungeonButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button settingButton;

    public event Action<string> OnSidebarAction;

    private void Awake()
    {
        BindButton(questButton, "Quest");
        BindButton(mailButton, "Mail");
        BindButton(achievementButton, "Achievement");
        BindButton(giftButton, "Gift");

        BindButton(rankButton, "Ranking");
        BindButton(dungeonButton, "Dungeon");
        BindButton(shopButton, "Shop");
        BindButton(settingButton, "Setting");
    }

    private void BindButton(Button btn, string actionName)
    {
        if (btn != null)
        {
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"[Sidebar] {actionName} clicked!");
                OnSidebarAction?.Invoke(actionName);
            });
        }
    }
}
