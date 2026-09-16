using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using ProjectSS.Expedition;
using ProjectSS.Expedition.Editor;
public static class UIAtlasQA
{
    public static string Execute()
    {
        var registryGuids=AssetDatabase.FindAssets("t:SpriteAtlasSO");
        if(registryGuids.Length!=1)throw new Exception("Expected exactly one sprite registry");
        var data=AssetDatabase.LoadAssetAtPath<SpriteAtlasSO>(EquipmentAtlasArt.RegistryPath);
        if(data==null||data.Atlases.Length!=3||data.Atlases.Distinct().Count()!=3)throw new Exception("Expected shared registry and three distinct atlases");
        foreach(var atlas in data.Atlases)
        {
            var importer=new SerializedObject(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(atlas)));
            var packing=importer.FindProperty("m_PackingSettings");
            if(packing.FindPropertyRelative("enableRotation").boolValue||packing.FindPropertyRelative("enableTightPacking").boolValue)throw new Exception("Unsafe UI packing settings: "+atlas.name);
        }
        var visited=new HashSet<Sprite>();int images=0,maskImages=0;
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Resources/Prefabs"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var manager in root.GetComponentsInChildren<SpriteManager>(true))
                if(new SerializedObject(manager).FindProperty("spriteAtlasData").objectReferenceValue!=data)throw new Exception("Wrong registry: "+path);
            foreach(var image in root.GetComponentsInChildren<Image>(true))
            {
                if(image.sprite==null)continue;
                string source=AssetDatabase.GetAssetPath(image.sprite);
                if(!source.StartsWith("Assets/"))continue;
                if(!source.StartsWith("Assets/Textures/"))throw new Exception("UI sprite outside Textures: "+source);
                bool nonAtlas=source.StartsWith("Assets/Textures/UI/NonAtlas/");
                bool packed=data.Atlases.Any(a=>a.CanBindTo(image.sprite));
                if(nonAtlas==packed)throw new Exception("Wrong atlas inclusion: "+source);
                visited.Add(image.sprite);images++;
                if(image.material!=null&&image.material.shader.name=="ProjectSS/UI/WindowFrame")
                {if(image.GetComponent<AtlasLocalUV>()==null)throw new Exception("Atlas mask UV missing: "+path+" / "+image.name);maskImages++;}
            }
        }
        var atlasInfo=string.Join("; ",data.Atlases.Select(a=>a.name+"="+a.spriteCount+" sprites"));
        var result=$"PASS: one SpriteAtlasScriptable; 3 atlases ({atlasInfo}); {images} UI image references / {visited.Count} unique sprites under Textures with explicit NonAtlas exclusions; {maskImages} custom masks use local atlas UVs.";
        File.WriteAllText("PrototypeQA/ui-atlases.txt",result);return result;
    }
}
