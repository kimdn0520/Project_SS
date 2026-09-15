using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectSS.Expedition;

namespace ProjectSS.ContentEditor
{
    internal sealed class SkillManagementPanel : IContentManagementPanel
    {
        public string Title => "스킬";
        internal static readonly string[] Effects = { "없음", "빙결", "화상", "피해", "회복", "보호막" };
        internal static readonly string[] Triggers = { "쿨타임마다", "공격 시", "치명타 시" };
        internal static readonly string[] Targets = { "단일 적", "범위 내 적", "자신", "아군" };
        private string search = "";
        private int effectFilter;
        private Vector2 listScroll, detailScroll;

        public void Draw(ContentManagementWindow host)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(290), GUILayout.ExpandHeight(true)))
                {
                    GUILayout.Label("스킬 라이브러리", EditorStyles.boldLabel);
                    search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
                    effectFilter = EditorGUILayout.Popup(effectFilter, new[] { "모든 효과" }.Concat(Effects).ToArray());
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("+ 새 스킬")) { host.skills.Create(); Created(host); }
                        using (new EditorGUI.DisabledScope(host.skills.Current == null))
                            if (GUILayout.Button("선택 복제"))
                            {
                                var current = host.skills.Current.draft;
                                host.skills.Create(current.settings, current.icon);
                                host.skills.Current.draft.settings.title += " (복사)"; Created(host);
                            }
                    }
                    if (GUILayout.Button("+ 서리 폭풍 예시")) { host.skills.Create(SkillLibrary.FrostSettings()); Created(host); }
                    listScroll = EditorGUILayout.BeginScrollView(listScroll);
                    foreach (var skill in SkillLibrary.All)
                    {
                        var edited = host.skills.Resolve(skill);
                        if (Matches(edited)) DrawRow(host, edited, () => host.skills.Select(skill), host.skills.Current?.source == skill);
                    }
                    foreach (var session in host.skills.edits.Where(e => e.IsNew))
                    {
                        if (Matches(session.draft)) DrawRow(host, session.draft, () => host.skills.selected = host.skills.edits.IndexOf(session), host.skills.Current == session, true);
                    }
                    EditorGUILayout.EndScrollView();
                    GUILayout.Label("저장한 스킬을 장비에서 선택할 수 있습니다.", EditorStyles.wordWrappedMiniLabel);
                }
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    var session = host.skills.Current;
                    if (session == null || session.draft == null)
                    {
                        EditorGUILayout.HelpBox("스킬을 선택하거나 새로 만드세요. 저장된 스킬은 여러 장비가 함께 사용할 수 있습니다.", MessageType.Info);
                        GUILayout.FlexibleSpace(); return;
                    }
                    detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
                    GUILayout.Label(session.draft.settings.title, new GUIStyle(EditorStyles.boldLabel) { fontSize = 20 });
                    GUILayout.Space(10);
                    EditorGUILayout.HelpBox(session.IsNew ? "새 스킬입니다. 전체 저장 후 장비에 연결할 수 있습니다." :
                        "이 스킬을 수정해 저장하면 연결된 모든 장비에 같은 설정이 반영됩니다.", MessageType.Info);
                    var data = new SerializedObject(session.draft);
                    data.Update();
                    EditorGUI.BeginChangeCheck();
                    Field(data.FindProperty("icon"), "스킬 아이콘");
                    var p = data.FindProperty("settings");
                    Field(p.FindPropertyRelative("id"), "스킬 ID"); Field(p.FindPropertyRelative("title"), "스킬 이름");
                    Field(p.FindPropertyRelative("description"), "스킬 설명");
                    GUILayout.Space(12); GUILayout.Label("발동 / 효과", EditorStyles.boldLabel);
                    Popup(p, "effect", "효과", Effects); Popup(p, "trigger", "발동 조건", Triggers); Popup(p, "target", "대상", Targets);
                    Field(p.FindPropertyRelative("cooldown"), "재사용 대기시간 (초)"); Field(p.FindPropertyRelative("chance"), "발동 확률 (%)");
                    Field(p.FindPropertyRelative("duration"), "지속 시간 (초)"); Field(p.FindPropertyRelative("radius"), "효과 반경");
                    Field(p.FindPropertyRelative("power"), "효과 수치"); Field(p.FindPropertyRelative("visualPrefab"), "이펙트 프리팹");
                    if (EditorGUI.EndChangeCheck()) { data.ApplyModifiedProperties(); host.Changed(); }
                    else data.ApplyModifiedProperties();
                    foreach (var error in SkillLibrary.Errors(session.draft.settings)) EditorGUILayout.HelpBox(error, MessageType.Error);
                    GUILayout.Space(15); GUILayout.Label("연결된 장비", EditorStyles.boldLabel);
                    if (session.source == null) GUILayout.Label("저장 후 장비 탭에서 연결하세요.");
                    else
                    {
                        int count = 0;
                        foreach (var catalog in SkillLibrary.Catalogs)
                        {
                            var shown = catalog == host.source ? host.draft : catalog;
                            if (shown?.gear == null) continue;
                            foreach (var gear in shown.gear.Where(g => g != null && g.skillAsset == session.source))
                            {
                                GUILayout.Label($"{gear.title}  ·  {catalog.name}"); count++;
                            }
                        }
                        if (count == 0) GUILayout.Label("아직 연결된 장비가 없습니다.");
                        if (GUILayout.Button("스킬 에셋 위치 보기")) EditorGUIUtility.PingObject(session.source);
                    }
                    GUILayout.Space(12);
                    EditorGUILayout.HelpBox("스킬 효과와 발동 조건을 저장하는 관리 도구입니다. 실제 전투 발동은 전투 시스템에서 연결합니다.", MessageType.None);
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private bool Matches(SkillDefinition s) => (effectFilter == 0 || (int)s.settings.effect == effectFilter - 1) &&
            (string.IsNullOrWhiteSpace(search) || ($"{s.settings.title} {s.settings.id} {s.settings.description}").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
        private static void DrawRow(ContentManagementWindow host, SkillDefinition skill, Action select, bool selected, bool isNew = false)
        {
            var rect = GUILayoutUtility.GetRect(260, 58, GUILayout.ExpandWidth(true));
            if (GUI.Button(rect, GUIContent.none)) { select(); if (Event.current != null) GUI.FocusControl(null); }
            if (selected) EditorGUI.DrawRect(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), new Color(.20f,.37f,.52f,.8f));
            Texture preview = skill.icon != null ? AssetPreview.GetAssetPreview(skill.icon) ?? AssetPreview.GetMiniThumbnail(skill.icon) : EditorGUIUtility.IconContent("ScriptableObject Icon").image;
            if (preview != null) GUI.DrawTexture(new Rect(rect.x + 7, rect.y + 7, 42, 42), preview, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(rect.x + 56, rect.y + 8, rect.width - 60, 20), skill.settings.title + (isNew ? "  +" : ""), EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.x + 56, rect.y + 30, rect.width - 60, 18), Effects[Mathf.Clamp((int)skill.settings.effect, 0, Effects.Length - 1)] + $" · {skill.settings.cooldown:0.#}초", EditorStyles.miniLabel);
        }
        private void Created(ContentManagementWindow host) { search = ""; effectFilter = 0; detailScroll = Vector2.zero; host.Changed(); }
        private static void Field(SerializedProperty p, string label) => EditorGUILayout.PropertyField(p, new GUIContent(label), true);
        private static void Popup(SerializedProperty p, string name, string label, string[] choices)
        {
            var value = p.FindPropertyRelative(name); value.intValue = EditorGUILayout.Popup(label, value.intValue, choices);
        }
    }
}
