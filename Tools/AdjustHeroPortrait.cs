using UnityEditor;using UnityEngine;using ProjectSS.Expedition;using ProjectSS.Expedition.Editor;
public static class AdjustHeroPortrait {
 public static string Execute(){const string path="Assets/Resources/Prefabs/PlayPage.prefab";var root=PrefabUtility.LoadPrefabContents(path);try{var page=root.GetComponent<PlayPage>();page.formationPanel.collectionCells[2].portraitOffset=new Vector2(0,-8);HeroCollectionArt.Configure(page);PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();return "Third portrait lowered 8 UI units; shared prefab preserved.";}finally{PrefabUtility.UnloadPrefabContents(root);}}
}
