using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ProjectSS.Expedition;
using UnityEditor;
using UnityEngine;

public static class EquipmentEditorQA
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    public static object Execute()
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetType("ProjectSS.ContentEditor.ContentManagementWindow") != null);
        var windowType = assembly.GetType("ProjectSS.ContentEditor.ContentManagementWindow");
        var validation = assembly.GetType("ProjectSS.ContentEditor.GearCatalogValidation");
        var validate = validation.GetMethod("Errors", BindingFlags.Static | BindingFlags.NonPublic);
        var original = AssetDatabase.LoadAssetAtPath<ExpeditionCatalog>("Assets/Prototype/ExpeditionCatalog.asset");
        string originalJson = EditorJsonUtility.ToJson(original);
        var catalog = UnityEngine.Object.Instantiate(original);
        string path = "Assets/EquipmentEditorQA_" + Guid.NewGuid().ToString("N") + ".asset";
        EditorWindow window = null;
        var checks = new List<string>();
        string createdSkillPath = null;
        try
        {
            AssetDatabase.CreateAsset(catalog, path);
            window = (EditorWindow)ScriptableObject.CreateInstance(windowType);
            windowType.GetMethod("SetSource", Hidden).Invoke(window, new object[] { catalog });
            var draft = (ExpeditionCatalog)windowType.GetField("draft", Hidden).GetValue(window);
            Check(new SerializedObject(draft).FindProperty("gear").editable, "draft properties editable in UI", checks);
            Check(((List<string>)validate.Invoke(null, new object[] { draft, null })).Count == 0, "legacy catalog validates", checks);
            var panelType = assembly.GetType("ProjectSS.ContentEditor.EquipmentManagementPanel");
            var panel = Activator.CreateInstance(panelType, true);
            panelType.GetMethod("AddFrost", Hidden).Invoke(panel, new object[] { window });
            Check(draft.gear.Length == original.gear.Length + 1 && catalog.gear.Length == original.gear.Length, "draft isolates additions", checks);
            var frost = draft.gear.Last();
            // Work on a QA-owned skill so checks cannot modify a user's shared example.
            var isolatedSkill = UnityEngine.Object.Instantiate(frost.skillAsset);
            createdSkillPath = "Assets/EquipmentSkillQA_" + Guid.NewGuid().ToString("N") + ".asset";
            AssetDatabase.CreateAsset(isolatedSkill, createdSkillPath);
            frost.skillAsset = isolatedSkill;
            Check(frost.rarity == GearRarity.Legendary && frost.maxRandomOptions == 1 && frost.ResolvedSkill.effect == GearSkillEffect.Freeze && frost.ResolvedSkill.duration == 2, "frost template", checks);
            Check(((List<string>)validate.Invoke(null, new object[] { draft, null })).Count == 0, "frost template validates", checks);
            frost.rarity = GearRarity.Epic;
            Check(((List<string>)validate.Invoke(null, new object[] { draft, null })).Any(s => s.Contains("전설")), "reject skill below legendary", checks);
            frost.rarity = GearRarity.Legendary;
            frost.randomOptions[0].min = 100;
            Check(((List<string>)validate.Invoke(null, new object[] { draft, null })).Any(s => s.Contains("최소값")), "reject inverted range", checks);
            frost.randomOptions[0].min = 3;
            frost.ResolvedSkill.chance = float.NaN;
            Check(((List<string>)validate.Invoke(null, new object[] { draft, null })).Count > 0, "reject non-finite skill values", checks);
            frost.ResolvedSkill.chance = 100;
            panelType.GetMethod("Add", Hidden).Invoke(panel, new object[] { window, true });
            var copy = draft.gear.Last();
            Check(copy.id != frost.id && copy.skillAsset == frost.skillAsset && copy.randomOptions[0] != frost.randomOptions[0], "duplicate has unique ID and independent nested data", checks);
            string copyId = copy.id; copy.id = frost.id;
            Check(((List<string>)validate.Invoke(null, new object[] { draft, null })).Any(s => s.Contains("중복 ID")), "reject duplicate ID", checks);
            copy.id = copyId;
            window.SaveChanges();
            Check(!window.hasUnsavedChanges && catalog.gear.Length == draft.gear.Length, "save completes", checks);
            Check(catalog.gear.Last().ResolvedSkill.duration == 2 && catalog.gear.Last().randomOptions[0].min == 3, "nested data saved", checks);
            Check(catalog.gear[0].icon == original.gear[0].icon && catalog.gear[0].prefab == original.gear[0].prefab, "Unity references preserved", checks);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var reloaded = AssetDatabase.LoadAssetAtPath<ExpeditionCatalog>(path);
            Check(reloaded.gear.Last().ResolvedSkill.title == "서리 폭풍", "disk round-trip", checks);
            var serialized = new SerializedObject(draft);
            serialized.FindProperty("gear").GetArrayElementAtIndex(0).FindPropertyRelative("damage").floatValue = 123;
            serialized.ApplyModifiedProperties(); Undo.FlushUndoRecordObjects();
            windowType.GetMethod("Changed", Hidden).Invoke(window, null);
            window.SaveChanges();
            Undo.PerformUndo();
            // Undoing the source save is detected as an external change, so reload before the next edit.
            window.DiscardChanges();
            draft = (ExpeditionCatalog)windowType.GetField("draft", Hidden).GetValue(window);
            float before = draft.gear[0].damage;
            Undo.IncrementCurrentGroup();
            var edit = new SerializedObject(draft);
            edit.FindProperty("gear").GetArrayElementAtIndex(0).FindPropertyRelative("damage").floatValue = before + 3;
            edit.ApplyModifiedProperties(); Undo.FlushUndoRecordObjects();
            windowType.GetMethod("Changed", Hidden).Invoke(window, null);
            Undo.PerformUndo();
            Check(draft.gear[0].damage == before && !window.hasUnsavedChanges, "undo restores draft and clean state", checks);
            Undo.PerformRedo();
            Check(draft.gear[0].damage == before + 3 && window.hasUnsavedChanges, "redo restores dirty state", checks);
            window.DiscardChanges();
            Check(!window.hasUnsavedChanges, "discard clears unsaved state", checks);
            var save = ExpeditionSave.Fresh(original.gear.Length);
            save.iron = 57; save.cleared = 12;
            Check(save.TryExpandInventory(original.gear.Length + 2) && save.iron == 57 && save.cleared == 12 && save.inventory[0] == 1 && save.inventory.Last() == 0, "append preserves player progress", checks);
            Check(!save.TryExpandInventory(3), "reject catalog shrink", checks);
            save.inventory[0] = -1;
            Check(!save.TryExpandInventory(save.inventory.Length + 1), "reject invalid save expansion", checks);
            Check(EditorJsonUtility.ToJson(original) == originalJson, "original catalog untouched", checks);
            Directory.CreateDirectory("PrototypeQA");
            File.WriteAllLines("PrototypeQA/equipment-editor-checks.txt", checks);
            return new { passed = checks.Count, checks };
        }
        finally
        {
            if (window != null) { window.DiscardChanges(); UnityEngine.Object.DestroyImmediate(window); }
            Undo.ClearUndo(catalog);
            AssetDatabase.DeleteAsset(path);
            if (createdSkillPath != null) AssetDatabase.DeleteAsset(createdSkillPath);
        }
    }

    public static object Open()
    {
        EditorApplication.ExecuteMenuItem("ProjectSS/콘텐츠 관리");
        var window = Resources.FindObjectsOfTypeAll<EditorWindow>().First(w => w.GetType().Name == "ContentManagementWindow");
        window.GetType().GetField("tab", Hidden).SetValue(window, 0);
        window.position = new Rect(70, 70, 1280, 900);
        window.Focus(); window.Repaint();
        return new { window = window.titleContent.text, width = window.position.width, height = window.position.height };
    }

    public static object Capture()
    {
        var window = Resources.FindObjectsOfTypeAll<EditorWindow>().First(w => w.GetType().Name == "ContentManagementWindow");
        var rect = window.position;
        window.Focus();
        var texture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
        try
        {
            texture.SetPixels(UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(rect.position, texture.width, texture.height));
            texture.Apply();
            Directory.CreateDirectory("PrototypeQA");
            File.WriteAllBytes("PrototypeQA/equipment-editor.png", texture.EncodeToPNG());
        }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
        return "PrototypeQA/equipment-editor.png";
    }
    private static void Check(bool pass, string label, List<string> checks)
    {
        if (!pass) throw new Exception("Equipment editor QA failed: " + label);
        checks.Add("PASS " + label);
    }
}
