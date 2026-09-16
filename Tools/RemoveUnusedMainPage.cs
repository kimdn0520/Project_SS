using System;using UnityEditor;
public static class RemoveUnusedMainPage {
 public static string Execute(){foreach(var p in new[]{"Assets/Resources/Prefabs/MainPage.prefab","Assets/Textures/UI/NonAtlas/Backgrounds/home-background-large.png"}){if(AssetDatabase.LoadMainAssetAtPath(p)!=null&&!AssetDatabase.DeleteAsset(p))throw new Exception("Delete failed: "+p);}AssetDatabase.Refresh();return "Deleted unused MainPage prefab and home background (including metadata).";}
}
