using UnityEngine;
using UnityEditor;
using System.IO;

public static class TextureAssetGenerator
{
    private const string GAME_TEX_PATH = "Assets/Textures/Game";
    private const string UI_TEX_PATH = "Assets/Textures/UI";

    [MenuItem("ProjectSS/Generate Core Textures")]
    public static void GenerateTextures()
    {
        if (!Directory.Exists(GAME_TEX_PATH)) Directory.CreateDirectory(GAME_TEX_PATH);
        if (!Directory.Exists(UI_TEX_PATH)) Directory.CreateDirectory(UI_TEX_PATH);

        CreateSoilBlockTexture(Path.Combine(GAME_TEX_PATH, "Block_Soil.png"));
        CreateRockBlockTexture(Path.Combine(GAME_TEX_PATH, "Block_Rock.png"));
        CreateCrackTexture(Path.Combine(GAME_TEX_PATH, "Block_Crack_1.png"), 1);
        CreateCrackTexture(Path.Combine(GAME_TEX_PATH, "Block_Crack_2.png"), 2);
        CreateCaveBackdropTexture(Path.Combine(GAME_TEX_PATH, "Cave_Backdrop.png"));
        CreateDigButtonBaseTexture(Path.Combine(UI_TEX_PATH, "Dig_Button_Base.png"));
        CreateDigButtonFaceTexture(Path.Combine(UI_TEX_PATH, "Dig_Button_Face.png"));
        CreateDigProgressRingTexture(Path.Combine(UI_TEX_PATH, "Dig_Progress_Ring.png"));

        AssetDatabase.Refresh();

        // 텍스처 임포터 설정 (Sprite 2D and UI)
        ConfigureImporter(Path.Combine(GAME_TEX_PATH, "Block_Soil.png"), 100);
        ConfigureImporter(Path.Combine(GAME_TEX_PATH, "Block_Rock.png"), 100);
        ConfigureImporter(Path.Combine(GAME_TEX_PATH, "Block_Crack_1.png"), 100);
        ConfigureImporter(Path.Combine(GAME_TEX_PATH, "Block_Crack_2.png"), 100);
        ConfigureImporter(Path.Combine(GAME_TEX_PATH, "Cave_Backdrop.png"), 100);
        ConfigureImporter(Path.Combine(UI_TEX_PATH, "Dig_Button_Base.png"), 100);
        ConfigureImporter(Path.Combine(UI_TEX_PATH, "Dig_Button_Face.png"), 100);
        ConfigureImporter(Path.Combine(UI_TEX_PATH, "Dig_Progress_Ring.png"), 100);

        // 복사된 아이콘들도 스프라이트로 임포트 설정
        string[] copiedIcons = new string[]
        {
            "Economy_Coin_02_Gold.png", "Economy_Gem_03_Blue.png",
            "Gear_Weapons_Pickaxe_01.png", "Gear_Weapons_Shovel_01.png",
            "Item_Chest_01_Purple.png", "Item_Chest_01_Wood.png",
            "Material_Ore_01.png", "Material_Ore_02_01.png", "Material_Ore_03_Gold.png"
        };
        foreach (var icon in copiedIcons)
        {
            ConfigureImporter(Path.Combine(GAME_TEX_PATH, icon), 100);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>[TextureAssetGenerator] 모든 핵심 스프라이트 텍스처 생성 및 임포터 설정 완료!</color>");
    }

    private static void ConfigureImporter(string path, float ppu)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private static void CreateSoilBlockTexture(string filePath)
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color baseColor = new Color(0.48f, 0.32f, 0.18f, 1f); // 풍부한 흙 갈색
        Color darkBorder = new Color(0.32f, 0.20f, 0.10f, 1f);
        Color topHighlight = new Color(0.62f, 0.44f, 0.26f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (float)x / size;
                float ny = (float)y / size;

                // 둥근 모서리 판정
                float cornerDist = GetCornerDistance(nx, ny, 0.12f);
                if (cornerDist > 0)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                // 테두리 음영
                bool isBorder = x < 12 || x >= size - 12 || y < 12 || y >= size - 12;
                bool isTop = y >= size - 20;

                Color c = baseColor;
                if (isTop) c = Color.Lerp(baseColor, topHighlight, 0.8f);
                else if (isBorder) c = darkBorder;

                // 미세 질감 노이즈
                float noise = (Mathf.Sin(x * 0.15f) * Mathf.Cos(y * 0.15f)) * 0.04f;
                c = new Color(c.r + noise, c.g + noise, c.b + noise, 1f);

                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void CreateRockBlockTexture(string filePath)
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color rockBase = new Color(0.35f, 0.38f, 0.44f, 1f); // 단단한 슬레이트 그레이
        Color rockBorder = new Color(0.20f, 0.22f, 0.26f, 1f);
        Color rockHighlight = new Color(0.50f, 0.54f, 0.62f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (float)x / size;
                float ny = (float)y / size;

                float cornerDist = GetCornerDistance(nx, ny, 0.14f);
                if (cornerDist > 0)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                bool isBorder = x < 14 || x >= size - 14 || y < 14 || y >= size - 14;
                bool isTopBevel = y >= size - 24 && x >= 24 && x < size - 24;

                Color c = rockBase;
                if (isTopBevel) c = Color.Lerp(rockBase, rockHighlight, 0.75f);
                else if (isBorder) c = rockBorder;

                // 암석 결 무늬
                float facet = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.08f - 0.04f;
                c = new Color(c.r + facet, c.g + facet, c.b + facet, 1f);

                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void CreateCrackTexture(string filePath, int stage)
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color crackColor = new Color(0.12f, 0.12f, 0.15f, 0.95f);
        Color crackGlow = new Color(0.7f, 0.5f, 0.2f, 0.6f); // 광석 내부 균열빛

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);

        // 균열 선 렌더링
        DrawCrackBranch(tex, new Vector2(128, 128), new Vector2(40, 210), stage >= 2 ? 4 : 2, crackColor);
        DrawCrackBranch(tex, new Vector2(128, 128), new Vector2(215, 60), stage >= 2 ? 4 : 2, crackColor);
        if (stage >= 2)
        {
            DrawCrackBranch(tex, new Vector2(128, 128), new Vector2(210, 200), 3, crackColor);
            DrawCrackBranch(tex, new Vector2(128, 128), new Vector2(60, 50), 3, crackColor);
            DrawCrackBranch(tex, new Vector2(100, 160), new Vector2(150, 230), 2, crackGlow);
        }

        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void DrawCrackBranch(Texture2D tex, Vector2 start, Vector2 end, int thickness, Color c)
    {
        int steps = 50;
        Vector2 cur = start;
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector2 target = Vector2.Lerp(start, end, t);
            float jitter = Mathf.Sin(t * 18f) * 6f;
            Vector2 pt = target + new Vector2(-jitter, jitter);

            for (int dx = -thickness; dx <= thickness; dx++)
            {
                for (int dy = -thickness; dy <= thickness; dy++)
                {
                    int px = Mathf.Clamp(Mathf.RoundToInt(pt.x + dx), 0, tex.width - 1);
                    int py = Mathf.Clamp(Mathf.RoundToInt(pt.y + dy), 0, tex.height - 1);
                    tex.SetPixel(px, py, c);
                }
            }
        }
    }

    private static void CreateCaveBackdropTexture(string filePath)
    {
        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color caveTop = new Color(0.09f, 0.08f, 0.13f, 1f); // 짙은 지하 동굴 상단
        Color caveBottom = new Color(0.14f, 0.12f, 0.18f, 1f); // 바닥 암반층

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float ny = (float)y / size;
                Color baseC = Color.Lerp(caveBottom, caveTop, ny);

                // 바위 질감 레이어링
                float n1 = Mathf.PerlinNoise(x * 0.02f, y * 0.02f) * 0.06f;
                float n2 = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.03f;
                float rock = n1 + n2 - 0.045f;

                Color finalC = new Color(
                    Mathf.Clamp01(baseC.r + rock),
                    Mathf.Clamp01(baseC.g + rock),
                    Mathf.Clamp01(baseC.b + rock),
                    1f);

                tex.SetPixel(x, y, finalC);
            }
        }
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void CreateDigButtonBaseTexture(string filePath)
    {
        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.48f;

        Color rimOuter = new Color(0.72f, 0.52f, 0.10f, 1f); // 진한 황금빛 외곽 테두리
        Color rimInner = new Color(0.95f, 0.76f, 0.18f, 1f); // 밝은 골드 림
        Color shadow = new Color(0.18f, 0.12f, 0.04f, 0.9f);  // 하단 깊은 입체 그림자

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist > radius)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float norm = dist / radius;
                Color c = Color.Lerp(rimInner, rimOuter, norm);

                // 아래쪽 3D 베벨 그림자
                if (y < center.y && norm > 0.6f)
                {
                    c = Color.Lerp(c, shadow, 0.45f);
                }

                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void CreateDigButtonFaceTexture(string filePath)
    {
        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.46f;

        Color topColor = new Color(1.0f, 0.92f, 0.35f, 1f);    // 상단 반사 하이라이트
        Color midColor = new Color(0.98f, 0.78f, 0.15f, 1f);    // 골드 옐로우
        Color botColor = new Color(0.85f, 0.60f, 0.08f, 1f);    // 하단 음영

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist > radius)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float ny = (float)y / size;
                Color c = ny > 0.5f ? Color.Lerp(midColor, topColor, (ny - 0.5f) * 2f)
                                    : Color.Lerp(botColor, midColor, ny * 2f);

                // 테두리 미세 라인
                float norm = dist / radius;
                if (norm > 0.92f)
                {
                    c = Color.Lerp(c, new Color(1f, 1f, 1f, 0.8f), 0.5f);
                }

                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void CreateDigProgressRingTexture(string filePath)
    {
        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float outerR = size * 0.49f;
        float innerR = size * 0.43f;

        Color ringColor = new Color(0.3f, 0.95f, 1.0f, 1f); // 사이언/아쿠아 블루 진행 링

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist >= innerR && dist <= outerR)
                {
                    tex.SetPixel(x, y, ringColor);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static float GetCornerDistance(float nx, float ny, float radius)
    {
        float cx = nx < 0.5f ? radius : 1f - radius;
        float cy = ny < 0.5f ? radius : 1f - radius;

        bool inCornerX = (nx < radius) || (nx > 1f - radius);
        bool inCornerY = (ny < radius) || (ny > 1f - radius);

        if (inCornerX && inCornerY)
        {
            float d = Vector2.Distance(new Vector2(nx, ny), new Vector2(cx, cy));
            return d > radius ? (d - radius) : 0f;
        }
        return 0f;
    }
}
