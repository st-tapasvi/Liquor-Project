using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace ST.LiquorTNT.Infrastructure.Database;

/// <summary>
/// Provider error-code mapping, in one place (standards §8.3), so no caller ever compares raw numbers.
/// </summary>
internal static class DbErrors
{
    private const int MySqlDuplicateKey = 1062;
    private const int SqlServerUniqueConstraint = 2627;     // PRIMARY KEY / UNIQUE constraint
    private const int SqlServerUniqueIndex = 2601;          // unique index (incl. the filtered ones)

    public static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is MySqlException { Number: MySqlDuplicateKey }
            or SqlException { Number: SqlServerUniqueConstraint or SqlServerUniqueIndex };
}
