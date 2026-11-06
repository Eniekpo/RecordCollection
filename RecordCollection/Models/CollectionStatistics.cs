namespace RecordCollection.Models;

/// <summary>Calculated summary values for the collection.</summary>
public sealed class CollectionStatistics
{
    public int TotalRecords { get; init; }
    public int TotalArtists { get; init; }
    public int TotalGenres { get; init; }
    public decimal TotalCollectionValue { get; init; }
    public decimal? AveragePurchasePrice { get; init; }
    public string MostCommonGenre { get; init; } = "—";
    public Dictionary<string, int> GenreBreakdown { get; init; } = [];
    public Dictionary<string, int> DecadeBreakdown { get; init; } = [];
}
