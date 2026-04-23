using Microsoft.EntityFrameworkCore;

namespace NosVemos.Auditoria.Worker;

public sealed class AuditoriaDbContext(DbContextOptions<AuditoriaDbContext> options) : DbContext(options)
{
    public DbSet<AuditoriaEvento> Eventos => Set<AuditoriaEvento>();
    public DbSet<AdminMovimiento> Movimientos => Set<AdminMovimiento>();
    public DbSet<AsignacionPaciente> Asignaciones => Set<AsignacionPaciente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditoriaEvento>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Fecha);
            entity.Property(x => x.RoutingKey).HasMaxLength(128);
            entity.Property(x => x.Payload).HasMaxLength(4000);
        });

        modelBuilder.Entity<AdminMovimiento>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Fecha);
            entity.Property(x => x.Tipo).HasMaxLength(64);
            entity.Property(x => x.Actor).HasMaxLength(120);
            entity.Property(x => x.UsuarioId).HasMaxLength(120);
            entity.Property(x => x.Detalle).HasMaxLength(2000);
        });

        modelBuilder.Entity<AsignacionPaciente>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.PacienteId).IsUnique();
            entity.Property(x => x.PacienteId).HasMaxLength(120);
            entity.Property(x => x.EstudianteId).HasMaxLength(120);
            entity.Property(x => x.Motivo).HasMaxLength(1200);
            entity.Property(x => x.Actor).HasMaxLength(120);
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

public sealed class AdminMovimiento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Tipo { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string UsuarioId { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
}

public sealed class AsignacionPaciente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PacienteId { get; set; } = string.Empty;
    public string EstudianteId { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Motivo { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
}
