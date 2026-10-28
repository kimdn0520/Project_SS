using System.IO;
using UnityEngine;
using RuneBreak;
public static class GateNumberPreview
{
    public static void Execute()
    {
        var pages=Object.FindFirstObjectByType<PageManager>();PopupManager.Clear();pages.ChangeImmediate(UIPageType.PlayPage,0);var p=pages.GetPage<RunePlayPage>(UIPageType.PlayPage);p.CanvasGroup.alpha=1;p.AdvancePresentation(.02f);
        Capture(p.Canvas,"GateNumbers-Play");
        // Preview the supplied digit with the real gate geometry and authored font preset.
        var old=p.gates[1].sprite;var text=p.gateCounts[1].text;p.gates[1].sprite=p.art.longGates[3];p.gateCounts[1].text="8";
        Capture(p.Canvas,"GateNumbers-Reference8");p.gates[1].sprite=old;p.gateCounts[1].text=text;
    }
    static void Capture(Canvas canvas,string name)
    {
        Canvas.ForceUpdateCanvases();var camera=canvas.worldCamera;var prior=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(Screen.width,Screen.height,24);var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();File.WriteAllBytes("Tools/RuneBreak/QA/"+name+".png",texture.EncodeToPNG());}
        finally{camera.targetTexture=prior;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(texture);}
    }
}
