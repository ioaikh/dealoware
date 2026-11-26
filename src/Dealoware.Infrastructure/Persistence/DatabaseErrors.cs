using Microsoft.Data.Sqlite;
using Npgsql;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// Classifies database faults by provider error code, never by message text.
/// </summary>
public static class DatabaseErrors
{
    private const int SqliteConstraint = 19;
    private const int SqliteConstraintPrimaryKey = 1555;
    private const int SqliteConstraintUnique = 2067;

    /// <summary>
    /// True only for a unique or primary-key violation: PostgreSQL SQLSTATE
    /// 23505, SQLite SQLITE_CONSTRAINT_UNIQUE / SQLITE_CONSTRAINT_PRIMARYKEY.
    /// Any other fault (other constraints, deadlocks, lost connections) is false.
    /// </summary>
    public static bool IsUniqueViolation(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return true;
            }

            if (current is SqliteException
                {
                    SqliteErrorCode: SqliteConstraint,
                    SqliteExtendedErrorCode: SqliteConstraintPrimaryKey or SqliteConstraintUnique
                })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// True only when PostgreSQL stopped a transaction because it collided
    /// with another one: SQLSTATE 40P01 (deadlock detected) or 40001
    /// (serialization failure). The transaction is rolled back; nothing of it
    /// is stored, and the same request can be made again.
    /// </summary>
    public static bool IsTransactionCollision(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException
                {
                    SqlState: PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.SerializationFailure
                })
            {
                return true;
            }
        }

        return false;
    }
}
