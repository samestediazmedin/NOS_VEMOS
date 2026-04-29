using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Text.Json;

internal static class NucleoEndpoints
{
    public static void MapNucleoEndpoints(this WebApplication app)
    {
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

        var telemetriaApi = app.MapGroup("/api/v1/telemetria").RequireAuthorization();

        telemetriaApi.MapPost("/ingestion", async (TelemetriaIngestionRequest request, NucleoDbContext db, IEventPublisher eventPublisher, CancellationToken ct) =>
        {
            var receivedAt = DateTime.UtcNow;

            var schemaError = ValidateTelemetria(request);
            if (schemaError is not null)
            {
                return Results.UnprocessableEntity(new
                {
                    status = "rejected",
                    code = "DATA_QUALITY_REJECTED",
                    message = schemaError,
                    deviceId = request.DeviceId,
                    sequence = request.Sequence,
                    receivedAt
                });
            }

            var duplicate = await db.TelemetriaEventos
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.DeviceId == request.DeviceId && x.Sequence == request.Sequence, ct);
            if (duplicate is not null)
            {
                return Results.Ok(new
                {
                    status = "duplicate",
                    eventId = duplicate.Id,
                    deviceId = duplicate.DeviceId,
                    sequence = duplicate.Sequence,
                    receivedAt = duplicate.ReceivedAt
                });
            }

            var lastSequence = await db.TelemetriaEventos
                .Where(x => x.DeviceId == request.DeviceId)
                .Select(x => (long?)x.Sequence)
                .MaxAsync(ct);

            if (lastSequence.HasValue && request.Sequence < lastSequence.Value)
            {
                return Results.Conflict(new
                {
                    status = "rejected",
                    code = "SEQUENCE_CONFLICT",
                    message = "Sequence menor al ultimo valor aceptado para el dispositivo.",
                    deviceId = request.DeviceId,
                    sequence = request.Sequence,
                    lastAcceptedSequence = lastSequence.Value,
                    receivedAt
                });
            }

            var entity = new TelemetriaEvento
            {
                Id = Guid.NewGuid(),
                DeviceId = request.DeviceId,
                SensorType = request.SensorType,
                Value = request.Value,
                Unit = request.Unit,
                CapturedAt = request.CapturedAt,
                Sequence = request.Sequence,
                Signal = request.Quality.Signal,
                Confidence = request.Quality.Confidence,
                Source = request.Meta.Source,
                Checksum = request.Checksum.ToUpperInvariant(),
                ReceivedAt = receivedAt
            };

            db.TelemetriaEventos.Add(entity);
            await db.SaveChangesAsync(ct);

            await eventPublisher.PublishAsync(
                "sensor.telemetria.recibida",
                new TelemetriaRecibidaEvent(
                    entity.Id,
                    entity.DeviceId,
                    entity.SensorType,
                    entity.Value,
                    entity.Unit,
                    entity.CapturedAt,
                    entity.Sequence,
                    entity.Signal,
                    entity.Confidence,
                    entity.Source,
                    entity.ReceivedAt),
                ct);

            if (string.Equals(entity.SensorType, "proximidad", StringComparison.OrdinalIgnoreCase))
            {
                await eventPublisher.PublishAsync(
                    "sensor.proximidad.detectada",
                    new
                    {
                        telemetriaId = entity.Id,
                        fecha = entity.ReceivedAt,
                        deviceId = entity.DeviceId,
                        distanciaCm = entity.Value,
                        alertaProximidad = entity.Value < 55
                    },
                    ct);
            }

            return Results.Accepted(value: new
            {
                status = "accepted",
                eventId = entity.Id,
                deviceId = entity.DeviceId,
                sequence = entity.Sequence,
                receivedAt = entity.ReceivedAt
            });
        });

        var dispositivosApi = app.MapGroup("/api/v1/dispositivos");

        dispositivosApi.MapGet("/{deviceId}/comandos", async (string deviceId, HttpRequest request, NucleoDbContext db, CancellationToken ct) =>
        {
            if (!IsValidDeviceToken(request, app.Configuration, deviceId))
            {
                return Results.Unauthorized();
            }

            var now = DateTime.UtcNow;
            var normalizedDeviceId = deviceId.Trim();

            var cmd = await db.DeviceCommands
                .Where(x => x.DeviceId == normalizedDeviceId && x.Status == "pending" && x.ExpiresAt > now)
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (cmd is null)
            {
                return Results.Ok(new
                {
                    comando = "ninguno",
                    deviceId = normalizedDeviceId,
                    issuedAt = now
                });
            }

            cmd.Status = "consumed";
            cmd.ConsumedAt = now;
            await db.SaveChangesAsync(ct);

            object payload;
            try
            {
                payload = JsonSerializer.Deserialize<object>(cmd.PayloadJson) ?? new { };
            }
            catch
            {
                payload = new { raw = cmd.PayloadJson };
            }

            return Results.Ok(new
            {
                commandId = cmd.Id,
                comando = cmd.Command,
                deviceId = cmd.DeviceId,
                payload,
                issuedAt = cmd.CreatedAt,
                expiresAt = cmd.ExpiresAt
            });
        });

        dispositivosApi.MapPost("/{deviceId}/comandos", async (string deviceId, DeviceCommandRequest request, HttpRequest http, NucleoDbContext db, CancellationToken ct) =>
        {
            if (!IsValidDeviceToken(http, app.Configuration, deviceId))
            {
                return Results.Unauthorized();
            }

            var normalizedCommand = (request.Command ?? string.Empty).Trim().ToLowerInvariant();
            if (normalizedCommand is not ("abrir" or "capturar_registro" or "ninguno"))
            {
                return Results.BadRequest(new { message = "Command invalido. Usa: abrir, capturar_registro o ninguno." });
            }

            var ttlSeconds = Math.Clamp(request.TtlSeconds ?? 30, 5, 300);
            var now = DateTime.UtcNow;
            var entity = new DeviceCommand
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId.Trim(),
                Command = normalizedCommand,
                PayloadJson = JsonSerializer.Serialize(request.Payload ?? new { }),
                Status = "pending",
                CreatedAt = now,
                ExpiresAt = now.AddSeconds(ttlSeconds)
            };

            db.DeviceCommands.Add(entity);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/v1/dispositivos/{entity.DeviceId}/comandos/{entity.Id}", new
            {
                commandId = entity.Id,
                comando = entity.Command,
                deviceId = entity.DeviceId,
                ttlSeconds,
                traceId = request.TraceId
            });
        });

        dispositivosApi.MapPost("/{deviceId}/resultado-acceso", async (string deviceId, DeviceAccessResultRequest request, HttpRequest http) =>
        {
            if (!IsValidDeviceToken(http, app.Configuration, deviceId))
            {
                return Results.Unauthorized();
            }

            var decision = (request.Decision ?? string.Empty).Trim().ToLowerInvariant();
            if (decision is not ("autorizado" or "denegado" or "fallback" or "sin_rostro"))
            {
                return Results.BadRequest(new { message = "Decision invalida." });
            }

            return Results.Accepted(value: new
            {
                ok = true,
                deviceId = deviceId.Trim(),
                usuario = request.Usuario,
                score = request.Score,
                decision,
                traceId = request.TraceId,
                receivedAt = DateTime.UtcNow
            });
        });
    }

    private static bool IsValidDeviceToken(HttpRequest request, IConfiguration configuration, string deviceId)
    {
        var provided = request.Headers["X-Device-Token"].ToString();
        if (string.IsNullOrWhiteSpace(provided))
        {
            return false;
        }

        var expected = configuration[$"DeviceTokens:{deviceId}"]
            ?? configuration["DeviceTokens:default"]
            ?? string.Empty;

        return !string.IsNullOrWhiteSpace(expected) && string.Equals(provided, expected, StringComparison.Ordinal);
    }

    private static string? ValidateTelemetria(TelemetriaIngestionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId) || request.DeviceId.Length > 64)
        {
            return "DeviceId invalido.";
        }

        if (request.Sequence < 0)
        {
            return "Sequence invalido.";
        }

        if (request.Quality is null || request.Meta is null)
        {
            return "Quality y Meta son obligatorios.";
        }

        if (request.Quality.Confidence is < 0 or > 1)
        {
            return "Confidence fuera de rango [0..1].";
        }

        if (!Regex.IsMatch(request.Checksum ?? string.Empty, "^[A-Fa-f0-9]{4,8}$"))
        {
            return "Checksum invalido.";
        }

        return request.SensorType switch
        {
            "proximidad" when request.Unit != "cm" || request.Value is < 0 or > 500 => "Valor de proximidad fuera de rango o unidad invalida.",
            "temperatura" when request.Unit != "c" || request.Value is < -40 or > 125 => "Valor de temperatura fuera de rango o unidad invalida.",
            "humedad" when request.Unit != "%" || request.Value is < 0 or > 100 => "Valor de humedad fuera de rango o unidad invalida.",
            _ => null
        };
    }
}
