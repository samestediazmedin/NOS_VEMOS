using Microsoft.EntityFrameworkCore;
using NosVemos.OrquestadorIA.Api.Contracts;
using NosVemos.OrquestadorIA.Api.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Globalization;

internal static class IaEndpoints
{
    public static void MapIaEndpoints(this WebApplication app)
    {
        var thresholdDefault = app.Configuration.GetValue<double?>("Biometria:ThresholdDefault") ?? 0.92;
        var minTopMargin = app.Configuration.GetValue<double?>("Biometria:MinTopMargin") ?? 0.04;

        app.MapGet("/api/v1/ia/rostros/enrolados", async (AnalisisDbContext db, CancellationToken ct) =>
        {
            var data = await db.BiometricProfiles
                .AsNoTracking()
                .Include(x => x.Samples)
                .OrderByDescending(x => x.UpdatedAt)
                .Select(x => new
                {
                    x.UserId,
                    x.UserName,
                    sampleCount = x.Samples.Count,
                    x.UpdatedAt
                })
                .ToListAsync(ct);

            return Results.Ok(data);
        });

        app.MapPost("/api/v1/ia/enrolar-rostro", async (
            HttpRequest request,
            AnalisisDbContext db,
            BiometricRecognitionService biometricRecognition,
            CancellationToken ct) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { message = "Debes enviar multipart/form-data con el campo 'frame'." });
            }

            var form = await request.ReadFormAsync(ct);
            var frame = form.Files["frame"];
            var userId = (form["userId"].ToString() ?? string.Empty).Trim();
            var userName = (form["userName"].ToString() ?? string.Empty).Trim();
            var angle = (form["angle"].ToString() ?? "frontal").Trim();
            var quality = ParseInt(form["quality"], 80);

            if (frame is null || frame.Length == 0)
            {
                return Results.BadRequest(new { message = "Debes enviar una imagen en el campo 'frame'." });
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.BadRequest(new { message = "userId es obligatorio para enrolar biometria." });
            }

            await using var stream = frame.OpenReadStream();
            Image<Rgba32> image;
            try
            {
                image = await Image.LoadAsync<Rgba32>(stream, ct);
            }
            catch
            {
                return Results.BadRequest(new { message = "El archivo enviado no es una imagen valida." });
            }

            using var img = image;
            var result = await biometricRecognition.EnrollAsync(db, userId, userName, angle, quality, img, ct);
            return Results.Ok(new
            {
                status = "enrolled",
                result.UserId,
                result.UserName,
                result.SampleCount
            });
        })
        .DisableAntiforgery();

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

        async Task<IResult> HandleRostroVerificacion(HttpRequest request, string? contexto, IEventPublisher eventPublisher, AnalisisDbContext db, ImageAnalysisService analysisService, BiometricRecognitionService biometricRecognition, CancellationToken ct)
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { message = "Debes enviar multipart/form-data con el campo 'frame'." });
            }

            var form = await request.ReadFormAsync();
            var frame = form.Files["frame"];
            var usuarioEsperado = (form["usuarioEsperado"].ToString() ?? string.Empty).Trim();
            var usuarioDetectado = (form["usuarioDetectado"].ToString() ?? string.Empty).Trim();
            var confianzaRostro = ParseDouble(form["confianzaRostro"]);
            var distanciaCm = ParseDouble(form["distanciaCm"]);
            var umbralReconocimiento = ParseDouble(form["umbralReconocimiento"]);
            if (umbralReconocimiento <= 0)
            {
                umbralReconocimiento = thresholdDefault;
            }

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

            BiometricRecognitionResult recognition;
            if (!string.IsNullOrWhiteSpace(usuarioEsperado))
            {
                recognition = await biometricRecognition.VerifyAgainstUserAsync(db, img, usuarioEsperado, umbralReconocimiento, ct);
            }
            else
            {
                recognition = await biometricRecognition.RecognizeAsync(db, img, umbralReconocimiento, minTopMargin, ct);
            }

            usuarioDetectado = recognition.IsMatch ? (recognition.UserId ?? string.Empty) : string.Empty;
            confianzaRostro = recognition.Confidence;

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
                    usuarioDetectadoNombre = recognition.UserName,
                    confianzaRostro = Math.Round(confianzaRostro, 2)
                },
                seguridad = new
                {
                    usuarioDetectado = recognition.UserId,
                    confianza = Math.Round(recognition.Confidence, 4),
                    segundaMejorConfianza = Math.Round(recognition.SecondBestConfidence, 4),
                    umbralExactitud = umbralReconocimiento,
                    margenMinimo = minTopMargin,
                    coincide = recognition.IsMatch,
                    esExacto = recognition.IsMatch,
                    accesoPermitido = recognition.IsMatch,
                    motivo = BuildSecurityReason(recognition.MatchReason)
                },
                sensor = new
                {
                    distanciaCm = Math.Round(distanciaCm, 2),
                    alertaProximidad = distanciaCm > 0 && distanciaCm < 55
                }
            };

            return Results.Ok(payload);
        }

        app.MapPost("/api/v1/ia/analizar-camara", HandleRostroVerificacion)
        .DisableAntiforgery();

        app.MapPost("/api/v1/ia/rostro/verificar", HandleRostroVerificacion)
        .DisableAntiforgery();
    }

    private static string BuildSecurityReason(string reasonCode)
    {
        return reasonCode switch
        {
            "THRESHOLD_OK" => "Coincidencia valida",
            "LOW_CONFIDENCE" => "Confianza insuficiente",
            "AMBIGUOUS_MATCH" => "Coincidencia ambigua",
            "NO_PROFILES" => "Sin perfiles enrolados",
            "USER_NOT_ENROLLED" => "Usuario esperado no enrolado",
            "INVALID_EXPECTED_USER" => "Usuario esperado invalido",
            _ => "Usuario no reconocido"
        };
    }

    private static double ParseDouble(string? raw)
    {
        var value = raw?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed))
        {
            return parsed;
        }

        value = value.Replace(',', '.');
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }

        return 0;
    }

    private static int ParseInt(string? raw, int fallback)
    {
        var value = raw?.Trim() ?? string.Empty;
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out parsed))
        {
            return parsed;
        }

        return fallback;
    }
}
