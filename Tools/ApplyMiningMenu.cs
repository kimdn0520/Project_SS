using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSS.Expedition;
using ProjectSS.Expedition.Editor;
public static class ApplyMiningMenu
{
 public static void Execute()
 {
  if(EditorApplication.isPlaying)throw new System.Exception("Stop play first");const string path="Assets/Resources/Prefabs/PlayPage.prefab";var root=PrefabUtility.LoadPrefabContents(path);
  try{var page=root.GetComponent<PlayPage>();SidebarShortcutArt.Configure(page);MiningMenuArt.Configure(page);DigFeedbackArt.ConfigureDecal(page);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
  AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");
 }
}
