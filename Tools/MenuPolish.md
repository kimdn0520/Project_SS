# Menu, cell and toast UI

`MenuPolishArt.Configure(page)` reapplies the shared style without rebuilding the scene. It is invoked by `ImportedUIArt.Configure`. Standalone entry: `Tools/ApplyMenuPolish.cs`.

- Panels retain the existing background sprite with a white tint. The former dark image tint caused the dim appearance.
- Hero cards use three columns, 192 x 244, 18 horizontal / 24 vertical spacing, and 54-unit side padding. Name and active status sit inside each card.
- Cells reuse the existing 9-slice primitive: near-black navy outer surface, 4-unit border, ivory inner surface and inset icon well. Inventory and equipment rows share this treatment; selected equipment uses a mint interior.
- Inventory and equipment content height derives from actual cell height plus spacing.
- `BattleHeaderArt.Configure(page)` extends the sky above the old HUD bar, removes that bar, and uses separate outlined/underlaid TMP materials for stage, resources and battle status. Decorative sky fills above the safe viewport on tall phones; touch controls remain inside the safe viewport.

## Toast

Prefab: `Assets/Resources/Prefabs/UI/ToastPopup.prefab`.

```csharp
using ProjectSS.Expedition;

ToastPopup.Show("장비를 변경했습니다");
ToastPopup.Show("알림", screenPos: screenPoint, duration: 2f);
ToastPopup.HideIfActive();
```

Lazy singleton, retained across scenes, replaces the current message, unscaled fade and rise, safe-area clamped positioning, wrapped text, no input blocking. No localization/profiling/result-wrapper dependencies or try-catch. No automatic gameplay call sites were added.

Validation: `Tools/MenuPolishQA.cs`, 720x1280 and 1080x2400. Captures in `PrototypeQA/polish-*.png`; results in `PrototypeQA/menu-polish.txt`. Tests cover card bounds, inventory filters, popup opening/closing, toast replacement, dismissal while timeScale is zero, explicit hide, and non-blocking graphics. Device notch hardware remains untested.
