using UnityEditor;
using UnityEditor.SceneManagement;
using RuneBreak.Editor;
public static class ApplyNumberReadability
{
    public static void Execute()
    {
        var stage=PrefabStageUtility.GetCurrentPrefabStage();
        if(stage!=null){if(stage.scene.isDirty)PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot,stage.assetPath);StageUtility.GoToMainStage();}
        RuneGateNumberAuthoring.Apply();
    }
}
