# Popup loading

All modal resource names end in Popup: CommonPopup (shared visual template), ExpeditionNoticePopup, EquipmentSelectionPopup, VeinSelectionPopup, ItemDetailsPopup and DismantleConfirmationPopup. Asset GUIDs remain unchanged by the rename.

PopupManager has no serialized prefab list and no prefab registry/cache. ShowAsync loads Resources/Prefabs/Popups/<name> only when requested. Concurrent Show requests with the same name share one popup/result. Closing destroys the instance and removes its live reference. Queue retains only the name, parameters and result callback; it loads/instantiates when it reaches the front. Clear invalidates in-flight loads so they cannot reopen a cleared UI.

PlayPage holds no serialized modal-prefab references. Its nonserialized notice/equipmentPopup/veinPopup fields are temporary state for the existing editor build pipeline only; runtime callers use resource names.

Resources asset memory is not necessarily freed immediately when an instance is destroyed. ReleaseUnusedAssetsAsync is an explicit idle/loading-boundary cleanup, guarded against open/pending popups. It is not called on every close. Shared atlases/fonts referenced by other UI remain resident as expected.

Validation: Tools/PopupLoadingQA.cs tests dependency removal, duplicate requests, lazy queues, close/reopen, clear during load, nested results and idle cleanup. Tools/GearStorageUIQA.cs covers renamed item/detail/dismantle calls.
