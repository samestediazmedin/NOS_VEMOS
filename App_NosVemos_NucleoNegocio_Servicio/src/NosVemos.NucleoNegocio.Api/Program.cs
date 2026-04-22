using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;

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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Missing Jwt:SecretKey configuration.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "NosVemos.Auth";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "NosVemos.Client";
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
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var rabbitSettings = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqSettings>() ?? new RabbitMqSettings();
var enableEvents = builder.Configuration.GetValue<bool?>("Messaging:EnableDomainEvents") ?? true;
if (enableEvents)
{
    builder.Services.AddSingleton<IEventPublisher>(sp =>
        new RabbitMqEventPublisher(rabbitSettings, sp.GetRequiredService<ILogger<RabbitMqEventPublisher>>()));
}
else
{
    builder.Services.AddSingleton<IEventPublisher, NoOpEventPublisher>();
}

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
    var db = scope.ServiceProvider.GetRequiredService<NucleoDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("NucleoDbInit");
    EnsureDatabaseReady(db, useInMemory, logger);
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

var expedientesApi = app.MapGroup("/api/v1/expedientes").RequireAuthorization();

expedientesApi.MapGet("", async (NucleoDbContext db) =>
{
    var expedientes = await db.Expedientes
        .Select(x => new ExpedienteResponse(x.Id, x.Codigo, x.Estado, x.FechaCreacion))
        .ToListAsync();
    return Results.Ok(expedientes);
});

expedientesApi.MapPost("", async (CrearExpedienteRequest request, NucleoDbContext db, IEventPublisher eventPublisher, CancellationToken ct) =>
{
    var entity = new Expediente
    {
        Id = Guid.NewGuid(),
        Codigo = request.Codigo,
        Estado = "Abierto",
        FechaCreacion = DateTime.UtcNow
    };
    db.Expedientes.Add(entity);
    await db.SaveChangesAsync(ct);
    await eventPublisher.PublishAsync(
        "expediente.creado",
        new ExpedienteCreadoEvent(entity.Id, entity.Codigo, entity.Estado, entity.FechaCreacion),
        ct);

    var created = new ExpedienteResponse(entity.Id, entity.Codigo, entity.Estado, entity.FechaCreacion);
    return Results.Created($"/api/v1/expedientes/{created.Id}", created);
});

expedientesApi.MapPost("/{id:guid}/cerrar", async (Guid id, NucleoDbContext db, IEventPublisher eventPublisher, CancellationToken ct) =>
{
    var entity = await db.Expedientes.FirstOrDefaultAsync(x => x.Id == id);
    if (entity is null)
    {
        return Results.NotFound();
    }

    entity.Estado = "Cerrado";
    await db.SaveChangesAsync(ct);
    await eventPublisher.PublishAsync(
        "expediente.cerrado",
        new ExpedienteCerradoEvent(entity.Id, entity.Codigo, entity.Estado, DateTime.UtcNow),
        ct);

    return Results.Ok(new ExpedienteResponse(entity.Id, entity.Codigo, entity.Estado, entity.FechaCreacion));
});

app.Run();

static void EnsureDatabaseReady(NucleoDbContext db, bool useInMemory, ILogger logger)
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

internal record CrearExpedienteRequest(string Codigo);

internal record ExpedienteResponse(Guid Id, string Codigo, string Estado, DateTime FechaCreacion);

internal sealed class NucleoDbContext(DbContextOptions<NucleoDbContext> options) : DbContext(options)
{
    public DbSet<Expediente> Expedientes => Set<Expediente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Expediente>(entity =>
        {
            entity.HasIndex(x => x.Codigo).IsUnique();
            entity.Property(x => x.Codigo).HasMaxLength(64);
            entity.Property(x => x.Estado).HasMaxLength(32);
        });
    }
}

internal sealed class Expediente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Codigo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}

internal sealed record ExpedienteCreadoEvent(Guid ExpedienteId, string Codigo, string Estado, DateTime FechaCreacion);

internal sealed record ExpedienteCerradoEvent(Guid ExpedienteId, string Codigo, string Estado, DateTime FechaCierre);

internal sealed class RabbitMqSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5673;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "nosvemos.domain.events";
}

internal interface IEventPublisher
{
    Task PublishAsync(string routingKey, object payload, CancellationToken cancellationToken = default);
}

internal sealed class NoOpEventPublisher : IEventPublisher
{
    public Task PublishAsync(string routingKey, object payload, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal sealed class RabbitMqEventPublisher(RabbitMqSettings settings, ILogger<RabbitMqEventPublisher> logger) : IEventPublisher
{
    public Task PublishAsync(string routingKey, object payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password,
                DispatchConsumersAsync = true
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(settings.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;

            channel.BasicPublish(
                exchange: settings.Exchange,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            logger.LogInformation("Evento publicado: {RoutingKey}", routingKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo publicar evento {RoutingKey}", routingKey);
        }

        return Task.CompletedTask;
    }
}

public partial class Program;
