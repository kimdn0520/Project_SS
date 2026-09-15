# Imported UI and miner appearance

- `Assets/Resources/Prefabs/Popups/PopupCommon.prefab`: shared nested Window / bg / Contents / Title / Close structure used by notice, equipment, and vein popups.
- `bg_popup`: popup background, border 49 on all sides, muted teal tint.
- Existing `popup-bg-minimal-578x765`: menu panels and vein rows, border 64.
- `menu_bg`: bottom menu buttons, border 22.
- `btn_close`: menu return and popup close buttons, preserved aspect, 58-unit hit area.
- `progressbar_green` / `bg_progressbar_navy`: health bars; sliced fill with a reveal mask retains caps near zero.
- DIG retains its original gold/steel frame and tick marks. Charge changes now interpolate; zero hides immediately.
- No particle-only ETC textures were assigned to UI. Break effects reuse the existing fragment sprite and prewarmed pool: 12 fragments normally, 16 during burst, route-specific tint.
- Miner uses existing CharacterMaker Parts Pack Vol.1 `FA_Chest_045_Gray` and `FA_Helmet_041_Gray`. Hair/beard hidden under the closed helmet; no new raster asset needed.

`ImportedUIArt.Configure` and `MinerArmorArt.Configure` are included in the prototype builder. `Tools/ApplyImportedUI.cs` and `Tools/ApplyMinerArmor.cs` apply changes to the existing page prefab without rebuilding the scene.

Sidebar controls use a full-screen overlay canvas anchored to Screen.safeArea. Popups use a full-screen dim layer and a safe-area-fitted common window. The world keeps its portrait camera composition. A background camera clears unused viewport pixels when mobile dimensions change.

Validation: Unity compilation; 720x1280 and 1080x2400 captures; simulated portrait notch/home and lateral safe-area anchor calculations; route selection/locking/pause/back; notice/equipment close; 1% and 0% health; DIG interpolation/zero; miner swing. See PrototypeQA/imported-ui.txt, dig-gauge-refined.txt, miner-armor.txt and matching PNGs. Physical notch devices have not been tested. Landscape desktop checks were discontinued at the user's request.
