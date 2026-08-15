param([Parameter(Mandatory = $true)][string]$OutputPath)

$ErrorActionPreference = "Stop"

$resolvedOutput = [IO.Path]::GetFullPath($OutputPath)
$resolvedParent = [IO.Path]::GetFullPath((Split-Path -Parent $resolvedOutput))
New-Item -ItemType Directory -Force -Path $resolvedParent | Out-Null

$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$brandExportRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot "..\..\Brand System\Logos\Exports\icon-mark-acid"))

$requiredSizes = @(16, 24, 32, 48, 64, 128, 256)
$missing = @()
foreach ($size in $requiredSizes) {
  $candidate = Join-Path $brandExportRoot ("icon-mark-acid-{0}.png" -f $size)
  if (!(Test-Path -LiteralPath $candidate)) {
    $missing += $candidate
  }
}

if ($missing.Count -gt 0) {
  throw "Approved CineForge icon exports are missing:`n$($missing -join "`n")"
}

$pythonCommand = Get-Command python -ErrorAction SilentlyContinue
if ($null -eq $pythonCommand) {
  throw "Python is required to generate the CineForge icon from approved brand exports."
}

$pythonScript = @'
from PIL import Image
import os
import sys

export_root = sys.argv[1]
output_path = sys.argv[2]
sizes = [16, 24, 32, 48, 64, 128, 256]

frames = []
for size in sizes:
    path = os.path.join(export_root, f"icon-mark-acid-{size}.png")
    img = Image.open(path).convert("RGBA")
    alpha = img.getchannel("A")
    bbox = alpha.getbbox()
    if bbox is None:
        raise RuntimeError(f"No visible mark found in {path}")

    cropped = img.crop(bbox)
    margin = max(1, round(size * 0.06))
    inner = size - (margin * 2)
    fitted = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    mark = cropped.resize((inner, inner), Image.Resampling.LANCZOS)
    fitted.alpha_composite(mark, ((size - inner) // 2, (size - inner) // 2))
    frames.append(fitted)

frames[-1].save(output_path, format="ICO", sizes=[(size, size) for size in sizes], append_images=frames[:-1])
'@

$scriptPath = Join-Path ([IO.Path]::GetTempPath()) ("cineforge-generate-icon-{0}.py" -f ([guid]::NewGuid().ToString("N")))
Set-Content -LiteralPath $scriptPath -Value $pythonScript -Encoding UTF8
try {
  & $pythonCommand.Source $scriptPath $brandExportRoot $resolvedOutput
}
finally {
  Remove-Item -LiteralPath $scriptPath -Force -ErrorAction SilentlyContinue
}
