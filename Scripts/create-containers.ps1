$ErrorActionPreference = "Stop"
$manageContainer = Join-Path $PSScriptRoot "manage-container.ps1"

& $manageContainer -Action create
