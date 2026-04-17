using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var servicePort = builder.Configuration.GetValue<int?>("ServicePort") ?? 7003;
builder.WebHost.UseUrls($"http://0.0.0.0:{servicePort}");
var useInMemory = builder.Configuration.GetValue<bool?>("UseInMemoryDatabase") ?? true;
builder.Services.AddDbContext<NucleoDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("NucleoDB");
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
    var db = scope.ServiceProvider.GetRequiredService<NucleoDbContext>();
    db.Database.EnsureCreated();
    if (!db.Expedientes.Any())
    {
        db.Expedientes.AddRange(
            new Expediente { Codigo = "EXP-0001", Estado = "Abierto", FechaCreacion = DateTime.UtcNow },
            new Expediente { Codigo = "EXP-0002", Estado = "EnSeguimiento", FechaCreacion = DateTime.UtcNow }
        );
        db.SaveChanges();
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "nucleo-negocio" }));

app.MapGet("/api/v1/expedientes", async (NucleoDbContext db) =>
{
    var expedientes = await db.Expedientes
        .Select(x => new ExpedienteResponse(x.Id, x.Codigo, x.Estado, x.FechaCreacion))
        .ToListAsync();
    return Results.Ok(expedientes);
});

app.MapPost("/api/v1/expedientes", async (CrearExpedienteRequest request, NucleoDbContext db) =>
{
    var entity = new Expediente
    {
        Id = Guid.NewGuid(),
        Codigo = request.Codigo,
        Estado = "Abierto",
        FechaCreacion = DateTime.UtcNow
    };
    db.Expedientes.Add(entity);
    await db.SaveChangesAsync();
    var created = new ExpedienteResponse(entity.Id, entity.Codigo, entity.Estado, entity.FechaCreacion);
    return Results.Created($"/api/v1/expedientes/{created.Id}", created);
});

app.MapPost("/api/v1/expedientes/{id:guid}/cerrar", async (Guid id, NucleoDbContext db) =>
{
    var entity = await db.Expedientes.FirstOrDefaultAsync(x => x.Id == id);
    if (entity is null)
    {
        return Results.NotFound();
    }

    entity.Estado = "Cerrado";
    await db.SaveChangesAsync();
    return Results.Ok(new ExpedienteResponse(entity.Id, entity.Codigo, entity.Estado, entity.FechaCreacion));
});

app.Run();

internal record CrearExpedienteRequest(string Codigo);

internal record ExpedienteResponse(Guid Id, string Codigo, string Estado, DateTime FechaCreacion);

internal sealed class NucleoDbContext(DbContextOptions<NucleoDbContext> options) : DbContext(options)
{
    public DbSet<Expediente> Expedientes => Set<Expediente>();
}

internal sealed class Expediente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Codigo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}
