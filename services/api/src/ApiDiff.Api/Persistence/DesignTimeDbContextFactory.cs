using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ApiDiff.Api.Persistence;

/// <summary>
/// Lets EF Core tooling (<c>dotnet ef migrations</c>) build the context without
/// starting the web host. The connection string is only used for provider
/// resolution during design time, not for a live connection.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApiDiffDbContext>
{
    public ApiDiffDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=apidiff;Username=apidiff;Password=apidiff";

        var options = new DbContextOptionsBuilder<ApiDiffDbContext>()
            .UseNpgsql(connection)
            .Options;

        return new ApiDiffDbContext(options);
    }
}
