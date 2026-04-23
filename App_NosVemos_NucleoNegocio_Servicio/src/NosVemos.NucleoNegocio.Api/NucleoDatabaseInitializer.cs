using Microsoft.EntityFrameworkCore;

internal static class NucleoDatabaseInitializer
{
    public static void Initialize(IServiceProvider services, bool useInMemory)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NucleoDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("NucleoDbInit");
        EnsureDatabaseReady(db, useInMemory, logger);

        if (!db.Expedientes.Any())
        {
            db.Expedientes.AddRange(
                new Expediente { Codigo = "EXP-0001", Estado = "Abierto", FechaCreacion = DateTime.UtcNow },
                new Expediente { Codigo = "EXP-0002", Estado = "EnSeguimiento", FechaCreacion = DateTime.UtcNow }
            );
            db.SaveChanges();
        }
    }

    private static void EnsureDatabaseReady(NucleoDbContext db, bool useInMemory, ILogger logger)
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
