using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Api.Tests;

/// <summary>
/// Security (a): SQLite is the design-time snapshot provider. Pending model
/// changes must fail CI there. Npgsql may ignore store-type mapping drift only.
/// </summary>
public class DatabasePendingModelChangesTests
{
    [Fact]
    public void TdAdm101_Sqlite_HasNoPendingModelChanges()
    {
        var options = new DbContextOptionsBuilder<DealowareDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var db = new DealowareDbContext(options);
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
