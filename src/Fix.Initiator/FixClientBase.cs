using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using QuickFix;
using QuickFix.Fields;

namespace SoftWell.Fix.Initiator;

public abstract class FixClientBase : IFixApplication, IDisposable
{
    private readonly AsyncManualResetEvent _isLoggedIn = new();

    private bool _attemptToChangePasswordWasMade = false;

    private bool _useNewPasswordAsPassword = false;

    protected FixClientBase(
        SessionSettings sessionSettings,
        ILogger logger)
    {
        SessionSettings = sessionSettings ?? throw new ArgumentNullException(nameof(sessionSettings));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public SessionSettings SessionSettings { get; private set; }

    protected ILogger Logger { get; }

    protected Session Session { get; private set; } = null!;

    protected bool IsDisposed { get; private set; }

    public virtual async Task SendMessageAsync(Message m, CancellationToken ct = default)
    {
        await _isLoggedIn.WaitAsync(ct);

        if (Session is null) throw new InvalidOperationException("Session is missing");

        Session.Send(m);
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
        if (IsDisposed) return;

        if (disposing)
        {
            try
            {
                Session?.Dispose();
            }
            catch { }
        }

        IsDisposed = true;
    }

    protected virtual bool IsInvalidPasswordLogout(Message logoutMessage)
    {
        ArgumentNullException.ThrowIfNull(logoutMessage);

        if (!logoutMessage.IsSetField(Text.TAG)) return false;

        var text = logoutMessage.GetField(new Text()).getValue();

        return string.Equals(text, "Rejected Logon Attempt: Invalid Username/Password", StringComparison.Ordinal);
    }

    protected virtual bool IsPasswordChangedLogon(Message logonMessage)
    {
        ArgumentNullException.ThrowIfNull(logonMessage);

        if (!logonMessage.IsSetField(SessionStatus.TAG)) return false;

        var sessionStatus = logonMessage.GetField(new SessionStatus()).getValue();

        return sessionStatus == SessionStatus.SESSION_PASSWORD_CHANGED;
    }

    protected virtual void ToAdminInner(Message message, SessionID sessionID)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(sessionID);

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
                    Logger.LogTrace("{session}: Using field NewPassword as password", sessionID);
                    message.SetField(new Password(settings.GetString("NewPassword")));
                }
                else if (!_attemptToChangePasswordWasMade)
                {
                    message.SetField(new NewPassword(settings.GetString("NewPassword")));
                    attemptToChangePasswordWasMade = true;
                    Logger.LogDebug("{session}: Setting new password", sessionID);
                }
            }

            _attemptToChangePasswordWasMade = attemptToChangePasswordWasMade;
        }
    }

    protected virtual void FromAdminInner(Message message, SessionID sessionID)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(sessionID);

        if (message.IsOfType(MsgType.LOGON))
        {
            if (_attemptToChangePasswordWasMade && IsPasswordChangedLogon(message))
            {
                // мы успешно сменили пароль, и теперь в рамках этого запуска все логины (например, при потере связи) 
                // должны идти с новым паролем
                _useNewPasswordAsPassword = true;
                Logger.LogDebug("New password was successfully set: {session}", sessionID);
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
                    Logger.LogDebug("{session}: New password could not be set due to invalid old password. Will try to use new password as password", sessionID);
                }
                else
                {
                    // мало ли, что могло пойти не так. Время позднее, еще что-то. 
                    // Поэтому если пытались поменять пароль, но нам не сказали "не тот пароль", то будет пытаться еще 
                    _attemptToChangePasswordWasMade = false;
                    Logger.LogDebug("{session}: New password could not be set due to unknown reasons. Will try to set new password again", sessionID);
                }
            }
        }

        HandleIncomingMessage(message, sessionID);
    }

    protected virtual void FromAppInner(Message message, SessionID sessionID)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(sessionID);

        HandleIncomingMessage(message, sessionID);
    }

    protected abstract void HandleIncomingMessage(Message message, SessionID sessionID);

    void IApplication.FromApp(Message message, SessionID sessionID)
    {
        FromAppInner(message, sessionID);
    }

    void IApplication.OnCreate(SessionID sessionID)
    {
        Session = Session.LookupSession(sessionID) ?? throw new ArgumentException("Unknown session id", nameof(sessionID));
        Logger.LogTrace("{session}: Session created", sessionID);
    }

    void IApplication.OnLogon(SessionID sessionID)
    {
        Logger.LogTrace("{session}: LOGON", sessionID);
        _isLoggedIn.Set();
    }

    void IApplication.OnLogout(SessionID sessionID)
    {
        Logger.LogTrace("{session}: LOGOUT", sessionID);
    }

    void IApplication.ToAdmin(Message message, SessionID sessionID)
    {
        ToAdminInner(message, sessionID);
    }

    void IApplication.ToApp(Message message, SessionID sessionID)
    {
    }

    void IApplication.FromAdmin(Message message, SessionID sessionID)
    {
        FromAdminInner(message, sessionID);
    }
}
