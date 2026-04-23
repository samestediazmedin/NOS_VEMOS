using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace NosVemos.Usuarios.Tests;

public class UsuariosAuthorizationTests : IClassFixture<UsuariosFactory>
{
    private const string TestJwtSecret = "nosvemos-test-secret-at-least-32-bytes";
    private readonly HttpClient _client;

    static UsuariosAuthorizationTests()
    {
        Environment.SetEnvironmentVariable("Jwt__SecretKey", TestJwtSecret);
        Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
    }

    public UsuariosAuthorizationTests(UsuariosFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsuarios_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/usuarios");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUsuarios_WithToken_ReturnsOk()
    {
        var token = BuildToken(TestJwtSecret);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/usuarios");
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

public sealed class UsuariosFactory : WebApplicationFactory<Program>
{
    private const string FactoryJwtSecret = "nosvemos-test-secret-at-least-32-bytes";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseInMemoryDatabase"] = "true",
                ["Jwt:SecretKey"] = FactoryJwtSecret
            });
        });
    }
}
