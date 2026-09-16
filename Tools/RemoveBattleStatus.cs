using System.Linq;using UnityEditor;using UnityEngine;
public static class RemoveBattleStatus {
 public static string Execute(){const string path="Assets/Resources/Prefabs/PlayPage.prefab";var root=PrefabUtility.LoadPrefabContents(path);try{foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="BattleStatus").ToArray())Object.DestroyImmediate(t.gameObject);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.Refresh();return "BattleStatus removed from PlayPage.";}
}
