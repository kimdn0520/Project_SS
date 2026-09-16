using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectSS.Expedition;

namespace ProjectSS.ContentEditor
{
    // Register another panel here to extend the same workspace without changing the equipment editor.
    public interface IContentManagementPanel
    {
        string Title { get; }
        void Draw(ContentManagementWindow host);
    }

    public sealed class ContentManagementWindow : EditorWindow
    {
        [SerializeField] internal ExpeditionCatalog source;
        [SerializeField] internal ExpeditionCatalog draft;
        [SerializeField] internal int selected;
        [SerializeField] private int tab;
        [SerializeField] private string baseline;
        [SerializeField] private string draftBaseline;
        [SerializeField] internal SkillWorkspace skills = new SkillWorkspace();
        internal bool GearDirty => draft != null && JsonUtility.ToJson(draft) != draftBaseline;
        private IContentManagementPanel[] panels;
        private string status = "장비를 선택해 편집하세요.";

        [MenuItem("ProjectSS/콘텐츠 관리", priority = 0)]
        public static void Open() => GetWindow<ContentManagementWindow>("콘텐츠 관리").Show();

        private void OnEnable()
        {
            minSize = new Vector2(940, 620);
            panels = new IContentManagementPanel[] { new EquipmentManagementPanel(),
                new PlannedPanel("캐릭터"), new PlannedPanel("몬스터"), new SkillManagementPanel(),
                new PlannedPanel("스테이지"), new PlannedPanel("상점") };
            Undo.undoRedoPerformed += OnUndoRedo;
            if (draft == null)
            {
                if (source == null)
                    source = AssetDatabase.LoadAssetAtPath<ExpeditionCatalog>("Assets/Prototype/ExpeditionCatalog.asset");
                LoadDraft();
            }
            else draft.hideFlags &= ~HideFlags.NotEditable;
            saveChangesMessage = "장비와 스킬의 변경 내용을 모두 저장하시겠습니까?";
        }

        private void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;
        private void OnUndoRedo()
        {
            hasUnsavedChanges = GearDirty || skills.Dirty;
            Repaint();
        }
        private void OnDestroy() { if (draft != null) DestroyImmediate(draft); skills.Dispose(); }
        internal void ShowSkills(SkillDefinition skill = null)
        {
            if (skill != null) skills.Select(skill);
            tab = 3; Repaint();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("PROJECT SS  /  콘텐츠 관리", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(hasUnsavedChanges ? "● 저장하지 않은 변경" : "저장됨", EditorStyles.miniLabel);
            }
            GUILayout.Space(8);
            tab = GUILayout.Toolbar(Mathf.Clamp(tab, 0, panels.Length - 1), panels.Select(p => p.Title).ToArray(), GUILayout.Height(32));
            GUILayout.Space(8);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                panels[tab].Draw(this);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorGUILayout.HelpBox("플레이 모드를 종료하면 장비를 편집할 수 있습니다.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if (GUILayout.Button("전체 저장", EditorStyles.toolbarButton, GUILayout.Width(75))) SaveChanges();
                    if (GUILayout.Button("전체 검증", EditorStyles.toolbarButton, GUILayout.Width(80))) Validate();
                    if (GUILayout.Button("전체 되돌리기", EditorStyles.toolbarButton, GUILayout.Width(100)) &&
                        (!hasUnsavedChanges || EditorUtility.DisplayDialog("변경 되돌리기", "장비와 스킬의 저장하지 않은 편집을 모두 버리고 마지막 저장 상태를 불러옵니다.", "되돌리기", "취소")))
                        DiscardChanges();
                }
                GUILayout.Label(status, EditorStyles.miniLabel);
            }
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.S &&
                (Event.current.control || Event.current.command))
            {
                SaveChanges(); Event.current.Use();
            }
        }

        internal void SetSource(ExpeditionCatalog catalog)
        {
            if (catalog == source) return;
            if (GearDirty)
            {
                int choice = EditorUtility.DisplayDialogComplex("카탈로그 변경", "현재 편집 내용을 저장하시겠습니까?", "저장", "취소", "버리기");
                if (choice == 1) return;
                if (choice == 0) { SaveChanges(); if (GearDirty) return; }
            }
            source = catalog; selected = 0; LoadDraft();
        }

        private void LoadDraft()
        {
            if (draft != null) DestroyImmediate(draft);
            draft = source == null ? null : Instantiate(source);
            baseline = source == null ? "" : EditorJsonUtility.ToJson(source);
            if (draft != null) { draft.name = source.name; draft.hideFlags = HideFlags.HideAndDontSave & ~HideFlags.NotEditable; }
            draftBaseline = draft == null ? "" : JsonUtility.ToJson(draft);
            hasUnsavedChanges = skills.Dirty;
        }

        internal void Changed() { hasUnsavedChanges = true; status = "편집 중 · 저장 버튼 또는 Ctrl+S"; Repaint(); }

        public override void DiscardChanges()
        {
            var selectedSkill = skills.Current?.source;
            skills.Dispose(); LoadDraft(); SkillLibrary.Refresh();
            if (selectedSkill != null) skills.Select(selectedSkill);
            status = "장비와 스킬을 마지막 저장 상태로 되돌렸습니다."; base.DiscardChanges();
        }

        private void Validate()
        {
            var errors = draft == null ? new List<string>() : GearCatalogValidation.Errors(draft, skills.Resolve);
            errors.AddRange(skills.Errors());
            foreach (var skill in SkillLibrary.All) errors.AddRange(SkillLibrary.Errors(skills.Resolve(skill).settings).Select(e => skill.name + ": " + e));
            status = errors.Count == 0 ? "장비·스킬 검증 통과" : $"오류 {errors.Count}개 · 상세 내용 확인";
            EditorUtility.DisplayDialog("콘텐츠 검증", errors.Count == 0 ? "장비와 스킬 설정이 유효합니다." : string.Join("\n", errors), "확인");
        }

        public override void SaveChanges()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var errors = GearDirty ? GearCatalogValidation.Errors(draft, skills.Resolve) : new List<string>();
            errors.AddRange(skills.Errors());
            if (errors.Count > 0)
            {
                status = $"저장 보류 · 오류 {errors.Count}개";
                EditorUtility.DisplayDialog("설정을 확인해주세요", string.Join("\n", errors), "확인"); return;
            }
            if (GearDirty && (source == null || EditorJsonUtility.ToJson(source) != baseline))
            {
                status = "외부 변경 감지 · 되돌리기로 최신 데이터를 불러오세요.";
                EditorUtility.DisplayDialog("외부 변경 감지", "다른 Inspector 또는 도구에서 원본 카탈로그가 변경됐습니다. 덮어쓰기를 방지하기 위해 저장을 중단했습니다. 편집 내용을 확인한 뒤 되돌리기로 최신 데이터를 불러오세요.", "확인"); return;
            }
            skills.Save();
            if (GearDirty)
            {
                Undo.RecordObject(source, "Save equipment catalog");
                source.weaponCapacity=draft.weaponCapacity;source.armorCapacity=draft.armorCapacity;source.accessoryCapacity=draft.accessoryCapacity;source.dismantleRefundRate=draft.dismantleRefundRate;
                source.gear = draft.gear.Select(g => JsonUtility.FromJson<GearDefinition>(JsonUtility.ToJson(g))).ToArray();
                source.materials = (draft.materials ?? Array.Empty<MaterialDefinition>()).Select(m => JsonUtility.FromJson<MaterialDefinition>(JsonUtility.ToJson(m))).ToArray();
                EditorUtility.SetDirty(source);
                AssetDatabase.SaveAssetIfDirty(source);
                baseline = EditorJsonUtility.ToJson(source);
                draftBaseline = JsonUtility.ToJson(draft);
            }
            status = $"장비·스킬 저장 완료 · {DateTime.Now:HH:mm:ss}";
            base.SaveChanges();
        }

        private sealed class PlannedPanel : IContentManagementPanel
        {
            public string Title { get; }
            public PlannedPanel(string title) { Title = title; }
            public void Draw(ContentManagementWindow host)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(Title + " 관리", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 22 });
                GUILayout.Space(12);
                GUILayout.Label("추후 확장할 메뉴입니다. 현재는 장비와 스킬 관리를 사용할 수 있습니다.", new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter });
                GUILayout.FlexibleSpace();
            }
        }
    }
}
