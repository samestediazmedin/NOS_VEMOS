using Microsoft.EntityFrameworkCore;

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UsuariosDbContext>();
    db.Database.EnsureCreated();
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

app.MapGet("/api/v1/usuarios", async (UsuariosDbContext db) =>
{
    var users = await db.Usuarios
        .Select(x => new UsuarioResponse(x.Id, x.Nombre, x.Email, x.Activo))
        .ToListAsync();
    return Results.Ok(users);
});

app.MapGet("/api/v1/usuarios/{id:guid}", async (Guid id, UsuariosDbContext db) =>
{
    var user = await db.Usuarios
        .Where(x => x.Id == id)
        .Select(x => new UsuarioResponse(x.Id, x.Nombre, x.Email, x.Activo))
        .FirstOrDefaultAsync();
    return user is null ? Results.NotFound() : Results.Ok(user);
});

app.MapPost("/api/v1/usuarios", async (CrearUsuarioRequest request, UsuariosDbContext db) =>
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

internal record CrearUsuarioRequest(string Nombre, string Email);

internal record UsuarioResponse(Guid Id, string Nombre, string Email, bool Activo);

internal sealed class UsuariosDbContext(DbContextOptions<UsuariosDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
}

internal sealed class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Activo { get; set; }
}
