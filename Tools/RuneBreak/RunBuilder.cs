using UnityEditor;
public static class RunRuneBuilder
{
    public static void Execute()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var type=System.AppDomain.CurrentDomain.GetAssemblies();
        foreach(var asm in type){var builder=asm.GetType("RuneBreak.Editor.RuneProjectBuilder");if(builder!=null){builder.GetMethod("Build").Invoke(null,null);return;}}
        UnityEngine.Debug.Log("Rune Break scripts importing; run builder again after compilation.");
    }
}
