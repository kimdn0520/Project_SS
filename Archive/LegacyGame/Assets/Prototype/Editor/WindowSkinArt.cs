using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace ProjectSS.Expedition.Editor
{
    public static class WindowSkinArt
    {
        public const string FramePath="Assets/Prototype/Art/WindowFrame-v2.png";
        public const string ClosePath="Assets/Textures/UI/Common/Buttons/WindowClose-red-v3.png";
        static Sprite Import(string path,Vector4 border)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spriteBorder=border;importer.spritePixelsPerUnit=100;importer.maxTextureSize=2048;
            importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;
            importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static RectTransform New(string name,Transform parent)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);return r;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static void Frame(Image image,Sprite sprite)
        {
            image.sprite=sprite;image.color=Color.white;image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=3;
            const string path="Assets/Prototype/Art/WindowFrameUI.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("ProjectSS/UI/WindowFrame"));AssetDatabase.CreateAsset(mat,path);}
            image.material=mat;
            if(image.GetComponent<AtlasLocalUV>()==null)image.gameObject.AddComponent<AtlasLocalUV>();
        }
        static void Close(Image image,Sprite sprite)
        {
            image.sprite=sprite;image.color=Color.white;image.type=Image.Type.Simple;image.preserveAspect=true;
            const string path="Assets/Prototype/Art/WindowCloseUI.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("ProjectSS/UI/WindowFrame"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetFloat("_HalfExtent",512);mat.SetFloat("_CornerRadius",208);EditorUtility.SetDirty(mat);image.material=mat;
            if(image.GetComponent<AtlasLocalUV>()==null)image.gameObject.AddComponent<AtlasLocalUV>();
        }
        public static void Configure(PlayPage page)
        {
            var frame=Import(FramePath,new Vector4(160,160,160,160));var close=Import(ClosePath,Vector4.zero);
            // Modify graphics only: the designer-owned Common close transform is preserved.
            foreach(var name in new[]{"CommonPopup","ExpeditionNoticePopup","EquipmentSelectionPopup","VeinSelectionPopup"})
            {
                string path="Assets/Resources/Prefabs/Popups/"+name+".prefab";
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var window=root.transform.Find(name=="CommonPopup"?"Window":"CommonPopup/Window");
                    Frame(window.Find("bg").GetComponent<Image>(),frame);Close(window.Find("Close").GetComponent<Image>(),close);
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            var layer=page.GetComponentInChildren<MenuPanelLayer>(true);
            if(layer==null)
            {
                var root=New("MenuPanelCanvas",page.transform);var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.planeDistance=10f;canvas.sortingOrder=500;
                var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(720,1280);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
                root.gameObject.AddComponent<GraphicRaycaster>();var group=root.gameObject.AddComponent<CanvasGroup>();
                layer=root.gameObject.AddComponent<MenuPanelLayer>();layer.page=page;layer.group=group;
                var curtain=New("Curtain",root);Stretch(curtain);var shade=curtain.gameObject.AddComponent<Image>();shade.color=new Color(.02f,.04f,.06f,.68f);
                var button=curtain.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;UnityEventTools.AddPersistentListener(button.onClick,layer.Close);layer.curtain=curtain.gameObject;
                var body=New("Composition",root);body.anchorMin=body.anchorMax=body.pivot=new Vector2(.5f,.5f);body.sizeDelta=new Vector2(720,1280);body.anchoredPosition=Vector2.zero;layer.composition=body;
                foreach(var panel in page.menuPanels){panel.transform.SetParent(body,false);Stretch((RectTransform)panel.transform);}
            }
            foreach(var panel in page.menuPanels)
            {
                foreach(var image in panel.GetComponentsInChildren<Image>(true))
                {
                    if(image.name=="Backdrop"||image.name=="BagBackground")
                    {
                        Frame(image,frame);image.rectTransform.anchoredPosition=new Vector2(16,-100);image.rectTransform.sizeDelta=new Vector2(688,1073);
                    }
                    if(image.name.StartsWith("BackToMine"))
                    {Close(image,close);}
                }
            }
            AlignPanelClose(page);
            WindowTitleArt.Configure(page);
            foreach(var text in new[]{page.stageLabel,page.resources})
            {
                text.fontStyle=FontStyles.Bold;text.fontWeight=FontWeight.Heavy;
                text.fontSize=text==page.stageLabel?28:22;
                text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Overflow;
                if(text==page.stageLabel){text.rectTransform.anchoredPosition=new Vector2(24,-8);text.rectTransform.sizeDelta=new Vector2(464,44);}
                var mat=text.fontSharedMaterial;mat.SetFloat(ShaderUtilities.ID_FaceDilate,.36f);mat.SetFloat(ShaderUtilities.ID_OutlineWidth,.23f);EditorUtility.SetDirty(mat);
                text.UpdateMeshPadding();
            }
            var sky=page.GetComponentInChildren<BattleSkyExtension>(true);
            if(sky!=null){sky.sky.enabled=false;sky.sky.rectTransform.anchorMin=sky.sky.rectTransform.anchorMax=Vector2.one;sky.sky.rectTransform.sizeDelta=Vector2.zero;}
            EditorUtility.SetDirty(page);
        }
        public static void AlignPanelClose(PlayPage page)
        {
            var common=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Popups/CommonPopup.prefab");
            var referenceFrame=(RectTransform)common.transform.Find("Window/bg");
            var referenceClose=(RectTransform)common.transform.Find("Window/Close");
            // Match the authored button pivot's offset from the frame's upper-right corner.
            Vector2 offset=referenceClose.anchoredPosition-referenceFrame.anchoredPosition-new Vector2(referenceFrame.rect.width,0);
            foreach(var panel in page.menuPanels)
            {
                var images=panel.GetComponentsInChildren<Image>(true);
                var frame=images.FirstOrDefault(i=>i.name=="Backdrop"||i.name=="BagBackground");
                if(frame==null)continue;
                foreach(var button in images.Where(i=>i.name.StartsWith("BackToMine")))
                {
                    var r=button.rectTransform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=referenceClose.pivot;
                    r.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,referenceClose.rect.width);
                    r.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,referenceClose.rect.height);
                    r.anchoredPosition=frame.rectTransform.anchoredPosition+new Vector2(frame.rectTransform.rect.width,0)+offset;
                }
            }
        }
    }
}
