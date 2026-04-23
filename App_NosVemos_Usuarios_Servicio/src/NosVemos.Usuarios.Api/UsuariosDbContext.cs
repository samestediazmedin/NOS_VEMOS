using Microsoft.EntityFrameworkCore;

internal sealed class UsuariosDbContext(DbContextOptions<UsuariosDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Nombre).HasMaxLength(150);
            entity.Property(x => x.Email).HasMaxLength(256);
        });
    }
}

internal sealed class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Activo { get; set; }
}
