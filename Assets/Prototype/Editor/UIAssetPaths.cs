using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectSS.Expedition.Editor
{
    public static class UIAssetPaths
    {
        // Older builders compose package paths from a prefix and filename. The migrated
        // UI collection is the canonical fallback for those references.
        public static string Resolve(string path)
        {
            if (File.Exists(path)) return path;
            var matches = Directory.GetFiles("Assets/Textures/UI", Path.GetFileName(path), SearchOption.AllDirectories);
            return matches.Length == 1 ? matches[0].Replace('\\', '/') : path;
        }
        public static Sprite LoadSprite(string path) => AssetDatabase.LoadAllAssetsAtPath(Resolve(path)).OfType<Sprite>().FirstOrDefault();
    }
}
