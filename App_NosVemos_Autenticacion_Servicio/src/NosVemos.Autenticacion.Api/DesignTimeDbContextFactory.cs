using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=AutenticacionDB;User Id=sa;Password=Your_strong_password_123!;TrustServerCertificate=True");
        return new AuthDbContext(optionsBuilder.Options);
    }
}
