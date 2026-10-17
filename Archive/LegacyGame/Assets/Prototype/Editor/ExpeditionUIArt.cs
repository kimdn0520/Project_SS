using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectSS.Expedition.Editor
{
    /// <summary>Small reusable UI primitives, baked once as 9-slice assets in the editor.</summary>
    public static class ExpeditionUIArt
    {
        public static Sprite Circle()
        {
            const string path = "Assets/Textures/UI/Common/Mining/DigCircle.png";
            if (!File.Exists(path))
            {
                var t = new Texture2D(256,256,TextureFormat.RGBA32,false);
                for(int y=0;y<256;y++)for(int x=0;x<256;x++)
                {
                    float d=new Vector2(x-127.5f,y-127.5f).magnitude;
                    float a=Mathf.Clamp01(126-d);float shade=d>120?.78f:Mathf.Lerp(.88f,1f,y/255f);
                    t.SetPixel(x,y,new Color(shade,shade,shade,a));
                }
                t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            }
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        public static Sprite Panel()
        {
            const string path = "Assets/Textures/UI/Common/Cells/Panel9Slice.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - 32) - 19;
                    float qy = Mathf.Abs(y + 0.5f - 32) - 19;
                    float sd = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - 12;
                    float alpha = Mathf.Clamp01(0.5f - sd);
                    float rim = sd > -1.8f ? 1 : 0.92f;
                    texture.SetPixel(x, y, new Color(rim, rim, rim, alpha));
                }
                texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(16, 16, 16, 16); importer.spritePixelsPerUnit = 100;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true; importer.filterMode = FilterMode.Bilinear; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static AudioClip Impact(bool broken)
        {
            string path = "Assets/Prototype/Art/" + (broken ? "RockBreak_v2.wav" : "PickImpact_v2.wav");
            if (!File.Exists(path))
            {
                const int rate = 44100;
                int count = (int)(rate * (broken ? 0.42f : 0.19f));
                var random = new System.Random(broken ? 42 : 17);
                using (var stream = new BinaryWriter(File.Create(path)))
                {
                    stream.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); stream.Write(36 + count * 2);
                    stream.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); stream.Write(16);
                    stream.Write((short)1); stream.Write((short)1); stream.Write(rate); stream.Write(rate * 2); stream.Write((short)2); stream.Write((short)16);
                    stream.Write(System.Text.Encoding.ASCII.GetBytes("data")); stream.Write(count * 2);
                    float filtered = 0, previousNoise = 0;
                    for (int i = 0; i < count; i++)
                    {
                        float t = (float)i / rate;
                        float noise = (float)random.NextDouble() * 2 - 1;
                        filtered = Mathf.Lerp(filtered, noise, 0.12f);
                        float high = noise - previousNoise; previousNoise = noise;
                        // Pick: brief metal edge, descending stone body, and granular grit.
                        float attack = Mathf.Min(1, t / 0.0008f);
                        float click = high * Mathf.Exp(-t * 320) * 0.28f;
                        float metal = (Mathf.Sin(2 * Mathf.PI * 1830 * t) + 0.45f * Mathf.Sin(2 * Mathf.PI * 3177 * t)) * Mathf.Exp(-t * 75) * 0.16f;
                        float body = Mathf.Sin(2 * Mathf.PI * (broken ? 115 * t - 90 * t * t : 225 * t - 300 * t * t)) * Mathf.Exp(-t * (broken ? 22 : 45)) * 0.48f;
                        float grit = filtered * Mathf.Exp(-t * (broken ? 12 : 42)) * (broken ? 1.1f : 0.65f);
                        float fragments = 0;
                        if (broken)
                            for (int chip = 0; chip < 7; chip++)
                            {
                                float age = t - (0.024f + chip * 0.038f);
                                if (age >= 0) fragments += (noise * 0.2f + Mathf.Sin(2 * Mathf.PI * (720 + chip * 231) * age) * 0.12f) * Mathf.Exp(-age * 95) * (1 - chip * 0.10f);
                            }
                        float sample = (click + metal + body + grit + fragments) * attack;
                        sample = (float)Math.Tanh(sample * 1.45f) * 0.82f;
                        sample *= Mathf.Clamp01((count - 1 - i) / (rate * 0.012f));
                        stream.Write((short)(sample * 32767));
                    }
                }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
