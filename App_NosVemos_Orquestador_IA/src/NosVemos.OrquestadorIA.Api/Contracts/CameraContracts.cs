namespace NosVemos.OrquestadorIA.Api.Contracts;

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

internal sealed record AnalisisCamaraEvent(Guid AnalisisId, DateTime Fecha, string Contexto, string NivelRiesgo, double BrilloPromedio, double Contraste);
internal sealed record RostroReconocidoEvent(Guid AnalisisId, DateTime Fecha, string UsuarioEsperado, string UsuarioDetectado, double ConfianzaRostro);
internal sealed record ProximidadDetectadaEvent(Guid AnalisisId, DateTime Fecha, double DistanciaCm, bool AlertaProximidad);
