using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectSS.Expedition;

namespace ProjectSS.ContentEditor
{
    internal sealed class EquipmentManagementPanel : IContentManagementPanel
    {
        public string Title => "장비";
        internal static readonly string[] Rarities = { "일반", "희귀", "영웅", "전설", "신화" };
        internal static readonly string[] Slots = { "무기", "투구", "갑옷", "장신구" };
        private static readonly Color[] Colors = { new Color(.66f,.70f,.74f), new Color(.35f,.66f,1), new Color(.72f,.45f,1), new Color(1,.71f,.24f), new Color(1,.36f,.44f) };
        private string search = "";
        private int rarityFilter, slotFilter;
        private Vector2 listScroll, detailScroll;
        private bool advanced;

        public void Draw(ContentManagementWindow host)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var catalog = (ExpeditionCatalog)EditorGUILayout.ObjectField("카탈로그", host.source, typeof(ExpeditionCatalog), false);
                if (catalog != host.source) host.SetSource(catalog);
                if (GUILayout.Button("프로젝트에서 보기", GUILayout.Width(125))) EditorGUIUtility.PingObject(host.source);
            }
            if (host.draft == null)
            {
                EditorGUILayout.HelpBox("편집할 ExpeditionCatalog 에셋을 선택하세요.", MessageType.Info);
                GUILayout.FlexibleSpace(); return;
            }
            if (host.draft.gear == null) host.draft.gear = Array.Empty<GearDefinition>();
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawList(host);
                DrawDetail(host);
            }
        }

        private void DrawList(ContentManagementWindow host)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(290), GUILayout.ExpandHeight(true)))
            {
                GUILayout.Label($"장비 목록  ·  {host.draft.gear.Length}", EditorStyles.boldLabel);
                search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
                using (new EditorGUILayout.HorizontalScope())
                {
                    rarityFilter = EditorGUILayout.Popup(rarityFilter, new[] { "모든 희귀도" }.Concat(Rarities).ToArray());
                    slotFilter = EditorGUILayout.Popup(slotFilter, new[] { "모든 부위" }.Concat(Slots).ToArray());
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ 새 장비")) Add(host, false);
                    using (new EditorGUI.DisabledScope(host.draft.gear.Length == 0))
                        if (GUILayout.Button("선택 복제")) Add(host, true);
                }
                if (GUILayout.Button("+ 전설 서리지팡이 예시")) AddFrost(host);
                GUILayout.Space(5);
                listScroll = EditorGUILayout.BeginScrollView(listScroll);
                int count = 0;
                for (int i = 0; i < host.draft.gear.Length; i++)
                {
                    var g = host.draft.gear[i];
                    if (g == null) continue;
                    if (rarityFilter > 0 && (int)g.rarity != rarityFilter - 1 || slotFilter > 0 && g.equipSlot != slotFilter - 1) continue;
                    if (!string.IsNullOrWhiteSpace(search) && ($"{g.title} {g.id} {g.description}").IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (count++ % 3 == 0) EditorGUILayout.BeginHorizontal();
                    Rect rect = GUILayoutUtility.GetRect(82, 105, GUILayout.Width(82));
                    if (GUI.Button(rect, new GUIContent("", g.title + "\n" + g.id))) { host.selected = i; GUI.FocusControl(null); }
                    var tint = Colors[Mathf.Clamp((int)g.rarity, 0, 4)];
                    if (host.selected == i) EditorGUI.DrawRect(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), new Color(.20f,.37f,.52f,.8f));
                    EditorGUI.DrawRect(new Rect(rect.x + 2, rect.yMax - 4, rect.width - 4, 3), tint);
                    Texture preview = g.icon != null ? AssetPreview.GetAssetPreview(g.icon) ?? AssetPreview.GetMiniThumbnail(g.icon) : EditorGUIUtility.IconContent("Prefab Icon").image;
                    if (preview != null) GUI.DrawTexture(new Rect(rect.x + 13, rect.y + 7, 56, 56), preview, ScaleMode.ScaleToFit);
                    GUI.Label(new Rect(rect.x + 3, rect.y + 66, 76, 19), g.title, new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip });
                    GUI.Label(new Rect(rect.x + 3, rect.y + 83, 76, 16), Rarities[Mathf.Clamp((int)g.rarity, 0, 4)], new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = tint } });
                    if (count % 3 == 0) EditorGUILayout.EndHorizontal();
                }
                if (count % 3 != 0) EditorGUILayout.EndHorizontal();
                if (count == 0) EditorGUILayout.HelpBox("검색 조건에 맞는 장비가 없습니다.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                GUILayout.Label("색상: 희귀도  /  클릭: 상세 편집", EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void DrawDetail(ContentManagementWindow host)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                if (host.draft.gear.Length == 0) { GUILayout.Label("새 장비를 추가해 시작하세요."); GUILayout.FlexibleSpace(); return; }
                host.selected = Mathf.Clamp(host.selected, 0, host.draft.gear.Length - 1);
                var data = new SerializedObject(host.draft);
                data.Update();
                var g = data.FindProperty("gear").GetArrayElementAtIndex(host.selected);
                detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
                GUILayout.Label($"{g.FindPropertyRelative("title").stringValue}  /  #{host.selected:D3}", new GUIStyle(EditorStyles.boldLabel) { fontSize = 19 });
                GUILayout.Space(10);
                EditorGUI.BeginChangeCheck();
                Section("기본 정보");
                Field(g, "title", "장비 이름"); Field(g, "id", "고유 ID (선택)");
                Popup(g, "rarity", "희귀도", Rarities);
                Popup(g, "equipSlot", "장착 부위", Slots);
                var hero = g.FindPropertyRelative("hero");
                hero.intValue = EditorGUILayout.Popup("사용 직업", hero.intValue + 1, new[] { "공용", "전사", "도적", "마법사" }) - 1;
                Field(g, "icon", "아이콘");
                GUILayout.Label("장비 설명", EditorStyles.miniLabel);
                var desc = g.FindPropertyRelative("description");
                desc.stringValue = EditorGUILayout.TextArea(desc.stringValue, GUILayout.MinHeight(48));

                Section("기본 능력치");
                Field(g, "damage", "공격력"); Field(g, "health", "추가 체력");
                Field(g, "interval", "공격 간격 (초)");
                float interval = g.FindPropertyRelative("interval").floatValue;
                if (interval > 0) EditorGUILayout.LabelField("기본 DPS", (g.FindPropertyRelative("damage").floatValue / interval).ToString("0.##"));

                Section("고유 효과");
                Popup(g, "effect", "효과", new[] { "없음", "방어구 파괴", "냉기", "화상", "흡혈", "방어" });
                EditorGUILayout.HelpBox("고유 효과는 장비의 고정 특성입니다. 랜덤 옵션과 전설 장비 스킬은 별도로 설정합니다.", MessageType.None);

                Section("랜덤 옵션");
                Field(g, "maxRandomOptions", "최대 옵션 개수");
                EditorGUILayout.HelpBox("초반 권장: 최대 1개. 아래 목록은 동시에 붙는 옵션이 아닌 추첨 후보입니다. 개수가 0이거나 후보가 없으면 옵션을 사용하지 않습니다. 가중치가 클수록 선택 확률이 높습니다.", MessageType.Info);
                DrawOptions(g);

                Section("장비 스킬  ·  전설 / 신화");
                bool eligible = g.FindPropertyRelative("rarity").enumValueIndex >= (int)GearRarity.Legendary;
                var reference = g.FindPropertyRelative("skillAsset");
                if (!eligible) EditorGUILayout.HelpBox("전설·신화 장비에 스킬 1개를 연결할 수 있습니다. 등급을 낮춘 경우 기존 연결을 해제하세요.", MessageType.Info);
                using (new EditorGUI.DisabledScope(!eligible))
                {
                    Field(g, "skillAsset", "장비 스킬");
                    var library = SkillLibrary.All;
                    int current = Array.IndexOf(library, reference.objectReferenceValue as SkillDefinition) + 1;
                    int chosen = EditorGUILayout.Popup("라이브러리에서 선택", current,
                        new[] { "없음" }.Concat(library.Select(s => $"{s.settings.title}  [{s.settings.id}]")).ToArray());
                    if (chosen != current) reference.objectReferenceValue = chosen == 0 ? null : library[chosen - 1];
                }
                var linked = reference.objectReferenceValue as SkillDefinition;
                if (linked != null)
                {
                    var preview = host.skills.Resolve(linked);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        GUILayout.Label(preview.settings.title, EditorStyles.boldLabel);
                        GUILayout.Label(preview.settings.description ?? "", EditorStyles.wordWrappedLabel);
                        GUILayout.Label($"{SkillManagementPanel.Effects[Mathf.Clamp((int)preview.settings.effect, 0, 5)]} · 쿨타임 {preview.settings.cooldown:0.#}초 · 확률 {preview.settings.chance:0.#}%");
                        GUILayout.Label($"지속 {preview.settings.duration:0.#}초 · 반경 {preview.settings.radius:0.#} · 수치 {preview.settings.power:0.#}");
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("스킬 탭에서 편집")) host.ShowSkills(linked);
                            if (GUILayout.Button("연결 해제")) { reference.objectReferenceValue = null; g.FindPropertyRelative("skill").FindPropertyRelative("enabled").boolValue = false; GUI.changed = true; }
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button("스킬 탭 열기")) host.ShowSkills();
                    var legacy = host.draft.gear[host.selected].skill;
                    if (legacy != null && (!string.IsNullOrWhiteSpace(legacy.id) || !string.IsNullOrWhiteSpace(legacy.title)))
                    {
                        EditorGUILayout.HelpBox($"이전 방식의 스킬 설정이 남아 있습니다: {legacy.title}. 별도 에셋으로 저장하면 현재 장비의 편집본에 연결합니다.", MessageType.Info);
                        using (new EditorGUI.DisabledScope(!eligible))
                            if (GUILayout.Button("기존 설정을 스킬 에셋으로 저장·연결"))
                            {
                                var asset = SkillLibrary.ImportLegacy(legacy, host.draft.gear[host.selected].icon);
                                reference.objectReferenceValue = asset; GUI.changed = true;
                            }
                        if (legacy.enabled && GUILayout.Button("기존 스킬 사용 해제")) { g.FindPropertyRelative("skill").FindPropertyRelative("enabled").boolValue = false; GUI.changed = true; }
                    }
                }
                if (reference.objectReferenceValue != null) g.FindPropertyRelative("skill").FindPropertyRelative("enabled").boolValue = false;
                EditorGUILayout.HelpBox("현재 범위: 장비 데이터 관리. 새 랜덤 옵션의 실제 추첨과 장비 스킬의 전투 발동은 후속 전투 시스템 연동이 필요합니다.", MessageType.Info);

                Section("제작 / 해금");
                Field(g, "iron", "필요 철"); Field(g, "crystal", "필요 수정"); Field(g, "relic", "필요 유물"); Field(g, "unlock", "해금 클리어 단계");
                advanced = EditorGUILayout.Foldout(advanced, "기존 게임 연결 / 고급 설정", true);
                if (advanced)
                {
                    Field(g, "prefab", "드롭 프리팹"); Field(g, "spriteKey", "캐릭터 파츠 키"); Field(g, "slot", "캐릭터 파츠 슬롯");
                    EditorGUILayout.HelpBox("기존 세이브가 목록 인덱스를 사용하므로 삭제·순서 변경은 제공하지 않습니다. 장비 추가 시 보유 수량 배열은 확장되어 기존 진행을 보존합니다. 새 장비의 게임 표시에는 고정 인벤토리 UI와 드롭 프리팹 연결이 별도로 필요합니다.", MessageType.Info);
                }
                if (EditorGUI.EndChangeCheck()) { data.ApplyModifiedProperties(); host.Changed(); }
                else data.ApplyModifiedProperties();
                var errors = GearCatalogValidation.EntryErrors(host.draft.gear[host.selected], host.skills.Resolve);
                foreach (var error in errors) EditorGUILayout.HelpBox(error, MessageType.Error);
                GUILayout.Space(14);
                EditorGUILayout.EndScrollView();
            }
        }

        private static void DrawOptions(SerializedProperty g)
        {
            var options = g.FindPropertyRelative("randomOptions");
            double total = 0;
            for (int i = 0; i < options.arraySize; i++) total += Math.Max(0, options.GetArrayElementAtIndex(i).FindPropertyRelative("weight").floatValue);
            for (int i = 0; i < options.arraySize; i++)
            {
                var option = options.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    bool remove;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label($"후보 {i + 1}", EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        float weight = option.FindPropertyRelative("weight").floatValue;
                        GUILayout.Label(total > 0 ? $"첫 추첨 확률 {weight / total * 100:0.#}%" : "가중치 확인", EditorStyles.miniLabel);
                        remove = GUILayout.Button("제거", GUILayout.Width(45));
                    }
                    if (remove) { options.DeleteArrayElementAtIndex(i); GUI.changed = true; break; }
                    Popup(option, "stat", "옵션 종류", new[] { "공격력", "체력", "공격 속도", "치명타 확률", "치명타 피해", "방어력", "흡혈" });
                    Popup(option, "unit", "단위", new[] { "고정 수치", "퍼센트 (%)" });
                    Field(option, "min", "최소값"); Field(option, "max", "최대값"); Field(option, "weight", "추첨 가중치");
                }
            }
            if (GUILayout.Button("+ 옵션 후보 추가"))
            {
                options.arraySize++;
                var p = options.GetArrayElementAtIndex(options.arraySize - 1);
                p.FindPropertyRelative("stat").enumValueIndex = 0; p.FindPropertyRelative("unit").enumValueIndex = 0;
                p.FindPropertyRelative("min").floatValue = 1; p.FindPropertyRelative("max").floatValue = 5; p.FindPropertyRelative("weight").floatValue = 1;
                GUI.changed = true;
            }
        }

        private static void Section(string title) { GUILayout.Space(14); GUILayout.Label(title, EditorStyles.boldLabel); }
        private static void Field(SerializedProperty parent, string name, string label) => EditorGUILayout.PropertyField(parent.FindPropertyRelative(name), new GUIContent(label), true);
        private static void Popup(SerializedProperty parent, string name, string label, string[] values)
        {
            var p = parent.FindPropertyRelative(name);
            p.intValue = EditorGUILayout.Popup(label, p.intValue, values);
        }

        private void Add(ContentManagementWindow host, bool duplicate)
        {
            var gear = duplicate ? JsonUtility.FromJson<GearDefinition>(JsonUtility.ToJson(host.draft.gear[Mathf.Clamp(host.selected, 0, host.draft.gear.Length - 1)])) :
                new GearDefinition { title = "새 장비", hero = -1, interval = 1, damage = 10 };
            if (duplicate) gear.title += " (복사)";
            gear.id = "gear_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            Append(host, gear);
        }

        private void AddFrost(ContentManagementWindow host)
        {
            var staff = host.draft.gear.FirstOrDefault(g => g != null && g.slot == "Wand");
            Append(host, new GearDefinition {
                id = "frost_staff_" + Guid.NewGuid().ToString("N").Substring(0, 8), title = "서리지팡이", rarity = GearRarity.Legendary,
                hero = 2, damage = 35, interval = 1.6f, effect = GearEffect.Frost, icon = staff?.icon, slot = "Wand", spriteKey = staff?.spriteKey,
                description = "서리 폭풍으로 범위 내 적을 얼리는 전설 지팡이.",
                randomOptions = new[] { new GearRandomOption { stat = GearOptionStat.Attack, unit = GearOptionUnit.Percent, min = 3, max = 8 } },
                skillAsset = SkillLibrary.ImportLegacy(SkillLibrary.FrostSettings(), staff?.icon)
            });
        }

        private void Append(ContentManagementWindow host, GearDefinition gear)
        {
            Undo.RecordObject(host.draft, "Add equipment");
            host.draft.gear = host.draft.gear.Concat(new[] { gear }).ToArray();
            host.selected = host.draft.gear.Length - 1;
            search = ""; rarityFilter = slotFilter = 0; detailScroll = Vector2.zero;
            host.Changed(); if (Event.current != null) GUI.FocusControl(null);
        }
    }

    internal static class GearCatalogValidation
    {
        internal static List<string> Errors(ExpeditionCatalog catalog, Func<SkillDefinition, SkillDefinition> resolve = null)
        {
            var result = new List<string>();
            if (catalog == null || catalog.gear == null) { result.Add("카탈로그가 없습니다."); return result; }
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < catalog.gear.Length; i++)
            {
                var g = catalog.gear[i];
                foreach (var error in EntryErrors(g, resolve)) result.Add($"#{i:D3} {g?.title}: {error}");
                if (g != null && !string.IsNullOrWhiteSpace(g.id) && !ids.Add(g.id.Trim())) result.Add($"#{i:D3}: 중복 ID '{g.id}'");
            }
            return result;
        }

        internal static List<string> EntryErrors(GearDefinition g, Func<SkillDefinition, SkillDefinition> resolve = null)
        {
            var errors = new List<string>();
            if (g == null) { errors.Add("비어 있는 장비입니다."); return errors; }
            if (string.IsNullOrWhiteSpace(g.title)) errors.Add("장비 이름을 입력하세요.");
            if (!Enum.IsDefined(typeof(GearRarity), g.rarity)) errors.Add("희귀도가 올바르지 않습니다.");
            if (g.equipSlot < 0 || g.equipSlot > 3 || g.hero < -1 || g.hero > 2) errors.Add("장착 부위 또는 사용 직업이 올바르지 않습니다.");
            if (!NonNegative(g.damage) || !NonNegative(g.health) || !NonNegative(g.interval) || (g.equipSlot == 0 && g.interval <= 0)) errors.Add("공격력·체력은 0 이상, 무기 공격 간격은 0보다 커야 합니다.");
            if (g.iron < 0 || g.crystal < 0 || g.relic < 0 || g.unlock < 0) errors.Add("제작 비용과 해금 단계는 음수일 수 없습니다.");
            if (g.maxRandomOptions < 0) errors.Add("최대 옵션 개수는 0 이상이어야 합니다.");
            var options = g.randomOptions ?? Array.Empty<GearRandomOption>();
            if (options.Length > 0 && g.maxRandomOptions > options.Length) errors.Add("최대 옵션 개수가 추첨 후보 수보다 많습니다.");
            var stats = new HashSet<GearOptionStat>();
            foreach (var o in options)
            {
                if (o == null) { errors.Add("비어 있는 옵션 후보입니다."); continue; }
                if (!Enum.IsDefined(typeof(GearOptionStat), o.stat) || !Enum.IsDefined(typeof(GearOptionUnit), o.unit)) errors.Add("옵션 종류 또는 단위가 올바르지 않습니다.");
                if (!Finite(o.min) || !Finite(o.max) || o.min > o.max || !Finite(o.weight) || o.weight <= 0) errors.Add("옵션은 최소값 ≤ 최대값, 가중치 > 0이어야 합니다.");
                if (!stats.Add(o.stat)) errors.Add("같은 종류의 옵션 후보가 중복됐습니다.");
            }
            if (g.skillAsset != null)
            {
                if (g.rarity < GearRarity.Legendary) errors.Add("장비 스킬은 전설·신화에서만 사용할 수 있습니다.");
                var definition = resolve == null ? g.skillAsset : resolve(g.skillAsset);
                errors.AddRange(SkillLibrary.Errors(definition.settings));
                if (!EditorUtility.IsPersistent(g.skillAsset)) errors.Add("스킬 탭에서 먼저 저장한 스킬을 연결하세요.");
                return errors;
            }
            var s = g.skill;
            if (s != null && s.enabled)
            {
                if (g.rarity < GearRarity.Legendary) errors.Add("장비 스킬은 전설·신화에서만 사용할 수 있습니다.");
                if (string.IsNullOrWhiteSpace(s.id) || string.IsNullOrWhiteSpace(s.title) || s.effect == GearSkillEffect.None) errors.Add("스킬 ID·이름·효과를 설정하세요.");
                if (!Enum.IsDefined(typeof(GearSkillEffect), s.effect) || !Enum.IsDefined(typeof(GearSkillTarget), s.target) || !Enum.IsDefined(typeof(GearSkillTrigger), s.trigger)) errors.Add("스킬 효과·대상·발동 조건을 확인하세요.");
                if (!Finite(s.cooldown) || s.cooldown <= 0 || !Finite(s.chance) || s.chance <= 0 || s.chance > 100 || !NonNegative(s.duration) || !NonNegative(s.radius) || !NonNegative(s.power)) errors.Add("스킬 쿨타임 > 0, 확률 0~100% (0 제외), 지속 시간·반경·수치 ≥ 0이어야 합니다.");
                if (s.target == GearSkillTarget.EnemyArea && s.radius <= 0) errors.Add("광역 스킬의 효과 반경은 0보다 커야 합니다.");
                if ((s.effect == GearSkillEffect.Freeze || s.effect == GearSkillEffect.Burn) && s.duration <= 0) errors.Add("빙결·화상의 지속 시간은 0보다 커야 합니다.");
            }
            return errors;
        }
        private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        private static bool NonNegative(float n) => Finite(n) && n >= 0;
    }
}
