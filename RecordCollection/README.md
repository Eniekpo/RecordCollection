# Needle Drop — Vinyl Collection

A personal vinyl collection manager built with .NET 10 and Blazor WebAssembly. It runs entirely in the browser, requires no account or server-side database, and stores the collection as JSON in browser local storage.

## Run locally

```sh
dotnet run
```

Open the local URL printed by the .NET development server. To publish a static build:

```sh
dotnet publish -c Release
```

## Features

- Add, edit, search, filter, sort, rate, tag, and delete records in card or table view.
- Track purchase information, storage location, notes, cover art, play counts, and listening dates.
- Keep a wishlist and move acquired albums directly into the collection.
- View collection, genre, decade, and listening statistics.
- Import JSON/CSV with required-field checks and duplicate detection; export JSON or CSV.
- Create and restore snapshots. Automatic snapshots are enabled by default and retain up to ten prior versions.
- Responsive interface for desktop and mobile layouts.

## Data and privacy

Collection JSON, wishlist data, settings, and backups are kept in this browser's local storage. No application server or external service is contacted. Cover images are stored as data URLs within the local JSON collection; very large images are capped at 2 MB. Browser storage can be cleared by browser settings, so use the export and backup features to keep an independent copy. Browser storage limits also apply to the size of collections and images.

The starter file at `wwwroot/sample-data/collection.json` is optional example data. Import it from **Import & export** when you want to explore the app with sample records.

## Project layout

- `Models/` — collection, record, wishlist, settings, and statistics models.
- `Services/` — JSON persistence, record and wishlist operations, backups, statistics, and import/export.
- `State/` — shared in-memory state.
- `Components/` — reusable record cards, rating, tags, page headers, and empty states.
- `Pages/` — dashboard, records and record form, wishlist, statistics, import/export, and settings.
- `wwwroot/` — static styles and optional sample JSON.

## Technology

.NET 10, Blazor WebAssembly, C#, Bootstrap 5, System.Text.Json, browser local storage, and dependency injection.
