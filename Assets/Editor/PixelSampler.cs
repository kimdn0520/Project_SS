using UnityEngine;
using System.IO;
using System.Text;

public static class PixelSampler
{
    public static void Execute()
    {
        Sample();
    }

    public static void Sample()
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots/GameScreenshot.png");
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Image Size: {tex.width}x{tex.height}");
        sb.AppendLine($"Pixel at top (360, 1200): {tex.GetPixel(360, 1200)} (hex: {ColorUtility.ToHtmlStringRGBA(tex.GetPixel(360, 1200))})");
        sb.AppendLine($"Pixel at sky (150, 900): {tex.GetPixel(150, 900)} (hex: {ColorUtility.ToHtmlStringRGBA(tex.GetPixel(150, 900))})");
        sb.AppendLine($"Pixel at sky-right (500, 900): {tex.GetPixel(500, 900)} (hex: {ColorUtility.ToHtmlStringRGBA(tex.GetPixel(500, 900))})");
        sb.AppendLine($"Pixel at divider (360, 660): {tex.GetPixel(360, 660)} (hex: {ColorUtility.ToHtmlStringRGBA(tex.GetPixel(360, 660))})");
        sb.AppendLine($"Pixel at mining (360, 400): {tex.GetPixel(360, 400)} (hex: {ColorUtility.ToHtmlStringRGBA(tex.GetPixel(360, 400))})");
        File.WriteAllText("Assets/Editor/pixels_result.txt", sb.ToString());
        Debug.Log("Pixels sampled to file");
    }
}
