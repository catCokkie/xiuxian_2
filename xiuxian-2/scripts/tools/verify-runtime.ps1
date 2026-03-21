$ErrorActionPreference = 'Stop'

function Test-PathOrThrow {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Missing ${Label}: $Path"
    }
}

Write-Host '[verify-runtime] Step 1/4: Check required project files...'
Test-PathOrThrow -Path 'project.godot' -Label 'project config'
Test-PathOrThrow -Path 'scenes/PrototypeRoot.tscn' -Label 'main scene'
Test-PathOrThrow -Path 'docs/design/09_level_monster_drop_sample.json' -Label 'level config JSON'
Test-PathOrThrow -Path 'scripts/game/PrototypeRootController.cs' -Label 'root runtime controller'
Test-PathOrThrow -Path 'scripts/game/ExploreProgressController.cs' -Label 'explore runtime controller'

Write-Host '[verify-runtime] Step 2/4: Validate level config JSON parse...'
$jsonText = Get-Content -LiteralPath 'docs/design/09_level_monster_drop_sample.json' -Raw
$null = $jsonText | ConvertFrom-Json

Write-Host '[verify-runtime] Step 3/4: Optional Godot headless smoke...'
$godotBin = $env:GODOT_BIN
if ([string]::IsNullOrWhiteSpace($godotBin)) {
    Write-Host '  GODOT_BIN is not set; skip headless launch smoke check.' -ForegroundColor Yellow
}
elseif (-not (Test-Path -LiteralPath $godotBin)) {
    Write-Host "  GODOT_BIN path does not exist: $godotBin" -ForegroundColor Yellow
}
else {
    & $godotBin --path . --headless --quit
    if ($LASTEXITCODE -ne 0) {
        throw "Godot headless launch failed with exit code $LASTEXITCODE"
    }
    Write-Host '  Godot headless launch passed.' -ForegroundColor Green
}

Write-Host '[verify-runtime] Step 4/4: Manual runtime checklist'
Write-Host '  1) Open res://scenes/PrototypeRoot.tscn, confirm no Parse Error.'
Write-Host '  2) Complete one battle, confirm recent battle log updates.'
Write-Host '  3) Reload and confirm explore runtime + recent battle log restore.'
Write-Host ''
Write-Host '[verify-runtime] DONE' -ForegroundColor Green
