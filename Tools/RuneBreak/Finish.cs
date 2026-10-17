using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class FinishRuneWork
{
    public static void Execute()
    {
        if(Application.isPlaying)throw new System.Exception("Stop Play mode before finishing.");
        PlayerPrefs.SetInt("RuneBreak.Clears",0);PlayerPrefs.SetInt("RuneBreak.Next",0);PlayerPrefs.Save();
        EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");
        typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries").GetMethod("Clear",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public).Invoke(null,null);
        Debug.Log("Rune Break ready. Splash -> Play / MainPage + PlayPage. Prototype-only controls removed. Validation reports: Tools/RuneBreak/QA.");
    }
}
