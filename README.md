# VinsPDF

A compact dark Windows PDF viewer/editor. Windows 10/11 x64, .NET Framework 4.8.

**[Download VinsPDF 0.29.1 — Windows installer](https://github.com/DangerousKitty666/VinsPDF/releases/download/v0.29.1/VinsPDF-Setup-0.29.1.exe)**

0.29.1 fixes the disposed editing-panel crash on closing a tab/window.

Setup installs shortcuts, PDF associations and an uninstaller. About > Check for updates downloads and verifies future installers, then updates in place. Choose the PDF default in Windows Default Apps.

- Fast cached viewing, single/two-page layouts, F11 full screen and page labels.
- Tabs with close buttons, tooltips and drag-out windows; compact thumbnail overview.
- Search with highlighted results and navigation; a separate text highlighter.
- Floating Select/Pan editor: text blocks, paragraph reflow, fonts/colour/alignment,
  images, resizing, rotation, grid and named editor layers. Faster visual undo.
- Organise thumbnails with Save as or deferred Save later; Combine, Extract and Print.
- Reduced editable or flattened PDFs and image export, with standard DPI options.

[Controls and scope](README.txt) | [Release changes](CHANGELOG.txt) | [Performance and tests](Performance.txt)

Paragraph grouping uses geometry and matching styles. It does not perform OCR or reflow unrelated boxes/pages. Mixed styles within one item and nested CAD geometry editing are unsupported. Page labels require PDF metadata. Editor layers are named page-level groups, not full optional-content layer authoring.

Editable exports preserve image quality and text. Fast compression optimises structure; Thorough recompresses streams and may take minutes. Already compressed files may barely shrink. Flattened PDF/image exports rasterise content. Extract/Combine may omit document-level features; Organise retains metadata and references to retained pages.

## Build and release

Run ./build.ps1 in PowerShell. It restores checksum-pinned PDFium and QPDF components and uses the Windows .NET Framework compiler. No Python or browser runtime is required. Build installer/VinsPDF.iss with Inno Setup 6.7.4 or compatible newer versions.

The release workflow builds and runs PDF editing/installer checks before publication. Bump release.json, assembly and installer versions together. Existing releases are never overwritten. GitHub supplies source archives.

PDF processing stays local. Only Check for updates contacts GitHub; documents are never uploaded. Test PDFs are synthetic. App and installer are unsigned.

[Source license](LICENSE.txt). Component notices are in licenses/. Supplied artwork and About animation are excluded from the source-code license. This software is based in part on the work of the Independent JPEG Group.
