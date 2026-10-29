using UnityEditor;
using UnityEngine;
using RuneBreak;
using RuneBreak.Editor;
public static class ApplyRewardArrival
{
    public static void Execute()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("Stop Play mode first.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var root=PrefabUtility.LoadPrefabContents(RuneProjectBuilder.ResultPopupPath);
        try{RuneRewardAuthoring.ConfigurePresentation(root.GetComponent<RuneResultPanel>());PrefabUtility.SaveAsPrefabAsset(root,RuneProjectBuilder.ResultPopupPath);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("REWARD ARRIVAL: alpha 230, dimensional Clear title, foreground coins and bounded impacts.");
    }
}
