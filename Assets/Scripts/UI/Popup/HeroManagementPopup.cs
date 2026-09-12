using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 현역 용사 관리 팝업 (무기 1슬롯 장착, 공격력/HP 스탯 확인 및 강화).
/// 팝업 오픈 시 게임플레이 클록 일시정지를 요청하고, 닫힐 때 해제합니다.
/// </summary>
public class HeroManagementPopup : BasePopupHandler
{
    [Header("[Stats Display]")]
    [SerializeField] private TextMeshProUGUI txtHeroName;
    [SerializeField] private TextMeshProUGUI txtHeroAtk;
    [SerializeField] private TextMeshProUGUI txtHeroHp;
    [SerializeField] private TextMeshProUGUI txtEquippedWeapon;

    [Header("[Upgrade Button]")]
    [SerializeField] private Button btnUpgradeAtk;
    [SerializeField] private TextMeshProUGUI txtUpgradeCost;

    public override void OnWillEnter(object param)
    {
        base.OnWillEnter(param);
        SessionPausePolicy.Instance?.RequestPause("HeroManagementPopup");

        RefreshUI();

        if (btnUpgradeAtk != null)
        {
            btnUpgradeAtk.onClick.RemoveAllListeners();
            btnUpgradeAtk.onClick.AddListener(OnClickUpgrade);
        }
    }

    public override void OnDidLeave()
    {
        base.OnDidLeave();
        SessionPausePolicy.Instance?.ReleasePause("HeroManagementPopup");
    }

    private void RefreshUI()
    {
        if (HeroStatsModel.Instance != null)
        {
            if (txtHeroName != null) txtHeroName.text = "현역 용사 (Lv. 1)";
            if (txtHeroAtk != null) txtHeroAtk.text = $"공격력: {HeroStatsModel.Instance.ActiveHeroAtk}";
            if (txtHeroHp != null) txtHeroHp.text = $"최대 체력: {HeroStatsModel.Instance.ActiveHeroMaxHp}";
            if (txtEquippedWeapon != null) txtEquippedWeapon.text = $"장착 무기: {HeroStatsModel.Instance.EquippedWeapon}";
        }

        if (txtUpgradeCost != null)
        {
            long cost = 100;
            txtUpgradeCost.text = $"공격력 강화 ({cost:N0} G)";
        }
    }

    private void OnClickUpgrade()
    {
        long cost = 100;
        if (PlayerInventoryModel.Instance != null && PlayerInventoryModel.Instance.SpendGold(cost))
        {
            HeroStatsModel.Instance?.UpgradeHeroAtk(5);
            RefreshUI();
        }
    }
}
