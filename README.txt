VinsPDF 0.29 — 22 November 2026

Install VinsPDF-Setup-0.29.exe, or choose About > Check for updates. Setup installs
per user with PDF associations, shortcuts and uninstall. Updates replace the
installed app after download verification.

Viewing
- Click the original Home logo, + or Ctrl+O to open. Tabs have filename tooltips
  and an x; drag a tab outside the strip to move it into a separate window.
- Layout: single/two pages. F11: full screen. F9: left thumbnails. PAGES » opens
  a translucent three-column overview beside the button. Select a page or click
  outside to dismiss it.
- Page labels use PDF metadata (e.g. i, ii, 1, 2). Enter a label to jump, or #5
  for physical page 5. The total is the physical count. Printed numbering without
  PDF labels is not inferred from artwork.
- Wheel scrolls zoomed pages; each notch turns a page when it fits vertically.
  Editing locks wheel page changes. Ctrl+wheel zooms. Ctrl+0 fits page; Ctrl+2
  fits width. Middle-drag / Alt+drag pans.
- Search / Ctrl+F highlights matches. F3 / Shift+F3 or arrows navigate; Aa matches
  case. Results arrive progressively, up to 5,000 per search. Search needs text,
  not scanned/outlined letters. OCR is not included.
- Highlight works outside the editor: drag across text to add a yellow PDF
  annotation. Ctrl+Z undoes and Ctrl+S saves. Saved annotations are displayed;
  this is not a general editor for existing annotations.

Editing
- Ctrl+E opens the compact floating panel at 90% opacity. Drag EDIT to move it;
  Display adjusts opacity. Sections expand inside the panel.
- Select draws a marquee even over an item. Drag a selected centre handle to
  move, edge/corner handles to resize, or the circular handle to rotate. Corners
  preserve proportions unless Shift is held; Shift rotation uses 15° steps.
  Pan moves the view. The Move selected tile has been removed.
- Double-click existing text or add text to type on the page. The thinner blue
  frame resizes. Font/text: family, size, bold/italic/underline/strike, colour and
  left/centre/right/justified alignment. Spacing sets characters, lines and
  paragraph gaps. Apply / Ctrl+Enter commits; Esc cancels. Marker and list controls
  are removed. Formatting applies to the whole item; mixed styles are unsupported.
- Similarly styled, aligned horizontal body text and nearby paragraphs group into
  one box. Paragraph breaks and wrapping survive font changes, saving and rotation.
  Text reflows inside that box; unrelated boxes/pages do not automatically move.
  Turn off Recognise paragraphs for individual objects. Recognition is geometric
  and cannot perfectly infer every PDF's structure.
- Images: PNG/JPEG/BMP/TIFF. Grid updates live and is neither exported nor snapped.
  Headers/footers and named editor layers remain available. Layer names persist;
  visibility/locks are session aids, and saved PDFs include non-deleted groups.
  This is not full optional-content layer authoring or nested CAD geometry editing.
- Undo preserves the camera. Recent matching view snapshots repaint immediately;
  the cache is limited to 32 MB per document. Evicted/changed views need rendering.
  Native work may finish after repaint.

Tools and saving
- Organise: drag thumbnails, rotate/remove. Save as creates a copy. Save later
  applies changes to this tab without writing the original. Closing then asks
  Save changes? Yes saves the original; No discards; Cancel keeps the tab open.
  Page organisation resets editing undo history.
- Extract accepts physical positions/ranges such as 1,3,5-8 or 5-1. Combine joins
  PDFs in the chosen order. Organise preserves metadata/page labels and retained
  page references; references to removed pages may no longer be useful. Extract
  and Combine may omit document-level bookmarks, forms and layer settings.
- Export: reduced editable PDF, reduced flattened PDF, PNG, JPEG, TIFF or BMP.
  Choose all/current/custom pages. Raster DPI: 72/96/120/150/200/300/600; JPEG
  quality and greyscale are available where applicable. Raster output is limited
  to 24 megapixels/page; use lower DPI for large sheets. Multiple images get
  ordered numbered filenames. Flattened output no longer has editable text.
- Editable export is lossless. Fast optimises structure while preserving existing
  compressed streams; Thorough also recompresses streams and can take minutes on
  large scans/CAD PDFs. Image quality is preserved. Already compressed PDFs may
  barely shrink; the smaller valid result is kept. DPI/JPEG quality affect raster
  formats only.
- Print / Ctrl+P opens printer settings; pages fit the paper margins.
- Ctrl+S offers an edited copy on the first ordinary save; Ctrl+Shift+S is Save As.
  Organise > Save later explicitly targets the original. Closing changed tabs
  offers Save/Don't save/Cancel (Yes/No/Cancel for deferred organisation), including
  pending typed text. Failed/cancelled saves keep the document open. Saving reopens
  the PDF and clears undo history. Toolbar rotation is view-only; component and
  Organise rotations are saved.

Scope and privacy
Windows 10/11 x64, .NET Framework 4.8. PDFium and QPDF run locally; no Python or
browser is installed. QPDF starts only for relevant document tools. Only Check
for updates contacts GitHub; documents are not uploaded. Password entry, OCR,
form-field editing and nested Form XObject editing are not included. Inline text
approximates font metrics; the final render after Apply shows spacing/rotation.
The shared viewer raster cache is 96 MB; native pages, tabs and undo add memory.
Complex first renders still take time. App and installer are unsigned.
This software is based in part on the work of the Independent JPEG Group.
Source/notices: https://github.com/DangerousKitty666/VinsPDF
