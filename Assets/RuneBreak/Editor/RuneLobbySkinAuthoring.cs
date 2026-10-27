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
        public const string PunchFolder="Assets/RuneBreak/Art/BlockPunch/Lobby/";
        public static Sprite PunchSkin(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(PunchFolder+"lobby_"+name+".png");
        public static void EnsurePunchSkins()
        {
            Directory.CreateDirectory(PunchFolder);
            string[] names={"violet","blue","gold","red","green","panel","inset","base"};
            string[] colors={"6654d8","008fd6","ffc900","ed2349","46bc15","343160","1a1c33","111329"};
            for(int n=0;n<names.Length;n++)
            {
                const int size=144;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);var pixels=new Color32[size*size];var main=RuneArtLibrary.Hex(colors[n]);
                Paint(pixels,size,0,5,48,43,13,(x,y)=>new Color(.01f,.02f,.07f,.40f));
                Paint(pixels,size,1,4,46,42,12,(x,y)=>Color.Lerp(main,RuneArtLibrary.Hex("080b21"),.67f));
                Paint(pixels,size,1,1,46,43,12,(x,y)=>Color.Lerp(Color.Lerp(main,Color.white,.57f),Color.Lerp(main,Color.black,.64f),Mathf.Clamp01((x+y)/76)));
                Paint(pixels,size,2,2,44,39,11,(x,y)=>Color.Lerp(Color.Lerp(main,Color.white,.55f),Color.Lerp(main,Color.black,.2f),Mathf.Clamp01((y-2)/36)));
                Paint(pixels,size,4,4,40,35,9,(x,y)=>{
                    float t=Mathf.Clamp01((y-4)/35);var c=Color.Lerp(main,Color.black,.02f+.15f*t);float light=Mathf.Exp(-Mathf.Pow((t-.08f)/.12f,2))*.26f;return Color.Lerp(c,Color.white,light);
                });
                Paint(pixels,size,6,5,36,1.1f,.55f,(x,y)=>new Color(1,1,1,.42f));
                texture.SetPixels32(pixels);texture.Apply();string path=PunchFolder+"lobby_"+names[n]+".png";byte[] bytes=texture.EncodeToPNG();if(!File.Exists(path)||!File.ReadAllBytes(path).SequenceEqual(bytes))File.WriteAllBytes(path,bytes);Object.DestroyImmediate(texture);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string path in Directory.GetFiles(PunchFolder,"*.png",SearchOption.AllDirectories))
            {
                var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=300;ti.spriteBorder=path.Contains("Blocks")?(Path.GetFileName(path).StartsWith("button_")?new Vector4(90,0,90,0):Vector4.zero):path.Contains("Icons")?Vector4.zero:new Vector4(51,57,51,51);ti.filterMode=FilterMode.Point;ti.mipmapEnabled=false;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.alphaIsTransparency=true;ti.maxTextureSize=4096;
                var settings=new TextureImporterSettings();ti.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;ti.SetTextureSettings(settings);ti.SaveAndReimport();
            }
            var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/RuneBreak/Art/BlockPunch/BlockPunch.spriteatlas");
            var packed=atlas.GetPackables();var additions=Directory.GetFiles(PunchFolder,"*.png",SearchOption.AllDirectories).Select(p=>(Object)AssetDatabase.LoadAssetAtPath<Sprite>(p)).Where(p=>!packed.Contains(p)).ToArray();if(additions.Length>0)atlas.Add(additions);
            EditorUtility.SetDirty(atlas);AssetDatabase.SaveAssets();SpriteAtlasUtility.PackAtlases(new[]{atlas},EditorUserBuildSettings.activeBuildTarget);
        }
        private static void Paint(Color32[] pixels,int size,float x,float y,float w,float h,float radius,System.Func<float,float,Color> shade)
        {
            for(int py=0;py<size;py++)for(int px=0;px<size;px++)
            {
                float xx=(px+.5f)/3,yy=(py+.5f)/3;var q=new Vector2(Mathf.Abs(xx-x-w/2)-w/2+radius,Mathf.Abs(yy-y-h/2)-h/2+radius);
                float distance=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-radius;float coverage=Mathf.Clamp01(.5f-distance*3);if(coverage<=0)continue;
                var c=shade(xx,yy);c.a*=coverage;int i=(size-1-py)*size+px;Color old=pixels[i];float a=c.a+old.a*(1-c.a);var rgb=(new Vector3(c.r,c.g,c.b)*c.a+new Vector3(old.r,old.g,old.b)*old.a*(1-c.a))/Mathf.Max(.0001f,a);pixels[i]=new Color(rgb.x,rgb.y,rgb.z,a);
            }
        }

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
