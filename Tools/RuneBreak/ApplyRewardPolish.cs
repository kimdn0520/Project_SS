using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using RuneBreak;
using RuneBreak.Editor;
public static class ApplyRewardPolish
{
    public static void Execute()
    {
        if(Application.isPlaying)throw new Exception("Stop Play mode before authoring.");AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var names=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("Tools/RuneBreak/Source/BlockPunch/Surface/reward-motion-manifest.json")).Properties().Select(p=>p.Name).Concat(new[]{"cabinet_lavender"}).ToArray();
        foreach(string name in names){var ti=(TextureImporter)AssetImporter.GetAtPath(RuneSurfaceAuthoring.Folder+name+".png");ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=300;ti.filterMode=FilterMode.Point;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;var settings=new TextureImporterSettings();ti.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;ti.SetTextureSettings(settings);ti.SaveAndReimport();}
        var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/RuneBreak/Art/BlockPunch/BlockPunch.spriteatlas");var old=atlas.GetPackables();atlas.Add(names.Select(n=>(UnityEngine.Object)AssetDatabase.LoadAssetAtPath<Sprite>(RuneSurfaceAuthoring.Folder+n+".png")).Where(s=>!old.Contains(s)).ToArray());EditorUtility.SetDirty(atlas);
        var art=AssetDatabase.LoadAssetAtPath<RuneArtLibrary>(RuneSurfaceAuthoring.ArtPath);var cabinet=AssetDatabase.LoadAssetAtPath<Sprite>(RuneSurfaceAuthoring.Folder+"cabinet_lavender.png");art.cabinets=new[]{cabinet,cabinet,cabinet};EditorUtility.SetDirty(art);
        Edit("Assets/Resources/Prefabs/PlayPage.prefab",root=>{var p=root.GetComponent<RunePlayPage>();p.background.sprite=cabinet;RuneSurfaceAuthoring.ConfigureBackdrop(p);RuneRewardAuthoring.ConfigurePlay(p);});
        Edit(RuneProjectBuilder.ResultPopupPath,root=>{RuneRewardAuthoring.Configure(root.GetComponent<RuneResultPanel>());RuneEnglishAuthoring.StyleTree(root);});
        AssetDatabase.SaveAssets();SpriteAtlasUtility.PackAtlases(new[]{atlas},EditorUserBuildSettings.activeBuildTarget);Debug.Log("REWARD POLISH: layered coin settling, individual collection origins and shared lobby backdrop.");
    }
    static void Edit(string path,Action<GameObject> change){var root=PrefabUtility.LoadPrefabContents(path);try{change(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
}
