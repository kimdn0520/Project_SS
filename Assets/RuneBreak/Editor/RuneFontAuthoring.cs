using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace RuneBreak.Editor
{
    public static class RuneFontAuthoring
    {
        private const string Root="Assets/RuneBreak/Fonts/Galmuri/";
        public static TMP_FontAsset Regular=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"Galmuri11UI.asset");
        public static TMP_FontAsset Bold=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"Galmuri11BoldUI.asset");
        public static void EnsureFonts()
        {
            AssetDatabase.Refresh();Make("Galmuri11UI","Galmuri11");Make("Galmuri11BoldUI","Galmuri11-Bold");
            Regular.fontWeightTable[7]=new TMP_FontWeightPair{regularTypeface=Bold,italicTypeface=Bold};
            foreach(var font in new[]{Regular,Bold})
            {
                font.boldStyle=0;font.normalStyle=0;
                // Bake every modern Hangul syllable and current UI character before shipping.
                if(font.atlasPopulationMode!=AtlasPopulationMode.Static)
                {
                    string source=string.Concat(Directory.GetFiles("Assets/RuneBreak","*.cs",SearchOption.AllDirectories).Select(File.ReadAllText))+File.ReadAllText("Assets/RuneBreak/Data/levels.json");
                    string chars=new string(Enumerable.Range(32,95).Concat(Enumerable.Range(0xAC00,11172)).Concat(source.Where(c=>!char.IsControl(c)).Select(c=>(int)c)).Distinct().Select(i=>(char)i).ToArray());
                    font.TryAddCharacters(chars,out string missing);
                    if(!string.IsNullOrEmpty(missing))Debug.Log("Galmuri skipped non-UI source symbols: "+missing);
                    font.atlasPopulationMode=AtlasPopulationMode.Static;
                }
                foreach(var atlas in font.atlasTextures){atlas.filterMode=FilterMode.Point;if(!AssetDatabase.Contains(atlas))AssetDatabase.AddObjectToAsset(atlas,font);EditorUtility.SetDirty(atlas);}
                EditorUtility.SetDirty(font);EditorUtility.SetDirty(font.material);
            }
            AssetDatabase.SaveAssets();
        }
        private static void Make(string assetName,string sourceName)
        {
            string path=Root+assetName+".asset";if(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path)!=null)return;
            var source=AssetDatabase.LoadAssetAtPath<Font>(Root+sourceName+".ttf");if(source==null)throw new Exception("Missing Galmuri source: "+sourceName);
            var font=TMP_FontAsset.CreateFontAsset(source,12,1,GlyphRenderMode.RASTER_HINTED,2048,2048,AtlasPopulationMode.Dynamic,false);
            font.name=assetName;AssetDatabase.CreateAsset(font,path);font.material.name=assetName+" Material";AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(var atlas in font.atlasTextures){atlas.name=assetName+" Atlas";atlas.filterMode=FilterMode.Point;AssetDatabase.AddObjectToAsset(atlas,font);}
        }
        public static void Style(TMP_Text text,bool bold=false)
        {
            text.font=bold?Bold:Regular;text.fontSharedMaterial=text.font.material;text.fontStyle=FontStyles.Normal;text.fontWeight=FontWeight.Regular;
            text.enableAutoSizing=false;text.extraPadding=false;text.margin=Vector4.zero;text.raycastTarget=false;
        }
    }
}
