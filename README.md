# VinsPDF

A compact dark Windows PDF viewer/editor. Windows 10/11 x64 and .NET Framework 4.8.

**[Download VinsPDF 0.27 - Windows installer](https://github.com/DangerousKitty666/VinsPDF/releases/download/v0.27/VinsPDF-Setup-0.27.exe)**

Setup installs the app, shortcuts, PDF associations and uninstaller. About > Check for updates installs future updates after verification. Select your PDF default in Windows Default Apps.

- Cached page revisits, idle next-page preparation, single/two-page layouts.
- Tabs with drag-out windows, thumbnails and a translucent page overview.
- Compact floating editor: Select, Move selected, Pan, inline text, images, resize and rotation.
- Fonts, colour, alignment, spacing, live grid, headers/footers and named editor layers.
- Combine PDFs, organise pages, extract ranges, export PDFs and print.

[Controls and limits](README.txt) | [Performance and tests](Performance.txt)

Editor layers group top-level text/images; names persist while visibility/locks are session aids. This is not full PDF optional-content layer authoring. Reduced exports flatten pages into images. Page-copy tools may not preserve document-level bookmarks, forms or optional-content settings. OCR and nested CAD geometry editing are not included.

## Build

Run ./build.ps1 in PowerShell. It restores a pinned checksum-verified PDFium DLL and uses Windows' .NET Framework C# compiler. No Python is required. Build installer/VinsPDF.iss with Inno Setup 6.7.4 or compatible newer versions.

The release workflow builds, tests and publishes the installer and SHA-256 checksums. Bump release.json, assembly and installer versions together. Existing releases are never overwritten. GitHub provides source archives.

PDF processing stays local. Only checking for updates contacts GitHub; documents are never uploaded. User drawings are excluded; the test PDF is synthetic. App and installer are unsigned.

[Source license](LICENSE.txt). Third-party notices are in licenses/. Supplied artwork is excluded from the source-code license.
