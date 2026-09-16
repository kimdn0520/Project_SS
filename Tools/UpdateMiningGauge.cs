using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectSS.Expedition;
public static class UpdateMiningGauge
{
 public static object Execute()
 {
  if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode before prefab editing");
  const string path="Assets/Resources/Prefabs/PlayPage.prefab";
  var root=PrefabUtility.LoadPrefabContents(path);
  try
  {
   var page=root.GetComponent<PlayPage>();
   foreach(var floor in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="MineFloor").ToArray())UnityEngine.Object.DestroyImmediate(floor.gameObject);
   var parent=page.minePanel.transform;
   foreach(var old in parent.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="BurstGaugeFrame"||t.name=="BurstGauge").ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
   var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Common/Cells/Panel9Slice.png");
   Make(parent,"BurstGaugeFrame",529,978,22,150,sprite,new Color(.16f,.25f,.28f));
   var fill=Make(parent,"BurstGauge",533,982,14,142,sprite,new Color(1,.77f,.36f));
   fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Vertical;fill.fillOrigin=0;fill.fillAmount=0;page.heatBar=fill;
   PrefabUtility.SaveAsPrefabAsset(root,path);
  }
  finally{PrefabUtility.UnloadPrefabContents(root);}
  AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Play.unity");
  return new {removed="MineFloor",gauge="DIG right / vertical bottom-to-top",prefab=path};
 }
 static Image Make(Transform parent,string name,float x,float y,float w,float h,Sprite sprite,Color color)
 {
  var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
  var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
  var image=go.GetComponent<Image>();image.sprite=sprite;image.type=Image.Type.Sliced;image.color=color;image.raycastTarget=false;return image;
 }
}
