# Unity Animator Controller Fix Script
# Run this if you're getting NullReferenceException errors from Unity Editor

Write-Host "=== Unity Animator Fix Script ===" -ForegroundColor Cyan
Write-Host ""

$projectPath = "e:\file\unity project\First Rpg"

# Check if Unity is running
$unityProcess = Get-Process -Name "Unity" -ErrorAction SilentlyContinue
if ($unityProcess) {
    Write-Host "WARNING: Unity is currently running!" -ForegroundColor Yellow
    Write-Host "Please close Unity before running this script." -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit
}

Write-Host "Cleaning Unity cache and temp files..." -ForegroundColor Green

# Remove cache directories
$pathsToClean = @(
    "$projectPath\Library\StateCache",
    "$projectPath\Library\ShaderCache",
    "$projectPath\Temp"
)

foreach ($path in $pathsToClean) {
    if (Test-Path $path) {
        Write-Host "Removing: $path" -ForegroundColor Yellow
        Remove-Item -Path $path -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  -> Cleaned!" -ForegroundColor Green
    } else {
        Write-Host "  -> Not found (OK)" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "=== Cleanup Complete! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Open Unity again" -ForegroundColor White
Write-Host "2. Let Unity rebuild the cache (may take a moment)" -ForegroundColor White
Write-Host "3. Check your Animator Controllers for broken transitions" -ForegroundColor White
Write-Host "   Location: Assets/Animations/Goblin/GoblinAC.controller" -ForegroundColor Gray
Write-Host ""

Read-Host "Press Enter to close"
