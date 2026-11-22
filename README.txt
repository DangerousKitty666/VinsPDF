VinsPDF 0.28 — 21 November 2026

Install VinsPDF-Setup-0.28.exe (the standard download). The installer registers
PDF Open with / Default Apps, shortcuts and an uninstaller for the current user.
About > Check for updates downloads, verifies and runs future installers in place.

Viewing and navigation
- Shared 96 MB rendered-page cache; cached revisits and panning within an already
  rendered margin avoid another native render. Next-page preparation runs idle.
- Home, Tools, About and + followed by document tabs. + / Ctrl+O opens files.
  Drag a tab out of the tab strip to move it into a separate window. Right-click
  or middle-click closes tabs. Home opens the empty viewer without closing PDFs.
- Layout: single or two pages. F9: left thumbnails. The » button beside PAGES
  opens a translucent, scrollable overview. Select a page or click outside to close.
- Wheel scrolls; Ctrl+wheel zooms. Editing locks wheel navigation to one page.

Editing
- A compact floating panel: drag EDIT to reposition. Display sets opacity.
- Select draws a selection rectangle, including drags over an object body.
  Drag the selected centre point to move, edge/corner handles to resize, or the
  circular handle to rotate. These controls work in Select and Move selected.
- Move selected moves/resizes/rotates an existing selection. Eight handles resize;
  corners preserve proportions unless Shift is held. The circular handle rotates
  with a green rectangle preview; Shift constrains it to 15-degree increments.
- Pan moves the view. Middle-drag / Alt+drag also pan. The selection centre shows
  a pointing-hand cursor in both selection modes.
- Add text types directly on the page; double-click existing text to edit it.
  Resize the typing box using its blue border handles; narrower boxes wrap words.
  Font/text: family, size, bold, italic, underline, strike, highlight, colour,
  left/centre/right/justified alignment, bullets, numbering and nested lists.
  For nested lists use Tab to indent the beginning of a line.
  Spacing controls characters, line height and paragraph gaps (blank lines).
  Ctrl+Enter / Apply commits text; Esc cancels. Formatting applies to the whole
  item. Applied text is stored as selectable lines; mixed styles inside one item
  are not supported. Existing drawing text is grouped conservatively.
- Grid appears immediately when checked; spacing and visibility update live.
  Grid is a viewing aid, is not exported, and does not snap objects.
- Add image imports PNG/JPEG/BMP/TIFF. Undo/redo retains the current-page camera.
- Layers are named editor groups for top-level text and images on each page.
  Assign selection creates/uses a name; Rename and Ungroup manage membership.
  Names persist in saved PDFs. Visibility and locking are session editing aids:
  saved/exported PDFs include all non-deleted objects. This is not a full editor
  for pre-existing PDF optional-content layers or nested CAD geometry.

Tools
- Combine files: select PDFs, order them and create a new combined PDF.
- Organise pages: drag thumbnail cards to reorder, remove or rotate pages, then
  save a new PDF. Ctrl+Left/Right also moves the selected card; Delete removes it.
- Extract pages: ranges and ordered lists such as 1,3,5-8 or 5-1.
- Export PDF / images: editable PDF, reduced-size image PDF, PNG, JPEG, TIFF or
  BMP. Choose all/current/custom pages, 36–600 dpi, JPEG quality and greyscale.
  Images use one file per page (ordered numbered suffixes for multiple pages).
  Raster output is limited to 24 megapixels per page; choose a lower resolution
  for large sheets. Original-quality PDF keeps vectors without this limit.
  Reduced export flattens pages: text is no longer selectable/editable as text.
  It may not make an already optimised document smaller.
- Print / Ctrl+P: choose printer, page range and printer settings; rendered output
  fits the selected paper's margins. Print rendering is tested; physical printers
  vary and have not been tested in this development session.
Page extraction/organisation/combination copies page content. Document-level
bookmarks, forms and optional-content layer configuration may not transfer.

Save and scope
Ctrl+S offers an edited copy first; Ctrl+Shift+S always offers Save As. Saved PDFs
are reopened and undo history clears. Closing changed tabs/windows offers Save,
Don't save or Cancel, including text still being typed. Cancelling a save or a
failed save keeps the document open. Native toolbar rotation is view-only;
component rotation and Organise pages rotation are saved. Keep original files.
Editing covers top-level PDF text and images. Scanned/outlined text, nested Form
XObjects, annotations, form fields, OCR and password entry are not supported.
The horizontal inline typing box approximates text; final PDF rendering shows
actual font metrics, spacing and rotation after Apply.

Performance and privacy
Windows 10/11 x64, .NET Framework 4.8. PDFium runs locally, no Python or browser is
required. Only Check for updates contacts GitHub; drawings are not uploaded.
The cache budget covers cached raster images, not total process memory. Open
native pages, undo histories, editable content and tabs add memory. Complex first
renders still take time; cached revisit timings are not first-open timings.
The application and installer are unsigned. Source and license notices:
https://github.com/DangerousKitty666/VinsPDF
