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

var jwtKey = app.Configuration["Jwt:SecretKey"] ?? "NOS_VEMOS_DEV_SECRET_KEY_2026_1234567890";

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    db.Database.EnsureCreated();
    if (!db.Usuarios.Any())
    {
        db.Usuarios.Add(new UsuarioAuth
        {
            Email = "admin@nosvemos.local",
            Password = "Admin123*",
            Role = "Administrador"
        });
        db.SaveChanges();
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "autenticacion" }));

app.MapPost("/api/v1/autenticacion/registro", async (RegisterRequest request, AuthDbContext db) =>
{
    var exists = await db.Usuarios.AnyAsync(x => x.Email == request.Email);
    if (exists)
    {
        return Results.Conflict(new { message = "El usuario ya existe." });
    }

    db.Usuarios.Add(new UsuarioAuth { Email = request.Email, Password = request.Password, Role = "Usuario" });
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/usuarios/{request.Email}", new { request.Email, Rol = "Usuario" });
});

app.MapPost("/api/v1/autenticacion/login", async (LoginRequest request, AuthDbContext db) =>
{
    var user = await db.Usuarios.FirstOrDefaultAsync(x => x.Email == request.Email && x.Password == request.Password);
    if (user is null)
    {
        return Results.Unauthorized();
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

internal record RegisterRequest(string Email, string Password);

internal record LoginRequest(string Email, string Password);

internal record UserCredential(string Email, string Password, string Role);

internal sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<UsuarioAuth> Usuarios => Set<UsuarioAuth>();
}

internal sealed class UsuarioAuth
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
