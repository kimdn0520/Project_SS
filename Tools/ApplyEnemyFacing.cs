using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectSS.Expedition;
public static class ApplyEnemyFacing
{
 public static void Execute()
 {
  if(EditorApplication.isPlaying)throw new System.Exception("Stop play first");
  const string path="Assets/Resources/Prefabs/PlayPage.prefab";var root=PrefabUtility.LoadPrefabContents(path);
  try
  {
   var page=root.GetComponent<PlayPage>();
   foreach(var enemy in page.enemies)
   {
    var so=new SerializedObject(enemy);var visual=(Transform)so.FindProperty("motionRoot").objectReferenceValue;
    if(visual.localScale.x<0){var scale=visual.localScale;scale.x=-scale.x;visual.localScale=scale;var p=visual.localPosition;p.x=-p.x;visual.localPosition=p;}
    so.FindProperty("restPosition").vector3Value=visual.localPosition;so.FindProperty("facingDirection").floatValue=-1;so.ApplyModifiedPropertiesWithoutUndo();
   }
   PrefabUtility.SaveAsPrefabAsset(root,path);
  }
  finally{PrefabUtility.UnloadPrefabContents(root);}
  AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");
 }
}
