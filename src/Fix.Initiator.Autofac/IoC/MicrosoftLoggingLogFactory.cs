using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Logger;

namespace SoftWell.Fix.Initiator.Autofac.IoC;

public class MicrosoftLoggingLogFactory : ILogFactory
{
    private readonly ILoggerFactory _loggerFactory;

    private readonly ConnectionTypeEnum _connectionType;

    public MicrosoftLoggingLogFactory(ConnectionTypeEnum connectionType, ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _connectionType = connectionType;
    }

    public ILog Create(SessionID sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        return new Logger(_connectionType, _loggerFactory.CreateLogger($"quickfix:{sessionId}"));
    }

    public ILog CreateNonSessionLog()
    {
        return new Logger(_connectionType, _loggerFactory.CreateLogger($"quickfix:nonsession"));
    }

    private sealed class Logger : ILog
    {
        private const string _newPasswordTag = "925=";

        private const string _passwordTag = "554=";

        private const string _logonTag = "35=A";

        private readonly ILogger _logger;

        private readonly ConnectionTypeEnum _connectionType;

        public Logger(ConnectionTypeEnum connectionType, ILogger logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _connectionType = connectionType;
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
            if (_connectionType == ConnectionTypeEnum.Acceptor && NeedHidePassword(msg, out var span))
            {
                LogTrace("Incoming", HidePassword(new string(msg), span));
            }
            else
            {
                LogTrace("Incoming", msg);
            }
        }

        public void OnOutgoing(string msg)
        {
            if (_connectionType == ConnectionTypeEnum.Initiator && NeedHidePassword(msg, out var span))
            {
                LogTrace("Outgoing", HidePassword(new string(msg), span));
            }
            else
            {
                LogTrace("Outgoing", msg);
            }
        }

        private void LogDebug(string category, string message)
        {
            _logger.LogDebug("{fixCategory}: {fixMessageStr}", category, message);
        }

        private void LogTrace(string category, string message)
        {
            _logger.LogTrace("{fixCategory}: {fixMessageStr}", category, message);
        }

        private string HidePassword(string msg, ReadOnlySpan<char> span)
        {
            // Удалим теги начиная с конца, чтобы индексы не сбились
            var newPasswordIndex = span.IndexOf(_newPasswordTag.AsSpan());

            if (newPasswordIndex != -1)
            {
                msg = HideChars(msg, span, newPasswordIndex, _newPasswordTag);
            }

            var passwordIndex = span.IndexOf(_passwordTag.AsSpan());

            if (passwordIndex != -1)
            {
                msg = HideChars(msg, span, passwordIndex, _passwordTag);
            }

            return msg;
        }

        private string HideChars(string msg, ReadOnlySpan<char> span, int tagIndex, string tag)
        {
            var valueStartIndex = tagIndex + tag.Length; // позиция после '554=' или '925='
            var valueEndRelative = span.Slice(valueStartIndex).IndexOf('\x01');

            if (valueEndRelative == -1)
                return msg; // если нет окончания тега, возвращаем как есть

            var valueEndIndex = valueStartIndex + valueEndRelative;

            unsafe
            {
                fixed (char* arr = msg)
                {
                    for (var i = valueStartIndex; i < valueEndIndex; i++)
                    {
                        arr[i] = '*';
                    }
                }
            }

            return msg;
        }

        private static bool NeedHidePassword(string msg, out ReadOnlySpan<char> span)
        {
            span = msg.AsSpan();

            // Проверка, что это логон
            if (span.IndexOf(_logonTag.AsSpan()) == -1) return false;

            return true;
        }
    }
}
