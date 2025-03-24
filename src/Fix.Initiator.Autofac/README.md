# SoftWell.Fix.Initiator.Autofac

Регистрация всего из [Fix.Initiator](../Fix.Initiator/) через Autofac.


### Регистрация FixInitiatorStarter

```c#
containerBuilder.RegisterFixInitiatorStarter(
    ctx =>
    {
        var initiator = new QuickFix.Transport.SocketInitiator(...);
        return initiator;
    },
    "Какое-то читаемое и понятное имя для логов");
```

или 

```c#
// внутри сам создаст QuickFix.Transport.SocketInitiator по настройкам сессии из FixClient
containerBuilder.RegisterFixClientInitiatorStarter<FixClient>("Какое-то читаемое и понятное имя для логов");
```


### Регистрация messages handling


```c#
containerBuilder.RegisterFixMessagesHandling<MyFixClient>(
    // MyExecutionReportHandler имплементирует IFixMessagesHandler<ExecutionReport>, и ему будут приходить только сообщения типа ExecutionReport
    opts => opts.RegisterMessagesHandler<ExecutionReport, MyExecutionReportHandler>()
        // MyMessagesHandler имплементирует IFixMessagesHandler, и ему будут приходить все сообщения
        .RegisterMessagesHandler<MyMessagesHandler>());
// если инициатор не стартанет, то и сообщений не будет
containerBuilder.RegisterFixClientInitiatorStarter<MyFixClient>("Какое-то читаемое и понятное имя для логов");
```