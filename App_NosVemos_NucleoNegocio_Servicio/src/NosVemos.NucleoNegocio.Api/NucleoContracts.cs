internal record CrearExpedienteRequest(string Codigo);

internal record ExpedienteResponse(Guid Id, string Codigo, string Estado, DateTime FechaCreacion);

internal sealed record ExpedienteCreadoEvent(Guid ExpedienteId, string Codigo, string Estado, DateTime FechaCreacion);

internal sealed record ExpedienteCerradoEvent(Guid ExpedienteId, string Codigo, string Estado, DateTime FechaCierre);

internal sealed record TelemetriaIngestionRequest(
    string DeviceId,
    string SensorType,
    double Value,
    string Unit,
    DateTime CapturedAt,
    long Sequence,
    TelemetriaQuality Quality,
    TelemetriaMeta Meta,
    string Checksum);

internal sealed record TelemetriaQuality(string Signal, double Confidence);

internal sealed record TelemetriaMeta(string FirmwareVersion, string BridgeVersion, string Source);

internal sealed record TelemetriaRecibidaEvent(
    Guid EventId,
    string DeviceId,
    string SensorType,
    double Value,
    string Unit,
    DateTime CapturedAt,
    long Sequence,
    string Signal,
    double Confidence,
    string Source,
    DateTime ReceivedAt);

internal sealed record DeviceCommandRequest(
    string Command,
    string? TraceId,
    int? TtlSeconds,
    object? Payload);

internal sealed record DeviceAccessResultRequest(
    string? Usuario,
    double? Score,
    string Decision,
    string? TraceId,
    string? Observacion);
