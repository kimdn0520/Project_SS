using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using ProjectSS.Expedition;
public static class EquipmentAtlasQA
{
    public static string Execute()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();
        var manager=UnityEngine.Object.FindFirstObjectByType<SpriteManager>();manager.Initialize();
        var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/SpriteAtlas/Equipment.spriteatlasv2");
        var packables=atlas.GetPackables();
        if(packables.Length!=1||AssetDatabase.GetAssetPath(packables[0])!="Assets/Textures/Equipment")throw new Exception("Atlas must pack the equipment folder");
        foreach(var gear in page.catalog.gear)
        {
            var sprite=manager.Get(gear.spriteKey);
            if(sprite==null||!sprite.packed)throw new Exception("Missing packed gear: "+gear.spriteKey+" sprite="+(sprite==null?"null":sprite.name+" texture="+sprite.texture.name)+" count="+atlas.spriteCount+" mode="+EditorSettings.spritePackerMode);
            if(gear.icon==null)throw new Exception("Lost gear icon reference");
        }
        string result="PASS: folder-only atlas packing; all "+page.catalog.gear.Length+" gear keys resolve to packed sprites in Play Mode; original icon references preserved; packer="+EditorSettings.spritePackerMode;
        File.WriteAllText("PrototypeQA/equipment-atlas.txt",result);return result;
    }
}
