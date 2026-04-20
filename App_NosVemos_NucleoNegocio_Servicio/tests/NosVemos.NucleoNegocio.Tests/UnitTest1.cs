using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;

namespace NosVemos.NucleoNegocio.Tests;

public class NucleoAuthorizationTests : IClassFixture<NucleoFactory>
{
    private const string TestJwtSecret = "nosvemos-test-secret-at-least-32-bytes";
    private readonly HttpClient _client;

    static NucleoAuthorizationTests()
    {
        Environment.SetEnvironmentVariable("Jwt__SecretKey", TestJwtSecret);
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

public sealed class NucleoFactory : WebApplicationFactory<Program>;
