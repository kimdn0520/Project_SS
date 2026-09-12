using UnityEngine;
using UnityEditor;
using System.Linq;

public static class TextureColorChecker
{
    public static void Execute()
    {
        Check();
    }

    public static void Check()
    {
        string path = "Assets/Layer Lab/2D Maps - Simple Sidescroll/Extenstions/Simple Sidescroll1/Sprite/map1.png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Total sprites in map1.png: {sprites.Length}");
        Sprite sprMtn = sprites.FirstOrDefault(s => s.name == "map01_12");
        if (sprMtn != null)
        {
            Rect r = sprMtn.rect;
            Color cTL = tex.GetPixel((int)r.xMin + 5, (int)r.yMax - 5);
            Color cTR = tex.GetPixel((int)r.xMax - 5, (int)r.yMax - 5);
            Color cBL = tex.GetPixel((int)r.xMin + 5, (int)r.yMin + 5);
            Color cBR = tex.GetPixel((int)r.xMax - 5, (int)r.yMin + 5);
            sb.AppendLine($"map01_12 corners: TL={cTL}, TR={cTR}, BL={cBL}, BR={cBR}");
        }
        System.IO.File.WriteAllText("Assets/Editor/map1_sprites_colors.txt", sb.ToString());
        Debug.Log("map01_12 corner alpha checked");
    }
}
