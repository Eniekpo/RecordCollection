namespace RecordCollection.Models;

/// <summary>User preferences for the collection manager.</summary>
public sealed class CollectionSettings
{
    public bool AutoBackupEnabled { get; set; } = true;
    public string BackupLocation { get; set; } = "Browser storage";
    public string DefaultSortOrder { get; set; } = "Artist";
}
