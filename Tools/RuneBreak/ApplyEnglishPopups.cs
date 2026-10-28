using UnityEditor;
using RuneBreak;
using RuneBreak.Editor;
public static class ApplyEnglishPopups
{
    public static void Execute()
    {
        RuneEnglishAuthoring.EnsureFonts();RuneProjectBuilder.RebuildResultPopup();
        var root=PrefabUtility.LoadPrefabContents(RuneProjectBuilder.SettingsPopupPath);
        try{RuneSurfaceAuthoring.ConfigureSettings(root.GetComponent<RuneSettingsPanel>());PrefabUtility.SaveAsPrefabAsset(root,RuneProjectBuilder.SettingsPopupPath);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        RuneEnglishAuthoring.Apply();
    }
}
