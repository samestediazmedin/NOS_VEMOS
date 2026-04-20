using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var servicePort = builder.Configuration.GetValue<int?>("ServicePort") ?? 7001;
builder.WebHost.UseUrls($"http://0.0.0.0:{servicePort}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var useInMemory = builder.Configuration.GetValue<bool?>("UseInMemoryDatabase") ?? true;
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("AutenticacionDB");
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var jwtKey = app.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Missing Jwt:SecretKey configuration.");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AuthDbInit");
    EnsureDatabaseReady(db, useInMemory, logger);
    if (!db.Usuarios.Any())
    {
        db.Usuarios.Add(new UsuarioAuth
        {
            Email = "admin@nosvemos.local",
            Password = BCrypt.Net.BCrypt.HashPassword("Admin123*"),
            Role = "Administrador"
        });
        db.SaveChanges();
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "autenticacion" }));

app.MapPost("/api/v1/autenticacion/registro", async (RegisterRequest request, AuthDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Email y password son obligatorios." });
    }

    var normalizedEmail = request.Email.Trim().ToLowerInvariant();
    var exists = await db.Usuarios.AnyAsync(x => x.Email == normalizedEmail);
    if (exists)
    {
        return Results.Conflict(new { message = "El usuario ya existe." });
    }

    db.Usuarios.Add(new UsuarioAuth
    {
        Email = normalizedEmail,
        Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
        Role = "Usuario"
    });
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/usuarios/{normalizedEmail}", new { Email = normalizedEmail, Rol = "Usuario" });
});

app.MapPost("/api/v1/autenticacion/login", async (LoginRequest request, AuthDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Email y password son obligatorios." });
    }

    var normalizedEmail = request.Email.Trim().ToLowerInvariant();
    var user = await db.Usuarios.FirstOrDefaultAsync(x => x.Email == normalizedEmail);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var passwordOk = VerifyPassword(request.Password, user.Password);
    if (!passwordOk)
    {
        return Results.Unauthorized();
    }

    if (!IsBcryptHash(user.Password))
    {
        user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
        await db.SaveChangesAsync();
    }

    var token = BuildToken(new UserCredential(user.Email, user.Password, user.Role), jwtKey);
    return Results.Ok(new { access_token = token, token_type = "Bearer", expires_in = 3600, role = user.Role });
});

app.Run();

static string BuildToken(UserCredential user, string secret)
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: "NosVemos.Auth",
        audience: "NosVemos.Client",
        claims:
        [
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        ],
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: creds);

    return new JwtSecurityTokenHandler().WriteToken(token);
}

static bool VerifyPassword(string inputPassword, string storedPassword)
{
    if (IsBcryptHash(storedPassword))
    {
        return BCrypt.Net.BCrypt.Verify(inputPassword, storedPassword);
    }

    return inputPassword == storedPassword;
}

static bool IsBcryptHash(string value)
{
    return value.StartsWith("$2a$") || value.StartsWith("$2b$") || value.StartsWith("$2y$");
}

static void EnsureDatabaseReady(AuthDbContext db, bool useInMemory, ILogger logger)
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

internal record RegisterRequest(string Email, string Password);

internal record LoginRequest(string Email, string Password);

internal record UserCredential(string Email, string Password, string Role);

internal sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<UsuarioAuth> Usuarios => Set<UsuarioAuth>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UsuarioAuth>(entity =>
        {
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.Role).HasMaxLength(64);
        });
    }
}

internal sealed class UsuarioAuth
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public partial class Program;
