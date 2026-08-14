$ErrorActionPreference = "Stop"
$appRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$nativeDevPython = Join-Path $appRoot "work\native-packaging-venv\Scripts\python.exe"
$pythonExe = if (Test-Path -LiteralPath $nativeDevPython) { $nativeDevPython } else { (Get-Command python -ErrorAction Stop).Source }
Set-Location -LiteralPath $appRoot
& $pythonExe -m cineforge.server
