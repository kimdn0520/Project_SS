using System;
using Cysharp.Threading.Tasks;
using TMPro;
namespace ProjectSS.Expedition
{
    public sealed class MoleSupportIcon : BaseSidebarIcon
    {
        public TMP_Text remainingText;
        bool busy;
        protected override bool CanShow
        {
            get
            {
                if(!base.CanShow)return false;
                Page.RefreshMoleSupport();
                int left=MoleSupportDaily.Remaining(Page.Model.Data);
                if(remainingText!=null)remainingText.text=$"{left}/5";
                return left>0;
            }
        }
        protected override void Open() { if(!busy)ShowAsync().Forget(); }
        async UniTaskVoid ShowAsync()
        {
            busy=true;
            try
            {
                var page=Page;
                var content=new ExpeditionNotice.Content
                {
                    title="두더지 지원품",
                    body=$"하루 5번, 광고를 보고 지원품을 받는 기능을 준비 중입니다.\n\n오늘 남은 횟수: {MoleSupportDaily.Remaining(page.Model.Data)}/5\n\n광고와 보상 종류는 아직 기획 중입니다.",
                    action="확인",pausePolicy=page.pausePolicy
                };
#if UNITY_EDITOR
                content.body+="\n\n에디터 미리보기: 아래 버튼은 광고 완료를 테스트합니다. 실제 광고·보상은 없습니다.";
                content.action="광고 완료 테스트";
#endif
                bool accepted=await PopupManager.ShowAsync<bool>(page.notice.PopupName,content).AttachExternalCancellation(destroyCancellationToken);
#if UNITY_EDITOR
                if(accepted){page.CompleteMoleSupportPreview();RefreshVisibility();}
#endif
            }
            catch(OperationCanceledException) { }
            finally { busy=false; }
        }
    }
}
