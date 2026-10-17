using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using RuneBreak;
public static class FixRuneSettingsLayout
{
    public static void Execute()
    {
        if(Application.isPlaying)throw new System.Exception("Stop first");
        foreach(string name in new[]{"MainPage","PlayPage"})
        {
            string path="Assets/Resources/Prefabs/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            var panel=root.GetComponentInChildren<RuneSettingsPanel>(true);var rect=panel.volume.fillRect;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var image=rect.GetComponent<Image>();image.type=Image.Type.Filled;image.fillMethod=Image.FillMethod.Horizontal;image.fillAmount=panel.volume.value;
            PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");
    }
}
