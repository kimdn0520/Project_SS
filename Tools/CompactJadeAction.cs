using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using ProjectSS.Expedition;
public static class CompactJadeAction
{
    public static string Inspect()
    {
        var s=AssetDatabase.LoadAllAssetsAtPath("Assets/Textures/UI/Common/Buttons/JadeAction-v3.png").OfType<Sprite>().First(x=>x.name=="Action");
        return $"rect={s.rect}, border={s.border}, PPU={s.pixelsPerUnit}, texture={s.texture.width}x{s.texture.height}";
    }
    public static void Capture(){CaptureAsync().Forget();}
    static async UniTaskVoid CaptureAsync()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();
        EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();
        page.pausePolicy.ReleasePause("AppFocusLoss");page.OpenMenu(1);
        await UniTask.Delay(400,ignoreTimeScale:true);await UniTask.WaitForEndOfFrame(page);
        var texture=ScreenCapture.CaptureScreenshotAsTexture();
        System.IO.File.WriteAllBytes("PrototypeQA/jade-restored.png",texture.EncodeToPNG());
        UnityEngine.Object.Destroy(texture);
    }
    public static string Execute()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop play first");
        const string path="Assets/Textures/UI/Common/Buttons/JadeAction-v3.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.maxTextureSize=256;
        foreach(var platform in new[]{"Standalone","Android","iPhone"})
        {
            var settings=importer.GetPlatformTextureSettings(platform);
            if(settings.overridden){settings.maxTextureSize=256;importer.SetPlatformTextureSettings(settings);}
        }
        importer.SaveAndReimport();
        var sprite=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First(s=>s.name=="Action");
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/Art/JadeActionUI.mat");
        // Unity scales both borders and sprite PPU on import. Keep the original image
        // multiplier so on-screen corner thickness stays unchanged.
        float multiplier=8f;
        int updated=0;
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Resources/Prefabs"}))
        {
            string prefabPath=AssetDatabase.GUIDToAssetPath(guid);
            var root=PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                bool changed=false;
                foreach(var image in root.GetComponentsInChildren<Image>(true))
                {
                    bool manage=image.name=="ManageGear";
                    if(!manage && (image.sprite==null || AssetDatabase.GetAssetPath(image.sprite)!=path))continue;
                    image.sprite=sprite;image.overrideSprite=null;image.material=material;
                    image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=multiplier;image.color=Color.white;
                    if(manage)
                    {
                        var r=image.rectTransform;
                        r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
                        r.anchoredPosition=new Vector2(28,-174);r.sizeDelta=new Vector2(136,34);
                        var text=image.GetComponentInChildren<TMP_Text>(true);
                        text.color=new Color(1,.96f,.82f);text.fontSizeMax=22;text.fontSizeMin=14;
                        text.rectTransform.offsetMin=new Vector2(14,8);text.rectTransform.offsetMax=new Vector2(-14,-5);
                    }
                    updated++;changed=true;
                }
                if(changed)PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
        return $"Restored original ManageGear design/layout. JadeAction imported texture={sprite.texture.width}x{sprite.texture.height}, sprite={sprite.rect.width}x{sprite.rect.height}, PPU multiplier={multiplier}, updated {updated} images.";
    }
}
