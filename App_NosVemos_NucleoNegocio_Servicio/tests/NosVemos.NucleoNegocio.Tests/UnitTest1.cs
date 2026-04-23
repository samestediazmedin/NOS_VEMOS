using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace NosVemos.NucleoNegocio.Tests;

public class NucleoAuthorizationTests : IClassFixture<NucleoFactory>
{
    private const string TestJwtSecret = "nosvemos-test-secret-at-least-32-bytes";
    private readonly HttpClient _client;

    static NucleoAuthorizationTests()
    {
        Environment.SetEnvironmentVariable("Jwt__SecretKey", TestJwtSecret);
        Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
        Environment.SetEnvironmentVariable("Messaging__EnableDomainEvents", "false");
    }

    public NucleoAuthorizationTests(NucleoFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetExpedientes_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/expedientes");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetExpedientes_WithToken_ReturnsOk()
    {
        var token = BuildToken(TestJwtSecret);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/expedientes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ingestion_ValidTelemetry_ReturnsAccepted()
    {
        var token = BuildToken(TestJwtSecret);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = BuildPayload(sequence: 100);
        var response = await _client.PostAsJsonAsync("/api/v1/telemetria/ingestion", payload);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task Ingestion_DuplicateSequence_ReturnsOkDuplicate()
    {
        var token = BuildToken(TestJwtSecret);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = BuildPayload(sequence: 101);
        var first = await _client.PostAsJsonAsync("/api/v1/telemetria/ingestion", payload);
        var second = await _client.PostAsJsonAsync("/api/v1/telemetria/ingestion", payload);

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task Ingestion_SequenceConflict_ReturnsConflict()
    {
        var token = BuildToken(TestJwtSecret);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newer = BuildPayload(sequence: 300);
        var older = BuildPayload(sequence: 299);

        var first = await _client.PostAsJsonAsync("/api/v1/telemetria/ingestion", newer);
        var second = await _client.PostAsJsonAsync("/api/v1/telemetria/ingestion", older);

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Ingestion_InvalidChecksum_ReturnsUnprocessableEntity()
    {
        var token = BuildToken(TestJwtSecret);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = BuildPayload(sequence: 400, checksum: "ZZZZ");
        var response = await _client.PostAsJsonAsync("/api/v1/telemetria/ingestion", payload);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }

    private static object BuildPayload(long sequence, string checksum = "A1F9")
    {
        return new
        {
            DeviceId = "arduino-001",
            SensorType = "proximidad",
            Value = 42.5,
            Unit = "cm",
            CapturedAt = DateTime.UtcNow,
            Sequence = sequence,
            Quality = new { Signal = "ok", Confidence = 0.95 },
            Meta = new { FirmwareVersion = "1.0.0", BridgeVersion = "1.0.0", Source = "serial" },
            Checksum = checksum
        };
    }

    private static string BuildToken(string secret)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "NosVemos.Auth",
            audience: "NosVemos.Client",
            claims: [new Claim(ClaimTypes.Email, "tester@nosvemos.local"), new Claim(ClaimTypes.Role, "Usuario")],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class NucleoFactory : WebApplicationFactory<Program>
{
    private const string FactoryJwtSecret = "nosvemos-test-secret-at-least-32-bytes";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseInMemoryDatabase"] = "true",
                ["Messaging:EnableDomainEvents"] = "false",
                ["Jwt:SecretKey"] = FactoryJwtSecret
            });
        });
    }
}
