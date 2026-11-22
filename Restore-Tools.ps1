$ErrorActionPreference = 'Stop'
$dependency = Get-Content -Raw (Join-Path $PSScriptRoot 'qpdf-dependency.json') | ConvertFrom-Json
$target = Join-Path $PSScriptRoot 'qpdf'
$valid = $true
foreach ($file in $dependency.files) {
    $path = Join-Path $target $file.name
    if (-not (Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.sha256) { $valid = $false; break }
}
if ($valid) { return }
$staging = Join-Path $PSScriptRoot 'work/runtime'
New-Item -ItemType Directory -Force $staging,$target | Out-Null
$archive = Join-Path $staging 'qpdf.zip'
Invoke-WebRequest -Uri $dependency.url -OutFile $archive
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $dependency.sha256) { throw 'QPDF archive checksum mismatch' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($archive)
try {
    foreach ($file in $dependency.files) {
        $path = Join-Path $target $file.name
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($zip.GetEntry($dependency.prefix + $file.name), $path, $true)
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.sha256) { throw "QPDF component checksum mismatch: $($file.name)" }
    }
} finally { $zip.Dispose() }
