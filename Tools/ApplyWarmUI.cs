using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ProjectSS.Expedition;
public static class ApplyWarmUI
{
 public static void Capture(){Run();}
 static async Cysharp.Threading.Tasks.UniTaskVoid Run()
 {
  EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();
  var page=Object.FindFirstObjectByType<PlayPage>();int original=page.Model.Data.route;
  page.pausePolicy.ReleasePause("AppFocusLoss");
  try
  {
   page.OpenMenu(4);await Shot(page,"settings");page.OpenMenu(5);await Shot(page,"mining");
   page.OpenMenu(0);page.OpenVeins();await Shot(page,"veins");
   var popup=(VeinSelectionPopup)PopupManager.CurrentPopup;
   if(popup.choices[original].interactable||popup.choiceLabels[original].text!="선택됨")throw new System.Exception("Current route state incorrect");
   if(page.Model.Data.cleared<10&&popup.choices[2].interactable)throw new System.Exception("Locked route enabled");
   int target=original==0?1:0;popup.choices[target].onClick.Invoke();await Cysharp.Threading.Tasks.UniTask.Delay(350);
   if(page.Model.Data.route!=target||!PopupManager.IsOpenAny)throw new System.Exception("Select button did not change route and keep popup open");
   await Shot(page,"selected");
   System.IO.File.WriteAllText("PrototypeQA/warm-ui.txt","PASS: separate route button changes route while popup stays open; selected button disabled; lock state checked; settings and mining previews captured.");
  }
  finally{PopupManager.Clear();page.ChangeVein(original);page.OpenMenu(0);}
 }
 static async Cysharp.Threading.Tasks.UniTask Shot(PlayPage page,string name)
 {
  await Cysharp.Threading.Tasks.UniTask.Delay(350);await Cysharp.Threading.Tasks.UniTask.WaitForEndOfFrame(page);
  var image=ScreenCapture.CaptureScreenshotAsTexture();System.IO.File.WriteAllBytes("PrototypeQA/warm-"+name+".png",image.EncodeToPNG());Object.Destroy(image);
 }
 public static void Execute()
 {
  const string path="Assets/Resources/Prefabs/PlayPage.prefab";var root=PrefabUtility.LoadPrefabContents(path);
  try{ProjectSS.Expedition.Editor.WarmUIArt.Configure(root.GetComponent<PlayPage>());PrefabUtility.SaveAsPrefabAsset(root,path);}
  finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.SaveAssets();
 }
 public static string Inspect()
 {
  var page=AssetDatabase.LoadAssetAtPath<PlayPage>("Assets/Resources/Prefabs/PlayPage.prefab");
  return string.Join("\n",page.menuPanels.SelectMany(p=>p.GetComponentsInChildren<Button>(true)).Select(b=>b.name+" : "+b.GetComponentInChildren<TMPro.TMP_Text>(true)?.text));
 }
}
