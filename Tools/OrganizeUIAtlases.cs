using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using ProjectSS.Expedition;
using ProjectSS.Expedition.Editor;
using Object=UnityEngine.Object;

public static class OrganizeUIAtlases
{
    [Serializable] public sealed class Move { public string from,to; }
    [Serializable] public sealed class Manifest { public List<Move> moves=new List<Move>(); }
    const string Common="Assets/Textures/UI/Common/", Icons="Assets/Textures/UI/Icons/";
    static void Folder(string path)
    {
        path=path.Replace('\\','/');if(AssetDatabase.IsValidFolder(path))return;
        var parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
    static string Destination(string path)
    {
        string file=Path.GetFileName(path);
        if(path.StartsWith("Assets/Textures/UI/NonAtlas/"))return path;
        if(file=="home-background-large.png")return "Assets/Textures/UI/NonAtlas/Backgrounds/"+file;
        if(path.StartsWith("Assets/Textures/Equipment/")||path.StartsWith(Common)||path.StartsWith(Icons))return path;
        if(path.Contains("/Thumbnail/"))return Icons+"Equipment/"+file;
        if(path.Contains("/Portraits/"))return Icons+"Portraits/"+file;
        if(path.Contains("/Icons/")||path.Contains("/Sidebar/"))
            return Icons+(file.StartsWith("Material_")||file.StartsWith("Economy_")||file.StartsWith("coin-")?"Materials/":"Navigation/")+file;
        if(file.Contains("Dig")||file.Contains("dig"))return Common+"Mining/"+file;
        if(file.Contains("progress")||file.Contains("Progress"))return Common+"Bars/"+file;
        if(file.Contains("Jade")||file.Contains("Close")||file.Contains("blue-sparkle"))return Common+"Buttons/"+file;
        if(file.Contains("cell")||file.Contains("Panel9")||file.Contains("menu_bg"))return Common+"Cells/"+file;
        if(file.Contains("background"))return Common+"Backgrounds/"+file;
        return Common+"Frames/"+file;
    }
    public static string Execute()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop play first");
        var prefabPaths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Resources/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        var sprites=new HashSet<Sprite>();
        foreach(var path in prefabPaths)
            foreach(var image in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Image>(true))
                if(image.sprite!=null&&AssetDatabase.GetAssetPath(image.sprite).StartsWith("Assets/"))sprites.Add(image.sprite);
        var manifest=new Manifest();
        var standalone=sprites.Where(s=>AssetDatabase.GetAssetPath(s).EndsWith(".asset")).ToArray();
        var assets=sprites.Select(AssetDatabase.GetAssetPath).ToList();
        assets.AddRange(standalone.Select(s=>AssetDatabase.GetAssetPath(s.texture)));
        // Also put the previous UI kit's existing Textures/UI assets into the same UI hierarchy.
        assets.AddRange(Directory.GetFiles("Assets/Textures/UI","*.png",SearchOption.TopDirectoryOnly).Select(p=>p.Replace('\\','/')));
        foreach(var source in assets.Distinct())
        {
            var target=Destination(source);
            if(source==target)continue;
            Folder(Path.GetDirectoryName(target));
            if(AssetDatabase.LoadMainAssetAtPath(target)!=null)throw new Exception("Asset collision: "+target);
            string error=AssetDatabase.MoveAsset(source,target);if(error!="")throw new Exception(error);
            manifest.moves.Add(new Move{from=source,to=target});
        }
        AssetDatabase.SaveAssets();
        // Legacy standalone Sprite assets cannot be folder-packed reliably. Preserve those
        // source assets and author equivalent imported slices on their original textures.
        var replacements=new Dictionary<Sprite,Sprite>();
        foreach(var group in standalone.GroupBy(s=>AssetDatabase.GetAssetPath(s.texture)))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(group.Key);
            var rects=new List<SpriteMetaData>();
            foreach(var sprite in group)
                rects.Add(new SpriteMetaData{name=sprite.name,rect=sprite.rect,alignment=9,
                    pivot=new Vector2(sprite.pivot.x/sprite.rect.width,sprite.pivot.y/sprite.rect.height),border=sprite.border});
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.spritesheet=rects.ToArray();importer.spritePixelsPerUnit=100;importer.mipmapEnabled=false;
            importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var imported=AssetDatabase.LoadAllAssetsAtPath(group.Key).OfType<Sprite>().ToArray();
            foreach(var sprite in group)replacements[sprite]=imported.First(s=>s.name==sprite.name);
        }
        // Move the existing registry (same GUID), then all prefabs keep that one shared asset.
        const string registry=EquipmentAtlasArt.RegistryPath;
        if(AssetDatabase.LoadAssetAtPath<SpriteAtlasSO>(registry)==null)
        {
            string old=AssetDatabase.FindAssets("t:SpriteAtlasSO").Select(AssetDatabase.GUIDToAssetPath).Single();
            string error=AssetDatabase.MoveAsset(old,registry);if(error!="")throw new Exception(error);
        }
        var data=AssetDatabase.LoadAssetAtPath<SpriteAtlasSO>(registry);data.name="SpriteAtlasScriptable";EditorUtility.SetDirty(data);
        var pageRoot=PrefabUtility.LoadPrefabContents("Assets/Resources/Prefabs/PlayPage.prefab");
        try
        {
            var manager=pageRoot.GetComponentInChildren<SpriteManager>(true);
            EquipmentAtlasArt.BindFolder(manager,Common.TrimEnd('/'),"UICommon");
            EquipmentAtlasArt.BindFolder(manager,Icons.TrimEnd('/'),"UIIcons");
        }
        finally{PrefabUtility.UnloadPrefabContents(pageRoot);}
        var atlases=new[]{"Equipment","UICommon","UIIcons"}.Select(n=>AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/SpriteAtlas/"+n+".spriteatlasv2")).ToArray();
        var registrySo=new SerializedObject(data);var list=registrySo.FindProperty("atlases");list.arraySize=atlases.Length;
        for(int i=0;i<atlases.Length;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=atlases[i];
        registrySo.ApplyModifiedPropertiesWithoutUndo();
        foreach(var atlas in atlases.Skip(1))
        {
            var platform=atlas.GetPlatformSettings("DefaultTexturePlatform");platform.maxTextureSize=4096;platform.textureCompression=TextureImporterCompression.Uncompressed;atlas.SetPlatformSettings(platform);
        }
        int images=0;
        foreach(var path in prefabPaths)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed=false;
                foreach(var image in root.GetComponentsInChildren<Image>(true))
                {
                    if(image.sprite!=null&&replacements.TryGetValue(image.sprite,out var replacement)){image.sprite=replacement;changed=true;}
                    if(image.material!=null&&image.material.shader.name=="ProjectSS/UI/WindowFrame")
                    {
                        if(image.GetComponent<AtlasLocalUV>()==null)image.gameObject.AddComponent<AtlasLocalUV>();
                        changed=true;images++;
                    }
                }
                foreach(var manager in root.GetComponentsInChildren<SpriteManager>(true))
                {
                    var so=new SerializedObject(manager);so.FindProperty("spriteAtlasData").objectReferenceValue=data;so.ApplyModifiedPropertiesWithoutUndo();changed=true;
                }
                if(changed)PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();SpriteAtlasUtility.PackAtlases(atlases,EditorUserBuildSettings.activeBuildTarget);
        if(manifest.moves.Count>0)
        {
            var recorded=new Newtonsoft.Json.Linq.JArray();
            foreach(var move in manifest.moves)recorded.Add(new Newtonsoft.Json.Linq.JObject(new Newtonsoft.Json.Linq.JProperty("from",move.from),new Newtonsoft.Json.Linq.JProperty("to",move.to)));
            File.WriteAllText("Tools/UIAssetMoves.json",new Newtonsoft.Json.Linq.JObject(new Newtonsoft.Json.Linq.JProperty("moves",recorded)).ToString());
        }
        // Direct paths in editor builders and repeatable tools follow the GUID-safe moves.
        var files=Directory.GetFiles("Assets","*.cs",SearchOption.AllDirectories).Where(p=>!p.Contains("Layer Lab")&&!p.Contains("Packages"))
            .Concat(Directory.GetFiles("Tools","*.cs")).Concat(Directory.GetFiles("Tools","*.md"));
        foreach(string file in files)
        {
            if(Path.GetFileName(file)=="OrganizeUIAtlases.cs")continue;
            string text=File.ReadAllText(file),updated=text;
            foreach(var move in manifest.moves)updated=updated.Replace(move.from,move.to);
            updated=updated.Replace("Assets/Prototype/Art/Portraits/","Assets/Textures/UI/Icons/Portraits/");
            if(updated!=text)File.WriteAllText(file,updated,new System.Text.UTF8Encoding(false));
        }
        AssetDatabase.Refresh();
        return $"Moved {manifest.moves.Count} UI assets; one SpriteAtlasScriptable with Equipment, UICommon, UIIcons; fixed atlas UVs on {images} UI images.";
    }
}
