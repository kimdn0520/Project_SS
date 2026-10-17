using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ProjectSS.Expedition.Editor
{
 public static class GoldChestArt
 {
  public static void Configure(PlayPage page)
  {
   const string path="Assets/Prototype/Art/GoldBlueChest-v3.png";
   var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;
   imp.spriteImportMode=SpriteImportMode.Multiple;imp.spritePixelsPerUnit=100;imp.maxTextureSize=2048;
   imp.textureCompression=TextureImporterCompression.Uncompressed;imp.mipmapEnabled=false;imp.filterMode=FilterMode.Bilinear;
   imp.spritesheet=new[]{
    new SpriteMetaData{name="ClosedLid",rect=new Rect(16,173,710,306),alignment=9,pivot=new Vector2(.5f,0)},
    new SpriteMetaData{name="OpenLid",rect=new Rect(724,168,724,393),alignment=9,pivot=new Vector2(.5f,0)},
    new SpriteMetaData{name="Body",rect=new Rect(1448,146,705,430),alignment=0,pivot=new Vector2(.5f,.5f)}};
   imp.SaveAndReimport();
   var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
   var body=sprites.First(s=>s.name=="Body");var closed=sprites.First(s=>s.name=="ClosedLid");var open=sprites.First(s=>s.name=="OpenLid");
   const string matPath="Assets/Prototype/Art/GoldBlueChest.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
   if(mat==null){mat=new Material(Shader.Find("ProjectSS/Sprites/ChestMatte"));AssetDatabase.CreateAsset(mat,matPath);}
   var so=new SerializedObject(page.miningView);var chest=(SpriteRenderer)so.FindProperty("chestVisual").objectReferenceValue;
   bool first=chest.sprite!=body;float bottom=chest.transform.position.y-chest.sprite.bounds.size.y*chest.transform.lossyScale.y*.5f;
   float scale=1.4f/body.bounds.size.x;
   chest.sprite=body;chest.sharedMaterial=mat;chest.color=Color.white;chest.transform.localScale=Vector3.one*scale;
   if(first)chest.transform.position=new Vector3(chest.transform.position.x,bottom+body.bounds.size.y*scale*.5f,chest.transform.position.z);
   var child=chest.transform.Find("ClosedLid");
   if(child==null){child=new GameObject("ClosedLid",typeof(SpriteRenderer)).transform;child.SetParent(chest.transform,false);}
   var cr=child.GetComponent<SpriteRenderer>();cr.sprite=closed;cr.sharedMaterial=mat;cr.sortingOrder=chest.sortingOrder+1;
   child.localScale=Vector3.one*(body.bounds.size.x/closed.bounds.size.x);child.localPosition=new Vector3(0,body.bounds.size.y*.5f-1.0f,0);
   var root=(GameObject)so.FindProperty("chestOpenRoot").objectReferenceValue;root.transform.position=chest.transform.position;
   var br=root.transform.Find("ChestBody").GetComponent<SpriteRenderer>();br.sprite=body;br.sharedMaterial=mat;br.color=Color.white;br.transform.localScale=Vector3.one*scale;br.transform.localPosition=Vector3.zero;
   var lid=(Transform)so.FindProperty("chestLid").objectReferenceValue;var face=lid.GetComponent<SpriteRenderer>();face.sprite=closed;face.sharedMaterial=mat;
   lid.localScale=Vector3.one*(1.4f/closed.bounds.size.x);lid.localPosition=new Vector3(0,child.localPosition.y*scale,0);lid.localRotation=Quaternion.identity;
   var inside=lid.Find("InsideFace").GetComponent<SpriteRenderer>();inside.sprite=open;inside.sharedMaterial=mat;inside.transform.localPosition=Vector3.zero;inside.transform.localScale=Vector3.one*(closed.bounds.size.x/open.bounds.size.x);
   so.FindProperty("lidRest").vector3Value=lid.localPosition;
   var interior=(SpriteRenderer)so.FindProperty("chestInterior").objectReferenceValue;interior.color=Color.clear;
   var glow=(SpriteRenderer)so.FindProperty("chestGlow").objectReferenceValue;glow.transform.localPosition=lid.localPosition+Vector3.up*.03f;glow.sortingOrder=br.sortingOrder+2;
   const string shinePath="Assets/ETC/shine07.png";
   var shineImport=(TextureImporter)AssetImporter.GetAtPath(shinePath);shineImport.textureType=TextureImporterType.Sprite;shineImport.spriteImportMode=SpriteImportMode.Single;shineImport.alphaIsTransparency=true;shineImport.mipmapEnabled=false;shineImport.SaveAndReimport();
   glow.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(shinePath);glow.transform.localScale=new Vector3(1.1f/glow.sprite.bounds.size.x,.65f/glow.sprite.bounds.size.y,1);
   const string sparkPath="Assets/ETC/spark03.png";
   var sparkImport=(TextureImporter)AssetImporter.GetAtPath(sparkPath);sparkImport.textureType=TextureImporterType.Sprite;sparkImport.spriteImportMode=SpriteImportMode.Single;sparkImport.alphaIsTransparency=true;sparkImport.mipmapEnabled=false;sparkImport.SaveAndReimport();
   var sparkSprite=AssetDatabase.LoadAssetAtPath<Sprite>(sparkPath);
   var sparkles=so.FindProperty("chestSparkles");sparkles.arraySize=6;
   for(int i=0;i<6;i++)
   {
    var t=root.transform.Find("RewardSpark"+i);
    if(t==null){t=new GameObject("RewardSpark"+i,typeof(SpriteRenderer)).transform;t.SetParent(root.transform,false);}
    var sr=t.GetComponent<SpriteRenderer>();sr.sprite=sparkSprite;sr.sharedMaterial=glow.sharedMaterial;sr.sortingOrder=glow.sortingOrder+1;sr.color=Color.clear;
    t.localPosition=glow.transform.localPosition;t.localScale=Vector3.one*((.10f+(i%2)*.04f)/sparkSprite.bounds.size.x);
    sparkles.GetArrayElementAtIndex(i).objectReferenceValue=sr;
   }
   so.ApplyModifiedPropertiesWithoutUndo();
  }
 }
}
