using NosVemos.Auditoria.Worker;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var servicePort = builder.Configuration.GetValue<int?>("ServicePort") ?? 7005;
builder.WebHost.UseUrls($"http://0.0.0.0:{servicePort}");
var useInMemory = builder.Configuration.GetValue<bool?>("UseInMemoryDatabase") ?? false;

builder.Services.AddHostedService<Worker>();
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddDbContext<AuditoriaDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("AuditoriaDB");
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
    }
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "auditoria" }));

app.MapGet("/api/v1/auditoria/eventos", async (
    string? routingKey,
    DateTime? from,
    DateTime? to,
    int? page,
    int? pageSize,
    int? take,
    AuditoriaDbContext db,
    CancellationToken ct) =>
{
    var resolvedPage = Math.Max(page ?? 1, 1);
    var resolvedPageSize = Math.Clamp(pageSize ?? take ?? 20, 1, 200);

    var query = db.Eventos.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(routingKey))
    {
        query = query.Where(x => x.RoutingKey == routingKey);
    }

    if (from.HasValue)
    {
        query = query.Where(x => x.Fecha >= from.Value);
    }

    if (to.HasValue)
    {
        query = query.Where(x => x.Fecha <= to.Value);
    }

    var total = await query.CountAsync(ct);
    var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)resolvedPageSize);

    var data = await query
        .OrderByDescending(x => x.Fecha)
        .Skip((resolvedPage - 1) * resolvedPageSize)
        .Take(resolvedPageSize)
        .Select(x => new { x.Id, x.Fecha, x.RoutingKey, x.Payload })
        .ToListAsync(ct);

    return Results.Ok(new
    {
        page = resolvedPage,
        pageSize = resolvedPageSize,
        total,
        totalPages,
        data
    });
});

app.MapGet("/api/v1/auditoria/eventos/{id:guid}", async (
    Guid id,
    AuditoriaDbContext db,
    CancellationToken ct) =>
{
    var item = await db.Eventos
        .AsNoTracking()
        .Where(x => x.Id == id)
        .Select(x => new { x.Id, x.Fecha, x.RoutingKey, x.Payload })
        .FirstOrDefaultAsync(ct);

    return item is null
        ? Results.NotFound(new { message = "Evento de auditoria no encontrado." })
        : Results.Ok(item);
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuditoriaDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AuditoriaDbInit");
    if (useInMemory)
    {
        db.Database.EnsureCreated();
    }
    else
    {
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudieron aplicar migraciones Auditoria; se usa EnsureCreated como fallback.");
            db.Database.EnsureCreated();
        }
    }
}

app.Run();
