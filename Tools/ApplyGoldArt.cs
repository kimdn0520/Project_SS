using UnityEditor;
using UnityEngine;
using ProjectSS.Expedition;
using ProjectSS.Expedition.Editor;
public static class ApplyGoldArt
{
 public static void Execute()
 {
  const string path="Assets/Resources/Prefabs/PlayPage.prefab";var root=PrefabUtility.LoadPrefabContents(path);
  try{var page=root.GetComponent<PlayPage>();WindowTitleArt.Configure(page);GoldChestArt.Configure(page);PrefabUtility.SaveAsPrefabAsset(root,path);}
  finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.SaveAssets();
 }
 public static void Capture(){Run();}
 static async Cysharp.Threading.Tasks.UniTaskVoid Run()
 {
  var page=Object.FindFirstObjectByType<PlayPage>();page.OpenMenu(3);await Shot(page,"title");
  page.OpenMenu(0);page.OnWillLeave();page.miningView.SetChest(true);await Shot(page,"closed");
  page.miningView.OpenChest();await Cysharp.Threading.Tasks.UniTask.Delay(520);await Shot(page,"opening");
  await Cysharp.Threading.Tasks.UniTask.Delay(450);await Shot(page,"open");
  page.miningView.SetChest(page.Model.Data.chest);page.OnDidEnter();
 }
 static async Cysharp.Threading.Tasks.UniTask Shot(PlayPage page,string name)
 {
  await Cysharp.Threading.Tasks.UniTask.WaitForEndOfFrame(page);var t=ScreenCapture.CaptureScreenshotAsTexture();System.IO.File.WriteAllBytes("PrototypeQA/gold-"+name+".png",t.EncodeToPNG());Object.Destroy(t);
 }
}
