using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace ProjectSS.Expedition
{
    public sealed class VeinSelectionPopup : BasePopupHandler
    {
        public Button[] choices;
        public TMP_Text[] labels;
        public TMP_Text[] choiceLabels, conditions;
        private PlayPage page;
        public override string PopupName => "VeinSelection";
        public override void OnWillEnter(object param)
        {
            base.OnWillEnter(param);
            page = (PlayPage)param;
            page.pausePolicy.RequestPause(PopupName);
            RefreshChoices();
        }
        private void RefreshChoices()
        {
            for (int i = 0; i < choices.Length; i++)
            {
                bool unlocked = i < 2 || page.Model.Data.cleared >= 10;
                bool selected=page.Model.Data.route==i;
                choices[i].interactable = unlocked&&!selected;
                labels[i].text = new[] { "철 광맥", "서리 광맥", "유적 광맥" }[i];
                choiceLabels[i].text=selected?"선택됨":unlocked?"선택":"잠김";
                conditions[i].text=unlocked?new[]{"철을 채굴합니다","결정을 채굴합니다","파편을 채굴합니다"}[i]:"10구간 클리어 후 해금";
            }
        }
        public void Choose(int route)
        {
            if (PopupManager.IsChanging || page == null) return;
            if(route<0||route>=choices.Length||route==page.Model.Data.route||(route>=2&&page.Model.Data.cleared<10))return;
            page.ChangeVein(route);
            RefreshChoices();
        }
        public override void OnDidLeave() { if (page != null) page.pausePolicy.ReleasePause(PopupName); page=null; }
        protected override void OnDestroy() { if (page != null) page.pausePolicy.ReleasePause(PopupName); base.OnDestroy(); }
    }
}
