using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NosVemos.OrquestadorIA.Api.Services;

internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AnalisisDbContext>
{
    public AnalisisDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AnalisisDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=IADB;User Id=sa;Password=Your_strong_password_123!;TrustServerCertificate=True");
        return new AnalisisDbContext(optionsBuilder.Options);
    }
}
