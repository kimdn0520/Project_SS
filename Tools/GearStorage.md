# Equipment storage and dismantling

- Weapon, armor (helmet + chest), and accessory capacities default to 100 each in ExpeditionCatalog. All owned items, including equipped items, count toward capacity. Materials use their existing independent quantities.
- Content Management > Equipment edits the three capacities and dismantle refund ratio; Save applies those settings to the catalog.
- Version 5 saves contain GearInstance GUIDs, definition indices, lock states, rolled options, and twelve equipped instance IDs. Existing quantity/equipment arrays remain synchronized for current visuals and compatibility.
- Version 3/4 migration creates one instance per owned item, assigns unique equipped copies, and preserves existing base stats without retroactively rerolling options. Over-cap legacy inventories are retained. Additional acquisitions are blocked until space is available.
- New crafting/chest rewards roll options once from the existing weighted authoring candidates, without replacement, up to maxRandomOptions. Individual options persist on reload. Attack, health, and weapon attack-speed options feed the existing combat snapshot; this change does not introduce new combat formulas for previously display-only critical/defense/lifesteal stats.
- Chest definition selection is saved before a capacity-blocked opening. A full category keeps the same chest reward, disables auto mining, and asks the player to free space. Crafting fails without spending materials when capacity is full.
- Bag rows show one instance per row, including duplicates. Equipped rows show 장비 중; eligible rows have a small dismantle icon with a 44px hit area. The icon opens a short confirmation. ItemDetails displays only the selected instance information and the top-right X. Equipment changes remain in the hero equipment selector.
- Dismantling checks the instance again on confirmation. Equipped and active-battle-snapshot items cannot be dismantled. The former lock UI is removed; legacy lock fields no longer prevent dismantling. Rewards default to floor(recipe cost * 0.25) per material, with a minimum of one iron. Removal and material grant happen together; repeated requests for the same GUID cannot grant twice.

Validation: Tools/GearStorageQA.cs (model/migration/persistence/capacity), Tools/GearStorageUIQA.cs (live UI with temporary isolated model and restored player save), Tools/InventoryVirtualizationQA.cs (pooled scrolling).

## Compact confirmation and common window chrome

DismantleConfirmation.prefab is a dedicated 500x280 variant of ExpeditionNotice. Larger help/notice windows retain their own content space. The common PopupCommon/Window owns CommonPopupChrome, which positions Close relative to the current bg top-right corner using the shared inset. Derived popups resize bg only; do not author independent Close coordinates. The component also updates in edit mode. ApplyCompactConfirmation.cs installs this shared relationship and builds the compact variant.
