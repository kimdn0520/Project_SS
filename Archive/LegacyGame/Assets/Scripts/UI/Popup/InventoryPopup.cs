using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 가방(인벤토리) 팝업: 보유 골드, 다이아몬드, 채굴에서 발굴한 장비 및 보물상자 목록 표시.
/// </summary>
public class InventoryPopup : BasePopupHandler
{
    [Header("[Currencies]")]
    [SerializeField] private TextMeshProUGUI txtGold;
    [SerializeField] private TextMeshProUGUI txtGems;

    [Header("[Item Lists]")]
    [SerializeField] private TextMeshProUGUI txtEquipmentList;
    [SerializeField] private TextMeshProUGUI txtChestList;

    public override void OnWillEnter(object param)
    {
        base.OnWillEnter(param);
        SessionPausePolicy.Instance?.RequestPause("InventoryPopup");

        RefreshUI();
    }

    public override void OnDidLeave()
    {
        base.OnDidLeave();
        SessionPausePolicy.Instance?.ReleasePause("InventoryPopup");
    }

    private void RefreshUI()
    {
        if (PlayerInventoryModel.Instance != null)
        {
            if (txtGold != null)
                txtGold.text = $"{PlayerInventoryModel.Instance.Gold:N0} 골드";

            if (txtGems != null)
                txtGems.text = $"{PlayerInventoryModel.Instance.Gems:N0} 다이아";

            if (txtEquipmentList != null)
            {
                var eq = PlayerInventoryModel.Instance.Equipments;
                txtEquipmentList.text = (eq.Count > 0) ? string.Join("\n", eq) : "발굴된 장비가 없습니다.";
            }

            if (txtChestList != null)
            {
                var ch = PlayerInventoryModel.Instance.Chests;
                txtChestList.text = (ch.Count > 0) ? string.Join("\n", ch) : "획득한 상자가 없습니다.";
            }
        }
    }
}
