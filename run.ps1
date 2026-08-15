$ErrorActionPreference = "Stop"
$appRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$env:CINEFORGE_SOURCE_ROOT = $appRoot
dotnet run --project (Join-Path $appRoot "desktop\CineForge.Desktop\CineForge.Desktop.csproj")
