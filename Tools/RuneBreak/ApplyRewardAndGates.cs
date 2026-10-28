using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using RuneBreak;
using RuneBreak.Editor;
public static class ApplyRewardAndGates
{
    public static void Execute()
    {
        if(Application.isPlaying)throw new Exception("Stop Play mode before authoring.");
        var stage=PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null){if(stage.scene.isDirty)PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot,stage.assetPath);StageUtility.GoToMainStage();}
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);var names=new[]{"cabinet_clean","reward_coins","reward_rays","reward_wallet"};
        foreach(string name in names){var ti=(TextureImporter)AssetImporter.GetAtPath(RuneSurfaceAuthoring.Folder+name+".png");ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=300;ti.filterMode=FilterMode.Point;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;ti.maxTextureSize=4096;var settings=new TextureImporterSettings();ti.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;ti.SetTextureSettings(settings);ti.SaveAndReimport();}
        TwoLayers();var art=AssetDatabase.LoadAssetAtPath<RuneArtLibrary>(RuneSurfaceAuthoring.ArtPath);var cabinet=Art("cabinet_lavender")??Art("cabinet_clean");art.cabinets=new[]{cabinet,cabinet,cabinet};EditorUtility.SetDirty(art);
        var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/RuneBreak/Art/BlockPunch/BlockPunch.spriteatlas");var old=atlas.GetPackables();atlas.Add(names.Select(n=>(UnityEngine.Object)Art(n)).Where(s=>!old.Contains(s)).ToArray());EditorUtility.SetDirty(atlas);
        Edit("Assets/Resources/Prefabs/PlayPage.prefab",root=>{var p=root.GetComponent<RunePlayPage>();p.background.sprite=cabinet;RuneRewardAuthoring.ConfigurePlay(p);p.LoadLevel(0);});
        Edit("Assets/Resources/Prefabs/MainPage.prefab",root=>{var p=root.GetComponent<RuneLobbyPage>();p.coinBalance=root.transform.Find("SafeContent/Currency").GetComponent<TMPro.TMP_Text>();});
        Edit(RuneProjectBuilder.ResultPopupPath,root=>{RuneRewardAuthoring.Configure(root.GetComponent<RuneResultPanel>());RuneEnglishAuthoring.StyleTree(root);});
        AssetDatabase.SaveAssets();SpriteAtlasUtility.PackAtlases(new[]{atlas},EditorUserBuildSettings.activeBuildTarget);Debug.Log("REWARD AND GATES AUTHORED: fullscreen Claim reward, baked coin pool, clean board, two layers, cracks.");
    }
    static void TwoLayers()
    {
        const string path="Assets/RuneBreak/Data/levels.json";string backup="Tools/RuneBreak/Source/BlockPunch/Surface/levels-before-two-layer.json";if(!File.Exists(backup))File.Copy(path,backup);
        var data=JsonUtility.FromJson<RuneLevelJson>(File.ReadAllText(path));int modified=0;
        foreach(var level in data.levels)
        {
            if(level.seals.N.Length<=2&&level.seals.E.Length<=2)continue;
            string[] colors={"red","blue","gold","green"};var totals=level.blocks.GroupBy(b=>b.color).ToDictionary(g=>g.Key,g=>g.Count());RuneSeals best=null;int score=int.MinValue;
            foreach(var a in colors)foreach(var b in colors.Where(c=>c!=a))foreach(var c in colors.Where(c=>c!=a&&c!=b))
            {
                string d=colors.First(x=>x!=a&&x!=b&&x!=c);var candidate=JsonUtility.FromJson<RuneLevel>(JsonUtility.ToJson(level));candidate.seals=new RuneSeals{N=new[]{new RuneLayer{color=a,hp=totals[a]},new RuneLayer{color=b,hp=totals[b]}},E=new[]{new RuneLayer{color=c,hp=totals[c]},new RuneLayer{color=d,hp=totals[d]}}};
                var rules=new RuneRules(candidate);var route=RuneAutoRules.Solve(rules,rules.Initial());if(route.status!=RuneSolveStatus.Solved)continue;
                int value=(a==level.seals.N[0].color?100:0)+(c==level.seals.E[0].color?100:0)+(b==level.seals.N[1].color?15:0)+(d==level.seals.E[1].color?15:0);
                if(value>score){score=value;best=candidate.seals;}
            }
            if(best==null)throw new Exception("No solvable two-layer arrangement for "+level.id);level.seals=best;level.desc="Two layers on each gate. Store unmatched colors, then break through.";level.tip="Watch the next color behind each gate before choosing your block.";modified++;
        }
        foreach(var level in data.levels){var rules=new RuneRules(level);if(RuneAutoRules.Solve(rules,rules.Initial()).status!=RuneSolveStatus.Solved)throw new Exception("Unsolvable "+level.id);}
        File.WriteAllText(path,JsonUtility.ToJson(data,true));var catalog=AssetDatabase.LoadAssetAtPath<RuneLevelCatalog>("Assets/RuneBreak/Data/RuneLevels.asset");catalog.levels=data.levels;EditorUtility.SetDirty(catalog);
        File.WriteAllText("Tools/RuneBreak/QA/two-layer-levels.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{status="passed",modified,levels=data.levels.Select(l=>new{l.id,n=l.seals.N.Length,e=l.seals.E.Length,blocks=l.blocks.Length})}));
    }
    static Sprite Art(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(RuneSurfaceAuthoring.Folder+name+".png");
    static void Edit(string path,Action<GameObject> change){var root=PrefabUtility.LoadPrefabContents(path);try{change(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
}
