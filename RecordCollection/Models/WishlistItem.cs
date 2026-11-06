using System.ComponentModel.DataAnnotations;

namespace RecordCollection.Models;

/// <summary>Represents an album the collector would like to acquire.</summary>
public sealed class WishlistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, StringLength(120)]
    public string Artist { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string AlbumTitle { get; set; } = string.Empty;

    public string DesiredPressing { get; set; } = string.Empty;
    public decimal? TargetPrice { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Notes { get; set; } = string.Empty;
}
