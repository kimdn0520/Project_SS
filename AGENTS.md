# Project conventions

- The current game is Rune Break. Entry flow: `Splash` scene → `Play` scene, with `MainPage` and `PlayPage` prefab instances registered in `PageManager`.
- Cache component dependencies in serialized prefab/scene fields. Do not use `GetComponent`, `GetComponentsInChildren`, `Find`, or `AddComponent` in runtime initialization. Editor authoring/build utilities may use these APIs to bake references.
- Inspect and reuse the existing `PoolManager`, `SpriteManager` / `SpriteAtlasSO`, and `PopupManager` regardless of their folder locations. Do not create parallel manager systems.
- Popups use the existing `PopupManager` with authored prefab references and a scene-authored `PopupParent`. Instantiating a complete popup prefab on open and destroying that instance after close is an approved exception; never build its UI hierarchy or add components at runtime. Cache its canvas, animation target, input blocker, controls and other dependencies in serialized fields.
- Default popup styling is a sliced pixel panel with a centered title plaque overlapping its top border, using the shared `PopupPanel` editor authoring helper. Settings is titled `설정` and contains only background music, sound effects and a bottom `닫기` button, identically in the lobby and gameplay.
- Author UI, boards, slots and effects in prefabs. Avoid runtime hierarchy construction; use the existing `PoolManager` baked-pool path for flights and a bounded mesh buffer for particles.
- Reuse `SpriteAtlasSO` / `SpriteManager` and point-filtered, uncompressed sprite atlases. Keep the HTML prototype's exact pixel palette and drawing geometry.
- `Tools/RuneBreak/Source/rune-break-layers.html` is the fidelity reference. The export script runs its original drawing functions. Preserve Aseprite sources and compare exported pixels before changing art.
- Rules reserve damage and storage when tapped. Animation timing must not affect rules. Keep rules/solver independent of MonoBehaviour and validate against the HTML oracle.
- Ready boosters are three empty slots; their mechanics are undecided. Lobby tabs are data-driven and currently placeholders except Home.
- The HTML is a reference for art, rules and effects, not the shipped UI. Do not restore its named chapter heading, north/east text panels, debug statistics, undo/restart/hint buttons, or Z/R/H shortcuts. PlayPage uses the board, recast slots, top-right settings, and three bottom booster placeholders.
- Gate presentation overrides live in `Tools/RuneBreak/presentation.cjs`: one colored block per remaining hit point, with one block removed per hit. Do not restore numeric gate badges, `OK`, or the `TO POUCH` plaque. Preserve the original palette, bevels and layer previews.
