using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;
using RecordCollection.Models;
using RecordCollection.State;

namespace RecordCollection.Services;

public sealed class RecordService(CollectionState state, IJsonStorageService storage) : IRecordService
{
    public async Task InitializeAsync()
    {
        if (!state.IsLoaded)
            state.Load(await storage.LoadAsync());
    }

    public async Task SaveAsync(VinylRecord record)
    {
        var existing = state.Records.FindIndex(x => x.Id == record.Id);
        if (existing < 0) state.Records.Add(record);
        else state.Records[existing] = record;
        await PersistAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        state.Records.RemoveAll(x => x.Id == id);
        await PersistAsync();
    }

    public async Task MarkPlayedAsync(Guid id)
    {
        var record = state.Records.FirstOrDefault(x => x.Id == id);
        if (record is null) return;
        record.PlayCount++;
        record.LastPlayed = DateTime.Now;
        await PersistAsync();
    }

    private async Task PersistAsync()
    {
        await storage.SaveAsync(state.Data);
        state.MarkSaved();
    }
}

public sealed class WishlistService(CollectionState state, IJsonStorageService storage) : IWishlistService
{
    public async Task SaveAsync(WishlistItem item)
    {
        var index = state.Wishlist.FindIndex(x => x.Id == item.Id);
        if (index < 0) state.Wishlist.Add(item);
        else state.Wishlist[index] = item;
        await PersistAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        state.Wishlist.RemoveAll(x => x.Id == id);
        await PersistAsync();
    }

    public async Task MarkAcquiredAsync(Guid id)
    {
        var wanted = state.Wishlist.FirstOrDefault(x => x.Id == id);
        if (wanted is null) return;
        state.Records.Add(new VinylRecord { Artist = wanted.Artist, AlbumTitle = wanted.AlbumTitle, PurchasePrice = wanted.TargetPrice });
        state.Wishlist.Remove(wanted);
        await PersistAsync();
    }

    private async Task PersistAsync()
    {
        await storage.SaveAsync(state.Data);
        state.MarkSaved();
    }
}

public sealed class BackupService(IJSRuntime js, IJsonStorageService storage, CollectionState state) : IBackupService
{
    private const string Key = "needle-drop.backups.v1";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public async Task CreateBackupAsync()
    {
        var backups = (await ReadAsync()).ToList();
        backups.Insert(0, JsonSerializer.Serialize(state.Data, Options));
        await js.InvokeVoidAsync("localStorage.setItem", Key, JsonSerializer.Serialize(backups.Take(10)));
    }

    public async Task RestoreBackupAsync(string json)
    {
        var data = JsonSerializer.Deserialize<CollectionData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new JsonException("Backup does not contain collection data.");
        await storage.SaveAsync(data);
        state.Load(data);
        state.MarkSaved();
    }

    public async Task<IReadOnlyList<string>> ListBackupsAsync() => await ReadAsync();

    public async Task DeleteBackupAsync(int index)
    {
        var backups = (await ReadAsync()).ToList();
        if (index < 0 || index >= backups.Count) return;
        backups.RemoveAt(index);
        await js.InvokeVoidAsync("localStorage.setItem", Key, JsonSerializer.Serialize(backups));
    }

    private async Task<List<string>> ReadAsync()
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", Key);
        return string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<string>>(json) ?? [];
    }
}

public sealed class StatisticsService : IStatisticsService
{
    public CollectionStatistics Calculate(IEnumerable<VinylRecord> source)
    {
        var records = source.ToList();
        var genres = records.Where(x => !string.IsNullOrWhiteSpace(x.Genre))
            .GroupBy(x => x.Genre.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(x => x.Count()).ToDictionary(x => x.Key, x => x.Count());
        var decades = records.Where(x => x.ReleaseYear.HasValue)
            .GroupBy(x => $"{x.ReleaseYear!.Value / 10 * 10}s")
            .OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Count());
        var prices = records.Where(x => x.PurchasePrice.HasValue).Select(x => x.PurchasePrice!.Value).ToList();
        return new CollectionStatistics
        {
            TotalRecords = records.Count,
            TotalArtists = records.Select(x => x.Artist.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            TotalGenres = genres.Count,
            TotalCollectionValue = prices.Sum(),
            AveragePurchasePrice = prices.Count == 0 ? null : prices.Average(),
            MostCommonGenre = genres.FirstOrDefault().Key ?? "—",
            GenreBreakdown = genres,
            DecadeBreakdown = decades
        };
    }
}

public sealed class ImportExportService : IImportExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private static readonly string[] CsvHeaders = ["Artist", "AlbumTitle", "ReleaseYear", "Genre", "Label", "CatalogNumber", "Format", "Condition", "PurchasePrice", "PurchaseDate", "StorageLocation", "Notes", "PlayCount", "LastPlayed", "Rating", "Tags"];

    public string ExportJson(CollectionData data) => JsonSerializer.Serialize(data, JsonOptions);

    public string ExportCsv(IEnumerable<VinylRecord> records)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', CsvHeaders));
        foreach (var record in records)
        {
            var values = new[]
            {
                record.Artist, record.AlbumTitle, record.ReleaseYear?.ToString(), record.Genre, record.Label, record.CatalogNumber,
                record.Format, record.Condition, record.PurchasePrice?.ToString(CultureInfo.InvariantCulture),
                record.PurchaseDate?.ToString("O"), record.StorageLocation, record.Notes, record.PlayCount.ToString(),
                record.LastPlayed?.ToString("O"), record.Rating.ToString(), string.Join('|', record.Tags)
            };
            builder.AppendLine(string.Join(',', values.Select(EscapeCsv)));
        }
        return builder.ToString();
    }

    public (List<VinylRecord> Records, List<string> Errors, int Duplicates) ImportJson(string json, IEnumerable<VinylRecord> existing)
    {
        var errors = new List<string>();
        try
        {
            using var document = JsonDocument.Parse(json);
            List<VinylRecord> payload;
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                payload = JsonSerializer.Deserialize<List<VinylRecord>>(json, JsonOptions) ?? [];
            }
            else if (document.RootElement.ValueKind == JsonValueKind.Object &&
                     document.RootElement.EnumerateObject().Any(x =>
                         x.Name.Equals("records", StringComparison.OrdinalIgnoreCase) && x.Value.ValueKind == JsonValueKind.Array))
            {
                payload = JsonSerializer.Deserialize<CollectionData>(json, JsonOptions)?.Records ?? [];
            }
            else
            {
                errors.Add("JSON must contain a records array or be an array of records.");
                return ([], errors, 0);
            }
            return ValidateAndDeduplicate(payload, existing, errors);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            errors.Add($"Invalid JSON: {exception.Message}");
            return ([], errors, 0);
        }
    }

    public (List<VinylRecord> Records, List<string> Errors, int Duplicates) ImportCsv(string csv, IEnumerable<VinylRecord> existing)
    {
        var errors = new List<string>();
        var records = new List<VinylRecord>();
        var rows = ParseCsv(csv);
        if (rows.Count < 2) return (records, ["CSV contains no records."], 0);
        var headers = rows[0];
        for (var i = 1; i < rows.Count; i++)
        {
            try
            {
                var cells = rows[i];
                string Get(string name) => headers.IndexOf(name) is var index && index >= 0 && index < cells.Count ? cells[index] : string.Empty;
                records.Add(new VinylRecord
                {
                    Artist = Get("Artist"), AlbumTitle = Get("AlbumTitle"), Genre = Get("Genre"), Label = Get("Label"),
                    CatalogNumber = Get("CatalogNumber"), Format = Get("Format") is { Length: > 0 } format ? format : "LP",
                    Condition = Get("Condition") is { Length: > 0 } condition ? condition : "Very Good+",
                    ReleaseYear = int.TryParse(Get("ReleaseYear"), out var year) ? year : null,
                    PurchasePrice = decimal.TryParse(Get("PurchasePrice"), NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : null,
                    PurchaseDate = DateTime.TryParse(Get("PurchaseDate"), out var date) ? date : null,
                    StorageLocation = Get("StorageLocation"), Notes = Get("Notes"),
                    PlayCount = int.TryParse(Get("PlayCount"), out var plays) ? plays : 0,
                    LastPlayed = DateTime.TryParse(Get("LastPlayed"), out var lastPlayed) ? lastPlayed : null,
                    Rating = int.TryParse(Get("Rating"), out var rating) ? rating : 0,
                    Tags = Get("Tags").Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                });
            }
            catch (FormatException exception) { errors.Add($"Line {i + 1}: {exception.Message}"); }
        }
        return ValidateAndDeduplicate(records, existing, errors);
    }

    private static (List<VinylRecord> Records, List<string> Errors, int Duplicates) ValidateAndDeduplicate(
        IEnumerable<VinylRecord> incoming, IEnumerable<VinylRecord> existing, List<string> errors)
    {
        var known = existing.Select(Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var valid = new List<VinylRecord>();
        var duplicates = 0;
        foreach (var record in incoming)
        {
            if (string.IsNullOrWhiteSpace(record.Artist) || string.IsNullOrWhiteSpace(record.AlbumTitle))
            {
                errors.Add($"Skipped “{record.AlbumTitle}”: artist and album title are required.");
                continue;
            }
            if (record.ReleaseYear is < 1880 or > 2100 || record.PlayCount < 0 || record.Rating is < 0 or > 5 || record.PurchasePrice < 0)
            {
                errors.Add($"Skipped “{record.AlbumTitle}”: year, play count, rating, or purchase price is outside its valid range.");
                continue;
            }
            if (!known.Add(Key(record))) { duplicates++; continue; }
            if (record.Id == Guid.Empty) record.Id = Guid.NewGuid();
            record.Tags ??= [];
            record.Tags = record.Tags.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            valid.Add(record);
        }
        return (valid, errors, duplicates);
    }

    private static string Key(VinylRecord record) => $"{record.Artist.Trim()}|{record.AlbumTitle.Trim()}";
    private static string EscapeCsv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";

    private static List<List<string>> ParseCsv(string csv)
    {
        var rows = new List<List<string>>();
        var fields = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < csv.Length; i++)
        {
            var character = csv[i];
            if (character == '"' && quoted && i + 1 < csv.Length && csv[i + 1] == '"') { value.Append('"'); i++; }
            else if (character == '"') quoted = !quoted;
            else if (character == ',' && !quoted) { fields.Add(value.ToString()); value.Clear(); }
            else if (character == '\n' && !quoted)
            {
                fields.Add(value.ToString().TrimEnd('\r'));
                value.Clear();
                if (fields.Any(x => x.Length > 0)) rows.Add(fields);
                fields = [];
            }
            else value.Append(character);
        }
        fields.Add(value.ToString().TrimEnd('\r'));
        if (fields.Any(x => x.Length > 0)) rows.Add(fields);
        return rows;
    }
}

public sealed class SettingsService(CollectionState state, IJsonStorageService storage) : ISettingsService
{
    public async Task SaveAsync(CollectionSettings settings)
    {
        state.Data.Settings = settings;
        await storage.SaveAsync(state.Data);
        state.MarkSaved();
    }
}
