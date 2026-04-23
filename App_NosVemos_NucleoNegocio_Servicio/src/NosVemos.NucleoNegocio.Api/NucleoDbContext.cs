using Microsoft.EntityFrameworkCore;

internal sealed class NucleoDbContext(DbContextOptions<NucleoDbContext> options) : DbContext(options)
{
    public DbSet<Expediente> Expedientes => Set<Expediente>();
    public DbSet<TelemetriaEvento> TelemetriaEventos => Set<TelemetriaEvento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Expediente>(entity =>
        {
            entity.HasIndex(x => x.Codigo).IsUnique();
            entity.Property(x => x.Codigo).HasMaxLength(64);
            entity.Property(x => x.Estado).HasMaxLength(32);
        });

        modelBuilder.Entity<TelemetriaEvento>(entity =>
        {
            entity.HasIndex(x => new { x.DeviceId, x.Sequence }).IsUnique();
            entity.Property(x => x.DeviceId).HasMaxLength(64);
            entity.Property(x => x.SensorType).HasMaxLength(32);
            entity.Property(x => x.Unit).HasMaxLength(16);
            entity.Property(x => x.Signal).HasMaxLength(16);
            entity.Property(x => x.Source).HasMaxLength(16);
            entity.Property(x => x.Checksum).HasMaxLength(8);
        });
    }
}

internal sealed class Expediente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Codigo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}

internal sealed class TelemetriaEvento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DeviceId { get; set; } = string.Empty;
    public string SensorType { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
    public long Sequence { get; set; }
    public string Signal { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
}
