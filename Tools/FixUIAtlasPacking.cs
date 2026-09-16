using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
public static class FixUIAtlasPacking
{
    public static string Inspect()
    {
        var importer=AssetImporter.GetAtPath("Assets/SpriteAtlas/UIIcons.spriteatlasv2");
        return importer.GetType().FullName+"\n"+string.Join("\n",importer.GetType().GetMembers().Where(m=>m.Name.Contains("Packing")||m.Name.Contains("Texture")||m.Name.Contains("Platform")).Select(m=>m.ToString()));
    }
    public static string Execute()
    {
        foreach(var name in new[]{"UICommon","UIIcons"})
        {
            var importer=AssetImporter.GetAtPath("Assets/SpriteAtlas/"+name+".spriteatlasv2");
            var so=new SerializedObject(importer);
            var packing=so.FindProperty("m_PackingSettings");
            if(packing==null)throw new Exception("Missing importer packing settings");
            packing.FindPropertyRelative("enableRotation").boolValue=false;
            packing.FindPropertyRelative("enableTightPacking").boolValue=false;
            packing.FindPropertyRelative("padding").intValue=4;
            so.FindProperty("m_TextureSettings").FindPropertyRelative("maxTextureSize").intValue=name=="UICommon"?4096:2048;
            so.ApplyModifiedPropertiesWithoutUndo();importer.SaveAndReimport();
        }
        var atlases=new[]{"UICommon","UIIcons"}.Select(n=>AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/SpriteAtlas/"+n+".spriteatlasv2")).ToArray();
        SpriteAtlasUtility.PackAtlases(atlases,EditorUserBuildSettings.activeBuildTarget);
        return "UI atlas rotation/tight packing disabled; full rect packing restored.";
    }
}
