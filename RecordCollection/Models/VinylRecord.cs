using System.ComponentModel.DataAnnotations;

namespace RecordCollection.Models;

/// <summary>Represents a record in the personal collection.</summary>
public sealed class VinylRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, StringLength(120)]
    public string Artist { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string AlbumTitle { get; set; } = string.Empty;

    [Range(1880, 2100)]
    public int? ReleaseYear { get; set; }

    [StringLength(80)]
    public string Genre { get; set; } = string.Empty;

    [StringLength(100)]
    public string Label { get; set; } = string.Empty;

    [StringLength(80)]
    public string CatalogNumber { get; set; } = string.Empty;

    public string Format { get; set; } = "LP";
    public string Condition { get; set; } = "Very Good+";
    public decimal? PurchasePrice { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public string StorageLocation { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Notes { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int PlayCount { get; set; }

    public DateTime? LastPlayed { get; set; }

    [Range(0, 5)]
    public int Rating { get; set; }

    public List<string> Tags { get; set; } = [];
    public string? CoverImagePath { get; set; }
}
