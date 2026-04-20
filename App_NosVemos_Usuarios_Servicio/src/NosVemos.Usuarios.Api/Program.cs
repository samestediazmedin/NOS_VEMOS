using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var servicePort = builder.Configuration.GetValue<int?>("ServicePort") ?? 7002;
builder.WebHost.UseUrls($"http://0.0.0.0:{servicePort}");
var useInMemory = builder.Configuration.GetValue<bool?>("UseInMemoryDatabase") ?? true;
builder.Services.AddDbContext<UsuariosDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("UsuariosDB");
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
    }
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Missing Jwt:SecretKey configuration.");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = "NosVemos.Auth",
            ValidAudience = "NosVemos.Client",
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
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

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "usuarios" }));

var usuarios = app.MapGroup("/api/v1/usuarios").RequireAuthorization();

usuarios.MapGet("", async (UsuariosDbContext db) =>
{
    var users = await db.Usuarios
        .Select(x => new UsuarioResponse(x.Id, x.Nombre, x.Email, x.Activo))
        .ToListAsync();
    return Results.Ok(users);
});

usuarios.MapGet("/{id:guid}", async (Guid id, UsuariosDbContext db) =>
{
    var user = await db.Usuarios
        .Where(x => x.Id == id)
        .Select(x => new UsuarioResponse(x.Id, x.Nombre, x.Email, x.Activo))
        .FirstOrDefaultAsync();
    return user is null ? Results.NotFound() : Results.Ok(user);
});

usuarios.MapPost("", async (CrearUsuarioRequest request, UsuariosDbContext db) =>
{
    var entity = new Usuario
    {
        Id = Guid.NewGuid(),
        Nombre = request.Nombre,
        Email = request.Email,
        Activo = true
    };
    db.Usuarios.Add(entity);
    await db.SaveChangesAsync();

    var created = new UsuarioResponse(entity.Id, entity.Nombre, entity.Email, entity.Activo);
    return Results.Created($"/api/v1/usuarios/{created.Id}", created);
});

app.Run();

static void EnsureDatabaseReady(UsuariosDbContext db, bool useInMemory, ILogger logger)
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

internal record CrearUsuarioRequest(string Nombre, string Email);

internal record UsuarioResponse(Guid Id, string Nombre, string Email, bool Activo);

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

public partial class Program;
