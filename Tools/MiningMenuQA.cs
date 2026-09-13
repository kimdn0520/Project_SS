using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ProjectSS.Expedition;
public static class MiningMenuQA
{
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 public static string Execute()
 {
  var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();
  try
  {
   string[] labels={"용사","채굴","가방","설정"};int[] routes={1,5,2,4};
   for(int i=0;i<4;i++)
   {
    var button=page.menuButtons[i].GetComponent<Button>();
    Check(button.GetComponentInChildren<TMP_Text>(true).text==labels[i],"Wrong menu label "+i);
    Check(page.menuIcons[i].GetComponent<Image>().sprite!=null,"Missing menu icon "+i);
    button.onClick.Invoke();Check(page.ActiveTab==routes[i],"Wrong route "+i);
    for(int p=0;p<page.menuPanels.Length;p++)Check(page.menuPanels[p].activeSelf==(p+1==routes[i]),"Wrong visible panel");
    Shot("menu-"+i);
    Check(page.HandleBack()&&page.ActiveTab==0,"Back failed");
   }
   var challenge=page.minePanel.transform.Find("SidebarChallenge");Check(challenge!=null,"Challenge missing");
   Check(((RectTransform)challenge).anchoredPosition.x>600,"Challenge not on right");
   challenge.GetComponent<Button>().onClick.Invoke();Check(page.ActiveTab==3&&page.menuPanels[2].activeSelf,"Challenge listener missing");
   Shot("challenge");page.HandleBack();
   var power=page.menuPanels[4].transform.Find("PickaxePowerPreview");var speed=page.menuPanels[4].transform.Find("MiningSpeedPreview");
   Check(power!=null&&speed!=null,"Upgrade previews missing");
   Check(power.GetComponent<Button>()==null&&speed.GetComponent<Button>()==null,"Preview unexpectedly interactive");
   var face=page.digButton.transform.Find("PressableFace").GetComponent<Image>();Check(face.sprite.name=="DomedDigCap"&&face.rectTransform.sizeDelta.y==144,"Wrong cap art/height");
   Shot("main");const string result="PASS: four menu labels/icons/routes; exclusive panel visibility; back navigation; right sidebar challenge persistent listener; two noninteractive upgrade previews; domed cap assigned.";
   File.WriteAllText("PrototypeQA/mining-menu.txt",result);return result;
  }
  catch(Exception e){File.WriteAllText("PrototypeQA/mining-menu.txt","FAIL: "+e);throw;}
  finally{page.OpenMenu(0);}
 }
 static void Shot(string name)
 {
  var cam=Camera.main;var old=cam.targetTexture;var rect=cam.rect;var active=RenderTexture.active;var rt=new RenderTexture(720,1280,24);var tex=new Texture2D(720,1280,TextureFormat.RGB24,false);
  try{cam.targetTexture=rt;cam.rect=new Rect(0,0,1,1);Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,720,1280),0,0);tex.Apply();File.WriteAllBytes("PrototypeQA/mining-"+name+".png",tex.EncodeToPNG());}
  finally{cam.targetTexture=old;cam.rect=rect;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
 }
}
