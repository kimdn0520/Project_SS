using UnityEditor;using UnityEngine;using System.Linq;
public static class SavePopupDependencies {
 public static string Execute(){
 foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Resources/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath)){
 var root=PrefabUtility.LoadPrefabContents(path);try{PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
 AssetDatabase.SaveAssets();return "Reserialized resource prefabs without popup preload references.";}
}
