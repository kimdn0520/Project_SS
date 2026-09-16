using System;using System.IO;using System.Linq;using Cysharp.Threading.Tasks;using UnityEngine;using ProjectSS.Expedition;
public static class HeroCellStatesQA{
 public static void Execute(){Run().Forget();}
 static async UniTaskVoid Run(){var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();var values=page.Model.Data.heroProgress.Select(p=>(p,p.stars,p.duplicates)).ToArray();try{
 page.OpenMenu(1);page.ShowHeroGrid();for(int h=0;h<3;h++){var state=page.Model.HeroProgression(h);state.stars=1+h*2;state.duplicates=h==0?2:5;}page.Refresh();
 foreach(var cell in page.formationPanel.collectionCells){var active=cell.stars.Where(s=>s.gameObject.activeSelf).ToArray();int expected=page.Model.HeroProgression(cell.hero).stars;if(active.Length!=expected)throw new Exception("Empty stars visible");float center=active.Average(s=>s.rectTransform.anchoredPosition.x+s.rectTransform.rect.width*.5f);if(Mathf.Abs(center-72)>.01f)throw new Exception("Stars not centered");if(cell.progressFill.type!=UnityEngine.UI.Image.Type.Sliced)throw new Exception("Fill not sliced");}
 if(page.formationPanel.hint.gameObject.activeSelf)throw new Exception("Hint visible");
 await UniTask.Delay(100,ignoreTimeScale:true);await UniTask.WaitForEndOfFrame(page);var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("PrototypeQA/hero-star-states.png",shot.EncodeToPNG());UnityEngine.Object.Destroy(shot);File.WriteAllText("PrototypeQA/hero-cell-states.txt","PASS: only earned stars, centered 1/3/5-star groups, sliced partial/full gauge and hidden hint.");
 }catch(Exception e){File.WriteAllText("PrototypeQA/hero-cell-states.txt","FAIL: "+e);Debug.LogException(e);}finally{foreach(var v in values){v.p.stars=v.stars;v.p.duplicates=v.duplicates;}page.Refresh();}}
}
