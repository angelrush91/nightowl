# Nightowl

Nightowl is a cross-platform book cataloging and reading tracker that helps readers catalog physical libraries via ISBN/barcode scanning and monitor reading momentum.

## Language

**Book**:
A publication tracked in the reader's personal library with reading status, metadata, and history.
_Avoid_: CatalogItem, LibraryItem

**ISBN**:
An International Standard Book Number (10 or 13 digits) uniquely identifying a book edition and its barcode representation.
_Avoid_: BarcodeNumber, BookCode

**Book Metadata**:
Published bibliographic details (title, subtitle, authors, page count, publisher, published date, cover art) resolved from an external provider.
_Avoid_: BookDto, ExternalBookInfo, IsbnLookupResult

**Reading Progress**:
The current page and completion percentage of a book being read.
_Avoid_: Bookmark, ReadState

**Reading Session**:
A discrete recorded reading sitting documenting pages read between a start page and end page with optional notes.
_Avoid_: ReadingLog, ReadingEntry, ReadingHistoryItem

**Reading Status**:
The lifecycle state of a book in the library (Want to Read, Currently Reading, Completed, On Hold).
_Avoid_: ShelfState, BookCategory
