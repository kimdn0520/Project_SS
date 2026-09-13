using UnityEngine;
using TMPro;

namespace ProjectSS.Expedition
{
    public sealed class ExpeditionNotice : BasePopupHandler
    {
        public sealed class Content
        {
            public string title, body, action = "계속 탐사하기";
            public bool confirmation;
            public SessionPausePolicy pausePolicy;
        }
        [SerializeField] private TMP_Text titleText, bodyText, actionText;
        [SerializeField] private SessionPausePolicy pausePolicy;
        public override string PopupName => "ExpeditionNotice";
        public void SetupRenderCamera(Camera camera) { Canvas.worldCamera = camera; }
        public override void OnWillEnter(object param)
        {
            base.OnWillEnter(param);
            var content = (Content)param;
            pausePolicy = content.pausePolicy;
            pausePolicy.RequestPause(PopupName);
            titleText.text = content.title; bodyText.text = content.body; actionText.text = content.action;
        }
        public void Accept() { if (!PopupManager.IsChanging) Close(true); }
        public override void OnDidLeave() { pausePolicy.ReleasePause(PopupName); }
        protected override void OnDestroy() { if (pausePolicy != null) pausePolicy.ReleasePause(PopupName); base.OnDestroy(); }
    }
}
