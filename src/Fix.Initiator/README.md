# SoftWell.Fix.Initiator

Всякие удобства для клиента, подключающегося по FIX. Позволяет потреблять поток FIX-сообщений как IAsyncEnumerable\<Message\> и отправлять сообщения. Работает с одной сессией.


## Базовые классы и интерфейсы


### [IFixMessagesReader](./IFixMessagesReader.cs)

Интерфейс, превращающий поток FIX-сообщений в IAsyncEnumerable\<Message\>


### [IFixMessagesSender](./IFixMessagesSender.cs)

Интерфейс для отправки FIX-сообщений.


### [IFixClient](./IFixClient.cs) и [FixClient](./FixClient.cs)

Имплементация обоих интерфейсов. 

*Важно:* отправка сообщений возможно только когда клиент получил логон от сервера и не получил после этого логаут (см филд _isLoggedIn). Поэтому вызов SendMessageAsync сначала дожидается успешного логона.


### [FixInitiatorStarterService](./FixInitiatorStarterService.cs)

IHostedService, который запускает IInitiator из [QuickFIXn](https://github.com/connamara/quickfixn) на StartAsync и останавливает на StopAsync

```c#
services.AddFixInitiatorStarter(
    sp =>
    {
        var initiator = new QuickFix.Transport.SocketInitiator(...);
        return initiator;
    },
    "Какое-то читаемое и понятное имя для логов");
```

или 

```c#
services.AddSingleton<FixClient>(...);
// внутри сам создаст QuickFix.Transport.SocketInitiator по настройкам сессии из FixClient
services.AddFixClientInitiatorStarter<FixClient>("Какое-то читаемое и понятное имя для логов");
```


## Messages handling

Базовые классы позволяют кому-то одному читать поток FIX-сообщений. Но что если требуется сделать так, чтобы разные классы обрабатывали разные FIX-сообщения (а в дальнейшем, возможно, распараллеливать их и тд)?

Для этого для каждого [IFixMessagesReader](./IFixMessagesReader.cs) можно настроить свою обработку.


```c#
services.AddSingleton<MyFixClient>(...);
services.AddFixMessagesHandling<MyFixClient>(
    // MyExecutionReportHandler имплементирует IFixMessagesHandler<ExecutionReport>, и ему будут приходить только сообщения типа ExecutionReport
    opts => opts.AddMessagesHandler<ExecutionReport, MyExecutionReportHandler>()
        // MyMessagesHandler имплементирует IFixMessagesHandler, и ему будут приходить все сообщения
        .AddMessagesHandler<MyMessagesHandler>());
// если инициатор не стартанет, то и сообщений не будет
services.AddFixClientInitiatorStarter<MyFixClient>("Какое-то читаемое и понятное имя для логов");
```

Пока что, если нескольким хэндлерам надо обработать одно и то же сообщение, они это будут делать параллельно. И пока все хэндлеры не обработают это сообщение, следующее сообщение будет ждать.

*Важно:* при включении FixMessagesHandling никто другой не должен читать из [IFixMessagesReader](./IFixMessagesReader.cs), для которого включается FixMessagesHandling.


## New password

Если в настройках указано поле NewPassword, то при первом LOGON будет отправлено поле NewPassword с новым паролем.
Если сервер на это ответит LOGOUT: TEXT="Rejected Logon Attempt: Invalid Username/Password", то будет считать, что пароль сменился во время предыдущего запуска, и дальше будем использовать NewPassword в качестве пароля.
Если сервер ответит LOGOUT с другим текстом, то мы будем повторять попытки смены пароля.
Если сервер ответит LOGON: SessionStatus=1 (SESSION_PASSWORD_CHANGED), то мы будем считать, что пароль успешно сменился и дальше будем использовать NewPassword в качестве пароля.

Проверки на некорректность пароля и успешность смены пароля можно изменить, перегрузив методы IsInvalidPasswordLogout и IsPasswordChangedLogon соответственно.