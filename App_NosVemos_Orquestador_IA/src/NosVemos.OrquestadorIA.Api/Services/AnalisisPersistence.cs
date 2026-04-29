using Microsoft.EntityFrameworkCore;
using NosVemos.OrquestadorIA.Api.Contracts;

namespace NosVemos.OrquestadorIA.Api.Services;

internal sealed class AnalisisDbContext(DbContextOptions<AnalisisDbContext> options) : DbContext(options)
{
    public DbSet<AnalisisCamaraEntity> Analisis => Set<AnalisisCamaraEntity>();
    public DbSet<BiometricProfileEntity> BiometricProfiles => Set<BiometricProfileEntity>();
    public DbSet<BiometricSampleEntity> BiometricSamples => Set<BiometricSampleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnalisisCamaraEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Contexto).HasMaxLength(128);
            entity.Property(x => x.Resolucion).HasMaxLength(64);
            entity.Property(x => x.NivelRiesgo).HasMaxLength(16);
        });

        modelBuilder.Entity<BiometricProfileEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).HasMaxLength(128);
            entity.Property(x => x.UserName).HasMaxLength(256);
            entity.Property(x => x.CreatedAt);
            entity.Property(x => x.UpdatedAt);
            entity.HasIndex(x => x.UserId).IsUnique();
        });

        modelBuilder.Entity<BiometricSampleEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Angle).HasMaxLength(32);
            entity.Property(x => x.FeatureVector).HasColumnType("nvarchar(max)");
            entity.Property(x => x.CapturedAt);
            entity.HasOne<BiometricProfileEntity>()
                .WithMany(x => x.Samples)
                .HasForeignKey(x => x.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
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

internal sealed class BiometricProfileEntity
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<BiometricSampleEntity> Samples { get; set; } = [];
}

internal sealed class BiometricSampleEntity
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string Angle { get; set; } = string.Empty;
    public int Quality { get; set; }
    public string FeatureVector { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
}
