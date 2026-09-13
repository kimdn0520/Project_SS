from pathlib import Path
p=Path('Assets/Editor/PagePrefabBuilder.cs');s=p.read_text(encoding='utf-8-sig');a=s.index('    public static void Execute()');b=s.index('    private static Camera GetOrCreateMainCamera()',a);s=s[:a]+'''    public static void Execute()
    {
        // Keep legacy setup entry points on the new direct-to-Play architecture.
        ProjectSS.Expedition.Editor.ExpeditionBuilder.Build();
    }

'''+s[b:];p.write_text(s,encoding='utf-8-sig')
p=Path('GEMINI.md');s=p.read_text(encoding='utf-8-sig');header='''# 현재 구현 기준 (2026-09-13)

- 시작은 `Splash -> Play`, 로비/MainPage 없이 `UIPageType.PlayPage`를 바로 표시합니다.
- `Assets/Resources/Prefabs/PlayPage.prefab`는 새 `ProjectSS.Expedition.PlayPage`이며 이전 인게임 프리팹을 대체했습니다.
- 월드/UI와 캐싱된 서비스/풀은 프리팹에 미리 구성됩니다. 런타임 맵 생성은 하지 않습니다.
- 현재 생성기는 `Assets/Prototype/Editor/ExpeditionBuilder.cs`입니다. 기존 PagePrefabBuilder 메뉴도 이 생성기에 위임합니다. 빌드 메뉴는 씬/프리팹 수동 편집을 덮어쓰므로 의도적으로만 실행합니다.
- 자세한 현재 동작과 검증 범위는 `Assets/Prototype/README.md`를 참고하세요.
- 아래 문서는 이전 로비 구조를 포함한 이력입니다. 현재 기준과 충돌하면 위 내용을 우선합니다.

''';p.write_text(header+s,encoding='utf-8-sig')
