using Microsoft.EntityFrameworkCore;
using NosVemos.OrquestadorIA.Api.Contracts;

namespace NosVemos.OrquestadorIA.Api.Services;

internal sealed class AnalisisDbContext(DbContextOptions<AnalisisDbContext> options) : DbContext(options)
{
    public DbSet<AnalisisCamaraEntity> Analisis => Set<AnalisisCamaraEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnalisisCamaraEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Contexto).HasMaxLength(128);
            entity.Property(x => x.Resolucion).HasMaxLength(64);
            entity.Property(x => x.NivelRiesgo).HasMaxLength(16);
        });
    }
}

internal sealed record AnalisisCamaraEntity(
    Guid Id,
    DateTime Fecha,
    string Resolucion,
    string Contexto,
    double BrilloPromedio,
    double Contraste,
    string NivelRiesgo,
    string Recomendacion
)
{
    public AnalisisCamara ToContract()
    {
        return new AnalisisCamara(Id, Fecha, Resolucion, Contexto, BrilloPromedio, Contraste, NivelRiesgo, Recomendacion);
    }
}
