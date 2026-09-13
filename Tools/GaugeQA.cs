using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using ProjectSS.Expedition;
public static class GaugeQA
{
    public static void Execute()
    {
        var page=UnityEngine.Object.FindFirstObjectByType<PlayPage>();var camera=Camera.main;
        var oldTarget=camera.targetTexture;var oldRect=camera.rect;var oldActive=RenderTexture.active;
        var rt=new RenderTexture(720,1280,24);var tex=new Texture2D(720,1280,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);
            foreach(float amount in new[]{0f,.01f,.25f,.5f,1f})
            {
                page.heatGauge.SetValue(amount,new Color(1,.77f,.36f));Canvas.ForceUpdateCanvases();
                if(page.heatBar.type!=Image.Type.Sliced||Mathf.Abs(page.heatBar.rectTransform.rect.height-134)>.01f)throw new Exception("Gauge artwork stretched");
                var mask=(RectTransform)page.heatBar.transform.parent;
                if(Mathf.Abs(mask.rect.height-134*amount)>.01f)throw new Exception("Gauge clipping incorrect");
                camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,720,1280),0,0);tex.Apply();File.WriteAllBytes("PrototypeQA/gauge-fixed-"+(int)(amount*100)+".png",tex.EncodeToPNG());
            }
            File.WriteAllText("PrototypeQA/gauge-fixed.txt","PASS: 0%, 1%, 25%, 50%, 100%; sliced art remains 20x134, only reveal mask height changes.");
        }
        finally{camera.targetTexture=oldTarget;camera.rect=oldRect;RenderTexture.active=oldActive;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);page.Refresh();}
    }
}
