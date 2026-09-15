# Warm window and action button skin

`WarmUIArt.Configure` applies the warm ivory/gold frame to menus and PopupCommon instances, preserving their transforms and close button positions. It runs last in the imported UI builder.

- Frame: `WarmWindow-v3.png`, 180 borders, Sliced Image with PPU multiplier 5. Use `WarmWindowUI.mat` to exclude the generated outside matte with the rounded silhouette mask.
- Action: `JadeAction-v3.png`, cropped sprite with 190/100 borders, Sliced Image with multiplier 8. `JadeActionUI.mat` removes its neutral outside matte. Neither generated source should be used without its assigned UI material.
- Reusable template: `Assets/Resources/Prefabs/UI/ActionButton.prefab`. Pressed/disabled states use Button color tint; labels remain centered inside symmetric padding.
- Applied to settings actions, hero-list return buttons, popup confirmation buttons, and vein selection actions. Mining upgrade previews show disabled `준비 중` actions; upgrade gameplay is not implemented by this skin change.
- Vein cards are informational. Only `SelectAction` changes the route; selected and locked choices are disabled. Runtime selection also validates the route and unlock condition before changing it. A successful selection keeps the popup open and refreshes all choice states.

Apply with `Tools/ApplyWarmUI.cs` in edit mode. `Capture` checks selection state and selection behavior, captures settings/mining/vein views, and restores the original route. Results are in `PrototypeQA/warm-ui.txt` and `warm-*.png`.
