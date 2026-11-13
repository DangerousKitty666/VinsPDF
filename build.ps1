$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Restore-Runtime.ps1')
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$sourceFiles = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'source') -Filter '*.cs' | ForEach-Object FullName
& $compiler /nologo /codepage:65001 /target:winexe /platform:x64 /optimize+ /r:System.dll /r:System.Core.dll /r:System.Web.Extensions.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "/win32manifest:$PSScriptRoot/source/app.manifest" "/win32icon:$PSScriptRoot/source/VinsPDF.ico" "/out:$PSScriptRoot/VinsPDF.exe" $sourceFiles
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
