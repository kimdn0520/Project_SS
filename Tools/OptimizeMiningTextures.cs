using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.U2D;using UnityEngine;using UnityEngine.U2D;
public static class OptimizeMiningTextures {
 public static string Execute(){
 const string root="Assets/Textures/UI/";foreach(var f in new[]{root+"NonAtlas",root+"NonAtlas/Backgrounds"})if(!AssetDatabase.IsValidFolder(f))AssetDatabase.CreateFolder(Path.GetDirectoryName(f).Replace('\\','/'),Path.GetFileName(f));
 string old=root+"Common/Backgrounds/home-background-large.png",dest=root+"NonAtlas/Backgrounds/home-background-large.png";
 if(AssetDatabase.LoadMainAssetAtPath(old)!=null){var error=AssetDatabase.MoveAsset(old,dest);if(error!="")throw new Exception(error);}
 foreach(var name in new[]{"DigAssembly","DigCapDomed","Dig_Button_Base","Dig_Button_Face","Dig_Progress_Ring"}){
 var i=(TextureImporter)AssetImporter.GetAtPath(root+"Common/Mining/"+name+".png");i.maxTextureSize=name.StartsWith("Dig_")?256:1024;i.mipmapEnabled=false;i.isReadable=false;
 foreach(var platform in new[]{"Standalone","Android","iPhone"}){var s=i.GetPlatformTextureSettings(platform);if(s.overridden){s.maxTextureSize=i.maxTextureSize;i.SetPlatformTextureSettings(s);}}
 i.SaveAndReimport();}
 var a=AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/SpriteAtlas/UICommon.spriteatlasv2");SpriteAtlasUtility.PackAtlases(new[]{a},EditorUserBuildSettings.activeBuildTarget);
 var bg=AssetDatabase.LoadAllAssetsAtPath(dest).OfType<Sprite>().FirstOrDefault();if(bg!=null&&a.CanBindTo(bg))throw new Exception("Background still packed");AssetDatabase.SaveAssets();return "Background excluded; mining imports reduced; original pixels and sprite IDs preserved.";
 }}
