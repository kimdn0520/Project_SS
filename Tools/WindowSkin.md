# Window skin integration

- Apply with `Tools/ApplyWindowSkin.cs` outside Play mode. The main UI builder also reapplies this skin.
- `WindowFrame-v2.png`: nine-slice borders 160, Image pixels-per-unit multiplier 3, white tint. Use with `WindowFrameUI.mat`.
- `WindowClose-red-v3.png`: red close button, Simple Image, preserve aspect. Use with `WindowCloseUI.mat`.
- Both materials use `WindowFrameUI.shader` to remove the generated outside matte with an antialiased rounded silhouette. The source PNGs are not standalone transparent sprites. Do not atlas these sprites or remove their materials without replacing the matte mask.
- PopupCommon close anchor, pivot, position and size are preserved; the importer no longer recreates an existing common prefab. Current authored position is (648, -215).
- MenuPanelCanvas sorts at 500, over bottom navigation and sidebars; popups sort above it. A full-screen curtain blocks background input and closes the menu when tapped. Composition follows the mobile safe area.
- BattleSkyExtension updates in edit mode and hides on disable, preventing the uninitialized 100x100 cyan RawImage from appearing at the center of the editor view.
- Stage and resource labels use bold outlined font materials. Stage text has extra height for glyph padding.

## Verification

`Tools/WindowSkinQA.cs` checks curtain raycasts, menu close, nested equipment popup order, preserved close position and notice close. Screenshots and report are under `PrototypeQA/window-*`. `EditorCheck` verifies the sky band after leaving Play mode.
