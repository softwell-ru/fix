using System.ComponentModel;
using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Logger;

namespace SoftWell.Fix.Initiator.Autofac.IoC;

public class MicrosoftLoggingLogFactory : ILogFactory
{
    private readonly ILoggerFactory _loggerFactory;

    private readonly ConnectionTypeEnum _connectionTypeEnum;

    public MicrosoftLoggingLogFactory(ConnectionTypeEnum connectionTypeEnum, ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _connectionTypeEnum = connectionTypeEnum;
    }

    public ILog Create(SessionID sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        return new Logger(_connectionTypeEnum, _loggerFactory.CreateLogger($"quickfix:{sessionId}"));
    }

    public ILog CreateNonSessionLog()
    {
        return new Logger(_connectionTypeEnum, _loggerFactory.CreateLogger($"quickfix:nonsession"));
    }

    private sealed class Logger : ILog
    {
        private const string _newPasswordTag = "925=";

        private const string _passwordTag = "554=";

        private readonly ILogger _logger;

        private readonly ConnectionTypeEnum _connectionTypeEnum;

        public Logger(ConnectionTypeEnum connectionTypeEnum, ILogger logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _connectionTypeEnum = connectionTypeEnum;
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
            if (_connectionTypeEnum == ConnectionTypeEnum.Acceptor)
            {
                msg = HidePassword(msg);
            }

            LogTrace("incoming", msg);
        }

        public void OnOutgoing(string msg)
        {
            if (_connectionTypeEnum == ConnectionTypeEnum.Initiator)
            {
                msg = HidePassword(msg);
            }

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

        private string HidePassword(string msg)
        {
            var span = msg.AsSpan();

            // Проверка, что это логон
            if (span.IndexOf("35=A".AsSpan()) == -1)
                return msg;

            // Удалим теги начиная с конца, чтобы индексы не сбились
            var newPasswordIndex = span.IndexOf(_newPasswordTag.AsSpan());
            
            if (newPasswordIndex != -1)
            {
                span = RemovePassword(span, newPasswordIndex);
            }

            var passwordIndex = span.IndexOf(_passwordTag.AsSpan());

            if (passwordIndex != -1)
            {
                span = RemovePassword(span, passwordIndex);
            }

            return span.ToString();
        }

        private string RemovePassword(ReadOnlySpan<char> span, int tagIndex)
        {
            var valueEndIndex = span.Slice(tagIndex).IndexOf('\x01');

            if (valueEndIndex == -1)
                return span[..tagIndex].ToString(); // если нет SOH, обрезаем до тега

            valueEndIndex += tagIndex;
            return string.Concat(span[..tagIndex], span[(valueEndIndex + 1)..]);
        }
    }
}
