using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

var builder = WebApplication.CreateBuilder(args);
var servicePort = builder.Configuration.GetValue<int?>("ServicePort") ?? 7004;
builder.WebHost.UseUrls($"http://0.0.0.0:{servicePort}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("open", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();
var analysisStore = new List<AnalisisCamara>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("open");

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "orquestador-ia" }));

app.MapGet("/api/v1/ia/analisis", () => Results.Ok(analysisStore.OrderByDescending(x => x.Fecha)));

app.MapGet("/api/v1/ia/analisis/{id:guid}", (Guid id) =>
{
    var found = analysisStore.FirstOrDefault(x => x.Id == id);
    return found is null ? Results.NotFound() : Results.Ok(found);
});

app.MapPost("/api/v1/ia/analizar-camara", async (HttpRequest request, string? contexto) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { message = "Debes enviar multipart/form-data con el campo 'frame'." });
    }

    var form = await request.ReadFormAsync();
    var frame = form.Files["frame"];

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

    var (brillo, contraste) = Analyze(img);
    var nivelRiesgo = GetRiskLevel(brillo, contraste);
    var recomendacion = GetRecommendation(nivelRiesgo, contexto);

    var response = new AnalisisCamara(
        Guid.NewGuid(),
        DateTime.UtcNow,
        $"{img.Width}x{img.Height}",
        contexto ?? "general",
        Math.Round(brillo, 2),
        Math.Round(contraste, 2),
        nivelRiesgo,
        recomendacion
    );

    analysisStore.Add(response);

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
        }
    };

    return Results.Ok(payload);
})
.DisableAntiforgery();

app.Run();

static (double Brightness, double Contrast) Analyze(Image<Rgba32> image)
{
    var values = new List<double>(image.Width * image.Height);

    image.ProcessPixelRows(accessor =>
    {
        for (var y = 0; y < accessor.Height; y++)
        {
            var row = accessor.GetRowSpan(y);
            for (var x = 0; x < row.Length; x++)
            {
                var p = row[x];
                var luminance = 0.2126 * p.R + 0.7152 * p.G + 0.0722 * p.B;
                values.Add(luminance);
            }
        }
    });

    var mean = values.Average();
    var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
    var stdev = Math.Sqrt(variance);
    return (mean, stdev);
}

static string GetRiskLevel(double brightness, double contrast)
{
    if (brightness < 55 || contrast < 28)
    {
        return "Alto";
    }

    if (brightness < 90 || contrast < 40)
    {
        return "Medio";
    }

    return "Bajo";
}

static string GetRecommendation(string risk, string? context)
{
    var area = string.IsNullOrWhiteSpace(context) ? "general" : context;

    return risk switch
    {
        "Alto" => $"Se recomienda revision inmediata del caso en modulo {area}.",
        "Medio" => $"Mantener seguimiento y nueva captura en 24 horas para {area}.",
        _ => $"Sin alertas criticas en {area}; continuar monitoreo regular."
    };
}

internal record AnalisisCamara(
    Guid Id,
    DateTime Fecha,
    string Resolucion,
    string Contexto,
    double BrilloPromedio,
    double Contraste,
    string NivelRiesgo,
    string Recomendacion
);
