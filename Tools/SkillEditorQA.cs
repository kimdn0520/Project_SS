using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ProjectSS.Expedition;

public static class SkillEditorQA
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method).Invoke(target, args);
    static object Current(object workspace) => workspace.GetType().GetProperty("Current").GetValue(workspace);
    static SkillDefinition Draft(object workspace) => (SkillDefinition)Current(workspace).GetType().GetField("draft").GetValue(Current(workspace));
    static SkillDefinition Source(object session) => (SkillDefinition)session.GetType().GetField("source").GetValue(session);
    static List<string> Errors(object workspace) => (List<string>)Call(workspace, "Errors");
    static void Check(bool pass, string label, List<string> checks)
    {
        if (!pass) throw new Exception("Skill editor QA failed: " + label);
        checks.Add("PASS " + label);
    }

    public static object Execute()
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetType("ProjectSS.ContentEditor.ContentManagementWindow") != null);
        var windowType = assembly.GetType("ProjectSS.ContentEditor.ContentManagementWindow");
        var library = assembly.GetType("ProjectSS.ContentEditor.SkillLibrary");
        var original = AssetDatabase.LoadAssetAtPath<ExpeditionCatalog>("Assets/Prototype/ExpeditionCatalog.asset");
        string baseline = EditorJsonUtility.ToJson(original);
        var catalog = UnityEngine.Object.Instantiate(original);
        var paths = new List<string>();
        var checks = new List<string>();
        var path = "Assets/SkillEditorQA_" + Guid.NewGuid().ToString("N") + ".asset";
        EditorWindow window = null;
        try
        {
            AssetDatabase.CreateAsset(catalog, path); paths.Add(path);
            window = (EditorWindow)ScriptableObject.CreateInstance(windowType);
            windowType.GetMethod("SetSource", Hidden).Invoke(window, new object[] { catalog });
            var workspace = windowType.GetField("skills", Hidden).GetValue(window);
            var gearDraft = (ExpeditionCatalog)windowType.GetField("draft", Hidden).GetValue(window);
            var settings = new GearSkillDefinition { enabled = true, title = "QA 서리", effect = GearSkillEffect.Freeze, cooldown = 10, radius = 3, duration = 2 };
            Call(workspace, "Create", settings, original.gear[0].icon);
            var firstSession = Current(workspace);
            var firstDraft = Draft(workspace);
            Check(!EditorUtility.IsPersistent(firstDraft) && firstDraft.settings.id.StartsWith("skill_"), "new skill is isolated draft with unique ID", checks);
            Check(new SerializedObject(firstDraft).FindProperty("settings").editable, "skill fields editable", checks);
            Check(Errors(workspace).Count == 0, "new skill validates", checks);
            window.SaveChanges();
            var first = Source(firstSession); paths.Add(AssetDatabase.GetAssetPath(first));
            Check(EditorUtility.IsPersistent(first) && first.icon == original.gear[0].icon && !window.hasUnsavedChanges, "skill-only save persists icon and definition", checks);

            Call(workspace, "Create", firstDraft.settings, firstDraft.icon);
            var secondSession = Current(workspace);
            var secondDraft = Draft(workspace);
            Check(secondDraft.settings.id != first.settings.id && secondDraft.settings != firstDraft.settings, "skill duplication has independent data and unique ID", checks);
            string secondId = secondDraft.settings.id;
            secondDraft.settings.id = first.settings.id;
            Check(Errors(workspace).Any(e => e.Contains("중복")), "duplicate skill IDs rejected", checks);
            secondDraft.settings.id = secondId;
            secondDraft.settings.radius = 0;
            Check(Errors(workspace).Any(e => e.Contains("반경")), "invalid area skill rejected", checks);
            secondDraft.settings.radius = 3;
            secondDraft.settings.chance = float.NaN;
            Check(Errors(workspace).Count > 0, "non-finite skill settings rejected", checks);
            secondDraft.settings.chance = 50;
            Call(workspace, "Select", first);
            firstDraft.settings.cooldown = 15;
            Check(secondDraft.settings.chance == 50 && first.settings.cooldown == 10, "switching skills preserves independent unsaved drafts", checks);
            window.SaveChanges();
            var second = Source(secondSession); paths.Add(AssetDatabase.GetAssetPath(second));
            Check(first.settings.cooldown == 15 && second.settings.chance == 50, "save all persists multiple skill drafts", checks);

            gearDraft.gear[0].rarity = gearDraft.gear[1].rarity = GearRarity.Legendary;
            gearDraft.gear[0].skillAsset = gearDraft.gear[1].skillAsset = first;
            window.SaveChanges();
            Check(catalog.gear[0].skillAsset == first && catalog.gear[1].skillAsset == first, "multiple gear entries share one asset", checks);
            firstDraft.settings.duration = 4;
            window.SaveChanges();
            Check(catalog.gear[0].ResolvedSkill.duration == 4 && catalog.gear[1].ResolvedSkill.duration == 4, "shared edit propagates to all connected gear", checks);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Check(AssetDatabase.LoadAssetAtPath<ExpeditionCatalog>(path).gear[0].skillAsset == first, "gear reference survives disk round-trip", checks);
            var epic = new GearDefinition { rarity = GearRarity.Epic, skillAsset = first };
            Check(epic.ResolvedSkill == null, "runtime resolves equipment skills only at legendary or mythic", checks);

            var legacy = new GearSkillDefinition { enabled = true, id = first.settings.id, title = "QA 이전 설정", effect = GearSkillEffect.Freeze, duration = 7, radius = 6, chance = 35 };
            string legacyJson = JsonUtility.ToJson(legacy);
            var imported = (SkillDefinition)library.GetMethod("ImportLegacy").Invoke(null, new object[] { legacy, first.icon });
            paths.Add(AssetDatabase.GetAssetPath(imported));
            Check(imported.settings.id != first.settings.id && imported.settings.duration == 7 && imported.settings.chance == 35 && JsonUtility.ToJson(legacy) == legacyJson, "legacy migration preserves values and avoids ID collision", checks);
            var reused = (SkillDefinition)library.GetMethod("ImportLegacy").Invoke(null, new object[] { imported.settings, imported.icon });
            Check(reused == imported, "identical migration reuses existing asset", checks);

            Call(workspace, "Select", first);
            firstDraft.settings.cooldown = 16;
            first.settings.radius = 8;
            Check(Errors(workspace).Any(e => e.Contains("외부")), "external edits block overwrite", checks);
            first.settings.radius = 3;
            firstDraft.settings.cooldown = 15;
            Undo.IncrementCurrentGroup();
            var serialized = new SerializedObject(firstDraft);
            serialized.FindProperty("settings").FindPropertyRelative("cooldown").floatValue = 21;
            serialized.ApplyModifiedProperties(); Undo.FlushUndoRecordObjects();
            windowType.GetMethod("Changed", Hidden).Invoke(window, null);
            Undo.PerformUndo();
            Check(firstDraft.settings.cooldown == 15 && !window.hasUnsavedChanges, "skill undo restores clean state", checks);
            Undo.PerformRedo();
            Check(firstDraft.settings.cooldown == 21 && window.hasUnsavedChanges, "skill redo restores dirty state", checks);
            window.DiscardChanges();
            Check(!window.hasUnsavedChanges && first.settings.cooldown == 15, "discard leaves saved skill untouched", checks);
            Check(EditorJsonUtility.ToJson(original) == baseline, "original game catalog untouched", checks);
            Directory.CreateDirectory("PrototypeQA");
            File.WriteAllLines("PrototypeQA/skill-editor-checks.txt", checks);
            return new { passed = checks.Count, checks };
        }
        finally
        {
            if (window != null) { window.DiscardChanges(); UnityEngine.Object.DestroyImmediate(window); }
            foreach (var assetPath in paths.AsEnumerable().Reverse())
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (asset != null) Undo.ClearUndo(asset);
                AssetDatabase.DeleteAsset(assetPath);
            }
            library.GetMethod("Refresh").Invoke(null, null);
        }
    }

    public static object Open()
    {
        EditorApplication.ExecuteMenuItem("ProjectSS/콘텐츠 관리");
        var window = Resources.FindObjectsOfTypeAll<EditorWindow>().First(w => w.GetType().Name == "ContentManagementWindow");
        var sample = AssetDatabase.FindAssets("t:SkillDefinition").Select(g => AssetDatabase.LoadAssetAtPath<SkillDefinition>(AssetDatabase.GUIDToAssetPath(g)))
            .FirstOrDefault(s => s.settings.id == "frost_nova");
        window.GetType().GetMethod("ShowSkills", Hidden).Invoke(window, new object[] { sample });
        window.Repaint();
        return new { tab = "스킬", sample = sample != null ? sample.settings.title : "없음" };
    }
}
