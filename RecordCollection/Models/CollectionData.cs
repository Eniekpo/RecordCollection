namespace RecordCollection.Models;

/// <summary>All data stored by this application.</summary>
public sealed class CollectionData
{
    public List<VinylRecord> Records { get; set; } = [];
    public List<WishlistItem> Wishlist { get; set; } = [];
    public CollectionSettings Settings { get; set; } = new();
}
