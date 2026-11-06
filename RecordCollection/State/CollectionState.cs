using RecordCollection.Models;

namespace RecordCollection.State;

/// <summary>In-memory application state, synchronized with local JSON storage.</summary>
public sealed class CollectionState
{
    public CollectionData Data { get; private set; } = new();
    public bool IsLoaded { get; private set; }
    public DateTime? LastSaved { get; private set; }

    public List<VinylRecord> Records => Data.Records;
    public List<WishlistItem> Wishlist => Data.Wishlist;
    public CollectionSettings Settings => Data.Settings;

    public event Action? Changed;

    public void Load(CollectionData data)
    {
        Data = data;
        IsLoaded = true;
        Notify();
    }

    public void MarkSaved()
    {
        LastSaved = DateTime.Now;
        Notify();
    }

    public void Notify() => Changed?.Invoke();
}
