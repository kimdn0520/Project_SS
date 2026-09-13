using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class ConfigureSplash
{
    public static void Execute()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop playing first");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Splash.unity");
        var app=UnityEngine.Object.FindFirstObjectByType<AppManager>();
        if(app==null)throw new Exception("Splash AppManager missing");
        var so=new SerializedObject(app);
        if(so.FindProperty("globalFadeCanvasGroup").objectReferenceValue==null)
        {
            var go=new GameObject("GlobalFadeOverlay",typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup));go.transform.SetParent(app.transform,false);
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=9999;
            var group=go.GetComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;
            var black=new GameObject("BlackBackdrop",typeof(RectTransform),typeof(Image));black.transform.SetParent(go.transform,false);
            black.GetComponent<Image>().color=Color.black;var r=(RectTransform)black.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            so.FindProperty("globalFadeCanvasGroup").objectReferenceValue=group;so.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Splash has baked fade canvas; next scene is Play.");
    }
}
