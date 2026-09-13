# Play prototype

Start `Assets/Scenes/Splash.unity`. It fades directly into `Assets/Scenes/Play.unity`; no lobby is registered.

The new `Assets/Resources/Prefabs/PlayPage.prefab` replaces the legacy PlayPage prefab. Its ordinary Transform root contains sibling world and UI roots, cached services, popup, and prewarmed effects. The Play scene contains the prefab instance and a camera/PageManager. The camera is injected through SetupRenderCamera.

- Hold the circular DIG face to accelerate mining. The central shaft descends vertically: the miner briefly falls and settles while three authored vein rows recycle upward. DIG and navigation remain stationary. Input pauses during the short descent.
- Veins show cracks without an HP bar or embedded resource icon. Materials float up after a break.
- Chests open their lids, reveal one equipment item, and remain open briefly. No pick strike or rock breaking audio is played for opening. An early chest and a forty-vein pity limit, an eight-vein minimum gap, and a 4% eligible roll accompany random chest appearances.
- Auto expedition walks between encounters and retries the same wave after defeat. Ten waves form a region; wave ten is a boss. The first regional boss unlocks relic mining and stronger mining. Later regions currently reuse the monster roster with increasing basic stats.
- Three deployed heroes use basic attacks only. Hero cards open individual weapon/helmet/armor/accessory slots. Inventory copies are counted separately; one armor cannot be equipped on multiple heroes without another copy. Uncollected hero cards are placeholders; recruiting/hero skills are not implemented.
- Bag is information-only with All / Weapons / Materials filters. Hero equipment slots open a PopupManager selection modal filtered by class, slot and ownership; selection equips a free copy, and non-weapon slots can be cleared. Resource vein choice is in Settings. Sidebars intentionally contain three empty square slots on each side.
- Challenge is a future-content placeholder. Settings has sound, help, and a scoped save reset.
- Menus pause simulation while open. Turning auto expedition off lets the current encounter finish and then waits. Back navigates from hero detail to collection, or from a menu to mining.

Equipment definitions live in ExpeditionCatalog. `Assets/Resources/Weapon/Prefabs` contains cached WeaponItem prefab views. Two instances per definition are baked into PlayPage and registered with PoolManager; loot presentation rents/returns them without runtime map construction. Player save data contains stable catalog indices, counts and twelve equipment slots, under `ProjectSS.Play.v3`.

Editor-only scene construction is in ExpeditionBuilder. `ProjectSS/Prototype/Build Expedition Prototype` regenerates Play scene/prefab and overwrites layout edits, so use it intentionally. Runtime code does not generate the map or discover/add its dependencies. Async loops use UniTask and visuals use DOTween.

Validation: `ProjectSS/Prototype/Validate Rules` covers 1-1..1-10 progression, boss unlocks, defeat retry, inventory copies/class restrictions, equipment effects on base stats, chests and saves. Live QA additionally checks circular input, UI actions, prewarmed pools, chest opening, ten-wave progression and scrolling seams. Reports and captures are in `PrototypeQA/`. Live chapter QA uses temporary equipment and restores the preceding save in finally.

Mobile-device performance and touch feel still require hardware testing. Current editor tests do not imply device certification or finished long-term balance.


Vertical layout and inventory/popup authoring are centralized in `VerticalPlayLayout`; `Tools/UpdateVerticalPlay.cs` applies them to the existing prefab without rebuilding the combat scene. `Tools/VerticalPlayQA.cs` checks descent, chest flow, filters and equipment selection, restoring its temporary inventory/save edits.

Art pass: Assets/Prototype/Art/Polished contains the authored mining shaft, intact/four-stage damaged stone, chip sprite, two-part DIG assembly and chest. PolishedMiningArt slices source textures in edit mode, preserves sprite proportions and assigns cached references. Fractures use five sprite states instead of LineRenderer strokes. Shaft tiles alternate vertically to match their shared seams, and recycle as preplaced objects. Built-in image generation prompts are stored in Art/Polished/Prompts.txt. Art QA captures are in PrototypeQA/art-*.png; polished-art.txt and dig-pulse.txt record runtime checks.
