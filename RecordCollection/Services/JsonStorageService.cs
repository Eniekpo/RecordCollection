using System.Text.Json;
using Microsoft.JSInterop;
using RecordCollection.Models;

namespace RecordCollection.Services;

/// <summary>Persists the collection as JSON in the browser's local storage.</summary>
public sealed class JsonStorageService(IJSRuntime js, ILogger<JsonStorageService> logger) : IJsonStorageService
{
    private const string StorageKey = "needle-drop.collection.v1";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public async Task<CollectionData> LoadAsync()
    {
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            return string.IsNullOrWhiteSpace(json)
                ? new CollectionData()
                : JsonSerializer.Deserialize<CollectionData>(json, Options) ?? new CollectionData();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load collection JSON from browser storage.");
            return new CollectionData();
        }
    }

    public async Task SaveAsync(CollectionData data)
    {
        try
        {
            var previous = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrWhiteSpace(previous) && data.Settings.AutoBackupEnabled)
            {
                try
                {
                    var backupsJson = await js.InvokeAsync<string?>("localStorage.getItem", "needle-drop.backups.v1");
                    var backups = string.IsNullOrWhiteSpace(backupsJson)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(backupsJson) ?? [];
                    backups.Insert(0, previous);
                    await js.InvokeVoidAsync("localStorage.setItem", "needle-drop.backups.v1", JsonSerializer.Serialize(backups.Take(10)));
                }
                catch (Exception backupException)
                {
                    logger.LogWarning(backupException, "Automatic backup could not be written; continuing with the collection save.");
                }
            }
            await js.InvokeVoidAsync("localStorage.setItem", StorageKey, JsonSerializer.Serialize(data, Options));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not save collection JSON to browser storage.");
            throw;
        }
    }

    public Task<bool> IsValidJsonAsync(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var isRecordArray = root.ValueKind == JsonValueKind.Array;
            var hasRecordsArray = root.ValueKind == JsonValueKind.Object &&
                root.EnumerateObject().Any(x =>
                    x.Name.Equals("records", StringComparison.OrdinalIgnoreCase) && x.Value.ValueKind == JsonValueKind.Array);
            return Task.FromResult(isRecordArray || hasRecordsArray);
        }
        catch (JsonException)
        {
            return Task.FromResult(false);
        }
    }
}
