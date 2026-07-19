using System.Threading.Channels;

namespace ApiDiff.Api.Orchestration;

/// <summary>Unbounded in-process queue of run ids awaiting orchestration.</summary>
public sealed class RunQueue : IRunQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public void Enqueue(Guid runId) => _channel.Writer.TryWrite(runId);

    public ValueTask<Guid> DequeueAsync(CancellationToken ct) => _channel.Reader.ReadAsync(ct);
}
