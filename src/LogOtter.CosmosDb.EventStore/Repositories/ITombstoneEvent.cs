namespace LogOtter.CosmosDb.EventStore;

public interface ITombstoneEvent<TSnapshot>
    where TSnapshot : ISnapshot
{
    public TSnapshot Snapshot { get; }
}
