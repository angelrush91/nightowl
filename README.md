# 🦉 Nightowl - Book Cataloging & Reading Tracker

Nightowl is a cross-platform mobile application built with **.NET MAUI Blazor Hybrid** designed to help readers catalog their personal libraries and track reading progress using book barcodes or ISBNs.

---

## 🚀 Key Features

- **Barcode Scanning & Manual ISBN Lookup**: Scan the barcode on the back of any physical book using your device camera, or type the ISBN directly with real-time ISBN-10 / ISBN-13 checksum validation.
- **Open-Source ISBN Integration**: Seamlessly fetches book titles, authors, publishers, publication dates, page counts, descriptions, and cover art from the open-source **OpenLibrary API** (Internet Archive) with fallback support.
- **Reading Progress Tracking**: Interactive page sliders, "+10 / +25 / +50 page" increment chips, percentage progress bars, and reading session logs with notes.
- **Shelf Lifecycle Management**: Organize books into *Want to Read*, *Currently Reading*, *Completed*, and *On Hold*.
- **Personal Reviews & Ratings**: Rate finished books (1–5 stars) and jot down personal reviews or memorable quotes.
- **Reading Statistics**: Comprehensive overview of completion rates, total pages read, active reads, and average ratings.
- **Offline Persistence**: Fast, reliable local persistence powered by **SQLite** and **Entity Framework Core**.
- **Modern UI / UX**: Sleek midnight theme powered by **MudBlazor**, responsive for both mobile and desktop screens.

---

## 🏛️ Clean Architecture & Design Principles

```
Nightowl.sln
├── src/
│   ├── Nightowl.Domain/         # Core Entities (Book, ReadingSession), Value Objects (Isbn, ReadingProgress), Enums, Invariants
│   ├── Nightowl.Application/    # Use Cases, DTOs, Business Services, Interfaces (IBookService, IIsbnLookupService)
│   ├── Nightowl.Infrastructure/ # EF Core SQLite DbContext, OpenLibrary & Google Books API Clients, Repositories
│   └── Nightowl.Maui/           # MAUI Blazor Hybrid Host (Android & Windows), MudBlazor UI, Camera Barcode Scanner
└── tests/
    └── Nightowl.Tests/          # xUnit Test Suite (TDD, In-memory SQLite, Domain & Service tests)
```

- **Domain-Driven Design (DDD)**: `Isbn` and `ReadingProgress` are encapsulated Value Objects protecting domain invariants; `Book` is an Aggregate Root managing its reading sessions.
- **SOLID & Clean Architecture**: Complete separation of concerns with dependency inversion via abstractions.
- **DRY & KISS**: Shared DTOs, clean reusable MudBlazor components, no unnecessary bloat.
- **TDD**: Fully verified with unit and integration tests covering checksums, boundary conditions, and database persistence.

---

## 🛠️ Tech Stack

- **Framework**: .NET MAUI Blazor Hybrid (.NET 10 / .NET 9 compatible)
- **Target Platforms**: Android (`net10.0-android`) & Windows Desktop (`net10.0-windows10.0.19041.0`)
- **UI Framework**: [MudBlazor](https://mudblazor.com/)
- **ORM & Database**: Entity Framework Core 10 & SQLite
- **Book API**: OpenLibrary API (100% open source & open data)
- **Testing**: xUnit, FluentAssertions, Moq, EF Core In-Memory SQLite

---

## 🧪 Running Tests

To run the complete automated test suite:

```powershell
dotnet test tests/Nightowl.Tests/Nightowl.Tests.csproj
```

---

## 📱 Running the App

### Windows Desktop (for quick development and testing):
```powershell
dotnet build src/Nightowl.Maui/Nightowl.Maui.csproj -f net10.0-windows10.0.19041.0
dotnet run --project src/Nightowl.Maui/Nightowl.Maui.csproj -f net10.0-windows10.0.19041.0
```

### Android:
Deploy to an attached Android phone or emulator:
```powershell
dotnet build src/Nightowl.Maui/Nightowl.Maui.csproj -f net10.0-android
```
