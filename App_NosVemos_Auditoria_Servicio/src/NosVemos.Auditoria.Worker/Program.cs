using Microsoft.EntityFrameworkCore;
using NosVemos.Auditoria.Worker;

var builder = WebApplication.CreateBuilder(args);
var servicePort = builder.Configuration.GetValue<int?>("ServicePort") ?? 7005;
builder.WebHost.UseUrls($"http://0.0.0.0:{servicePort}");

builder.Services.AddHostedService<Worker>();
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
var useInMemory = builder.Configuration.GetValue<bool?>("UseInMemoryDatabase") ?? false;
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuditoriaDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "auditoria" }));

app.MapGet("/api/v1/auditoria/eventos", async (
    string? routingKey,
    string? modulo,
    DateTime? desde,
    DateTime? hasta,
    int? limit,
    AuditoriaDbContext db,
    CancellationToken ct) =>
{
    var cappedLimit = Math.Clamp(limit ?? 100, 1, 500);
    var query = db.Eventos.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(routingKey))
    {
        var normalizedRoutingKey = routingKey.Trim();
        query = query.Where(x => x.RoutingKey == normalizedRoutingKey);
    }

    if (!string.IsNullOrWhiteSpace(modulo))
    {
        var normalizedModulo = modulo.Trim();
        query = query.Where(x => x.RoutingKey.StartsWith(normalizedModulo + "."));
    }

    if (desde.HasValue)
    {
        query = query.Where(x => x.Fecha >= desde.Value);
    }

    if (hasta.HasValue)
    {
        query = query.Where(x => x.Fecha <= hasta.Value);
    }

    var data = await query
        .OrderByDescending(x => x.Fecha)
        .Take(cappedLimit)
        .Select(x => new
        {
            x.Id,
            x.Fecha,
            x.RoutingKey,
            x.Payload
        })
        .ToListAsync(ct);

    return Results.Ok(new
    {
        total = data.Count,
        limit = cappedLimit,
        filtros = new { routingKey, modulo, desde, hasta },
        eventos = data
    });
});

app.MapGet("/api/v1/auditoria/movimientos", async (int? limit, AuditoriaDbContext db, CancellationToken ct) =>
{
    var cappedLimit = Math.Clamp(limit ?? 100, 1, 500);
    var data = await db.Movimientos
        .AsNoTracking()
        .OrderByDescending(x => x.Fecha)
        .Take(cappedLimit)
        .ToListAsync(ct);

    return Results.Ok(new { total = data.Count, limit = cappedLimit, movimientos = data });
});

app.MapPost("/api/v1/auditoria/movimientos", async (AdminMovimientoRequest request, AuditoriaDbContext db, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Tipo) || string.IsNullOrWhiteSpace(request.Detalle))
    {
        return Results.BadRequest(new { message = "Tipo y detalle son obligatorios." });
    }

    var entity = new AdminMovimiento
    {
        Id = Guid.NewGuid(),
        Fecha = DateTime.UtcNow,
        Tipo = request.Tipo.Trim(),
        Actor = (request.Actor ?? "admin.ui").Trim(),
        UsuarioId = (request.UsuarioId ?? string.Empty).Trim(),
        Detalle = request.Detalle.Trim()
    };

    db.Movimientos.Add(entity);
    await db.SaveChangesAsync(ct);

    return Results.Created($"/api/v1/auditoria/movimientos/{entity.Id}", entity);
});

app.MapGet("/api/v1/auditoria/asignaciones", async (AuditoriaDbContext db, CancellationToken ct) =>
{
    var data = await db.Asignaciones
        .AsNoTracking()
        .OrderByDescending(x => x.Fecha)
        .ToListAsync(ct);

    return Results.Ok(data);
});

app.MapPost("/api/v1/auditoria/asignaciones", async (AsignacionRequest request, AuditoriaDbContext db, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.PacienteId) || string.IsNullOrWhiteSpace(request.EstudianteId))
    {
        return Results.BadRequest(new { message = "PacienteId y EstudianteId son obligatorios." });
    }

    if (string.Equals(request.PacienteId, request.EstudianteId, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { message = "Paciente y estudiante no pueden ser el mismo usuario." });
    }

    var motivo = (request.Motivo ?? string.Empty).Trim();
    if (request.Reasignacion && string.IsNullOrWhiteSpace(motivo))
    {
        return Results.BadRequest(new { message = "El motivo es obligatorio en reasignacion." });
    }

    var existing = await db.Asignaciones.FirstOrDefaultAsync(x => x.PacienteId == request.PacienteId, ct);
    if (existing is null)
    {
        existing = new AsignacionPaciente
        {
            Id = Guid.NewGuid(),
            PacienteId = request.PacienteId.Trim(),
            EstudianteId = request.EstudianteId.Trim(),
            Fecha = DateTime.UtcNow,
            Motivo = motivo,
            Actor = (request.Actor ?? "admin.ui").Trim()
        };
        db.Asignaciones.Add(existing);
    }
    else
    {
        existing.EstudianteId = request.EstudianteId.Trim();
        existing.Fecha = DateTime.UtcNow;
        existing.Motivo = motivo;
        existing.Actor = (request.Actor ?? "admin.ui").Trim();
    }

    db.Movimientos.Add(new AdminMovimiento
    {
        Id = Guid.NewGuid(),
        Fecha = DateTime.UtcNow,
        Tipo = request.Reasignacion ? "reasignacion" : "asignacion",
        Actor = (request.Actor ?? "admin.ui").Trim(),
        UsuarioId = request.PacienteId.Trim(),
        Detalle = $"Paciente {request.PacienteId} -> Estudiante {request.EstudianteId}. Motivo: {motivo}"
    });

    await db.SaveChangesAsync(ct);
    return Results.Ok(existing);
});

app.Run();

internal sealed record AdminMovimientoRequest(string Tipo, string? Actor, string? UsuarioId, string Detalle);
internal sealed record AsignacionRequest(string PacienteId, string EstudianteId, string? Motivo, bool Reasignacion, string? Actor);
