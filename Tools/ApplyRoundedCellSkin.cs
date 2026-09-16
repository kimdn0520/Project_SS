using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ProjectSS.Expedition;

public static class ApplyRoundedCellSkin
{
    const string FontPath = "Assets/Fonts/Arial Rounded Bold SDF.asset";
    const string CellPath = "Assets/Textures/UI/Common/Cells/bg_cell.png";
    static int texts, cells;

    public static string Execute()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop play first.");
        AssetDatabase.Refresh();
        var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Arial Rounded Bold.ttf");
        var korean = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Prototype/Art/ExpeditionKorean.asset");
        if (source == null || korean == null) throw new InvalidOperationException("Required fonts missing.");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            font = TMP_FontAsset.CreateFontAsset(source, 64, 7,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            font.name = "Arial Rounded Bold SDF";
            AssetDatabase.CreateAsset(font, FontPath);
            font.material.name = "Arial Rounded Bold SDF Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures)
            {
                atlas.name = "Arial Rounded Bold Atlas";
                AssetDatabase.AddObjectToAsset(atlas, font);
            }
        }
        font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { korean };
        font.TryAddCharacters(new string(Enumerable.Range(32, 95).Select(i => (char)i).ToArray()));
        EditorUtility.SetDirty(font);
        const string outlinePath = "Assets/Fonts/Arial Rounded Bold SDF Outline.mat";
        var outline = AssetDatabase.LoadAssetAtPath<Material>(outlinePath);
        if (outline == null)
        {
            outline = new Material(font.material);
            outline.name = "Arial Rounded Bold SDF Outline";
            AssetDatabase.CreateAsset(outline, outlinePath);
        }
        outline.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.025f, 0.035f, 0.045f, 1));
        outline.SetFloat(ShaderUtilities.ID_OutlineWidth, .14f);
        outline.EnableKeyword("OUTLINE_ON");
        EditorUtility.SetDirty(outline);
        var tmpSettings = new SerializedObject(TMP_Settings.instance);
        tmpSettings.FindProperty("m_matchMaterialPreset").boolValue = true;
        tmpSettings.ApplyModifiedPropertiesWithoutUndo();
        var importer = (TextureImporter)AssetImporter.GetAtPath(CellPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        // Leave a stretchable center while preserving the supplied rounded corners.
        importer.spriteBorder = new Vector4(40, 40, 40, 40);
        importer.spritePixelsPerUnit = 100;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CellPath);
        texts = cells = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/Prefabs" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = false;
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    text.font = font;
                    text.fontSharedMaterial = outline;
                    // The source face is already bold; avoid synthetic double bolding.
                    text.fontStyle &= ~FontStyles.Bold;
                    text.fontWeight = FontWeight.Regular;
                    text.UpdateMeshPadding();
                    texts++; changed = true;
                }
                var page = root.GetComponent<PlayPage>();
                if (page != null)
                {
                    foreach (var row in page.inventoryView.gearRows.Concat(page.inventoryView.materialRows)) Skin(row.root.GetComponent<Image>(), sprite);
                    foreach (var icon in page.slotIcons) Skin(icon.transform.parent.GetComponent<Image>(), sprite);
                    foreach (var im in page.heroGrid.GetComponentsInChildren<Image>(true))
                        if (im.name.StartsWith("HeroCard") || im.name.StartsWith("FormationSlot")) Skin(im, sprite);
                    foreach (var panel in page.menuPanels)
                        foreach (var im in panel.GetComponentsInChildren<Image>(true))
                            if (im.name.EndsWith("Preview")) Skin(im, sprite);
                    changed = true;
                }
                var equipment = root.GetComponent<EquipmentSelectionPopup>();
                if (equipment != null) foreach (var row in equipment.rows) { Skin(row.root.GetComponent<Image>(), sprite); changed = true; }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        return $"Applied Arial Rounded Bold Outline preset with Korean fallback to {texts} texts; bg_cell to {cells} cells.";
    }

    static void Skin(Image image, Sprite sprite)
    {
        if (image == null) throw new InvalidOperationException("Cell background missing.");
        image.sprite = sprite;
        image.overrideSprite = null;
        image.material = null;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 2;
        image.color = Color.white;
        image.preserveAspect = false;
        if (image.name.StartsWith("FormationSlot"))
            foreach (var label in image.GetComponentsInChildren<TMP_Text>(true))
                label.color = new Color(.14f, .24f, .29f);
        var surface = image.transform.Find("Surface");
        if (surface != null) surface.gameObject.SetActive(false);
        cells++;
    }
}
