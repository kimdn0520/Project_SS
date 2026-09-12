using System.IO;
using UnityEngine;
using UnityEditor;
using TMPro;

public static class FontAndScreenshotTools
{
    private const string FONT_TTF_PATH = "Assets/Fonts/MalgunGothic.ttf";
    private const string FONT_SDF_PATH = "Assets/Fonts/MalgunGothic SDF.asset";

    [MenuItem("ProjectSS/Setup Korean Font")]
    public static void SetupKoreanFont()
    {
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_SDF_PATH);
        if (fontAsset == null)
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FONT_TTF_PATH);
            if (font == null)
            {
                Debug.LogError($"[FontTools] TTF not found at {FONT_TTF_PATH}");
                return;
            }

            fontAsset = TMP_FontAsset.CreateFontAsset(font, 70, 7, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            if (fontAsset == null)
            {
                Debug.LogError("[FontTools] Failed to create font asset");
                return;
            }

            AssetDatabase.CreateAsset(fontAsset, FONT_SDF_PATH);
            if (fontAsset.material != null)
            {
                fontAsset.material.name = "MalgunGothic SDF Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }
            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    if (fontAsset.atlasTextures[i] != null)
                    {
                        fontAsset.atlasTextures[i].name = $"MalgunGothic Atlas {i}";
                        AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[i], fontAsset);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[FontTools] Successfully created {FONT_SDF_PATH}");
        }

        TMP_Settings settings = TMP_Settings.instance;
        if (settings != null)
        {
            SerializedObject so = new SerializedObject(settings);
            SerializedProperty fallbackList = so.FindProperty("m_fallbackFontAssets");
            if (fallbackList != null)
            {
                bool exists = false;
                for (int i = 0; i < fallbackList.arraySize; i++)
                {
                    if (fallbackList.GetArrayElementAtIndex(i).objectReferenceValue == fontAsset)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    fallbackList.InsertArrayElementAtIndex(fallbackList.arraySize);
                    fallbackList.GetArrayElementAtIndex(fallbackList.arraySize - 1).objectReferenceValue = fontAsset;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                    Debug.Log("[FontTools] Added MalgunGothic SDF to TMP Settings fallback fonts.");
                }
            }
        }
    }

    [MenuItem("ProjectSS/Capture Screenshot")]
    public static void CaptureScreenshot()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = Object.FindAnyObjectByType<Camera>();
        }

        if (cam == null)
        {
            Debug.LogError("[Screenshot] No Camera found in scene!");
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
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        cam.targetTexture = prevRt;
        RenderTexture.active = prevActive;
        Object.DestroyImmediate(rt);

        byte[] pngData = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        string dir = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string filePath = Path.Combine(dir, "GameScreenshot.png");
        File.WriteAllBytes(filePath, pngData);
        Debug.Log($"[Screenshot] Saved Game screenshot to: {filePath}");

        string brainPath = @"C:\Users\kimdn\.gemini\antigravity-cli\brain\2b5e56ef-80ba-43ba-b65a-6a88407376f8\GameScreenshot.png";
        try
        {
            File.WriteAllBytes(brainPath, pngData);
            Debug.Log($"[Screenshot] Also copied to brain artifact: {brainPath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Screenshot] Could not copy to brain: {ex.Message}");
        }
    }

    [MenuItem("ProjectSS/Inspect Characters")]
    public static void InspectCharacters()
    {
        string[] paths = new string[]
        {
            "Assets/Layer Lab/2D Maps - Simple Sidescroll/Extenstions/Simple Sidescroll1/Prefabs/Adaptive_MapFocused/Map1_AdaptiveView.prefab",
            "Assets/Layer Lab/2D Maps - Simple Sidescroll/Extenstions/Simple Sidescroll1/Prefabs/Portrait_UIFocused/Map1_PortraitUI.prefab"
        };

        foreach (var path in paths)
        {
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null)
            {
                Debug.LogError($"Not found: {path}");
                continue;
            }

            Debug.Log($"=== Inspecting: {go.name} ===");
            for (int i = 0; i < go.transform.childCount; i++)
            {
                Transform c = go.transform.GetChild(i);
                var srs = c.GetComponentsInChildren<SpriteRenderer>(true);
                Debug.Log($"Child[{i}]: {c.name}, LocalPos: {c.localPosition}, LocalScale: {c.localScale}, Srs: {srs.Length}");
                if (srs.Length > 0)
                {
                    Debug.Log($"   First SR: {srs[0].name}, sprite: {srs[0].sprite?.name}, order: {srs[0].sortingOrder}, drawMode: {srs[0].drawMode}, size: {srs[0].size}");
                }
            }
        }
    }
}
