using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSS.Expedition.Editor
{
    public static class DigButtonArt
    {
        public static void Configure(PlayPage page)
        {
            const string path = "Assets/Prototype/Art/DigCapGold_v3.png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var root = (RectTransform)page.digButton.transform;
            var inputImage = root.GetComponent<Image>();
            inputImage.color = Color.clear;
            var face = root.Find("PressableFace") as RectTransform;
            if (face == null)
            {
                face = new GameObject("PressableFace", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                face.SetParent(root, false);
                page.digLabel.transform.SetParent(face, false);
            }
            face.anchorMin = face.anchorMax = face.pivot = new Vector2(.5f, .5f);
            face.anchoredPosition = Vector2.zero;
            face.sizeDelta = new Vector2(280, 184);
            face.localScale = Vector3.one;
            var image = face.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            var label = page.digLabel.rectTransform;
            label.anchorMin = label.anchorMax = label.pivot = new Vector2(.5f, .5f);
            label.anchoredPosition = new Vector2(0, 9);
            label.sizeDelta = new Vector2(230, 80);
            page.digLabel.raycastTarget = false;
            var serialized = new SerializedObject(page.holdDig);
            serialized.FindProperty("rectTransform").objectReferenceValue = root;
            serialized.FindProperty("pressableFace").objectReferenceValue = face;
            serialized.FindProperty("pressedScale").vector3Value = new Vector3(.98f, .96f, 1);
            serialized.FindProperty("pressOffset").vector3Value = new Vector3(0, -12, 0);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
