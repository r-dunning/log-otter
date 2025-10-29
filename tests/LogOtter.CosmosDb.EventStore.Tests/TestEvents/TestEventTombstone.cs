namespace LogOtter.CosmosDb.EventStore.Tests.TestEvents;

public class TestEventTombstone(TestEventProjection snapshot, string id) : TestEvent(id), ITombstoneEvent<TestEventProjection>
{
    public override void Apply(TestEventProjection model, EventInfo eventInfo)
    {
        //noop
    }

    public TestEventProjection Snapshot { get; } = snapshot;
}
