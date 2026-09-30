namespace ST.LiquorTNT.Contracts.Users;

public sealed class UserListRequest
{
    /// <summary>Matches user name or full name (contains).</summary>
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
