using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSS.Expedition;
public static class ReduceChestsAndControls
{
 public static object Execute()
 {
  if(EditorApplication.isPlaying)throw new Exception("Stop playing before prefab editing");
  const string path="Assets/Resources/Prefabs/PlayPage.prefab";
  var root=PrefabUtility.LoadPrefabContents(path);
  try
  {
   foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="AutoMine"||t.name=="StartBattle").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
   PrefabUtility.SaveAsPrefabAsset(root,path);
  }
  finally{PrefabUtility.UnloadPrefabContents(root);}
  AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");
  var catalog=AssetDatabase.LoadAssetAtPath<ExpeditionCatalog>("Assets/Prototype/ExpeditionCatalog.asset");
  var model=new ExpeditionModel(catalog,ExpeditionSave.Fresh(catalog.gear.Length),43);
  int chests=0,veins=0,gap=0,min=int.MaxValue,max=0;
  for(int i=0;i<10000;i++)
  {
   bool chest=model.Data.chest;model.Dig(10);
   if(chest){chests++;if(chests>1){min=Math.Min(min,gap);max=Math.Max(max,gap);if(gap<8||gap>40)throw new Exception("Chest spacing violated");}gap=0;}
   else{veins++;gap++;}
  }
  return new{chests,veins,minimumGap=min,maximumGap=max,rate=(double)chests/10000,controls="removed"};
 }
}
