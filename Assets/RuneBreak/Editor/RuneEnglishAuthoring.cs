using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace RuneBreak.Editor
{
    // Imports the official static TTFs. This bakes TMP atlases, never draws or designs glyphs.
    public static class RuneEnglishAuthoring
    {
        public const string Root="Assets/RuneBreak/Fonts/Fredoka/";
        public static TMP_FontAsset Medium=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"Fredoka-Medium SDF.asset");
        public static TMP_FontAsset SemiBold=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"Fredoka-SemiBold SDF.asset");
        public static TMP_FontAsset Bold=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"Fredoka-Bold SDF.asset");
        public static void EnsureFonts()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string weight in new[]{"Medium","SemiBold","Bold"})
            {
                string path=Root+"Fredoka-"+weight+" SDF.asset";var asset=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if(asset!=null&&(asset.atlasTextures.Length==0||asset.atlasTextures.Any(t=>t==null))){AssetDatabase.DeleteAsset(path);asset=null;}
                if(asset!=null&&asset.atlasPopulationMode==AtlasPopulationMode.Static)continue;
                if(asset==null)
                {
                    var source=AssetDatabase.LoadAssetAtPath<Font>(Root+"Fredoka-"+weight+".ttf");if(source==null)throw new InvalidOperationException("Missing official Fredoka font: "+weight);
                    asset=TMP_FontAsset.CreateFontAsset(source,64,8,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,false);
                    asset.name="Fredoka-"+weight+" SDF";asset.boldStyle=asset.normalStyle=0;AssetDatabase.CreateAsset(asset,path);asset.material.name=asset.name+" Material";AssetDatabase.AddObjectToAsset(asset.material,asset);
                    foreach(var atlas in asset.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,asset);
                }
                string characters=new string(Enumerable.Range(32,95).Select(i=>(char)i).ToArray())+"·×–—…";
                if(!asset.TryAddCharacters(characters,out string missing)&&missing.Length>0)throw new InvalidOperationException("Fredoka missing characters: "+missing);
                asset.atlasPopulationMode=AtlasPopulationMode.Static;
                foreach(var atlas in asset.atlasTextures){atlas.name=asset.name+" Atlas";atlas.filterMode=FilterMode.Bilinear;if(!AssetDatabase.Contains(atlas))AssetDatabase.AddObjectToAsset(atlas,asset);EditorUtility.SetDirty(atlas);}
                EditorUtility.SetDirty(asset);EditorUtility.SetDirty(asset.material);
            }
            AssetDatabase.SaveAssets();
        }
        private static readonly Dictionary<string,string> English=new Dictionary<string,string>
        {
            {"설정","Settings"},{"배경음","Music"},{"효과음","Sound Effects"},{"닫기","Close"},{"홈으로","Home"},{"계속하기","Resume"},
            {"준비 중","Coming Soon"},{"코인 0","0"},{"하트 5","5"},{"상점","Shop"},{"리더보드","Rankings"},{"홈","Home"},{"팀","Team"},{"모드","Modes"},
            {"이벤트","Events"},{"보상","Rewards"},{"미션","Missions"},{"출석","Daily"},{"우편","Mail"},{"소식","News"},{"보관함","STORAGE"},
            {"준비 중이에요.\n새로운 소식을 기다려 주세요.","Coming soon.\nStay tuned!"},{"준비 중이에요","Coming soon!"},{"결계 돌파","BREAK THROUGH"}
        };
        public static void Style(TMP_Text text)
        {text.font=Medium;text.fontSharedMaterial=Medium.material;text.fontStyle=FontStyles.Normal;text.fontWeight=FontWeight.Regular;text.enableAutoSizing=false;text.extraPadding=false;text.margin=Vector4.zero;text.raycastTarget=false;}
        public static void StyleTree(GameObject root)
        {
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if(English.TryGetValue(text.text,out string english))text.text=english;
                bool title=text.name.Contains("Title")||text.name.Contains("Wordmark")||text.name=="StageNumber"||text.name=="PunchGateCount"||text.text.StartsWith("Lv.");
                bool body=text.name=="Description"||text.name=="MechanicHint"||text.name=="Placeholder";
                var font=title?Bold:body?Medium:SemiBold;if(font==null)throw new InvalidOperationException("Run EnsureFonts before styling.");
                text.font=font;text.fontSharedMaterial=font.material;text.fontStyle=FontStyles.Normal;text.fontWeight=FontWeight.Regular;
                // Existing narrow settings buttons and tab labels retain their authored rectangles.
                if(text.text=="Settings"&&text.fontSize<26)text.fontSize=17;
                if(text.name=="MechanicHint"){text.fontSize=14;text.textWrappingMode=TextWrappingModes.Normal;}
                if(text.name=="Currency")text.text="0";if(text.name=="Energy")text.text="5";
                if(text.name.StartsWith("PunchGateCount"))RuneGateNumberAuthoring.Style(text);
                if(text.name=="ResultTitle"||text.name=="RewardAmount"||(text.name=="Label"&&text.transform.parent.name.StartsWith("Caption_")))RuneRewardAuthoring.StyleHeadline(text);
            }
        }
        [MenuItem("Rune Break/Apply English and Fredoka")]
        public static void Apply()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play mode before authoring.");EnsureFonts();
            var catalog=AssetDatabase.LoadAssetAtPath<RuneLevelCatalog>("Assets/RuneBreak/Data/RuneLevels.asset");catalog.levels=JsonUtility.FromJson<RuneLevelJson>(File.ReadAllText("Assets/RuneBreak/Data/levels.json")).levels;EditorUtility.SetDirty(catalog);
            foreach(string path in new[]{"Assets/Resources/Prefabs/MainPage.prefab","Assets/Resources/Prefabs/PlayPage.prefab",RuneProjectBuilder.SettingsPopupPath,RuneProjectBuilder.ResultPopupPath})
            {
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var play=root.GetComponent<RunePlayPage>();if(play!=null)play.mechanicHint.text=catalog.levels[0].desc;
                    StyleTree(root);PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            // Update authored Splash text as well; preserve all scene objects and bindings.
            var splash=EditorSceneManager.OpenScene("Assets/Scenes/Splash.unity",OpenSceneMode.Additive);
            foreach(var root in splash.GetRootGameObjects())StyleTree(root);EditorSceneManager.SaveScene(splash);EditorSceneManager.CloseScene(splash,true);
            // Existing scene overrides must not pin old translated text or font assets.
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach(var root in scene.GetRootGameObjects())foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if(!PrefabUtility.IsPartOfPrefabInstance(text))continue;
                var so=new SerializedObject(text);foreach(string property in new[]{"m_text","m_fontAsset","m_sharedMaterial","m_fontStyle","m_fontWeight"}){var p=so.FindProperty(property);if(p!=null&&p.prefabOverride)PrefabUtility.RevertPropertyOverride(p,InteractionMode.AutomatedAction);}
            }
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("ENGLISH UI: Fredoka official TTFs, English labels and level copy, independent result popup.");
        }
    }
}
