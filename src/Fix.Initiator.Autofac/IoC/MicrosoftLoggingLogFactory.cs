using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Logger;

namespace SoftWell.Fix.Initiator.Autofac.IoC;

public class MicrosoftLoggingLogFactory : ILogFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public MicrosoftLoggingLogFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public ILog Create(SessionID sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        return new Logger(_loggerFactory.CreateLogger($"quickfix:{sessionId}"));
    }

    public ILog CreateNonSessionLog()
    {
        return new Logger(_loggerFactory.CreateLogger($"quickfix:nonsession"));
    }

    private sealed class Logger : ILog
    {
        private readonly ILogger _logger;

        public Logger(ILogger logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public void Clear()
        {
        }

        public void Dispose()
        {
        }

        public void OnEvent(string s)
        {
            LogDebug("event", s);
        }

        public void OnIncoming(string msg)
        {
            // TODO: mask password for 35=A
            LogTrace("incoming", msg);
        }

        public void OnOutgoing(string msg)
        {
            LogTrace("outgoing", msg);
        }

        private void LogDebug(string category, string message)
        {
            _logger.LogDebug("{fixCategory}: {fixMessageStr}", category, message);
        }

        private void LogTrace(string category, string message)
        {
            _logger.LogTrace("{fixCategory}: {fixMessageStr}", category, message);
        }
    }
}
