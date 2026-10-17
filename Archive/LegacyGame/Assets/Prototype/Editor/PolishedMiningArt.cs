using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSS.Expedition.Editor
{
    public static class PolishedMiningArt
    {
        const string Root="Assets/Prototype/Art/Cartoon/";
        static string TexturePath(string file) => System.IO.File.Exists("Assets/Textures/UI/Common/Mining/"+file+".png") ? "Assets/Textures/UI/Common/Mining/"+file+".png" : System.IO.File.Exists(Root+file+".png") ? Root+file+".png" : "Assets/Prototype/Art/Polished/"+file+".png";
        static Texture2D Texture(string file)
        {
            string path=TexturePath(file);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var im=(TextureImporter)AssetImporter.GetAtPath(path);im.textureType=TextureImporterType.Sprite;if(im.spriteImportMode!=SpriteImportMode.Multiple)im.spriteImportMode=SpriteImportMode.Single;
            im.maxTextureSize=(file=="DigAssembly"||file=="DigCapDomed")?1024:2048;im.mipmapEnabled=false;im.alphaIsTransparency=true;im.isReadable=true;im.textureCompression=TextureImporterCompression.Uncompressed;im.filterMode=FilterMode.Bilinear;im.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Rect Crop(Texture2D texture,Rect cell)
        {
            var pixels=texture.GetPixels32();int minX=(int)cell.xMax,minY=(int)cell.yMax,maxX=(int)cell.x,minYMax=(int)cell.y;
            for(int y=(int)cell.y;y<(int)cell.yMax;y++)for(int x=(int)cell.x;x<(int)cell.xMax;x++)
                if(pixels[y*texture.width+x].a>200){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);minYMax=Math.Max(minYMax,y);}
            if(maxX<minX)throw new Exception("Empty sprite cell");
            return Rect.MinMaxRect(Math.Max(cell.x,minX-2),Math.Max(cell.y,minY-2),Math.Min(cell.xMax,maxX+3),Math.Min(cell.yMax,minYMax+3));
        }
        static Sprite Sprite(Texture2D texture,Rect rect,string name,Vector2? pivot=null)
        {
            var imported=AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(texture)).OfType<Sprite>().FirstOrDefault(s=>s.name==name);
            if(imported!=null)return imported;
            string path=Root+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var sprite=UnityEngine.Sprite.Create(texture,rect,pivot??new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);sprite.name=name;
            if(old!=null){EditorUtility.CopySerialized(sprite,old);UnityEngine.Object.DestroyImmediate(sprite);EditorUtility.SetDirty(old);return old;}
            AssetDatabase.CreateAsset(sprite,path);return sprite;
        }
        public static void ConfigureDomedCap(PlayPage page)
        {
            var texture=Texture("DigCapDomed");
            var sprite=Sprite(texture,Crop(texture,new Rect(0,0,texture.width,texture.height)),"DomedDigCap");
            var face=page.digButton.transform.Find("PressableFace").GetComponent<Image>();
            face.sprite=sprite;face.preserveAspect=false;
            face.rectTransform.sizeDelta=new Vector2(268,144);
            // Raise the crown while keeping the lower rim seated in the existing socket.
            face.rectTransform.anchoredPosition=new Vector2(0,15);
            var importer=(TextureImporter)AssetImporter.GetAtPath(TexturePath("DigCapDomed"));
            importer.isReadable=false;importer.SaveAndReimport();
        }
        static void Size(SpriteRenderer sr,float width){sr.transform.localScale=Vector3.one*(width/sr.sprite.bounds.size.x);}
        static Vector3 Point(float x,float y)=>new Vector3((x-360)/100,(640-y)/100,0);
        public static void ConfigureVeins(PlayPage page)
        {
            var rockTexture=Texture("VeinDamageBlocky");var rockSprites=new Sprite[6];float cw=rockTexture.width/2f,ch=rockTexture.height/3f;
            for(int i=0;i<6;i++){int row=i/2,col=i%2;var cell=new Rect(Mathf.Floor(col*cw),Mathf.Floor((2-row)*ch),Mathf.Floor(cw),Mathf.Floor(ch));rockSprites[i]=Sprite(rockTexture,Crop(rockTexture,cell),"Vein_"+i);}
            var so=new SerializedObject(page.miningView);var damage=so.FindProperty("damageSprites");damage.arraySize=5;
            for(int i=0;i<5;i++)damage.GetArrayElementAtIndex(i).objectReferenceValue=rockSprites[i];so.FindProperty("fragmentSprite").objectReferenceValue=rockSprites[5];
            const float veinWidth=3.0f;
            float height=veinWidth*rockSprites[0].bounds.size.y/rockSprites[0].bounds.size.x;float pitch=height+.01f;
            const int count=7;
            if(page.blocks.Length<count)
            {
                int previous=page.blocks.Length;Array.Resize(ref page.blocks,count);
                for(int i=previous;i<count;i++){var go=new GameObject("VeinRock_"+i);go.transform.SetParent(page.miningWorld,false);var sr=go.AddComponent<SpriteRenderer>();sr.sharedMaterial=page.blocks[0].sharedMaterial;sr.sortingOrder=10-i;page.blocks[i]=sr;}
            }
            var positions=so.FindProperty("rockPositions");var scales=so.FindProperty("rockScales");
            var rocks=so.FindProperty("rocks");rocks.arraySize=positions.arraySize=scales.arraySize=page.blocks.Length;
            for(int i=0;i<page.blocks.Length;i++){var rock=page.blocks[i];rock.sprite=rockSprites[0];Size(rock,veinWidth);rock.transform.position=Point(360,825+height*50+i*pitch*100);rocks.GetArrayElementAtIndex(i).objectReferenceValue=rock;positions.GetArrayElementAtIndex(i).vector3Value=rock.transform.localPosition;scales.GetArrayElementAtIndex(i).vector3Value=rock.transform.localScale;}
            so.FindProperty("hitOffset").vector3Value=new Vector3(.55f,height*.5f-.10f,0);
            foreach(string field in new[]{"cracks","crackHighlights"}){var arr=so.FindProperty(field);for(int i=0;i<arr.arraySize;i++)((LineRenderer)arr.GetArrayElementAtIndex(i).objectReferenceValue).enabled=false;}
            so.ApplyModifiedPropertiesWithoutUndo();
            var importer=(TextureImporter)AssetImporter.GetAtPath(TexturePath("VeinDamageBlocky"));importer.isReadable=false;importer.SaveAndReimport();
        }
        public static void Configure(PlayPage page)
        {
            ConfigureVeins(page);
            var so=new SerializedObject(page.miningView);
            var shaft=Texture("Shaft");var shaftSprite=Sprite(shaft,new Rect(0,0,shaft.width,shaft.height),"ShaftTile");
            var bands=so.FindProperty("shaftBands");var rest=so.FindProperty("bandRest");bands.arraySize=rest.arraySize=2;
            var root=page.miningWorld.Find("ShaftBands");foreach(Transform child in root.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            const float centerWidth=7.2f;
            float wallHeight=centerWidth*shaft.height/shaft.width;
            var material=page.blocks[0].sharedMaterial;material.SetVector("_WorldClipRect",new Vector4(-3.6f,-6.4f,3.6f,2.4f));EditorUtility.SetDirty(material);
            for(int i=0;i<2;i++)
            {
                var band=new GameObject("ShaftTile_"+i).transform;band.SetParent(root,false);band.position=Point(360,400+i*wallHeight*100);
                // One full-width authored image avoids mirrored duplicate stone shapes at the sides.
                for(int piece=0;piece<1;piece++)
                {
                    var go=new GameObject("Artwork");go.transform.SetParent(band,false);
                    go.transform.localPosition=new Vector3(0,-wallHeight*.5f,0);
                    var sr=go.AddComponent<SpriteRenderer>();sr.sprite=shaftSprite;sr.sharedMaterial=material;sr.sortingOrder=-15;sr.flipY=i%2==1;Size(sr,centerWidth);
                }
                bands.GetArrayElementAtIndex(i).objectReferenceValue=band;rest.GetArrayElementAtIndex(i).vector3Value=band.localPosition;
            }
            so.FindProperty("bandHeight").floatValue=wallHeight;so.FindProperty("bandCycle").floatValue=wallHeight*2;
            var buttonTexture=Texture("DigAssembly");var cap=Sprite(buttonTexture,Crop(buttonTexture,new Rect(0,buttonTexture.height/2,buttonTexture.width,buttonTexture.height/2)),"DigCap");
            var pedestal=Sprite(buttonTexture,Crop(buttonTexture,new Rect(0,0,buttonTexture.width,buttonTexture.height/2)),"DigPedestal");
            var face=page.digButton.transform.Find("PressableFace").GetComponent<Image>();face.sprite=cap;face.rectTransform.sizeDelta=new Vector2(268,268*cap.bounds.size.y/cap.bounds.size.x);face.rectTransform.anchoredPosition=new Vector2(0,4);
            var parent=page.digButton.transform.parent;var baseImage=parent.Find("DigBase").GetComponent<Image>();baseImage.sprite=pedestal;baseImage.color=Color.white;baseImage.type=Image.Type.Simple;baseImage.preserveAspect=true;
            var baseRect=baseImage.rectTransform;baseRect.anchorMin=baseRect.anchorMax=new Vector2(0,1);baseRect.pivot=new Vector2(.5f,.5f);baseRect.anchoredPosition=new Vector2(360,-1092);baseRect.sizeDelta=new Vector2(324,324*pedestal.bounds.size.y/pedestal.bounds.size.x);
            parent.Find("DigShadow").gameObject.SetActive(false);page.digLabel.color=new Color(.20f,.11f,.05f);page.digLabel.fontSize=42;page.digLabel.fontStyle=TMPro.FontStyles.Bold;page.digLabel.rectTransform.anchoredPosition=new Vector2(0,7);
            var chestTexture=Texture("Chest");var fullRect=Crop(chestTexture,new Rect(0,0,chestTexture.width,chestTexture.height));
            var closed=Sprite(chestTexture,fullRect,"ChestClosed");float seam=chestTexture.height*.494f;
            var body=Sprite(chestTexture,new Rect(fullRect.x,fullRect.y,fullRect.width,seam-fullRect.y),"ChestBody");
            var lid=Sprite(chestTexture,new Rect(fullRect.x,seam,fullRect.width,fullRect.yMax-seam),"ChestLid",new Vector2(.5f,0));
            var chest=(SpriteRenderer)so.FindProperty("chestVisual").objectReferenceValue;chest.sprite=closed;Size(chest,1.40f);chest.transform.position=Point(430,825-70*closed.bounds.size.y/closed.bounds.size.x);
            var opening=(GameObject)so.FindProperty("chestOpenRoot").objectReferenceValue;opening.transform.position=chest.transform.position;
            var bodyRenderer=opening.transform.Find("ChestBody").GetComponent<SpriteRenderer>();bodyRenderer.sprite=body;Size(bodyRenderer,1.4f);
            float scale=1.4f/closed.bounds.size.x;float totalHeight=closed.bounds.size.y*scale;
            bodyRenderer.transform.localPosition=new Vector3(0,-totalHeight*.5f+body.bounds.size.y*scale*.5f);
            var lidTransform=(Transform)so.FindProperty("chestLid").objectReferenceValue;var lidRenderer=lidTransform.GetComponent<SpriteRenderer>();lidRenderer.sprite=lid;Size(lidRenderer,1.4f);lidTransform.localRotation=Quaternion.identity;
            lidTransform.localPosition=new Vector3(0,-totalHeight*.5f+body.bounds.size.y*scale);
            so.FindProperty("lidRest").vector3Value=lidTransform.localPosition;
            var glow=(SpriteRenderer)so.FindProperty("chestGlow").objectReferenceValue;glow.transform.localPosition=lidTransform.localPosition+Vector3.down*.06f;glow.transform.localScale=new Vector3(.9f/glow.sprite.bounds.size.x,.3f/glow.sprite.bounds.size.y,1);
            var insideTexture=Texture("ChestLidInside");var insideSprite=Sprite(insideTexture,Crop(insideTexture,new Rect(0,0,insideTexture.width,insideTexture.height)),"ChestInsideFace",new Vector2(.5f,0));
            var inside=lidTransform.Find("InsideFace");if(inside==null){inside=new GameObject("InsideFace").transform;inside.SetParent(lidTransform,false);inside.gameObject.AddComponent<SpriteRenderer>();}
            var insideRenderer=inside.GetComponent<SpriteRenderer>();insideRenderer.sprite=insideSprite;insideRenderer.sharedMaterial=chest.sharedMaterial;insideRenderer.sortingOrder=lidRenderer.sortingOrder;
            inside.localScale=Vector3.one*(lid.bounds.size.x/insideSprite.bounds.size.x);insideRenderer.enabled=false;
            var interior=opening.transform.Find("Interior");if(interior==null){interior=new GameObject("Interior").transform;interior.SetParent(opening.transform,false);interior.gameObject.AddComponent<SpriteRenderer>();}
            var interiorRenderer=interior.GetComponent<SpriteRenderer>();interiorRenderer.sprite=ExpeditionUIArt.Circle();interiorRenderer.sharedMaterial=chest.sharedMaterial;interiorRenderer.sortingOrder=28;interiorRenderer.color=new Color(.045f,.065f,.08f);
            interior.localPosition=lidTransform.localPosition;interior.localScale=new Vector3(1.25f/interiorRenderer.sprite.bounds.size.x,.30f/interiorRenderer.sprite.bounds.size.y,1);interiorRenderer.enabled=false;
            so.FindProperty("lidFace").objectReferenceValue=lidRenderer;so.FindProperty("lidInside").objectReferenceValue=insideRenderer;so.FindProperty("chestInterior").objectReferenceValue=interiorRenderer;
            so.ApplyModifiedPropertiesWithoutUndo();
            MiningConsoleArt.Configure(page);
            foreach(string file in new[]{"VeinDamage","Shaft","DigAssembly","Chest","ChestLidInside"}){var importer=(TextureImporter)AssetImporter.GetAtPath(TexturePath(file));importer.isReadable=false;importer.SaveAndReimport();}
            AssetDatabase.SaveAssets();
        }
    }
}
