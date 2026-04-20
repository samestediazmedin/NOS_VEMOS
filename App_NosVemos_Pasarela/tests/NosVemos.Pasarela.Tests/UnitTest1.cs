using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;

namespace NosVemos.Pasarela.Tests;

public class GatewayAuthorizationTests : IClassFixture<GatewayFactory>
{
    private const string TestJwtSecret = "nosvemos-test-secret-at-least-32-bytes";

    static GatewayAuthorizationTests()
    {
        Environment.SetEnvironmentVariable("Jwt__SecretKey", TestJwtSecret);
    }

    private readonly HttpClient _client;

    public GatewayAuthorizationTests(GatewayFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UsuariosRoute_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/usuarios");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UsuariosRoute_WithToken_IsNotRejectedByGatewayAuth()
    {
        var token = BuildToken(TestJwtSecret);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/usuarios");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
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

public sealed class GatewayFactory : WebApplicationFactory<Program>;
