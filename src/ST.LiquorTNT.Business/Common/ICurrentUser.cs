namespace ST.LiquorTNT.Business.Common;

/// <summary>The authenticated caller, resolved from the token by the API layer.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    int? UserId { get; }
    string? UserName { get; }
    IReadOnlyCollection<string> Rights { get; }
}
