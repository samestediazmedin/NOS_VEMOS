using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Microsoft.EntityFrameworkCore;
using NosVemos.OrquestadorIA.Api.Contracts;
using NosVemos.OrquestadorIA.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var servicePort = builder.Configuration.GetValue<int?>("ServicePort") ?? 7004;
builder.WebHost.UseUrls($"http://0.0.0.0:{servicePort}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ImageAnalysisService>();
var useInMemory = builder.Configuration.GetValue<bool?>("UseInMemoryDatabase") ?? false;
builder.Services.AddDbContext<AnalisisDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("IADB");
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
    }
});
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("open", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AnalisisDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("open");

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "orquestador-ia" }));

app.MapGet("/api/v1/ia/analisis", async (AnalisisDbContext db) =>
{
    var data = await db.Analisis
        .OrderByDescending(x => x.Fecha)
        .Select(x => x.ToContract())
        .ToListAsync();
    return Results.Ok(data);
});

app.MapGet("/api/v1/ia/analisis/{id:guid}", async (Guid id, AnalisisDbContext db) =>
{
    var found = await db.Analisis.Where(x => x.Id == id).Select(x => x.ToContract()).FirstOrDefaultAsync();
    return found is null ? Results.NotFound() : Results.Ok(found);
});

app.MapPost("/api/v1/ia/analizar-camara", async (HttpRequest request, string? contexto, IEventPublisher eventPublisher, AnalisisDbContext db, CancellationToken ct) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { message = "Debes enviar multipart/form-data con el campo 'frame'." });
    }

    var form = await request.ReadFormAsync();
    var frame = form.Files["frame"];
    var usuarioEsperado = (form["usuarioEsperado"].ToString() ?? string.Empty).Trim();
    var usuarioDetectado = (form["usuarioDetectado"].ToString() ?? string.Empty).Trim();
    _ = double.TryParse(form["confianzaRostro"], out var confianzaRostro);
    _ = double.TryParse(form["distanciaCm"], out var distanciaCm);

    if (frame is null || frame.Length == 0)
    {
        return Results.BadRequest(new { message = "Debes enviar una imagen en el campo 'frame'." });
    }

    await using var stream = frame.OpenReadStream();
    Image<Rgba32> image;
    try
    {
        image = await Image.LoadAsync<Rgba32>(stream);
    }
    catch
    {
        return Results.BadRequest(new { message = "El archivo enviado no es una imagen valida." });
    }

    using var img = image;

    var analysisService = app.Services.GetRequiredService<ImageAnalysisService>();
    var (brillo, contraste) = analysisService.Analyze(img);
    var nivelRiesgo = analysisService.GetRiskLevel(brillo, contraste);
    var recomendacion = analysisService.GetRecommendation(nivelRiesgo, contexto);

    var response = new AnalisisCamaraEntity(
        Guid.NewGuid(),
        DateTime.UtcNow,
        $"{img.Width}x{img.Height}",
        contexto ?? "general",
        Math.Round(brillo, 2),
        Math.Round(contraste, 2),
        nivelRiesgo,
        recomendacion
    );

    db.Analisis.Add(response);
    await db.SaveChangesAsync(ct);

    await eventPublisher.PublishAsync(
        "ia.camara.analizado",
        new AnalisisCamaraEvent(response.Id, response.Fecha, response.Contexto, response.NivelRiesgo, response.BrilloPromedio, response.Contraste),
        ct);

    var hayRostroReconocido = !string.IsNullOrWhiteSpace(usuarioDetectado);
    if (hayRostroReconocido)
    {
        await eventPublisher.PublishAsync(
            "ia.rostro.reconocido",
            new RostroReconocidoEvent(response.Id, response.Fecha, usuarioEsperado, usuarioDetectado, confianzaRostro),
            ct);
    }

    if (distanciaCm > 0)
    {
        await eventPublisher.PublishAsync(
            "sensor.proximidad.detectada",
            new ProximidadDetectadaEvent(response.Id, response.Fecha, distanciaCm, distanciaCm < 55),
            ct);
    }

    var payload = new
    {
        id = response.Id,
        fecha = response.Fecha,
        resolucion = response.Resolucion,
        contexto = response.Contexto,
        metricas = new
        {
            brilloPromedio = response.BrilloPromedio,
            contraste = response.Contraste
        },
        evaluacion = new
        {
            nivelRiesgo = response.NivelRiesgo,
            recomendacion = response.Recomendacion
        },
        biometria = new
        {
            usuarioEsperado,
            usuarioDetectado,
            confianzaRostro = Math.Round(confianzaRostro, 2)
        },
        sensor = new
        {
            distanciaCm = Math.Round(distanciaCm, 2),
            alertaProximidad = distanciaCm > 0 && distanciaCm < 55
        }
    };

    return Results.Ok(payload);
})
.DisableAntiforgery();

app.Run();
