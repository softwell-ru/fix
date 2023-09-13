using System.Collections.Concurrent;
using System.Threading.Channels;
using Moq;
using QuickFix;

namespace SoftWell.Fix.Initiator.Testing;

public class TestFixClient<TFixClient> : IDisposable
    where TFixClient : class, IFixClient
{
    private readonly Mock<TFixClient> _mock = new(MockBehavior.Strict);

    private readonly Channel<Message> _incomingChannel;

    private readonly ConcurrentBag<Message> _outgoingMessages = new();

    private bool _disposedValue;

    public TestFixClient()
    {
        _incomingChannel = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = true
        });

        _mock.Setup(x => x.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct => _incomingChannel.Reader.ReadAllAsync(ct).GetAsyncEnumerator(ct))
            .Verifiable();

        _mock.Setup(x => x.SendMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Callback<Message, CancellationToken>((m, _) => _outgoingMessages.Add(m))
            .Returns(Task.CompletedTask)
            .Verifiable();
    }

    public TFixClient Object => _mock.Object;

    protected TimeSpan WaitForOutgoingMessageInterval { get; } = TimeSpan.FromMilliseconds(100);

    public void EmulateIncomingMessage(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _incomingChannel.Writer.TryWrite(message);
    }

    public async Task<TMessage> WaitForOutgoingMessageAsync<TMessage>(
        string msgType,
        Func<TMessage, bool> filter,
        CancellationToken ct = default)
            where TMessage : Message
    {
        var message = await WaitForOutgoingMessageAsync(
            m => m.IsOfType<TMessage>(msgType, out var tm) && filter(tm),
            ct);

        return (TMessage)message;
    }

    public async Task<TMessage> WaitForOutgoingMessageAsync<TMessage>(
        string msgType,
        CancellationToken ct = default)
            where TMessage : Message
    {
        ArgumentNullException.ThrowIfNull(msgType);

        var message = await WaitForOutgoingMessageAsync(
            m => m.IsOfType<TMessage>(msgType, out var _),
            ct);

        return (TMessage)message;
    }

    public async Task<Message> WaitForOutgoingMessageAsync(
        Func<Message, bool> filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        while (true)
        {
            await Task.Delay(WaitForOutgoingMessageInterval, ct);

            var current = _outgoingMessages.ToList();

            foreach (var m in current)
            {
                if (filter(m)) return m;
            }
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue) return;

        if (disposing)
        {
            _incomingChannel.Writer.TryComplete();
        }

        _disposedValue = true;
    }
}