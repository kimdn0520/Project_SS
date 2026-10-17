$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$archiveRoot = [IO.Path]::GetFullPath((Join-Path $workspace 'Archive/LegacyGame'))
$entries = @(
    'Assets/Prototype', 'Assets/Editor',
    'Assets/Scripts/GameView', 'Assets/Scripts/UIView', 'Assets/Scripts/Model',
    'Assets/Scripts/UI/MainPage/MainPageView.cs', 'Assets/Scripts/UI/HoldDigButton.cs',
    'Assets/Scripts/UI/Popup/HeroManagementPopup.cs', 'Assets/Scripts/UI/Popup/InventoryPopup.cs',
    'Assets/Scripts/Core/WorldAreaLayoutBinder.cs', 'Assets/Scripts/Core/SplashScene.cs',
    'Assets/Resources/Prefabs/UI', 'Assets/Resources/Prefabs/Popups', 'Assets/Resources/Weapon'
)
foreach ($entry in $entries) {
    foreach ($relative in @($entry, "$entry.meta")) {
        $source = [IO.Path]::GetFullPath((Join-Path $workspace $relative))
        $target = [IO.Path]::GetFullPath((Join-Path $archiveRoot $relative))
        if (-not $source.StartsWith($workspace + [IO.Path]::DirectorySeparatorChar)) { throw "Source escapes workspace: $source" }
        if (-not $target.StartsWith($archiveRoot + [IO.Path]::DirectorySeparatorChar)) { throw "Target escapes archive: $target" }
        if (Test-Path -LiteralPath $source) {
            if (Test-Path -LiteralPath $target) { throw "Archive target already exists: $target" }
            New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($target)) | Out-Null
            Move-Item -LiteralPath $source -Destination $target
            Write-Output $relative
        }
    }
}
