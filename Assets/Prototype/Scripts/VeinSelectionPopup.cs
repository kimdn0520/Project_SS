using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition
{
    public sealed class VeinSelectionPopup : BasePopupHandler
    {
        public Button[] choices;
        public TMP_Text[] labels;
        private PlayPage page;
        public override string PopupName => "VeinSelection";
        public override void OnWillEnter(object param)
        {
            base.OnWillEnter(param);
            page = (PlayPage)param;
            page.pausePolicy.RequestPause(PopupName);
            for (int i = 0; i < choices.Length; i++)
            {
                bool unlocked = i < 2 || page.Model.Data.cleared >= 10;
                choices[i].interactable = unlocked;
                labels[i].text = new[] { "철 광맥", "서리 광맥", "유적 광맥" }[i]
                    + (page.Model.Data.route == i ? "  ·  선택됨" : !unlocked ? "  ·  10구간 클리어 후" : "  ·  선택");
            }
        }
        public void Choose(int route)
        {
            if (PopupManager.IsChanging || page == null) return;
            page.ChangeVein(route);
            Close();
        }
        public override void OnDidLeave() { if (page != null) page.pausePolicy.ReleasePause(PopupName); }
        protected override void OnDestroy() { if (page != null) page.pausePolicy.ReleasePause(PopupName); base.OnDestroy(); }
    }
}
