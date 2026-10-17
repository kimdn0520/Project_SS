namespace ProjectSS.Expedition
{
    public static class SidebarNotice
    {
        public static void Show(PlayPage page, string title, string body)
        {
            PopupManager.Show("ExpeditionNoticePopup", new ExpeditionNoticePopup.Content
            { title=title, body=body, action="확인", pausePolicy=page.pausePolicy });
        }
    }
}
