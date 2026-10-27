using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ApplyBlockCrashTitle
{
    public static void Execute()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play mode before authoring.");
        int changed=0;
        foreach(var path in new[]{"Assets/Resources/Prefabs/MainPage.prefab","Assets/Resources/Prefabs/PlayPage.prefab"})
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try{changed+=Rename(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        const string splashPath="Assets/Scenes/Splash.unity";
        var splash=SceneManager.GetSceneByPath(splashPath);bool wasLoaded=splash.isLoaded;
        if(!wasLoaded)splash=EditorSceneManager.OpenScene(splashPath,OpenSceneMode.Additive);
        try{foreach(var root in splash.GetRootGameObjects())changed+=Rename(root);EditorSceneManager.SaveScene(splash);}
        finally{if(!wasLoaded)EditorSceneManager.CloseScene(splash,true);}
        AssetDatabase.SaveAssets();Debug.Log("BLOCK CRASH: renamed "+changed+" player-facing title labels.");
    }
    private static int Rename(GameObject root)
    {
        int changed=0;
        foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if(text.text=="RUNE BREAK")text.text="BLOCK CRASH";
            else if(text.text=="RUNE <color=#FFD84D>BREAK</color>")text.text="BLOCK <color=#FFD84D>CRASH</color>";
            else continue;
            EditorUtility.SetDirty(text);changed++;
        }
        return changed;
    }
}
