using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ProjectSS.Expedition;
public static class AlignHeroBack
{
 public static void Execute()
 {
  const string path="Assets/Resources/Prefabs/PlayPage.prefab";var root=PrefabUtility.LoadPrefabContents(path);
  try{ProjectSS.Expedition.Editor.WarmUIArt.AlignHeroBackButtons(root.GetComponent<PlayPage>());PrefabUtility.SaveAsPrefabAsset(root,path);}
  finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.SaveAssets();
 }
 public static void Capture(){Run();}
 static async Cysharp.Threading.Tasks.UniTaskVoid Run()
 {
  EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();
  var page=Object.FindFirstObjectByType<PlayPage>();ProjectSS.Expedition.Editor.WarmUIArt.AlignHeroBackButtons(page);
  page.OpenMenu(1);page.OpenHero(0);
  await Cysharp.Threading.Tasks.UniTask.Delay(250);await Cysharp.Threading.Tasks.UniTask.WaitForEndOfFrame(page);
  var image=ScreenCapture.CaptureScreenshotAsTexture();System.IO.File.WriteAllBytes("PrototypeQA/hero-back-aligned.png",image.EncodeToPNG());Object.Destroy(image);
 }
 public static string Inspect()
 {
  var page=AssetDatabase.LoadAssetAtPath<PlayPage>("Assets/Resources/Prefabs/PlayPage.prefab");
  return string.Join("\n",page.heroEquipmentPanels.SelectMany(p=>p.GetComponentsInChildren<Button>(true)).Where(b=>b.name.StartsWith("BackToHeroes")).Select(b=>{var r=(RectTransform)b.transform;return b.name+" parent="+r.parent.name+" pos="+r.anchoredPosition+" size="+r.sizeDelta;}));
 }
}
