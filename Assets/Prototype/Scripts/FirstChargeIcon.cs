namespace ProjectSS.Expedition
{
    public sealed class FirstChargeIcon : BaseSidebarIcon
    {
        protected override void Open() { SidebarNotice.Show(Page, "첫 충전", "첫 충전 혜택을 준비하고 있습니다."); }
    }
}
