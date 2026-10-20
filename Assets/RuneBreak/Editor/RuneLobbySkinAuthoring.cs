using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace RuneBreak.Editor
{
    // Editable pixel recipes for UI geometry. Original rune art and supplied references stay intact.
    public static class RuneLobbySkinAuthoring
    {
        public const string Folder="Assets/RuneBreak/Art/Lobby/";
        public static Sprite Skin(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Folder+name+".png");

        public static void EnsureSkins()
        {
            Directory.CreateDirectory(Folder);
            Make("teal","344c4d","76877f","516662","253c40","182e36");
            Make("blue","355875","669fb4","516662","223c52","101f29");
            Make("gold","e2b860","fff1af","d3c398","96723d","513d29");
            Make("green","83ae83","d4e7ac","a3afa0","436b57","293f36");
            Make("panel","253c40","76877f","516662","1c3138","101f29");
            Make("inset","17282f","3a5553","294044","344c4d","516662");
            Make("base","101f29","253c40","182e36","101d27","101d27");
            AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles(Folder,"*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;
                importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=50;
                importer.spriteBorder=new Vector4(7,7,7,7);
                importer.filterMode=FilterMode.Point;
                importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency=true;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/RuneBreak/Art/RuneBreak.spriteatlas");
            var existing=SpriteAtlasExtensions.GetPackables(atlas);
            var sprites=Directory.GetFiles(Folder,"*.png").Select(p=>(Object)AssetDatabase.LoadAssetAtPath<Sprite>(p)).Where(p=>!existing.Contains(p)).ToArray();
            if(sprites.Length>0)SpriteAtlasExtensions.Add(atlas,sprites);
            EditorUtility.SetDirty(atlas);AssetDatabase.SaveAssets();
            SpriteAtlasUtility.PackAtlases(new[]{atlas},EditorUserBuildSettings.activeBuildTarget);
        }

        private static void Make(string name,string fill,string light,string side,string shade,string foot)
        {
            var texture=new Texture2D(32,32,TextureFormat.RGBA32,false);
            var pixels=new Color32[32*32];
            Cut(pixels,0,0,32,32,4,"101d27");
            Cut(pixels,1,1,30,30,3,foot);
            Cut(pixels,1,1,30,28,3,shade);
            Cut(pixels,2,2,28,25,2,side);
            Cut(pixels,3,2,26,24,2,light);
            Cut(pixels,4,4,24,23,1,shade);
            Cut(pixels,4,4,23,21,1,fill);
            texture.SetPixels32(pixels);texture.Apply();
            string path=Folder+name+".png";byte[] bytes=texture.EncodeToPNG();
            if(!File.Exists(path)||!File.ReadAllBytes(path).SequenceEqual(bytes))File.WriteAllBytes(path,bytes);
            Object.DestroyImmediate(texture);
        }
        private static void Cut(Color32[] pixels,int x,int y,int width,int height,int cut,string hex)
        {
            Color32 color=RuneArtLibrary.Hex(hex);
            for(int row=0;row<height;row++)
            {
                int inset=Mathf.Max(0,cut-Mathf.Min(row,height-1-row));
                for(int col=inset;col<width-inset;col++)pixels[(31-y-row)*32+x+col]=color;
            }
        }
    }
}
