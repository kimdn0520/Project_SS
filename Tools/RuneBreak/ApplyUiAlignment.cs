using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using RuneBreak;
using RuneBreak.Editor;

public static class ApplyUiAlignment
{
    public static void Execute()
    {
        if(Application.isPlaying)throw new Exception("Stop Play mode before authoring.");
        var stage=PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null){if(stage.scene.isDirty)PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot,stage.assetPath);StageUtility.GoToMainStage();}
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string path=RuneSurfaceAuthoring.Folder+(System.IO.File.Exists(RuneSurfaceAuthoring.Folder+"cabinet_lavender.png")?"cabinet_lavender.png":System.IO.File.Exists(RuneSurfaceAuthoring.Folder+"cabinet_clean.png")?"cabinet_clean.png":System.IO.File.Exists(RuneSurfaceAuthoring.Folder+"cabinet_refined.png")?"cabinet_refined.png":"cabinet_aligned.png");var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=300;ti.filterMode=FilterMode.Point;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;ti.maxTextureSize=4096;
        var settings=new TextureImporterSettings();ti.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;ti.SetTextureSettings(settings);ti.SaveAndReimport();
        var cabinet=AssetDatabase.LoadAssetAtPath<Sprite>(path);var art=AssetDatabase.LoadAssetAtPath<RuneArtLibrary>(RuneSurfaceAuthoring.ArtPath);art.cabinets=new[]{cabinet,cabinet,cabinet};EditorUtility.SetDirty(art);
        var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/RuneBreak/Art/BlockPunch/BlockPunch.spriteatlas");if(!atlas.GetPackables().Contains(cabinet))atlas.Add(new UnityEngine.Object[]{cabinet});EditorUtility.SetDirty(atlas);
        Edit("Assets/Resources/Prefabs/PlayPage.prefab",root=>{var p=root.GetComponent<RunePlayPage>();p.background.sprite=cabinet;RuneSurfaceAuthoring.ConfigurePlayAlignment(p);});
        Edit("Assets/Resources/Prefabs/MainPage.prefab",root=>RuneSurfaceAuthoring.ConfigureSettingsButton(root.transform.Find("SafeContent/Settings").GetComponent<UnityEngine.UI.Button>(),24));
        Edit(RuneProjectBuilder.SettingsPopupPath,root=>{RuneSurfaceAuthoring.ConfigureSettings(root.GetComponent<RuneSettingsPanel>());RuneEnglishAuthoring.StyleTree(root);});
        Edit(RuneProjectBuilder.ResultPopupPath,root=>{RuneSurfaceAuthoring.ConfigureResult(root.GetComponent<RuneResultPanel>());RuneEnglishAuthoring.StyleTree(root);});
        RuneGateNumberAuthoring.Apply();AssetDatabase.SaveAssets();SpriteAtlasUtility.PackAtlases(new[]{atlas},EditorUserBuildSettings.activeBuildTarget);
        Debug.Log("UI ALIGNMENT: recessed storage, centered gear and popup glyphs, even margins and button gap.");
    }
    static void Edit(string path,Action<GameObject> change){var root=PrefabUtility.LoadPrefabContents(path);try{change(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
}
