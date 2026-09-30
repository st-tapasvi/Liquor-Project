namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>USER_PASSWORD_HISTORY</c>: one previous password hash of a user, so that the last N
/// passwords cannot be reused. Rows are appended by <see cref="USERS.SetPassword"/> only.
/// </summary>
public class USER_PASSWORD_HISTORY
{
    private USER_PASSWORD_HISTORY()
    {
        PasswordHash = string.Empty;
    }

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public string PasswordHash { get; private set; }
    public DateTime CreatedAt { get; private set; }

    internal static USER_PASSWORD_HISTORY Create(string passwordHash, DateTime now) =>
        new() { PasswordHash = passwordHash, CreatedAt = now };
}
