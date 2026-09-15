# Gold title and chest art

- `WindowTitleArt` applies the gold/cream nameplate to PopupCommon and menu panels, preserving the existing TMP title references and close buttons. `GoldTitle-v2.png` has genuine alpha and a cropped nine-slice sprite with 160/130/160/130 borders.
- `GoldChestArt` imports three matching animation parts from `GoldBlueChest-v3.png`, inspired by the supplied box_goldblue sprites with chunky cartoon outlines and stepped shading. Originals remain available.
- The generated chest sheet contains a neutral checker matte. `GoldBlueChest.mat` / `ChestSprite.shader` excludes that matte while rendering the chromatic artwork. Keep this material on all three chest parts; do not use the sheet as a standalone transparent sprite or put it into an atlas.
- The closed lid and open inside share a hinge. The existing opening sequence shakes, turns the lid, swaps its face, then settles. Six reused spark sprites animate from the seam using the dedicated `Assets/ETC/spark03.png` particle texture. Reward timing is unchanged.
- Run `Tools/ApplyGoldArt.cs` in edit mode to apply; its `Capture` method records the title and closed/opening/open chest in Play mode, then restores the chest visibility from current game data.
- Visual captures: `PrototypeQA/gold-title.png`, `gold-closed.png`, `gold-opening.png`, `gold-open.png`. Unity compilation passed.
