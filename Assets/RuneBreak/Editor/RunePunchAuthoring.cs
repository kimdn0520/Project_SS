using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace RuneBreak.Editor
{
    public static class RunePunchAuthoring
    {
        private const string Root="Assets/RuneBreak/";
        public const string ArtPath=Root+"Data/RunePunchArt.asset";
        private static readonly string[] Colors={"red","blue","gold","green"};
        private static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/BlockPunch/punch_"+name+".png");
        [MenuItem("Rune Break/Apply Block Punch to PlayPage only")]
        public static void Apply()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play mode before authoring.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var path in Directory.GetFiles(Root+"Art/BlockPunch","*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=3;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;importer.maxTextureSize=4096;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            }
            var art=AssetDatabase.LoadAssetAtPath<RuneArtLibrary>(ArtPath);
            if(art==null){art=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<RuneArtLibrary>(Root+"Data/RuneArt.asset"));AssetDatabase.CreateAsset(art,ArtPath);}
            art.punchStyle=true;art.cabinets=new[]{Sprite("cabinet"),Sprite("cabinet"),Sprite("cabinet")};
            art.runes=(from c in Colors from d in new[]{"N","E","S","W","gem"} select Sprite("rune_"+c+"_"+d)).ToArray();
            art.storedRunes=Colors.Select(c=>Sprite("stored_"+c)).ToArray();art.longGates=(from c in Colors from d in new[]{"N","E"} select Sprite("gate_"+c+"_"+d)).ToArray();
            art.waitSlots=Colors.Select(c=>Sprite("wait_"+c)).ToArray();art.fullWaitSlots=Colors.Select(c=>Sprite("wait_"+c+"_full")).ToArray();
            art.slot=Sprite("slot");art.fullSlot=Sprite("slot_full");art.flightShadow=Sprite("shadow");EditorUtility.SetDirty(art);
            const string atlasPath=Root+"Art/BlockPunch/BlockPunch.spriteatlas";
            var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);if(atlas==null){atlas=new SpriteAtlas();AssetDatabase.CreateAsset(atlas,atlasPath);}
            var packed=atlas.GetPackables();if(packed.Length>0)atlas.Remove(packed);
            atlas.Add(Directory.GetFiles(Root+"Art/BlockPunch","*.png",SearchOption.AllDirectories).Select(p=>(UnityEngine.Object)AssetDatabase.LoadAssetAtPath<Sprite>(p)).ToArray());
            atlas.SetPackingSettings(new SpriteAtlasPackingSettings{padding=4,enableRotation=false,enableTightPacking=false});
            atlas.SetTextureSettings(new SpriteAtlasTextureSettings{filterMode=FilterMode.Point,generateMipMaps=false,sRGB=true});
            var platform=atlas.GetPlatformSettings("DefaultTexturePlatform");platform.maxTextureSize=4096;platform.textureCompression=TextureImporterCompression.Uncompressed;atlas.SetPlatformSettings(platform);EditorUtility.SetDirty(atlas);
            var registry=AssetDatabase.LoadAssetAtPath<SpriteAtlasSO>(Root+"Data/RuneAtlasRegistry.asset");var so=new SerializedObject(registry);var array=so.FindProperty("atlases");
            if(!registry.Atlases.Contains(atlas)){int n=array.arraySize;array.arraySize++;array.GetArrayElementAtIndex(n).objectReferenceValue=atlas;so.ApplyModifiedPropertiesWithoutUndo();}
            var root=PrefabUtility.LoadPrefabContents("Assets/Resources/Prefabs/PlayPage.prefab");
            try
            {
                var p=root.GetComponent<RunePlayPage>();Configure(p,art);
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Prefabs/PlayPage.prefab");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();SpriteAtlasUtility.PackAtlases(new[]{atlas},EditorUserBuildSettings.activeBuildTarget);
            Debug.Log("BLOCK PUNCH authored: PlayPage; legacy pixel library retained; 48 baked rebound flights.");
        }
        // Also called by the normal rebuild path, so a page rebuild preserves this skin.
        public static void ConfigureIfAvailable(RunePlayPage p)
        {
            var art=AssetDatabase.LoadAssetAtPath<RuneArtLibrary>(ArtPath);if(art!=null)Configure(p,art);
        }
        private static void Configure(RunePlayPage p,RuneArtLibrary art)
        {
            p.art=art;p.background.sprite=art.cabinets[0];p.effects.art=art;
            p.transform.Find("FullScreenBackdrop").GetComponent<Image>().color=RuneArtLibrary.Hex("15162b");
            foreach(string name in new[]{"NorthCompass","SouthCompass","RecastFooter","CountBacking"}){var t=p.cabinet.Find(name);if(t!=null)t.gameObject.SetActive(false);}
            var title=p.cabinet.Find("RecastTitle");if(title!=null)title.GetComponent<TMP_Text>().text="STORAGE";
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Arial Rounded Bold SDF.asset");
            p.gateCounts=new TMP_Text[2];int previews=p.catalog.levels.Max(l=>Math.Max(l.seals.N.Length,l.seals.E.Length));p.gatePreviews=new Image[previews*2];
            for(int side=0;side<2;side++)
            {
                for(int j=previews-1;j>=0;j--)
                {
                    var img=Image("PunchLayer_"+side+"_"+j,p.cabinet);float d=(j+1)*3.5f;
                    SetRect(img.rectTransform,side==0?102:423+d,side==0?47-d:90,side==0?276:3.5f,side==0?3.5f:276);
                    img.transform.SetSiblingIndex(p.gates[side].transform.GetSiblingIndex());p.gatePreviews[side*previews+j]=img;
                }
                SetRect(p.gates[side].rectTransform,side==0?88:390,side==0?38:76,side==0?304:42,side==0?42:304);
                string name="PunchGateCount_"+side;var t=p.cabinet.Find(name);var go=t!=null?t.gameObject:new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(p.cabinet,false);
                var label=go.GetComponent<TextMeshProUGUI>();label.font=font;label.fontSize=22;label.color=RuneArtLibrary.Hex("fff7df");label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;label.enableAutoSizing=false;label.text="2";
                SetRect(label.rectTransform,side==0?218:394,side==0?43:212,side==0?44:34,32);p.gateCounts[side]=label;
            }
            foreach(var f in p.pool.GetComponentsInChildren<RuneFlight>(true))
            {
                f.shadow=Image("PunchFlightShadow",f.transform);f.shadow.sprite=art.flightShadow;Center(f.shadow.rectTransform);f.shadow.transform.SetAsFirstSibling();f.shadow.enabled=false;
                var rt=f.transform.Find("PunchRibbon");var ribbon=rt!=null?rt.gameObject:new GameObject("PunchRibbon",typeof(RectTransform),typeof(CanvasRenderer),typeof(RuneRibbonGraphic));ribbon.transform.SetParent(f.transform,false);
                f.ribbon=ribbon.GetComponent<RuneRibbonGraphic>();f.ribbon.raycastTarget=false;SetRect(f.ribbon.rectTransform,0,0,480,584);f.ribbon.transform.SetSiblingIndex(1);
                f.afterImages=new Image[2];for(int i=0;i<2;i++){var ghost=Image("PunchAfterImage_"+i,f.transform);Center(ghost.rectTransform);ghost.enabled=false;ghost.transform.SetSiblingIndex(f.body.GetSiblingIndex());f.afterImages[i]=ghost;}
            }
            foreach(var s in p.slots){s.gem.sprite=art.storedRunes[0];s.panel.sprite=art.slot;}
            if(p.sounds.Length<14)Array.Resize(ref p.sounds,14);
            p.sounds[11]=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/rebound.wav");p.sounds[12]=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/swoosh.wav");p.sounds[13]=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/catch.wav");

            p.LoadLevel(0);
            RuneSurfaceAuthoring.ConfigureIfAvailable(p);
        }
        private static Image Image(string name,Transform parent)
        {
            var t=parent.Find(name);var go=t!=null?t.gameObject:new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.raycastTarget=false;return image;
        }
        private static void SetRect(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        private static void Center(RectTransform r){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(48,48);}
    }
}
