param([Parameter(Mandatory = $true)][string]$OutputPath)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$resolvedParent = [IO.Path]::GetFullPath((Split-Path -Parent $OutputPath))
New-Item -ItemType Directory -Force -Path $resolvedParent | Out-Null

$bitmap = [Drawing.Bitmap]::new(256, 256)
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([Drawing.Color]::FromArgb(9, 9, 8))
$brush = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(215, 255, 69))

$bars = @(
  [Drawing.Point[]]@([Drawing.Point]::new(58, 46), [Drawing.Point]::new(91, 46), [Drawing.Point]::new(69, 210), [Drawing.Point]::new(36, 210)),
  [Drawing.Point[]]@([Drawing.Point]::new(113, 66), [Drawing.Point]::new(146, 66), [Drawing.Point]::new(127, 190), [Drawing.Point]::new(94, 190)),
  [Drawing.Point[]]@([Drawing.Point]::new(168, 88), [Drawing.Point]::new(201, 88), [Drawing.Point]::new(185, 168), [Drawing.Point]::new(152, 168))
)
foreach ($bar in $bars) { $graphics.FillPolygon($brush, $bar) }

$graphics.Dispose()
$brush.Dispose()
$icon = [Drawing.Icon]::FromHandle($bitmap.GetHicon())
$stream = [IO.File]::Open($OutputPath, [IO.FileMode]::Create)
$icon.Save($stream)
$stream.Dispose()
$icon.Dispose()
$bitmap.Dispose()
