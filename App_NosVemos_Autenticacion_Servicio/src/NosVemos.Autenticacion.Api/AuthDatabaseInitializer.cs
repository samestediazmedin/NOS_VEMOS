using Microsoft.EntityFrameworkCore;

internal static class AuthDatabaseInitializer
{
    public static void Initialize(IServiceProvider services, bool useInMemory)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AuthDbInit");
        EnsureDatabaseReady(db, useInMemory, logger);

        if (!db.Usuarios.Any())
        {
            db.Usuarios.Add(new UsuarioAuth
            {
                Email = "admin@nosvemos.local",
                Password = AuthPasswordService.Hash("Admin123*"),
                Role = "Administrador"
            });
            db.SaveChanges();
        }
    }

    private static void EnsureDatabaseReady(AuthDbContext db, bool useInMemory, ILogger logger)
    {
        if (useInMemory)
        {
            db.Database.EnsureCreated();
            return;
        }

        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudieron aplicar migraciones; se usa EnsureCreated como fallback.");
            db.Database.EnsureCreated();
        }
    }
}
