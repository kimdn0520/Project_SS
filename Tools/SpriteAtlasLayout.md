# Shared sprite atlas registry

`Assets/SpriteAtlas/SpriteAtlasScriptable.asset` is the single `SpriteAtlasSO` registry. All `SpriteManager` prefab bindings point to it. It contains only three project atlases:

- `Equipment.spriteatlasv2`: `Assets/Textures/Equipment`, character equipment parts.
- `UICommon.spriteatlasv2`: `Assets/Textures/UI/Common`, frames, buttons, cells, bars, mining controls and backgrounds.
- `UIIcons.spriteatlasv2`: `Assets/Textures/UI/Icons`, equipment thumbnails, navigation, portraits and material icons.

UI subfolders organize source art; they do not create additional atlas assets. Imported package atlases are left with their source packages. `Tools/UIAssetMoves.json` records the 47 GUID-preserving UI moves.

Atlas V2 packing settings must be saved on `SpriteAtlasImporter`: rotation and tight packing are disabled, padding is 4, and mipmaps are disabled. `EquipmentAtlasArt.BindFolder` persists these settings and always appends to the shared registry instead of creating per-category registries.

`AtlasLocalUV` supplies sprite-local UVs to the WindowFrame shader independently of atlas UVs. Keep this component on custom window/button graphics. Original nine-slice borders and JadeAction's 256px import remain unchanged.

Equipment is first in the registry. Bare sprite names resolve first-match, so an equipment thumbnail cannot replace the character part with the same name. Qualified keys such as `UIIcons/FA_WP_Main_Sword_019_WoodSilver` can select a specific atlas.

# Inventory cell reuse

`ExpeditionInventory` maintains filtered item indices independently of its rendered cells. One hidden authored row is the cloning template; old authored rows are retired on initialization. The private pool is local to the bag, so it does not consume gameplay effect pools.

The visible range includes two offscreen rows above and below (Inspector `Overscan Rows`). Pool capacity is viewport rows + one partially visible row + both buffer bands. Scrolling releases cells before renting replacements and never adds another click listener. Closing the bag returns all active cells; reopening reuses them. Tab changes reset the scroll; shrinking inventory clamps it.

Validation: `Tools/UIAtlasQA.cs`, `Tools/EquipmentAtlasQA.cs`, `Tools/InventoryVirtualizationQA.cs`, and `Tools/BagCategoriesQA.cs`. The virtualization check uses an isolated 1,000-item catalog and does not modify player inventory or the source catalog.

## Large textures and mining import sizes

`MainPage.prefab` and its unused `home-background-large.png` were removed. Keep large screen backgrounds in NonAtlas; do not register that folder in the shared registry.

Mining source PNGs are retained at original resolution. TextureImporter limits are 1024 for DigAssembly (1024x683 imported) and DigCapDomed (1024x512 imported). Their existing sprite crops, GUIDs, pivots, and UI dimensions are preserved. The legacy Dig_Button_Base, Dig_Button_Face and Dig_Progress_Ring import at 256x256; current PlayPage uses the newer pedestal/cap art. Atlas page allocation depends on all packed sprites and is not reduced in direct proportion to these individual image sizes.
