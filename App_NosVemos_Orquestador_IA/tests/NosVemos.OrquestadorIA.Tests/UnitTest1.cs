using System.Net;
using System.Net.Http.Json;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace NosVemos.OrquestadorIA.Tests;

public class OrquestadorIaEndpointsTests : IClassFixture<OrquestadorIaFactory>
{
    private readonly HttpClient _client;

    static OrquestadorIaEndpointsTests()
    {
        Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
        Environment.SetEnvironmentVariable("Messaging__EnableDomainEvents", "false");
    }

    public OrquestadorIaEndpointsTests(OrquestadorIaFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzeCamera_WithoutMultipart_ReturnsBadRequest()
    {
        using var content = JsonContent.Create(new { frame = "invalid" });
        var response = await _client.PostAsync("/api/v1/ia/analizar-camara", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzeCamera_WithImage_ReturnsOkAndRisk()
    {
        using var form = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(CreateJpegBytes());
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

        form.Add(imageContent, "frame", "captura.jpg");
        form.Add(new StringContent("usuario_demo"), "usuarioEsperado");
        form.Add(new StringContent("usuario_demo"), "usuarioDetectado");
        form.Add(new StringContent("0.95"), "confianzaRostro");
        form.Add(new StringContent("40"), "distanciaCm");

        var response = await _client.PostAsync("/api/v1/ia/analizar-camara?contexto=test", form);
        var rawBody = await response.Content.ReadAsStringAsync();
        var payload = response.StatusCode == HttpStatusCode.OK
            ? JsonSerializer.Deserialize<AnalisisResponse>(rawBody)
            : null;

        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Status={(int)response.StatusCode} Body={rawBody}");
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.evaluacion.nivelRiesgo));
        Assert.False(string.IsNullOrWhiteSpace(payload.evaluacion.recomendacion));
    }

    private static byte[] CreateJpegBytes()
    {
        using var image = new Image<Rgba32>(8, 8);
        for (var x = 0; x < 8; x++)
        {
            for (var y = 0; y < 8; y++)
            {
                image[x, y] = (x + y) % 2 == 0 ? new Rgba32(25, 140, 220) : new Rgba32(230, 240, 250);
            }
        }

        using var stream = new MemoryStream();
        image.Save(stream, new JpegEncoder());
        return stream.ToArray();
    }

    internal sealed record AnalisisResponse(Evaluacion evaluacion);

    internal sealed record Evaluacion(string nivelRiesgo, string recomendacion);
}

public sealed class OrquestadorIaFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseInMemoryDatabase"] = "true",
                ["Messaging:EnableDomainEvents"] = "false"
            });
        });
    }
}
