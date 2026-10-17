# Project conventions

- The current game is Rune Break. Entry flow: `Splash` scene → `Play` scene, with `MainPage` and `PlayPage` prefab instances registered in `PageManager`.
- Cache component dependencies in serialized prefab/scene fields. Do not use `GetComponent`, `GetComponentsInChildren`, `Find`, or `AddComponent` in runtime initialization. Editor authoring/build utilities may use these APIs to bake references.
- Author UI, boards, slots and effects in prefabs. Avoid runtime hierarchy construction; use the existing `PoolManager` baked-pool path for flights and a bounded mesh buffer for particles.
- Reuse `SpriteAtlasSO` / `SpriteManager` and point-filtered, uncompressed sprite atlases. Keep the HTML prototype's exact pixel palette and drawing geometry.
- `Tools/RuneBreak/Source/rune-break-layers.html` is the fidelity reference. The export script runs its original drawing functions. Preserve Aseprite sources and compare exported pixels before changing art.
- Rules reserve damage and storage when tapped. Animation timing must not affect rules. Keep rules/solver independent of MonoBehaviour and validate against the HTML oracle.
- Ready boosters are three empty slots; their mechanics are undecided. Lobby tabs are data-driven and currently placeholders except Home.
- The HTML is a reference for art, rules and effects, not the shipped UI. Do not restore its named chapter heading, north/east text panels, debug statistics, undo/restart/hint buttons, or Z/R/H shortcuts. PlayPage uses the board, recast slots, top-right settings, and three bottom booster placeholders.
- Gate presentation overrides live in `Tools/RuneBreak/presentation.cjs`: one colored block per remaining hit point, with one block removed per hit. Do not restore numeric gate badges, `OK`, or the `TO POUCH` plaque. Preserve the original palette, bevels and layer previews.
