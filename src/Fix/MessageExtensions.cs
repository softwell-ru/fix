using System.Diagnostics.CodeAnalysis;
using QuickFix.Fields;

namespace QuickFix;

public static class MessageExtensions
{
    /// <summary>
    /// Проверяет, является ли сообщение нужным нам типом и кастит его в результирующий объект
    /// </summary>
    /// <typeparam name="TMessage">Тип результирующего сообщения</typeparam>
    /// <param name="message">Сообщение</param>
    /// <param name="msgType">Код типа сообщения (значение тэга 35)</param>
    /// <param name="typedMessage">Результирующий объект</param>
    /// <returns>true, если тип нужный, иначе false</returns>
    /// <example>
    /// Проверяем, является ли message сообщением TradeCaptureReport
    /// <code>
    ///     if (msg.IsOfType<TradeCaptureReport>(TradeCaptureReport.MsgType, out var tcr))
    ///     {
    ///         // делаем все, что хотим, с tcr
    ///     }
    /// </code>
    /// </example>
    public static bool IsOfType<TMessage>(
        this Message message,
        string msgType,
        [NotNullWhen(true)] out TMessage? typedMessage)
            where TMessage : Message
    {
        if (!message.IsOfType(msgType))
        {
            typedMessage = null;
            return false;
        }

        typedMessage = (TMessage)message;
        return true;
    }

    /// <summary>
    /// Проверяет, является ли сообщение нужным нам типом
    /// </summary>
    /// <param name="message">Сообщение</param>
    /// <param name="msgType">Код типа сообщения (значение тэга 35)</param>
    /// <returns>true, если тип нужный, иначе false</returns>
    /// <example>
    /// Проверяем, является ли message сообщением TradeCaptureReport
    /// <code>
    ///     if (msg.IsOfType(TradeCaptureReport.MsgType))
    ///     {
    ///         // ...
    ///     }
    /// </code>
    /// </example>
    public static bool IsOfType(
        this Message message,
        string msgType)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(msgType);

        return message.Header.GetString(Tags.MsgType) == msgType;
    }

    /// <summary>
    /// Возвращает типизированное перечисление группы
    /// </summary>
    /// <typeparam name="TGroup">Тип элеметов группы</typeparam>
    /// <param name="fieldMap">Сообщение или группа в сообщении</param>
    /// <param name="groupCountFieldTag">Тэг с количеством элементов нужной группы</param>
    /// <returns>Типизированное перечисление группы</returns>
    /// <example>
    /// Получение сторон из TradeCaptureReport и пати из каждой стороны
    /// <code>
    ///     var tcr = GetTradeCaptureReport(); // как-то получили сообщение GetTradeCaptureReport
    ///     foreach (var side in tcr.GetGroups<TradeCaptureReport.NoSidesGroup>(QuickFix.Fields.Tags.NoSides))
    ///     {
    ///         foreach (var party in side.GetGroups<TradeCaptureReport.NoSidesGroup.NoPartyIDsGroup>(QuickFix.Fields.Tags.NoPartyIDs))
    ///         {
    ///             ...
    ///         }
    ///     }
    /// </code>
    /// </example>
    public static IEnumerable<TGroup> GetGroups<TGroup>(
        this FieldMap fieldMap,
        int groupCountFieldTag)
            where TGroup : Group, new()
    {
        ArgumentNullException.ThrowIfNull(fieldMap);

        if (!fieldMap.IsSetField(groupCountFieldTag)) yield break;

        var count = fieldMap.GetInt(groupCountFieldTag);

        for (var i = 1; i <= count; i++)
        {
            var gr = fieldMap.GetGroup<TGroup>(i);
            yield return gr;
        }
    }

    /// <summary>
    /// Возвращает типизированную группу
    /// </summary>
    /// <typeparam name="TGroup">Тип элеметов группы</typeparam>
    /// <param name="fieldMap">Сообщение или группа в сообщении</param>
    /// <param name="num">Номер группы</param>
    /// <returns>Типизированная группа</returns>
    public static TGroup GetGroup<TGroup>(
        this FieldMap fieldMap,
        int num)
            where TGroup : Group, new()
    {
        ArgumentNullException.ThrowIfNull(fieldMap);

        var gr = new TGroup();
        fieldMap.GetGroup(num, gr);
        return gr;
    }

    /// <summary>
    /// Проверяет, есть ли в <paramref name="fieldMap"/> группа с номером <paramref name="num"/>, подходящая под <paramref name="predicate"/>
    /// </summary>
    /// <typeparam name="TGroup">Тип элеметов группы</typeparam>
    /// <param name="fieldMap">Сообщение или группа в сообщении</param>
    /// <param name="num">Номер группы</param>
    /// <param name="predicate">Предикат</param>
    /// <returns>true/false</returns>
    public static bool HasGroup<TGroup>(
        this FieldMap fieldMap,
        int num,
        Func<TGroup, bool> predicate)
            where TGroup : Group, new()
    {
        ArgumentNullException.ThrowIfNull(fieldMap);

        try
        {
            var gr = fieldMap.GetGroup<TGroup>(num);
            return predicate(gr);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Проверяет, есть ли в <paramref name="fieldMap"/> группа, подходящая под <paramref name="predicate"/>
    /// </summary>
    /// <typeparam name="TGroup">Тип элеметов группы</typeparam>
    /// <param name="fieldMap">Сообщение или группа в сообщении</param>
    /// <param name="groupCountFieldTag">Тэг с количеством элементов нужной группы</param>
    /// /// <param name="predicate">Предикат</param>
    /// <returns>true/false</returns>
    public static bool HasAnyGroup<TGroup>(
        this FieldMap fieldMap,
        int groupCountFieldTag,
        Func<TGroup, bool> predicate)
            where TGroup : Group, new()
    {
        ArgumentNullException.ThrowIfNull(fieldMap);

        foreach (var gr in fieldMap.GetGroups<TGroup>(groupCountFieldTag))
        {
            if (predicate(gr)) return true;
        }

        return false;
    }
}
