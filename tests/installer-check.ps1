param([string]$Installer)
$ErrorActionPreference = 'Stop'
$root = Join-Path $env:RUNNER_TEMP 'VinsPDF-installed-test'
$arguments = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /NORUN=1 /DIR="' + $root + '"'
$process = Start-Process -FilePath $Installer -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
if ($process.ExitCode -ne 0) { throw "Install failed: $($process.ExitCode)" }
$exe = Join-Path $root 'VinsPDF.exe'
$expected = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot '../VinsPDF.exe')).Hash
if ((Get-FileHash -LiteralPath $exe).Hash -ne $expected) { throw 'Installed app mismatch' }
$command = (Get-Item -LiteralPath 'HKCU:/Software/Classes/VinsPDF.Document/shell/open/command').GetValue('')
if ($command -ne ('"' + $exe + '" "%1"')) { throw 'PDF open command not registered' }
$icon = (Get-Item -LiteralPath 'HKCU:/Software/Classes/VinsPDF.Document/DefaultIcon').GetValue('')
if ($icon -notlike '*VinsPDF-document.ico*') { throw 'Document icon not registered' }
if ((Get-Item -LiteralPath 'HKCU:/Software/VinsPDF/Capabilities/FileAssociations').GetValue('.pdf') -ne 'VinsPDF.Document') { throw 'Default Apps capability missing' }
[IO.File]::WriteAllText((Join-Path $root 'README.txt'),'older install marker')
$process = Start-Process -FilePath $Installer -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
if ($process.ExitCode -ne 0) { throw 'In-place update failed' }
if ([IO.File]::ReadAllText((Join-Path $root 'README.txt')) -eq 'older install marker') { throw 'In-place update did not replace payload' }
if ((Get-FileHash -LiteralPath $exe).Hash -ne $expected) { throw 'Updated app mismatch' }
foreach ($file in (Get-Content -Raw (Join-Path $PSScriptRoot '../qpdf-dependency.json') | ConvertFrom-Json).files) {
  if ((Get-FileHash -LiteralPath (Join-Path $root ('qpdf/' + $file.name))).Hash -ne $file.sha256) { throw 'Installed QPDF checksum mismatch' }
}
$testFolder = Join-Path $env:RUNNER_TEMP 'VinsPDF-installed-engine'
$fixture = Join-Path $PSScriptRoot 'paragraph-fixture.pdf'
$check = Start-Process -FilePath $exe -ArgumentList ('--v029-engine-test "' + $testFolder + '" "' + $fixture + '"') -Wait -PassThru -WindowStyle Hidden
Get-Content (Join-Path $testFolder 'engine-029.txt')
if ($check.ExitCode -ne 0) { throw 'Installed PDF tools failed' }
$process = Start-Process -FilePath (Join-Path $root 'unins000.exe') -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART' -Wait -PassThru -WindowStyle Hidden
if ($process.ExitCode -ne 0) { throw 'Uninstall failed' }
if (Test-Path -LiteralPath 'HKCU:/Software/Classes/VinsPDF.Document') { throw 'Uninstall retained own association' }
'PASS installer payload, per-user associations/icons, in-place replacement and uninstall'
