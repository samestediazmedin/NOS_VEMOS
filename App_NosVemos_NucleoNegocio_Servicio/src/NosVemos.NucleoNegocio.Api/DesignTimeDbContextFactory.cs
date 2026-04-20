using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<NucleoDbContext>
{
    public NucleoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<NucleoDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=NucleoDB;User Id=sa;Password=Your_strong_password_123!;TrustServerCertificate=True");
        return new NucleoDbContext(optionsBuilder.Options);
    }
}
