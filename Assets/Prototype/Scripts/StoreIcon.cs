namespace ProjectSS.Expedition
{
    public sealed class StoreIcon : BaseSidebarIcon
    {
        protected override void Open() { SidebarNotice.Show(Page, "스토어", "스토어를 준비하고 있습니다."); }
    }
}
