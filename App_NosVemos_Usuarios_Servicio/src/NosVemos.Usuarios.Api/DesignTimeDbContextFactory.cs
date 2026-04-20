using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<UsuariosDbContext>
{
    public UsuariosDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UsuariosDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=UsuariosDB;User Id=sa;Password=Your_strong_password_123!;TrustServerCertificate=True");
        return new UsuariosDbContext(optionsBuilder.Options);
    }
}
