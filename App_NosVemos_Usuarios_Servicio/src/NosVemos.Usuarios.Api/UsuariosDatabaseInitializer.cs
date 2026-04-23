using Microsoft.EntityFrameworkCore;

internal static class UsuariosDatabaseInitializer
{
    public static void Initialize(IServiceProvider services, bool useInMemory)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UsuariosDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("UsuariosDbInit");
        EnsureDatabaseReady(db, useInMemory, logger);

        if (!db.Usuarios.Any())
        {
            db.Usuarios.AddRange(
                new Usuario { Nombre = "Ana Torres", Email = "ana@nosvemos.local", Activo = true },
                new Usuario { Nombre = "Luis Perez", Email = "luis@nosvemos.local", Activo = true }
            );
            db.SaveChanges();
        }
    }

    private static void EnsureDatabaseReady(UsuariosDbContext db, bool useInMemory, ILogger logger)
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
