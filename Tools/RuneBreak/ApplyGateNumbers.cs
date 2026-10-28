using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RuneBreak.Editor;

public static class ApplyGateNumbers
{
    public static void Execute()
    {
        var stage=PrefabStageUtility.GetCurrentPrefabStage();
        if(stage!=null){if(stage.scene.isDirty)PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot,stage.assetPath);StageUtility.GoToMainStage();}
        foreach(var name in new[]{"SettingsPopup","ResultPopup"})
        {
            string previous="Assets/Resources/Prefabs/Popups/Rune"+name+".prefab",path="Assets/Resources/Prefabs/Popups/"+name+".prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(previous)!=null)
            {
                string guid=AssetDatabase.AssetPathToGUID(previous);string error=AssetDatabase.MoveAsset(previous,path);if(!string.IsNullOrEmpty(error))throw new Exception(error);
                if(AssetDatabase.AssetPathToGUID(path)!=guid)throw new Exception("Prefab GUID changed.");
            }
            var root=PrefabUtility.LoadPrefabContents(path);try{root.name=name;PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        RuneGateNumberAuthoring.Apply();AssetDatabase.SaveAssets();Debug.Log("GATE NUMBERS: cream face, warm outline, soft depth. Popup assets renamed with GUIDs preserved.");
    }
    public static void SelectResult(){PrefabStageUtility.OpenPrefab("Assets/Resources/Prefabs/Popups/ResultPopup.prefab");}
}
