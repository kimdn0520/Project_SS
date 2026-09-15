using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D;
namespace ProjectSS.Expedition.Editor
{
    public static class EquipmentAtlasArt
    {
        public const string Folder="Assets/Textures/Equipment";
        public const string AtlasFolder="Assets/SpriteAtlas";
        static void DirectoryAsset(string path){if(AssetDatabase.IsValidFolder(path))return;DirectoryAsset(Path.GetDirectoryName(path).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));}
        public static void Configure(SpriteManager manager,IEnumerable<Sprite> sprites)
        {
            DirectoryAsset(Folder);DirectoryAsset(AtlasFolder);
            foreach(string source in sprites.Where(s=>s!=null).Select(s=>AssetDatabase.GetAssetPath(s).Replace('\\','/')).Distinct())
            {
                string target=Folder+"/"+Path.GetFileName(source);if(source==target)continue;
                if(AssetDatabase.LoadMainAssetAtPath(target)!=null)throw new InvalidOperationException("Asset collision: "+source+" -> "+target);
                string error=AssetDatabase.MoveAsset(source,target);if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
            }
            BindFolder(manager,Folder,"Equipment");
        }
        public static void BindFolder(SpriteManager manager,string folder,string name)
        {
            DirectoryAsset(AtlasFolder);
            EditorSettings.spritePackerMode=SpritePackerMode.SpriteAtlasV2;
            string path=AtlasFolder+"/"+name+".spriteatlasv2";
            var source=new SpriteAtlasAsset();source.Add(new[]{AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder)});
            var packing=source.GetPackingSettings();packing.enableRotation=false;packing.enableTightPacking=false;packing.padding=4;source.SetPackingSettings(packing);
            var texture=source.GetTextureSettings();texture.generateMipMaps=false;texture.filterMode=FilterMode.Bilinear;source.SetTextureSettings(texture);
            SpriteAtlasAsset.Save(source,path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            string dataPath=AtlasFolder+"/"+name+"AtlasData.asset";
            if(name=="Equipment"&&AssetDatabase.LoadAssetAtPath<SpriteAtlasSO>(dataPath)==null&&AssetDatabase.LoadAssetAtPath<SpriteAtlasSO>("Assets/Resources/SpriteAtlasSO.asset")!=null)
            {
                string error=AssetDatabase.MoveAsset("Assets/Resources/SpriteAtlasSO.asset",dataPath);if(error!="")throw new InvalidOperationException(error);
            }
            var data=AssetDatabase.LoadAssetAtPath<SpriteAtlasSO>(dataPath);
            if(data==null){data=ScriptableObject.CreateInstance<SpriteAtlasSO>();AssetDatabase.CreateAsset(data,dataPath);}
            var so=new SerializedObject(data);var list=so.FindProperty("atlases");
            // Preserve any previously assigned atlases in the user's registry.
            bool found=false;for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==atlas)found=true;
            if(!found){int index=-1;for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==null){index=i;break;}if(index<0){index=list.arraySize;list.arraySize++;}list.GetArrayElementAtIndex(index).objectReferenceValue=atlas;}
            so.ApplyModifiedPropertiesWithoutUndo();
            var binding=new SerializedObject(manager);binding.FindProperty("spriteAtlasData").objectReferenceValue=data;binding.ApplyModifiedPropertiesWithoutUndo();
            SpriteAtlasUtility.PackAtlases(new[]{atlas},EditorUserBuildSettings.activeBuildTarget);
            AssetDatabase.SaveAssets();
        }
    }
}
