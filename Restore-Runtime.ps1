$ErrorActionPreference = 'Stop'
$dependency = Get-Content -Raw (Join-Path $PSScriptRoot 'runtime-dependency.json') | ConvertFrom-Json
$target = Join-Path $PSScriptRoot 'pdfium.dll'
if ((Test-Path -LiteralPath $target) -and (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -eq $dependency.dllSha256) { return }
$staging = Join-Path $PSScriptRoot 'work/runtime'
New-Item -ItemType Directory -Force -Path $staging | Out-Null
$archive = Join-Path $staging 'runtime.zip'
Invoke-WebRequest -Uri $dependency.url -OutFile $archive
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $dependency.sha256) { throw 'Runtime archive checksum mismatch' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($archive)
try { [System.IO.Compression.ZipFileExtensions]::ExtractToFile($zip.GetEntry('pypdfium2_raw/pdfium.dll'), $target, $true) } finally { $zip.Dispose() }
if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $dependency.dllSha256) { throw 'PDFium checksum mismatch' }
