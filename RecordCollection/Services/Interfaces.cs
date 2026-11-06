using RecordCollection.Models;

namespace RecordCollection.Services;

public interface IJsonStorageService
{
    Task<CollectionData> LoadAsync();
    Task SaveAsync(CollectionData data);
    Task<bool> IsValidJsonAsync(string json);
}

public interface IRecordService
{
    Task InitializeAsync();
    Task SaveAsync(VinylRecord record);
    Task DeleteAsync(Guid id);
    Task MarkPlayedAsync(Guid id);
}

public interface IWishlistService
{
    Task SaveAsync(WishlistItem item);
    Task DeleteAsync(Guid id);
    Task MarkAcquiredAsync(Guid id);
}

public interface IBackupService
{
    Task CreateBackupAsync();
    Task RestoreBackupAsync(string json);
    Task<IReadOnlyList<string>> ListBackupsAsync();
    Task DeleteBackupAsync(int index);
}

public interface IStatisticsService
{
    CollectionStatistics Calculate(IEnumerable<VinylRecord> records);
}

public interface IImportExportService
{
    string ExportJson(CollectionData data);
    string ExportCsv(IEnumerable<VinylRecord> records);
    (List<VinylRecord> Records, List<string> Errors, int Duplicates) ImportJson(string json, IEnumerable<VinylRecord> existing);
    (List<VinylRecord> Records, List<string> Errors, int Duplicates) ImportCsv(string csv, IEnumerable<VinylRecord> existing);
}

public interface ISettingsService
{
    Task SaveAsync(CollectionSettings settings);
}
