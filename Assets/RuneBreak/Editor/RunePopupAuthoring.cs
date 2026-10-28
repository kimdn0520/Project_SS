using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RuneBreak.Editor
{
    public static partial class RuneProjectBuilder
    {
        public const string SettingsPopupPath="Assets/Resources/Prefabs/Popups/SettingsPopup.prefab";

        [MenuItem("Rune Break/Rebuild settings popup only")]
        public static void RebuildSettingsPopupOnly()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before rebuilding.");
            art=AssetDatabase.LoadAssetAtPath<RuneArtLibrary>(Root+"Data/RuneArt.asset");
            RuneEnglishAuthoring.EnsureFonts();
            BuildSettingsPopupPrefab();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Rune Break/Rebuild settings popup and wire manager")]
        public static void RebuildSettingsPopup()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before rebuilding.");
            art=AssetDatabase.LoadAssetAtPath<RuneArtLibrary>(Root+"Data/RuneArt.asset");
            RuneEnglishAuthoring.EnsureFonts();RuneLobbySkinAuthoring.EnsureSkins();
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");
            BuildSettingsPopupPrefab();
            MigrateSettingsPage("Assets/Resources/Prefabs/MainPage.prefab");
            MigrateSettingsPage("Assets/Resources/Prefabs/PlayPage.prefab");
            ConfigurePopupManager(UnityEngine.Object.FindFirstObjectByType<PageManager>(),UnityEngine.Object.FindFirstObjectByType<Camera>());
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("RUNE SETTINGS: cached prefab registered with PopupManager; PopupParent authored; embedded settings removed from both pages.");
        }

        private static void MigrateSettingsPage(string path)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var old in root.GetComponentsInChildren<RuneSettingsPanel>(true))UnityEngine.Object.DestroyImmediate(old.gameObject);
                var lobby=root.GetComponent<RuneLobbyPage>();
                if(lobby!=null)lobby.showcase.page=lobby;
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }

        private static void ConfigurePopupManager(PageManager pages,Camera camera)
        {
            var manager=UnityEngine.Object.FindFirstObjectByType<PopupManager>(FindObjectsInactive.Include);
            if(manager==null)manager=new GameObject("PopupManager").AddComponent<PopupManager>();
            var root=manager.transform.Find("PopupParent");
            if(root==null){root=new GameObject("PopupParent").transform;root.SetParent(manager.transform,false);}
            Set(manager,"popupRoot",root);Set(manager,"renderCamera",camera);
            Set(manager,"persistAcrossScenes",false);Set(manager,"handleBackInput",false);
            var so=new SerializedObject(manager);var prefabs=so.FindProperty("popupPrefabs");
            foreach(string path in new[]{SettingsPopupPath,ResultPopupPath})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<BasePopupHandler>(path);if(prefab==null)continue;int index=-1;
                for(int i=0;i<prefabs.arraySize;i++)if(prefabs.GetArrayElementAtIndex(i).objectReferenceValue==prefab)index=i;
                if(index<0){index=prefabs.arraySize;prefabs.arraySize++;}
                prefabs.GetArrayElementAtIndex(index).objectReferenceValue=prefab;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Set(pages,"popupRoot",root);Set(pages,"popupManager",manager);
            foreach(var page in UnityEngine.Object.FindObjectsByType<RuneLobbyPage>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {page.popups=manager;PrefabUtility.RecordPrefabInstancePropertyModifications(page);}
            foreach(var page in UnityEngine.Object.FindObjectsByType<RunePlayPage>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {page.popups=manager;PrefabUtility.RecordPrefabInstancePropertyModifications(page);}
        }

        private static void BuildSettingsPopupPrefab()
        {
            var go=new GameObject(RuneSettingsPanel.PrefabName,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CanvasGroup));
            try
            {
                var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.pixelPerfect=true;canvas.planeDistance=10;
                var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution=new Vector2(540,960);scaler.matchWidthOrHeight=.5f;
                var popup=go.AddComponent<RuneSettingsPanel>();
                Set(popup,"canvas",canvas);Set(popup,"canvasGroup",go.GetComponent<CanvasGroup>());
                var curtain=Image("Curtain",go.transform,0,0,540,960,null,new Color(.04f,.09f,.12f,.9f));Stretch(curtain.rectTransform);curtain.raycastTarget=true;
                var curtainButton=curtain.gameObject.AddComponent<Button>();curtainButton.targetGraphic=curtain;curtainButton.transition=Selectable.Transition.None;Set(popup,"curtainButton",curtainButton);
                var content=Rect("SafeContent",go.transform,0,0,540,960);
                content.anchorMin=content.anchorMax=content.pivot=new Vector2(.5f,.5f);content.anchoredPosition=Vector2.zero;
                var layout=go.AddComponent<RuneSafeLayout>();layout.canvasRect=(RectTransform)go.transform;layout.content=content;
                var panel=PopupPanel(content,"Settings",460,376);
                popup.panel=panel.rectTransform;Set(popup,"animationRoot",panel.transform);
                popup.musicToggle=AudioToggle(panel.transform,"Music","Music",74);
                popup.audioToggle=AudioToggle(panel.transform,"SoundEffects","Sound Effects",160);
                UnityEventTools.AddPersistentListener(popup.musicToggle.onValueChanged,popup.SetMusic);
                UnityEventTools.AddPersistentListener(popup.audioToggle.onValueChanged,popup.SetAudio);
                var dismiss=LobbyButton("Close",panel.transform,100,278,260,64,"Close","gold");
                Set(popup,"closeButton",dismiss);
                popup.lobbyActions=dismiss.gameObject;
                var playActions=Rect("PlayActions",panel.transform,0,278,460,64);
                popup.playActions=playActions.gameObject;
                var home=LobbyButton("Home",playActions,28,0,194,64,"Home","gold");
                var resume=LobbyButton("Resume",playActions,238,0,194,64,"Resume","gold");
                UnityEventTools.AddPersistentListener(home.onClick,popup.GoHome);
                UnityEventTools.AddPersistentListener(resume.onClick,popup.Dismiss);
                playActions.gameObject.SetActive(false);
                var blocker=Image("TransitionInputBlocker",go.transform,0,0,540,960,null,Color.clear);
                Stretch(blocker.rectTransform);blocker.raycastTarget=true;Set(popup,"transitionInputBlocker",blocker.gameObject);blocker.gameObject.SetActive(false);
                RuneBlockUiAuthoring.ConfigureSettings(popup);RuneEnglishAuthoring.StyleTree(go);
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPopupPath));go.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(go,SettingsPopupPath);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }

        // Shared popup frame; the selected material authoring controls title placement.
        private static Image PopupPanel(Transform parent,string title,float width,float height)
        {
            var panel=LobbyPanel("Panel",parent,270,480,width,height,"panel");
            panel.raycastTarget=true;
            panel.rectTransform.pivot=new Vector2(.5f,.5f);
            var plate=LobbyPanel("TitlePlate",panel.transform,(width-220)*.5f,-28,220,64,"gold");
            plate.raycastTarget=true;
            CenteredText("Title",plate.transform,12,4,196,52,title,24,RuneArtLibrary.Hex("513d29"));
            RuneBlockUiAuthoring.ConfigureFrame(panel,title,width,height);
            return panel;
        }

        private static Toggle AudioToggle(Transform parent,string name,string label,float y)
        {
            var row=LobbyPanel(name,parent,28,y,404,68,"inset");
            row.raycastTarget=true;
            var toggle=row.gameObject.AddComponent<Toggle>();
            var box=LobbyPanel("Box",row.transform,338,12,44,44,"teal");
            var mark=Rect("Check",box.transform,0,-1,44,44).gameObject.AddComponent<RuneLobbyIcon>();
            mark.kind="check";mark.pixelSize=3;mark.color=RuneArtLibrary.Hex("d4e7ac");mark.raycastTarget=false;
            toggle.targetGraphic=box;toggle.graphic=mark;toggle.toggleTransition=UnityEngine.UI.Toggle.ToggleTransition.None;
            var colors=toggle.colors;colors.highlightedColor=Color.white;colors.pressedColor=new Color(.9f,.9f,.9f);colors.fadeDuration=0;toggle.colors=colors;
            Text("Label",row.transform,24,14,280,40,label,24,Paper);
            return toggle;
        }

        private static void Stretch(RectTransform rect)
        {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    }
}
