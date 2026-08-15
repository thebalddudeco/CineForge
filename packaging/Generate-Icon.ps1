param([Parameter(Mandatory = $true)][string]$OutputPath)

$ErrorActionPreference = "Stop"

$resolvedOutput = [IO.Path]::GetFullPath($OutputPath)
$resolvedParent = [IO.Path]::GetFullPath((Split-Path -Parent $resolvedOutput))
New-Item -ItemType Directory -Force -Path $resolvedParent | Out-Null

$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$brandIcon = [IO.Path]::GetFullPath((Join-Path $projectRoot "..\..\Brand System\Logos\Exports\favicon\cineforge-app-icon.ico"))

if (!(Test-Path -LiteralPath $brandIcon)) {
  throw "Approved CineForge brand icon not found at $brandIcon"
}

Copy-Item -LiteralPath $brandIcon -Destination $resolvedOutput -Force
