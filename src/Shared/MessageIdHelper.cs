using NServiceBus.Extensibility;

namespace Shared;

public static class MessageIdHelper
{
    private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static string GetHumanReadableMessageId()
    {
        var messageId = new string(Enumerable.Range(0, 4).Select(x => Letters[Random.Shared.Next(Letters.Length)]).ToArray());
        return messageId;
    }
}