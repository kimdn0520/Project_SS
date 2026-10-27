using System;
using UnityEditor;
using UnityEngine;
using RuneBreak;
using RuneBreak.Editor;
public static class SettingsNavigationQA
{
    public static void Build()
    {
        RuneProjectBuilder.RebuildSettingsPopupOnly();
        const string path="Assets/Resources/Prefabs/MainPage.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach(var preview in root.GetComponentsInChildren<RuneLobbyShowcase>(true))
                foreach(var source in preview.GetComponents<AudioSource>())UnityEngine.Object.DestroyImmediate(source);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
        var prefab=AssetDatabase.LoadAssetAtPath<RuneSettingsPanel>(RuneProjectBuilder.SettingsPopupPath);
        if(prefab.lobbyActions==null||prefab.playActions==null)throw new Exception("Missing settings actions");
        var instance=UnityEngine.Object.Instantiate(prefab);
        try
        {
            instance.OnWillEnter(null);
            if(!instance.lobbyActions.activeSelf||instance.playActions.activeSelf)throw new Exception("Lobby actions incorrect");
            var play=AssetDatabase.LoadAssetAtPath<RunePlayPage>("Assets/Resources/Prefabs/PlayPage.prefab");
            instance.OnWillEnter(play);
            if(instance.lobbyActions.activeSelf||!instance.playActions.activeSelf)throw new Exception("Play actions incorrect");
            foreach(var button in instance.playActions.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                if(button.onClick.GetPersistentEventCount()!=1)throw new Exception("Missing button binding");
        }
        finally{UnityEngine.Object.DestroyImmediate(instance.gameObject);}
        Debug.Log("SETTINGS QA PASS: lobby/play actions and button bindings; preview AudioSource removed.");
    }
}
