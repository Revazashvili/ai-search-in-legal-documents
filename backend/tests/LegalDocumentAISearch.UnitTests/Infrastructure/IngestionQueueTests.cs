using LegalDocumentAISearch.Infrastructure.Services;

namespace LegalDocumentAISearch.UnitTests.Infrastructure;

public class IngestionQueueTests
{
    [Fact]
    public async Task EnqueueThenReadAllAsync_ReturnsEnqueuedId()
    {
        var queue = new IngestionQueue();
        var id = Guid.NewGuid();
        queue.Enqueue(id);

        using var cts = new CancellationTokenSource();
        var collected = new List<Guid>();

        try
        {
            await foreach (var item in queue.ReadAllAsync(cts.Token))
            {
                collected.Add(item);
                // Cancel after receiving the first item so the loop ends
                cts.Cancel();
            }
        }
        catch (OperationCanceledException) { /* expected when CTS is cancelled */ }

        Assert.Single(collected);
        Assert.Equal(id, collected[0]);
    }

    [Fact]
    public async Task MultipleEnqueues_ReturnsAllIdsInOrder()
    {
        var queue = new IngestionQueue();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        queue.Enqueue(id1);
        queue.Enqueue(id2);
        queue.Enqueue(id3);

        using var cts = new CancellationTokenSource();
        var collected = new List<Guid>();

        try
        {
            await foreach (var item in queue.ReadAllAsync(cts.Token))
            {
                collected.Add(item);
                if (collected.Count == 3) cts.Cancel();
            }
        }
        catch (OperationCanceledException) { /* expected when CTS is cancelled */ }

        Assert.Equal(3, collected.Count);
        Assert.Equal(id1, collected[0]);
        Assert.Equal(id2, collected[1]);
        Assert.Equal(id3, collected[2]);
    }

    [Fact]
    public async Task QueueIsEmpty_AfterCancellation()
    {
        var queue = new IngestionQueue();
        using var cts = new CancellationTokenSource();

        // Cancel immediately — no items enqueued
        cts.Cancel();

        var collected = new List<Guid>();

        try
        {
            await foreach (var item in queue.ReadAllAsync(cts.Token))
            {
                collected.Add(item);
            }
        }
        catch (OperationCanceledException) { /* expected */ }

        Assert.Empty(collected);
    }
}
