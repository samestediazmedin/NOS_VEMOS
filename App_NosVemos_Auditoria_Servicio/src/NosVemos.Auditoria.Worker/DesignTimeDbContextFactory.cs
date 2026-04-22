using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NosVemos.Auditoria.Worker;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AuditoriaDbContext>
{
    public AuditoriaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuditoriaDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=AuditoriaDB;User Id=sa;Password=Your_strong_password_123!;TrustServerCertificate=True");
        return new AuditoriaDbContext(optionsBuilder.Options);
    }
}
