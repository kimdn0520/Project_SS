using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using RuneBreak;
using RuneBreak.Editor;
public static class ApplySurfaceRefinement
{
    public static void Execute()
    {
        if(Application.isPlaying)throw new Exception("Stop Play mode before authoring.");
        var stage=PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null){if(stage.scene.isDirty)PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot,stage.assetPath);StageUtility.GoToMainStage();}
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var names=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("Tools/RuneBreak/Source/BlockPunch/Surface/refined-manifest.json")).Properties().Select(p=>p.Name).ToArray();
        foreach(string name in names)
        {
            var ti=(TextureImporter)AssetImporter.GetAtPath(RuneSurfaceAuthoring.Folder+name+".png");ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=300;ti.filterMode=FilterMode.Point;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;ti.maxTextureSize=4096;
            var settings=new TextureImporterSettings();ti.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;ti.SetTextureSettings(settings);ti.SaveAndReimport();
        }
        var art=AssetDatabase.LoadAssetAtPath<RuneArtLibrary>(RuneSurfaceAuthoring.ArtPath);var cabinet=Art("cabinet_lavender")??Art("cabinet_clean")??Art("cabinet_refined");art.cabinets=new[]{cabinet,cabinet,cabinet};
        art.longGates=(from c in new[]{"red","blue","gold","green"} from d in new[]{"N","E"} select Art("gate_studs_"+c+"_"+d)).ToArray();EditorUtility.SetDirty(art);
        var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/RuneBreak/Art/BlockPunch/BlockPunch.spriteatlas");var existing=atlas.GetPackables();atlas.Add(names.Select(n=>(UnityEngine.Object)Art(n)).Where(s=>!existing.Contains(s)).ToArray());EditorUtility.SetDirty(atlas);
        Edit("Assets/Resources/Prefabs/PlayPage.prefab",root=>{var p=root.GetComponent<RunePlayPage>();p.background.sprite=cabinet;RuneSurfaceAuthoring.ConfigurePlayAlignment(p);p.LoadLevel(0);});
        Edit("Assets/Resources/Prefabs/MainPage.prefab",root=>{
            var button=root.transform.Find("SafeContent/Settings").GetComponent<Button>();RuneSurfaceAuthoring.ConfigureSettingsButton(button,24);
            var layout=root.GetComponent<RuneSafeLayout>();foreach(var pin in layout.pins)if(pin.target==button.transform){pin.position=((RectTransform)button.transform).anchoredPosition;pin.right=true;}
        });
        AssetDatabase.SaveAssets();SpriteAtlasUtility.PackAtlases(new[]{atlas},EditorUserBuildSettings.activeBuildTarget);
        Debug.Log("SURFACE REFINEMENT: two round studs, four matching empty channels, consistent storage size and common settings image.");
    }
    static Sprite Art(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(RuneSurfaceAuthoring.Folder+name+".png");
    static void Edit(string path,Action<GameObject> change){var root=PrefabUtility.LoadPrefabContents(path);try{change(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
}
