param(
  [Parameter(Mandatory = $true)][string]$CertificateThumbprint,
  [string]$Version = "0.2.0",
  [string]$TimestampServer = "http://timestamp.digicert.com"
)

$ErrorActionPreference = "Stop"
$appRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$releaseRoot = Join-Path $appRoot "release\CineForge-$Version-win-x64"
$installer = Join-Path $releaseRoot "CineForge-Desktop-Setup-$Version-win-x64.exe"
if (!(Test-Path -LiteralPath $installer)) { throw "Release installer not found: $installer" }

$normalizedThumbprint = ($CertificateThumbprint -replace '\s', '').ToUpperInvariant()
$certificate = Get-ChildItem Cert:\CurrentUser\My | Where-Object Thumbprint -eq $normalizedThumbprint | Select-Object -First 1
if (!$certificate) { throw "The requested code-signing certificate was not found in Cert:\CurrentUser\My." }
if (!$certificate.HasPrivateKey) { throw "The selected certificate does not have an accessible private key." }

$signature = Set-AuthenticodeSignature -LiteralPath $installer -Certificate $certificate -HashAlgorithm SHA256 -TimestampServer $TimestampServer
if ($signature.Status -ne 'Valid') { throw "Authenticode signing failed: $($signature.StatusMessage)" }

$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $installer).Hash
"$hash  $(Split-Path -Leaf $installer)" | Set-Content -LiteralPath (Join-Path $releaseRoot "SHA256SUMS.txt") -Encoding ASCII
$manifestPath = Join-Path $releaseRoot "CineForge-Release.json"
$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
$manifest.sha256 = $hash
$manifest.sizeBytes = (Get-Item -LiteralPath $installer).Length
$manifest.codeSigned = $true
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

Write-Host "Signed release ready. New SHA-256: $hash"
