namespace ST.LiquorTNT.Contracts.Common;

/// <summary>The body of an action that has no data to return (logout, verify, reset …): says what happened.</summary>
public sealed class MessageResponse
{
    public string Message { get; set; } = string.Empty;

    public static MessageResponse Of(string message) => new() { Message = message };
}
