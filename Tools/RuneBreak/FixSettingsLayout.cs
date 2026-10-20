using RuneBreak.Editor;
public static class FixRuneSettingsLayout
{
    // Retain the old editor entry point; the authored settings layout no longer has a slider.
    public static void Execute()=>RuneProjectBuilder.RebuildSettingsPopupOnly();
}
