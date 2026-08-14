param(
  [string]$Version = "0.3.0",
  [switch]$SkipToolBootstrap
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression.FileSystem
$appRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$workBase = if ($env:CINEFORGE_BUILD_WORK_ROOT) { [IO.Path]::GetFullPath($env:CINEFORGE_BUILD_WORK_ROOT) } else { Join-Path $appRoot "work" }
$workRoot = Join-Path $workBase "release-build"
$venvRoot = Join-Path $workBase "native-packaging-venv"
$distRoot = Join-Path $workRoot "dist"
$buildRoot = Join-Path $workRoot "build"
$payloadRoot = Join-Path $PSScriptRoot "payload"
$assetsRoot = Join-Path $PSScriptRoot "assets"
$installerPublish = Join-Path $workRoot "installer-publish"
$releaseRoot = Join-Path $appRoot "release\CineForge-$Version-win-x64"
$iconPath = Join-Path $assetsRoot "CineForge.ico"

function Assert-WorkPath([string]$Path) {
  $full = [IO.Path]::GetFullPath($Path)
  $allowed = [IO.Path]::GetFullPath($workBase).TrimEnd('\') + '\'
  if (!$full.StartsWith($allowed, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clean a path outside the CineForge work area: $full"
  }
}

foreach ($path in @($workRoot)) {
  if (Test-Path -LiteralPath $path) {
    Assert-WorkPath $path
    Remove-Item -LiteralPath $path -Recurse -Force
  }
}
if (Test-Path -LiteralPath $payloadRoot) { Remove-Item -LiteralPath $payloadRoot -Recurse -Force }
if (Test-Path -LiteralPath $releaseRoot) { Remove-Item -LiteralPath $releaseRoot -Recurse -Force }
New-Item -ItemType Directory -Force -Path $workRoot,$distRoot,$buildRoot,$payloadRoot,$assetsRoot,$releaseRoot | Out-Null

& (Join-Path $PSScriptRoot "Generate-Icon.ps1") -OutputPath $iconPath

$venvPython = Join-Path $venvRoot "Scripts\python.exe"
if (!(Test-Path -LiteralPath $venvPython)) {
  if ($SkipToolBootstrap) { throw "The packaging environment is missing and -SkipToolBootstrap was supplied." }
  $nativeBase = if ($env:CINEFORGE_NATIVE_PYTHON) { $env:CINEFORGE_NATIVE_PYTHON } else { (Get-Command python -ErrorAction Stop).Source }
  if (!(Test-Path -LiteralPath $nativeBase)) { throw "Set CINEFORGE_NATIVE_PYTHON to an independent CUDA-enabled Python runtime." }
  & $nativeBase -m venv --system-site-packages $venvRoot
}
if (!$SkipToolBootstrap) {
  & $venvPython -m pip install --disable-pip-version-check --upgrade pip
  & $venvPython -m pip install --disable-pip-version-check pyinstaller
  & $venvPython -m pip install --disable-pip-version-check "torch==2.10.0+cu130" --index-url https://download.pytorch.org/whl/cu130
  & $venvPython -m pip install --disable-pip-version-check -r (Join-Path $PSScriptRoot "requirements-native.txt")
}

& $venvPython -c "import torch, diffusers, transformers, safetensors, PIL; assert torch.version.cuda"
if ($LASTEXITCODE -ne 0) { throw "The native CUDA inference dependencies are not available to the packaging environment." }
$diffusersSource = (& $venvPython -c "import pathlib,diffusers; print(pathlib.Path(diffusers.__file__).parent)").Trim()

Write-Host "Building the standalone CineForge application..."
& $venvPython -m PyInstaller `
  --noconfirm `
  --clean `
  --windowed `
  --name CineForge `
  --icon $iconPath `
  --version-file (Join-Path $PSScriptRoot "version_info.txt") `
  --distpath $distRoot `
  --workpath $buildRoot `
  --specpath $workRoot `
  --add-data "$(Join-Path $appRoot 'web');web" `
  --add-data "$(Join-Path $appRoot 'cineforge\workflows');cineforge\workflows" `
  --add-data "$diffusersSource;diffusers" `
  --hidden-import torch `
  --hidden-import diffusers `
  --hidden-import transformers `
  --hidden-import safetensors `
  --hidden-import huggingface_hub `
  --hidden-import tokenizers `
  --hidden-import PIL `
  --hidden-import numpy `
  --collect-submodules diffusers `
  --collect-submodules transformers `
  --copy-metadata requests `
  --exclude-module cv2 `
  --exclude-module torchaudio `
  --exclude-module scipy `
  --exclude-module sklearn `
  --exclude-module pandas `
  --exclude-module matplotlib `
  (Join-Path $appRoot "cineforge_entry.py")
if ($LASTEXITCODE -ne 0) { throw "PyInstaller failed with exit code $LASTEXITCODE." }

$appDist = Join-Path $distRoot "CineForge"
if (!(Test-Path -LiteralPath (Join-Path $appDist "CineForge.exe"))) { throw "The standalone CineForge executable was not created." }
Copy-Item -LiteralPath (Join-Path $appRoot "README.md") -Destination (Join-Path $appDist "README.md")
Copy-Item -LiteralPath (Join-Path $appRoot "config.example.json") -Destination (Join-Path $appDist "config.example.json")

$payloadZip = Join-Path $payloadRoot "CineForge-Payload.zip"
[IO.Compression.ZipFile]::CreateFromDirectory($appDist, $payloadZip, [IO.Compression.CompressionLevel]::Optimal, $false)

Write-Host "Building the native Windows installer..."
$installerProject = Join-Path $PSScriptRoot "CineForge.Installer\CineForge.Installer.csproj"
dotnet publish $installerProject -c Release -r win-x64 --self-contained true -o $installerPublish `
  /p:Version=$Version /p:FileVersion="$Version.0" /p:InformationalVersion=$Version
if ($LASTEXITCODE -ne 0) { throw "The installer build failed with exit code $LASTEXITCODE." }

$publishedInstaller = Join-Path $installerPublish "CineForge Setup.exe"
$releaseInstaller = Join-Path $releaseRoot "CineForge-Setup-$Version-win-x64.exe"
if (!(Test-Path -LiteralPath $publishedInstaller)) { throw "The installer executable was not created." }
Copy-Item -LiteralPath $publishedInstaller -Destination $releaseInstaller
Copy-Item -LiteralPath (Join-Path $appRoot "docs\GITHUB_RELEASE.md") -Destination (Join-Path $releaseRoot "RELEASE_NOTES.md")
Copy-Item -LiteralPath (Join-Path $appRoot "docs\INSTALLATION.md") -Destination (Join-Path $releaseRoot "INSTALLATION.md")
Copy-Item -LiteralPath (Join-Path $appRoot "docs\RELEASE_VERIFICATION.md") -Destination (Join-Path $releaseRoot "VERIFICATION.md")

$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $releaseInstaller).Hash
"$hash  $(Split-Path -Leaf $releaseInstaller)" | Set-Content -LiteralPath (Join-Path $releaseRoot "SHA256SUMS.txt") -Encoding ASCII
$manifest = [ordered]@{
  schemaVersion = 1
  product = "CineForge"
  version = $Version
  architecture = "win-x64"
  installer = Split-Path -Leaf $releaseInstaller
  sha256 = $hash
  sizeBytes = (Get-Item -LiteralPath $releaseInstaller).Length
  builtAt = (Get-Date).ToUniversalTime().ToString("o")
  installScope = "CurrentUser"
  installRoot = "Selected by user; default %LOCALAPPDATA%\Programs\CineForge"
  dataRoot = "Selected by user; default %USERPROFILE%\Videos\CineForge Library"
  generationRuntime = "Bundled CineForge Engine with PyTorch CUDA; ComfyUI is not required"
  modelRepository = "https://huggingface.co/TheBaldDudeCo/CineForge-Wan-Models"
  modelRevision = "3abefe070febb87cf51e038edda29934743639fb"
  modelDelivery = "Automatic resumable download with SHA-256 verification"
  codeSigned = $false
} | ConvertTo-Json -Depth 5
$manifest | Set-Content -LiteralPath (Join-Path $releaseRoot "CineForge-Release.json") -Encoding UTF8

Write-Host "Release ready: $releaseRoot"
Get-ChildItem -File -LiteralPath $releaseRoot | Select-Object Name,Length
