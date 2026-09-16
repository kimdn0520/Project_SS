using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectSS.Expedition;

namespace ProjectSS.ContentEditor
{
    internal sealed class MaterialManagementPanel
    {
        int selected;
        Vector2 scroll, detailScroll;
        string search = "";
        public void Draw(ContentManagementWindow host)
        {
            if (host.draft.materials == null) host.draft.materials = Array.Empty<MaterialDefinition>();
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(290), GUILayout.ExpandHeight(true)))
                {
                    GUILayout.Label("재료 목록", EditorStyles.boldLabel);
                    search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("+ 새 재료")) Add(host, false);
                        using (new EditorGUI.DisabledScope(host.draft.materials.Length == 0))
                            if (GUILayout.Button("선택 복제")) Add(host, true);
                    }
                    scroll = EditorGUILayout.BeginScrollView(scroll);
                    for (int i = 0; i < host.draft.materials.Length; i++)
                    {
                        var item = host.draft.materials[i];
                        if (item == null || (item.title + " " + item.id).IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        if (GUILayout.Toggle(i == selected, item.title, "Button", GUILayout.Height(38))) selected = i;
                    }
                    EditorGUILayout.EndScrollView();
                }
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    if (host.draft.materials.Length == 0) { GUILayout.Label("새 재료를 추가하세요."); return; }
                    selected = Mathf.Clamp(selected, 0, host.draft.materials.Length - 1);
                    var serialized = new SerializedObject(host.draft);
                    serialized.Update();
                    var item = serialized.FindProperty("materials").GetArrayElementAtIndex(selected);
                    detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
                    GUILayout.Label("재료 정보", EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();
                    Field(item, "title", "이름");
                    // Stable IDs protect custom material counts stored in existing saves.
                    using (new EditorGUI.DisabledScope(true)) Field(item, "id", "고유 ID");
                    Field(item, "icon", "아이콘");
                    var rarity = item.FindPropertyRelative("rarity");
                    rarity.enumValueIndex = EditorGUILayout.Popup("희귀도", rarity.enumValueIndex, EquipmentManagementPanel.Rarities);
                    GUILayout.Label("아이템 설명");
                    var description = item.FindPropertyRelative("description");
                    description.stringValue = EditorGUILayout.TextArea(description.stringValue, GUILayout.MinHeight(100));
                    var storage = item.FindPropertyRelative("storage");
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.Popup("수량 연결", storage.enumValueIndex, new[] { "개별 인벤토리", "기존 철", "기존 수정", "기존 유물" });
                    EditorGUILayout.HelpBox("철·수정·유물은 기존 보유량에 연결됩니다. 새 재료는 고유 ID로 수량을 저장하며, 획득하면 가방에 표시됩니다. 획득처·제작 비용 연결은 별도 설정이 필요합니다.", MessageType.Info);
                    if (EditorGUI.EndChangeCheck()) { serialized.ApplyModifiedProperties(); host.Changed(); }
                    else serialized.ApplyModifiedProperties();
                    EditorGUILayout.EndScrollView();
                }
            }
        }
        static void Field(SerializedProperty parent, string name, string label) => EditorGUILayout.PropertyField(parent.FindPropertyRelative(name), new GUIContent(label));
        void Add(ContentManagementWindow host, bool duplicate)
        {
            Undo.RecordObject(host.draft, "Add material");
            var item = duplicate ? JsonUtility.FromJson<MaterialDefinition>(JsonUtility.ToJson(host.draft.materials[selected])) :
                new MaterialDefinition { title = "새 재료", description = "제작에 사용할 수 있는 재료다." };
            if (duplicate) item.title += " (복사)";
            item.id = "material_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            item.storage = MaterialStorage.Inventory;
            host.draft.materials = host.draft.materials.Concat(new[] { item }).ToArray();
            selected = host.draft.materials.Length - 1;
            search = ""; host.Changed();
        }
        internal static List<string> Errors(ExpeditionCatalog catalog)
        {
            var errors = new List<string>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var storage = new HashSet<MaterialStorage>();
            foreach (var item in catalog.materials ?? Array.Empty<MaterialDefinition>())
            {
                if (item == null) { errors.Add("비어 있는 재료입니다."); continue; }
                if (string.IsNullOrWhiteSpace(item.title) || string.IsNullOrWhiteSpace(item.id)) errors.Add("재료 이름과 ID가 필요합니다.");
                if (!ids.Add(item.id ?? "")) errors.Add("재료 ID가 중복됩니다: " + item.id);
                if (!Enum.IsDefined(typeof(GearRarity), item.rarity) || !Enum.IsDefined(typeof(MaterialStorage), item.storage)) errors.Add("재료 희귀도 또는 수량 연결이 잘못되었습니다.");
                if (item.storage != MaterialStorage.Inventory && !storage.Add(item.storage)) errors.Add("기존 재료 수량 연결이 중복됩니다.");
            }
            return errors;
        }
    }
}
