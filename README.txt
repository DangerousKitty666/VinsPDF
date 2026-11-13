VinsPDF 0.26 — Windows PDF viewer and editor

INSTALL / UPDATE
Run VinsPDF-Setup-0.26.exe. It installs for the current Windows user, adds a
shortcut and registers VinsPDF in PDF Open with / Default Apps. Windows still
lets you choose your preferred default; Setup does not overwrite UserChoice.
The supplied icon22 artwork is used for associated PDFs. Explorer may show
PDF thumbnails instead of file icons. The application and installer are unsigned.

Check for updates uses public GitHub releases at:
https://github.com/DangerousKitty666/VinsPDF/releases
After you accept an update, the app downloads and checks the installer's SHA-256
against GitHub's asset digest, waits for documents to be saved, closes, and
runs Setup to update the existing installation and reopen VinsPDF. Unsaved edits
block installation. Other running VinsPDF processes must also be closed; Setup
does not force-terminate processes or discard edits. No background checks run.
A portable ZIP is also available; extract the whole folder before running.

WINDOW / LAYOUT
Open (+ or Ctrl+O) adds a document tab. Switch by clicking tabs. Right-click or
middle-click a tab to close it. Detach tab moves that document and its unsaved
editing state into a separate window. Each tab retains its document/view/history.
Layout selects Single page or Two pages (current page and following page).
Editing uses a single active page. The left strip contains page previews and
page numbers; F9 hides/shows it. The right editor appears when Edit is enabled.
Wheel scrolls within a page and advances at its edge in viewing mode. In Edit
mode it stays on the current page. Deliberate page selection remains possible.
Ctrl+wheel zooms; drag pans in view mode; Alt+drag or middle-drag pans in Edit.
Existing page detail is scaled immediately while a sharper render is prepared.
Highly complex drawing pages can still require seconds for the final render.

EDITING
Click Edit (Ctrl+E). Click text/image, or drag a rectangle to select multiple
objects. Shift+click adds/removes items. Light outlines mark editable objects.
Drag a selection to move. Eight handles resize it; corner handles preserve
proportions unless Shift is held. The circular handle above a selection rotates
it; Shift snaps rotation to 15 degrees. The Rotate selection section also accepts
an exact angle in degrees clockwise. Ctrl+Z/Y undo/redo without recentering the
current page. Undo may deliberately switch pages for a change on another page.

Double-click text, choose Edit text on page, or Add text to type inside a border
on the PDF. Font / text expands in the right toolbar. Choose font, size, bold or
italic there. Ctrl+Enter / Apply text commits; Esc / Cancel text cancels.
Apply/cancel inline text before saving, navigating or closing its document.
Inline text remains available when switching/detaching its tab. Font choices
apply to the whole text item, not individual selected characters.

Click a section heading to expand/collapse Font, Spacing, Rotate, Grid or
Header/footer settings. No separate properties dialogs are used. Panel position / opacity can float the
editor over the document; drag its caption to move it, adjust transparency, or
dock it back on the right.
Spacing sets character spacing, line height and the gap after a blank paragraph
line. Grid sets millimetre spacing, visibility and snapping; it is never saved
into the PDF. Header/footer supports current/all pages, alignment, margin and
{page}/{pages}. Add image uses a standard Windows file picker.
Delete removes the selection. Recognise text lines can be disabled to work on
individual PDF fragments when a document groups unexpectedly.

SAVE / SHORTCUTS
Ctrl+S offers an edited copy on first save; subsequent saves use that destination.
Ctrl+Shift+S always opens Save As. Saving flushes a temporary PDF before replacing
the destination, reopens the saved PDF and clears undo history. Failed replacement
retains a recovery copy and reports its path. An asterisk marks unsaved edits.
Ctrl+W closes a tab. Ctrl+0 Fit page; Ctrl+1 Actual size; Ctrl+2 Fit width.
Page Up/Down changes pages. Circular toolbar arrows rotate only the view; view
rotation is not saved. Selection rotation edits PDF objects and is saved.

SCOPE / LIMITS
Editable content is top-level PDF text and images, including many fragmented
horizontal text lines. Nested Form XObjects, outlined/scanned lettering, OCR,
annotations and form fields are not editable through these tools. Line recognition
is geometric and can require disabling for unusual layouts. Existing fragmented
whitespace is normalised when replacing text. Original font shaping, clipping and
layer membership are not reconstructed for replacement text. Move/resize/rotate
preserve existing object content. Replacement uses the chosen embedded Windows
font; it retains the selected text's direction. The on-page typing box is horizontal
and does not reproduce custom line/character spacing until applied.
No automatic paragraph reflow, layer-management panel, compression presets,
printing, page rearrangement, password entry, continuous multi-page canvas or
text search in this release. Existing PDF layers are retained on saving.

BUILD / PRIVACY
Windows 10/11 x64, .NET Framework 4.8. No Python/browser needed to run the app.
Source and installer recipe: https://github.com/DangerousKitty666/VinsPDF
PDF processing is local. Only Check for updates contacts GitHub; PDF content is
never uploaded. Rendering/editing run off the UI thread. Unsaved edited pages stay
loaded for undo and each open tab retains its own document, so memory increases
with tabs/edits. Retain licenses/ when redistributing. Supplied artwork is excluded
from the application source license. See the GitHub build checks for validation.
