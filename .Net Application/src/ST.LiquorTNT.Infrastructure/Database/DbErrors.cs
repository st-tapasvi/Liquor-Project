using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace ST.LiquorTNT.Infrastructure.Database;

/// <summary>
/// Provider error-code mapping, in one place (standards §8.3). MySQL today; the SQL Server codes
/// (2627 / 2601) are added here when that provider is wired, so no caller ever compares raw numbers.
/// </summary>
internal static class DbErrors
{
    private const int MySqlDuplicateKey = 1062;

    public static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is MySqlException { Number: MySqlDuplicateKey };
}
