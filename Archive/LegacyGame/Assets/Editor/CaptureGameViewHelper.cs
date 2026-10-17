using System.IO;
using UnityEngine;
using UnityEditor;

public static class CaptureGameViewHelper
{
    public static void Execute()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = Object.FindFirstObjectByType<Camera>();
        }

        if (cam == null)
        {
            Debug.LogError("[CaptureGameViewHelper] No camera found!");
            return;
        }

        int width = 720;
        int height = 1280;

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture prevRt = cam.targetTexture;
        RenderTexture prevActive = RenderTexture.active;

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        cam.targetTexture = prevRt;
        RenderTexture.active = prevActive;
        Object.DestroyImmediate(rt);

        byte[] bytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        string savePath = Path.Combine(Application.dataPath, "../gameview_capture.png");
        File.WriteAllBytes(savePath, bytes);
        Debug.Log($"[CaptureGameViewHelper] Captured to: {savePath}, size: {bytes.Length} bytes");
    }
}
