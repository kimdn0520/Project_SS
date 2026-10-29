using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using RuneBreak;
using RuneBreak.Editor;
public static class ApplyButtonCoins
{
    // Palette-only updates keep all serialized prefab links and animation data.
    public static void RefreshCoinMaterial()
    {
        if(Application.isPlaying)throw new Exception("Stop Play mode before importing coin material.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(var path in new[]{RuneLobbySkinAuthoring.PunchFolder+"Icons/lobby_coin.png",RuneSurfaceAuthoring.Folder+"reward_coins.png"}.Concat(Enumerable.Range(0,8).Select(i=>RuneSurfaceAuthoring.Folder+"reward_piece_"+i+".png")))
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
        var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/RuneBreak/Art/BlockPunch/BlockPunch.spriteatlas");
        SpriteAtlasUtility.PackAtlases(new[]{atlas},EditorUserBuildSettings.activeBuildTarget);
        Debug.Log("Refreshed the shared yellow coin material in the existing BlockPunch atlas.");
    }
    public static void Execute()
    {
        if(Application.isPlaying)throw new Exception("Stop Play mode before authoring.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var paths=new[]{"molded_gold_372_88","molded_gold_340_76","molded_gold_300_82","molded_green_210_78","molded_amber_210_78"}
            .Select(n=>RuneLobbySkinAuthoring.PunchFolder+"Blocks/"+n+".png")
            .Concat(new[]{RuneLobbySkinAuthoring.PunchFolder+"Icons/lobby_coin.png",RuneSurfaceAuthoring.Folder+"reward_coins.png"})
            .Concat(Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("Tools/RuneBreak/Source/BlockPunch/Surface/reward-motion-manifest.json")).Properties().Select(p=>RuneSurfaceAuthoring.Folder+p.Name+".png")).ToArray();
        foreach(var path in paths)
        {
            var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=300;ti.spriteBorder=Vector4.zero;ti.filterMode=FilterMode.Point;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;
            var settings=new TextureImporterSettings();ti.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;ti.SetTextureSettings(settings);ti.SaveAndReimport();
        }
        var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/RuneBreak/Art/BlockPunch/BlockPunch.spriteatlas");var old=atlas.GetPackables();
        var add=paths.Select(p=>(UnityEngine.Object)AssetDatabase.LoadAssetAtPath<Sprite>(p)).Where(s=>!old.Contains(s)).ToArray();if(add.Length>0)atlas.Add(add);EditorUtility.SetDirty(atlas);
        Edit("Assets/Resources/Prefabs/MainPage.prefab",root=>RuneBlockUiAuthoring.ConfigurePlayButton(root.GetComponent<RuneLobbyPage>()));
        Edit(RuneProjectBuilder.SettingsPopupPath,root=>RuneSurfaceAuthoring.Button(root.GetComponent<RuneSettingsPanel>().lobbyActions.GetComponent<UnityEngine.UI.Button>(),"close",340,76,"Close",24));
        Edit(RuneProjectBuilder.ResultPopupPath,root=>{var p=root.GetComponent<RuneResultPanel>();RuneRewardAuthoring.ConfigureActions(p);RuneRewardAuthoring.ConfigurePile(p);});
        AssetDatabase.SaveAssets();SpriteAtlasUtility.PackAtlases(new[]{atlas},EditorUserBuildSettings.activeBuildTarget);
        Debug.Log("BUTTON COINS: single block faces, cached scale interaction and layered yellow gold artwork authored.");
    }
    static void Edit(string path,Action<GameObject> change){var root=PrefabUtility.LoadPrefabContents(path);try{change(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
}
