using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak.Editor
{
    public static partial class RuneProjectBuilder
    {
        public const string ResultPopupPath="Assets/Resources/Prefabs/Popups/ResultPopup.prefab";
        public static void RebuildResultPopup()
        {
            if(Application.isPlaying)throw new System.InvalidOperationException("Stop Play mode before authoring.");
            art=AssetDatabase.LoadAssetAtPath<RuneArtLibrary>(Root+"Data/RuneSurfaceArt.asset");
            RuneEnglishAuthoring.EnsureFonts();font=RuneEnglishAuthoring.Medium;
            BuildResultPopupPrefab();
            var page=PrefabUtility.LoadPrefabContents("Assets/Resources/Prefabs/PlayPage.prefab");
            try
            {
                foreach(var child in page.GetComponentsInChildren<Transform>(true))if(child!=null&&child.name=="ResultModal")UnityEngine.Object.DestroyImmediate(child.gameObject);
                PrefabUtility.SaveAsPrefabAsset(page,"Assets/Resources/Prefabs/PlayPage.prefab");
            }
            finally{PrefabUtility.UnloadPrefabContents(page);}
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/Play.unity")scene=EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");
            ConfigurePopupManager(UnityEngine.Object.FindFirstObjectByType<PageManager>(),UnityEngine.Object.FindFirstObjectByType<Camera>());
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        private static void BuildResultPopupPrefab()
        {
            var go=new GameObject(RuneResultPanel.PrefabName,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CanvasGroup));
            try
            {
                var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.pixelPerfect=true;canvas.planeDistance=10;
                var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(540,960);scaler.matchWidthOrHeight=.5f;
                var popup=go.AddComponent<RuneResultPanel>();Set(popup,"canvas",canvas);Set(popup,"canvasGroup",go.GetComponent<CanvasGroup>());
                var curtain=Image("Curtain",go.transform,0,0,540,960,null,new Color(.21f,.18f,.22f,.61f));Stretch(curtain.rectTransform);curtain.raycastTarget=true;
                var content=Rect("SafeContent",go.transform,0,0,540,960);content.anchorMin=content.anchorMax=content.pivot=new Vector2(.5f,.5f);content.anchoredPosition=Vector2.zero;
                var layout=go.AddComponent<RuneSafeLayout>();layout.canvasRect=(RectTransform)go.transform;layout.content=content;
                var panel=PopupPanel(content,"LevelClear",440,350);popup.panel=panel.rectTransform;Set(popup,"animationRoot",panel.transform);
                popup.title=Text("ResultTitle",panel.transform,38,36,364,55,"LevelClear",34,Paper);
                popup.description=Text("Description",panel.transform,40,120,360,86,"Level 1 complete!\nReady for the next challenge?",21,Muted);
                popup.primaryButton=LobbyButton("NextOrRetry",panel.transform,176,254,230,70,"Next Level","gold");popup.primaryLabel=popup.primaryButton.GetComponentInChildren<TMP_Text>();
                popup.homeButton=LobbyButton("Home",panel.transform,30,254,140,70,"Home","gold");
                UnityEventTools.AddPersistentListener(popup.primaryButton.onClick,popup.ChoosePrimary);UnityEventTools.AddPersistentListener(popup.homeButton.onClick,popup.ChooseHome);
                var blocker=Image("TransitionInputBlocker",go.transform,0,0,540,960,null,Color.clear);Stretch(blocker.rectTransform);blocker.raycastTarget=true;Set(popup,"transitionInputBlocker",blocker.gameObject);blocker.gameObject.SetActive(false);
                RuneSurfaceAuthoring.ConfigureResult(popup);RuneEnglishAuthoring.StyleTree(go);
                Directory.CreateDirectory(Path.GetDirectoryName(ResultPopupPath));go.SetActive(false);PrefabUtility.SaveAsPrefabAsset(go,ResultPopupPath);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
