# Hero collection cells

The collection uses four columns of compact cards. Each card shows only grade, centered portrait, five star indicators and duplicate progress. Portrait placement uses the nontransparent pixel bounds, so transparent source margins do not shift the character. Name, deployment status, role and ManageGear controls are hidden. Existing card selection still opens equipment, or assigns a hero when a formation slot is selected.

HeroGrade is E/D/C/B/A/S and is independent of star level. Existing heroes start at E and one star. ExpeditionCatalog.heroes and heroStarCosts are editable under ProjectSS > 콘텐츠 관리 > 캐릭터. Initial duplicate costs are 5/10/20/40; these are provisional authoring values, not fixed economy rules.

HeroProgress is stored by stable hero ID. Old saves gain one-star records for owned heroes without resetting other progress. PlayPage.GrantHeroCopies is the persistent reward entry point for a future summoning system; ExpeditionModel.GrantHeroCopies implements unlock, duplicate accumulation and automatic promotion up to five stars. Excess duplicates at maximum stars are preserved. No summoning prices/probabilities, new roster entries or combat multipliers are introduced here.

Validation: HeroProgressionQA (migration, exact and multiple promotions, cap, overflow, first unlock, save/reload and grade independence), HeroCollectionQA (display and existing equipment navigation).
`nEarned stars only are shown in a centered row; unowned cells show none. HeroStarGraphic renders rounded gold stars. Duplicate gauges use sliced track/fill sprites with a clipped full-width fill, preserving cap geometry at every value. Collection hint text is hidden. HeroCellStatesQA covers 1/3/5 stars and partial/full progress.

HeroCard is shared at Assets/Resources/Prefabs/UI/HeroCard.prefab. PlayPage uses eight nested instances (three heroes and five placeholders); edit common visuals in this prefab. Instances bind the page, hero ID, portrait and placeholder state. The editor builder reuses the existing prefab without rebuilding its common visuals.
