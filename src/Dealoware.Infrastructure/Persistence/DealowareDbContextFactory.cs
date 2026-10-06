using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for <c>dotnet ef migrations</c>. Uses SQLite only to
/// scaffold provider-agnostic migrations; it is not used at API runtime.
/// </summary>
public sealed class DealowareDbContextFactory : IDesignTimeDbContextFactory<DealowareDbContext>
{
    public DealowareDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DealowareDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;
        return new DealowareDbContext(options);
    }
}
