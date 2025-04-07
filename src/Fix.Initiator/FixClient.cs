using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using QuickFix;

namespace SoftWell.Fix.Initiator;

public class FixClient : FixClientBase, IFixClient, IDisposable
{
    private readonly Channel<Message> _channel;

    public FixClient(
        SessionSettings sessionSettings,
        ILogger<FixClient> logger) : this(sessionSettings, (ILogger)logger)
    {
    }

    protected FixClient(
        SessionSettings sessionSettings,
        ILogger logger) : base(sessionSettings, logger)
    {
        _channel = InitializeChannel();
    }

    public virtual IAsyncEnumerator<Message> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
    }

    protected virtual Channel<Message> InitializeChannel()
    {
        return Channel.CreateUnbounded<Message>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = true
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (disposing)
        {
            _channel.Writer.TryComplete();
        }
    }

    protected override void HandleIncomingMessage(Message message, SessionID sessionID)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(sessionID);

        _channel.Writer.TryWrite(message);
    }
}
