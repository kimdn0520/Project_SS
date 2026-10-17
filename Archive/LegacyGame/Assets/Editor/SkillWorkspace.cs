using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectSS.Expedition;

namespace ProjectSS.ContentEditor
{
    [Serializable]
    internal sealed class SkillEditSession
    {
        public SkillDefinition source, draft;
        public string sourcePath, baseline, draftBaseline;
        public bool IsNew => string.IsNullOrEmpty(sourcePath);
        public bool Dirty => draft != null && (IsNew || JsonUtility.ToJson(draft) != draftBaseline);
    }

    [Serializable]
    internal sealed class SkillWorkspace
    {
        public List<SkillEditSession> edits = new List<SkillEditSession>();
        public int selected = -1;
        public SkillEditSession Current => selected >= 0 && selected < edits.Count ? edits[selected] : null;
        public bool Dirty => edits.Any(e => e.Dirty);

        public void Select(SkillDefinition source)
        {
            selected = edits.FindIndex(e => e.source == source && !e.IsNew);
            if (selected >= 0) return;
            var draft = UnityEngine.Object.Instantiate(source);
            draft.name = source.name;
            draft.hideFlags = HideFlags.HideAndDontSave & ~HideFlags.NotEditable;
            edits.Add(new SkillEditSession { source = source, draft = draft, sourcePath = AssetDatabase.GetAssetPath(source),
                baseline = EditorJsonUtility.ToJson(source), draftBaseline = JsonUtility.ToJson(draft) });
            selected = edits.Count - 1;
        }

        public void Create(GearSkillDefinition settings = null, Sprite icon = null)
        {
            var draft = ScriptableObject.CreateInstance<SkillDefinition>();
            draft.name = "NewSkill";
            draft.hideFlags = HideFlags.HideAndDontSave & ~HideFlags.NotEditable;
            draft.icon = icon;
            draft.settings = settings == null ? new GearSkillDefinition { enabled = true, title = "새 스킬", effect = GearSkillEffect.Damage } :
                JsonUtility.FromJson<GearSkillDefinition>(JsonUtility.ToJson(settings));
            draft.settings.id = "skill_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            edits.Add(new SkillEditSession { draft = draft });
            selected = edits.Count - 1;
        }

        public SkillDefinition Resolve(SkillDefinition source) => edits.FirstOrDefault(e => e.source == source && !e.IsNew)?.draft ?? source;

        public List<string> Errors()
        {
            var errors = new List<string>();
            var candidates = SkillLibrary.All.Select(Resolve).Concat(edits.Where(e => e.IsNew).Select(e => e.draft)).Where(s => s != null).ToArray();
            foreach (var e in edits.Where(e => e.Dirty))
            {
                if (!e.IsNew && (e.source == null || EditorJsonUtility.ToJson(e.source) != e.baseline))
                    errors.Add($"{e.draft.settings.title}: 원본 스킬이 외부에서 변경되거나 삭제됐습니다. 되돌리기로 다시 불러오세요.");
                errors.AddRange(SkillLibrary.Errors(e.draft.settings).Select(s => e.draft.settings.title + ": " + s));
                if (candidates.Count(s => string.Equals(s.settings?.id?.Trim(), e.draft.settings.id?.Trim(), StringComparison.OrdinalIgnoreCase)) > 1)
                    errors.Add($"중복 스킬 ID: {e.draft.settings.id}");
            }
            return errors;
        }

        // Called only after the complete workspace (gear and skills) passes validation.
        public void Save()
        {
            foreach (var e in edits.Where(e => e.Dirty))
            {
                if (e.IsNew)
                {
                    e.source = SkillLibrary.CreateAsset(e.draft.settings, e.draft.icon);
                    e.sourcePath = AssetDatabase.GetAssetPath(e.source);
                }
                else
                {
                    Undo.RecordObject(e.source, "Save shared skill");
                    e.source.settings = JsonUtility.FromJson<GearSkillDefinition>(JsonUtility.ToJson(e.draft.settings));
                    e.source.icon = e.draft.icon;
                    EditorUtility.SetDirty(e.source);
                    AssetDatabase.SaveAssetIfDirty(e.source);
                }
                e.baseline = EditorJsonUtility.ToJson(e.source);
                e.draftBaseline = JsonUtility.ToJson(e.draft);
            }
            SkillLibrary.Refresh();
        }

        public void Dispose()
        {
            foreach (var e in edits) if (e.draft != null) { Undo.ClearUndo(e.draft); UnityEngine.Object.DestroyImmediate(e.draft); }
            edits.Clear(); selected = -1;
        }
    }

    [InitializeOnLoad]
    internal static class SkillLibrary
    {
        private static SkillDefinition[] cache;
        private static ExpeditionCatalog[] catalogs;
        public static ExpeditionCatalog[] Catalogs => catalogs ?? (catalogs = AssetDatabase.FindAssets("t:ExpeditionCatalog")
            .Select(g => AssetDatabase.LoadAssetAtPath<ExpeditionCatalog>(AssetDatabase.GUIDToAssetPath(g))).Where(c => c != null).ToArray());
        public static SkillDefinition[] All => cache ?? (cache = AssetDatabase.FindAssets("t:SkillDefinition")
            .Select(g => AssetDatabase.LoadAssetAtPath<SkillDefinition>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(s => s != null).OrderBy(s => s.settings?.title).ToArray());
        static SkillLibrary() { EditorApplication.projectChanged += Refresh; }
        public static void Refresh() { cache = null; catalogs = null; }

        public static SkillDefinition CreateAsset(GearSkillDefinition settings, Sprite icon)
        {
            const string folder = "Assets/Prototype/Skills";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Prototype", "Skills");
            var asset = ScriptableObject.CreateInstance<SkillDefinition>();
            asset.settings = JsonUtility.FromJson<GearSkillDefinition>(JsonUtility.ToJson(settings));
            asset.icon = icon;
            string filename = string.Concat((settings.id ?? "skill").Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_'));
            if (string.IsNullOrEmpty(filename)) filename = "skill";
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + filename + ".asset"));
            AssetDatabase.SaveAssetIfDirty(asset);
            Refresh(); return asset;
        }

        public static GearSkillDefinition FrostSettings() => new GearSkillDefinition { enabled = true, id = "frost_nova", title = "서리 폭풍",
            description = "10초마다 반경 3 내 적을 2초간 빙결시킨다.", effect = GearSkillEffect.Freeze, duration = 2, radius = 3, cooldown = 10 };

        public static SkillDefinition ImportLegacy(GearSkillDefinition settings, Sprite icon)
        {
            // Reuse only an identical definition. An ID collision must never overwrite another skill.
            var existing = All.FirstOrDefault(s => JsonUtility.ToJson(s.settings) == JsonUtility.ToJson(settings) && s.icon == icon);
            if (existing != null) return existing;
            var copy = JsonUtility.FromJson<GearSkillDefinition>(JsonUtility.ToJson(settings));
            if (string.IsNullOrWhiteSpace(copy.id)) copy.id = "skill_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            if (All.Any(s => string.Equals(s.settings.id, copy.id, StringComparison.OrdinalIgnoreCase)))
                copy.id += "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            return CreateAsset(copy, icon);
        }

        public static List<string> Errors(GearSkillDefinition s)
        {
            var errors = new List<string>();
            if (s == null) { errors.Add("스킬 설정이 없습니다."); return errors; }
            if (string.IsNullOrWhiteSpace(s.id) || string.IsNullOrWhiteSpace(s.title) || s.effect == GearSkillEffect.None) errors.Add("스킬 ID·이름·효과를 설정하세요.");
            if (!Enum.IsDefined(typeof(GearSkillEffect), s.effect) || !Enum.IsDefined(typeof(GearSkillTarget), s.target) || !Enum.IsDefined(typeof(GearSkillTrigger), s.trigger)) errors.Add("스킬 효과·대상·발동 조건을 확인하세요.");
            if (!Finite(s.cooldown) || s.cooldown <= 0 || !Finite(s.chance) || s.chance <= 0 || s.chance > 100 || !NonNegative(s.duration) || !NonNegative(s.radius) || !NonNegative(s.power)) errors.Add("쿨타임 > 0, 확률 0~100% (0 제외), 지속 시간·반경·수치 ≥ 0이어야 합니다.");
            if (s.target == GearSkillTarget.EnemyArea && s.radius <= 0) errors.Add("광역 스킬의 효과 반경은 0보다 커야 합니다.");
            if ((s.effect == GearSkillEffect.Freeze || s.effect == GearSkillEffect.Burn) && s.duration <= 0) errors.Add("빙결·화상의 지속 시간은 0보다 커야 합니다.");
            return errors;
        }
        private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        private static bool NonNegative(float n) => Finite(n) && n >= 0;
    }
}
