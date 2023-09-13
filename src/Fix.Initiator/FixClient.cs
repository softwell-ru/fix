using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using QuickFix;
using QuickFix.Fields;

namespace SoftWell.Fix.Initiator;

public class FixClient : IFixClient, IDisposable
{
    private readonly Channel<Message> _channel;

    private readonly ILogger _logger;

    private readonly AsyncManualResetEvent _isLoggedIn = new();

    private bool _attemptToChangePasswordWasMade = false;

    private bool _useNewPasswordAsPassword = false;

    private bool _disposedValue;

    public FixClient(
        SessionSettings sessionSettings,
        ILogger<FixClient> logger) : this(sessionSettings, (ILogger)logger)
    {
    }

    protected FixClient(
        SessionSettings sessionSettings,
        ILogger logger)
    {
        SessionSettings = sessionSettings ?? throw new ArgumentNullException(nameof(sessionSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _channel = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = true
        });
    }

    protected Session Session { get; private set; } = null!;

    public SessionSettings SessionSettings { get; private set; }

    public virtual async Task SendMessageAsync(Message m, CancellationToken ct = default)
    {
        await _isLoggedIn.WaitAsync(ct);

        if (Session is null) throw new InvalidOperationException("Session is missing");

        Session.Send(m);
    }

    public virtual IAsyncEnumerator<Message> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
    }

    public virtual void Logout()
    {
        _isLoggedIn.Reset();
        if (Session is null) throw new InvalidOperationException("Session is missing");

        Session.Logout();
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
            _channel.Writer.TryComplete();
            try
            {
                Session?.Dispose();
            }
            catch { }
        }

        _disposedValue = true;
    }

    protected virtual bool IsInvalidPasswordLogout(Message logoutMessage)
    {
        ArgumentNullException.ThrowIfNull(logoutMessage);

        if (!logoutMessage.IsSetField(new Text())) return false;

        var text = logoutMessage.GetField(new Text()).getValue();

        return string.Equals(text, "Rejected Logon Attempt: Invalid Username/Password", StringComparison.Ordinal);
    }

    protected virtual bool IsPasswordChangedLogon(Message logonMessage)
    {
        ArgumentNullException.ThrowIfNull(logonMessage);

        if (!logonMessage.IsSetField(new SessionStatus())) return false;

        var sessionStatus = logonMessage.GetField(new SessionStatus()).getValue();

        return sessionStatus == SessionStatus.SESSION_PASSWORD_CHANGED;
    }

    void IApplication.FromAdmin(Message message, SessionID sessionID)
    {
        if (message.IsOfType(MsgType.LOGON))
        {
            if (_attemptToChangePasswordWasMade && IsPasswordChangedLogon(message))
            {
                // мы успешно сменили пароль, и теперь в рамках этого запуска все логины (например, при потере связи) 
                // должны идти с новым паролем
                _useNewPasswordAsPassword = true;
                _logger.LogDebug("New password was successfully set: {session}", sessionID);
            }
        }
        else if (message.IsOfType(MsgType.LOGOUT))
        {
            if (_attemptToChangePasswordWasMade)
            {
                if (IsInvalidPasswordLogout(message))
                {
                    // значит, либо нам текущий пароль задали криво, либо мы уже поменяли его на новый, 
                    // но еще не обновили конфиг,
                    // и теперь надо использовать новый
                    _useNewPasswordAsPassword = true;
                    _logger.LogDebug("{session}: New password could not be set due to invalid old password. Will try to use new password as password", sessionID);
                }
                else
                {
                    // мало ли, что могло пойти не так. Время позднее, еще что-то. 
                    // Поэтому если пытались поменять пароль, но нам не сказали "не тот пароль", то будет пытаться еще 
                    _attemptToChangePasswordWasMade = false;
                    _logger.LogDebug("{session}: New password could not be set due to unknown reasons. Will try to set new password again", sessionID);
                }
            }
        }

        _channel.Writer.TryWrite(message);
    }

    void IApplication.FromApp(Message message, SessionID sessionID)
    {
        _channel.Writer.TryWrite(message);
    }

    void IApplication.OnCreate(SessionID sessionID)
    {
        Session = Session.LookupSession(sessionID);
        _logger.LogTrace("{session}: Session created", sessionID);
    }

    void IApplication.OnLogon(SessionID sessionID)
    {
        _logger.LogTrace("{session}: LOGON", sessionID);
        _isLoggedIn.Set();
    }

    void IApplication.OnLogout(SessionID sessionID)
    {
        _logger.LogTrace("{session}: LOGOUT", sessionID);
    }

    void IApplication.ToAdmin(Message message, SessionID sessionID)
    {
        if (message.IsOfType(MsgType.LOGON))
        {
            var settings = SessionSettings.Get(sessionID);

            var attemptToChangePasswordWasMade = false;

            if (settings.Has("Username"))
            {
                message.SetField(new Username(settings.GetString("Username")));
            }

            if (settings.Has("Password") && !_useNewPasswordAsPassword)
            {
                message.SetField(new Password(settings.GetString("Password")));
            }

            if (settings.Has("NewPassword"))
            {
                if (_useNewPasswordAsPassword)
                {
                    _logger.LogTrace("{session}: Using field NewPassword as password", sessionID);
                    message.SetField(new Password(settings.GetString("NewPassword")));
                }
                else if (!_attemptToChangePasswordWasMade)
                {
                    message.SetField(new NewPassword(settings.GetString("NewPassword")));
                    attemptToChangePasswordWasMade = true;
                    _logger.LogDebug("{session}: Setting new password", sessionID);
                }
            }

            _attemptToChangePasswordWasMade = attemptToChangePasswordWasMade;
        }
    }

    void IApplication.ToApp(Message message, SessionID sessionID)
    {
    }
}
