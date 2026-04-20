using Microsoft.EntityFrameworkCore;

namespace NosVemos.Auditoria.Worker;

public sealed class AuditoriaDbContext(DbContextOptions<AuditoriaDbContext> options) : DbContext(options)
{
    public DbSet<AuditoriaEvento> Eventos => Set<AuditoriaEvento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditoriaEvento>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Fecha);
            entity.Property(x => x.RoutingKey).HasMaxLength(128);
            entity.Property(x => x.Payload).HasMaxLength(4000);
        });
    }
}

public sealed class AuditoriaEvento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string RoutingKey { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
}
