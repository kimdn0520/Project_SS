using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSS.Expedition.Editor
{
    public static class PolishedMiningArt
    {
        const string Root="Assets/Prototype/Art/Polished/";
        static Texture2D Texture(string file)
        {
            string path=Root+file+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var im=(TextureImporter)AssetImporter.GetAtPath(path);im.textureType=TextureImporterType.Sprite;im.spriteImportMode=SpriteImportMode.Single;
            im.maxTextureSize=2048;im.mipmapEnabled=false;im.alphaIsTransparency=true;im.isReadable=true;im.textureCompression=TextureImporterCompression.Uncompressed;im.filterMode=FilterMode.Bilinear;im.SaveAndReimport();
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
            string path=Root+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(old!=null)return old;
            var sprite=UnityEngine.Sprite.Create(texture,rect,pivot??new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);sprite.name=name;AssetDatabase.CreateAsset(sprite,path);return sprite;
        }
        static void Size(SpriteRenderer sr,float width){sr.transform.localScale=Vector3.one*(width/sr.sprite.bounds.size.x);}
        static Vector3 Point(float x,float y)=>new Vector3((x-360)/100,(640-y)/100,0);
        public static void Configure(PlayPage page)
        {
            var rockTexture=Texture("VeinDamage");var rockSprites=new Sprite[6];float cw=rockTexture.width/2f,ch=rockTexture.height/3f;
            for(int i=0;i<6;i++){int row=i/2,col=i%2;var cell=new Rect(Mathf.Floor(col*cw),Mathf.Floor((2-row)*ch),Mathf.Floor(cw),Mathf.Floor(ch));rockSprites[i]=Sprite(rockTexture,Crop(rockTexture,cell),"Vein_"+i);}
            var so=new SerializedObject(page.miningView);var damage=so.FindProperty("damageSprites");damage.arraySize=5;
            for(int i=0;i<5;i++)damage.GetArrayElementAtIndex(i).objectReferenceValue=rockSprites[i];so.FindProperty("fragmentSprite").objectReferenceValue=rockSprites[5];
            const float veinWidth=3.0f;
            float height=veinWidth*rockSprites[0].bounds.size.y/rockSprites[0].bounds.size.x;float pitch=height+.04f;
            var positions=so.FindProperty("rockPositions");var scales=so.FindProperty("rockScales");
            for(int i=0;i<3;i++){var rock=page.blocks[i];rock.sprite=rockSprites[0];Size(rock,veinWidth);rock.transform.position=Point(360,825+height*50+i*pitch*100);positions.GetArrayElementAtIndex(i).vector3Value=rock.transform.localPosition;scales.GetArrayElementAtIndex(i).vector3Value=rock.transform.localScale;}
            so.FindProperty("hitOffset").vector3Value=new Vector3(.55f,height*.5f-.10f,0);
            foreach(string field in new[]{"cracks","crackHighlights"}){var arr=so.FindProperty(field);for(int i=0;i<arr.arraySize;i++)((LineRenderer)arr.GetArrayElementAtIndex(i).objectReferenceValue).enabled=false;}
            var shaft=Texture("Shaft");var shaftSprite=Sprite(shaft,new Rect(0,0,shaft.width,shaft.height),"ShaftTile");
            var bands=so.FindProperty("shaftBands");var rest=so.FindProperty("bandRest");bands.arraySize=rest.arraySize=2;
            var root=page.miningWorld.Find("ShaftBands");foreach(Transform child in root.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            const float centerWidth=5.4f, extensionWidth=.9f;
            float wallHeight=centerWidth*shaft.height/shaft.width;
            float edgePixels=shaft.width*extensionWidth/centerWidth;
            var leftEdge=Sprite(shaft,new Rect(0,0,edgePixels,shaft.height),"ShaftLeftExtension");
            var rightEdge=Sprite(shaft,new Rect(shaft.width-edgePixels,0,edgePixels,shaft.height),"ShaftRightExtension");
            var material=page.blocks[0].sharedMaterial;material.SetVector("_WorldClipRect",new Vector4(-3.6f,-6.4f,3.6f,2.4f));EditorUtility.SetDirty(material);
            for(int i=0;i<2;i++)
            {
                var band=new GameObject("ShaftTile_"+i).transform;band.SetParent(root,false);band.position=Point(360,400+i*wallHeight*100);
                for(int piece=0;piece<3;piece++)
                {
                    var go=new GameObject(piece==0?"Artwork":piece==1?"LeftExtension":"RightExtension");go.transform.SetParent(band,false);
                    go.transform.localPosition=new Vector3(piece==0?0:piece==1?-3.15f:3.15f,-wallHeight*.5f,0);
                    var sr=go.AddComponent<SpriteRenderer>();sr.sprite=piece==0?shaftSprite:piece==1?leftEdge:rightEdge;sr.sharedMaterial=material;sr.sortingOrder=-15;sr.flipY=i%2==1;sr.flipX=piece!=0;Size(sr,piece==0?centerWidth:extensionWidth);
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
            var closed=Sprite(chestTexture,fullRect,"ChestClosed");float seam=chestTexture.height*.485f;
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
            so.ApplyModifiedPropertiesWithoutUndo();
            foreach(string file in new[]{"VeinDamage","Shaft","DigAssembly","Chest"}){var importer=(TextureImporter)AssetImporter.GetAtPath(Root+file+".png");importer.isReadable=false;importer.SaveAndReimport();}
            AssetDatabase.SaveAssets();
        }
    }
}
