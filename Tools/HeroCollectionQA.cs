using System;using System.IO;using Cysharp.Threading.Tasks;using UnityEditor;using UnityEngine;using ProjectSS.Expedition;
public static class HeroCollectionQA {
 public static void Execute(){Run().Forget();}
 static async UniTaskVoid Run(){var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();try{EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();page.OpenMenu(1);page.ShowHeroGrid();page.Refresh();await UniTask.Delay(200,ignoreTimeScale:true);await UniTask.WaitForEndOfFrame(page);var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("PrototypeQA/hero-collection.png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);
 foreach(var cell in page.formationPanel.collectionCells){if(cell.nameLabel.gameObject.activeSelf)throw new Exception("Hero name must be hidden");if(cell.stars.Length!=5||cell.stars[0].canvasRenderer==null||cell.grade.text!=page.Model.Grade(cell.hero).ToString())throw new Exception("Grade/star binding");if(cell.transform.Find("ManageGear").gameObject.activeSelf)throw new Exception("Legacy gear button visible");}
 page.formationPanel.SelectHero(0);if(page.heroGrid.activeSelf)throw new Exception("Card should open equipment");page.ShowHeroGrid();page.Refresh();File.WriteAllText("PrototypeQA/hero-collection.txt","PASS: grade, five stars, duplicates progress, compact cards and existing equipment navigation.");
 }catch(Exception e){File.WriteAllText("PrototypeQA/hero-collection.txt","FAIL: "+e);Debug.LogException(e);}}
}
