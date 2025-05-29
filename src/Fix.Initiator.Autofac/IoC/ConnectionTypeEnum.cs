namespace SoftWell.Fix.Initiator.Autofac.IoC;

/// <summary>
/// Тип FIX соединения
/// </summary>
public enum ConnectionTypeEnum
{
    /// <summary>
    /// Сервер
    /// </summary>
    Acceptor,

    /// <summary>
    /// Клиент
    /// </summary>
    Initiator
}