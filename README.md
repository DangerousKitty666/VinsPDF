# VinsPDF

A compact, dark Windows PDF viewer and editor. Free to distribute; Windows 10/11 x64 and .NET Framework 4.8.

[Download the latest installer](https://github.com/DangerousKitty666/VinsPDF/releases/latest)

- Single/two-page views, left page previews, document tabs and detachable windows.
- Inline text editing, cascading font/spacing/grid/header controls, text and image selection, move, resize and rotate.
- PDF saving with undo/redo; current-page view stays in place when undoing.
- Per-user PDF association registration and in-app **Check for updates**.

Use the installer to add VinsPDF to Windows **Open with**. Select it in Default Apps if desired; the installer does not force the default. Updates download from this repository and verify GitHub's SHA-256 asset digest before running Setup. Save all documents before updating.

See [README.txt](README.txt) for controls and limitations. Editing covers top-level PDF text and images; OCR, nested Form editing, compression presets and automatic paragraph reflow are not included. Two-page layout is for viewing; editing focuses one page. The app and installer are unsigned.

## Build

Run `./build.ps1` in PowerShell. It restores a pinned, checksum-verified PDFium DLL from the pypdfium2 wheel and uses Windows' .NET Framework C# compiler. Python is not required. Build `installer/VinsPDF.iss` with Inno Setup 6.7.4 or compatible newer versions.

The release workflow builds, tests and publishes an installer and portable ZIP. Bump `release.json`, `source/AssemblyInfo.cs` and `installer/VinsPDF.iss` together for each new release. Existing release versions are never overwritten by the workflow.

PDF processing stays on your computer. Only explicitly checking for updates contacts GitHub; documents are never uploaded. User drawing samples are not in this repository. The test PDF is synthetic.

Application source: see [LICENSE.txt](LICENSE.txt). PDFium and dependencies retain their licenses in `licenses/`. Supplied VinsPDF artwork is excluded from the source-code license.
