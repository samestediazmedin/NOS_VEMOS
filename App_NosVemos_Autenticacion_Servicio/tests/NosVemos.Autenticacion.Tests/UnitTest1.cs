using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NosVemos.Autenticacion.Tests;

public class AuthEndpointsTests : IClassFixture<AuthFactory>
{
    private readonly HttpClient _client;

    static AuthEndpointsTests()
    {
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "nosvemos-test-secret-at-least-32-bytes");
    }

    public AuthEndpointsTests(AuthFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_NewUser_ReturnsCreated()
    {
        var email = $"user-{Guid.NewGuid():N}@nosvemos.local";
        var response = await _client.PostAsJsonAsync("/api/v1/autenticacion/registro", new
        {
            Email = email,
            Password = "Pass123*"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateUser_ReturnsConflict()
    {
        var email = $"dup-{Guid.NewGuid():N}@nosvemos.local";

        var first = await _client.PostAsJsonAsync("/api/v1/autenticacion/registro", new
        {
            Email = email,
            Password = "Pass123*"
        });

        var second = await _client.PostAsJsonAsync("/api/v1/autenticacion/registro", new
        {
            Email = email,
            Password = "Pass123*"
        });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_RegisteredUser_ReturnsJwtWithExpectedClaims()
    {
        var email = $"login-{Guid.NewGuid():N}@nosvemos.local";
        await _client.PostAsJsonAsync("/api/v1/autenticacion/registro", new
        {
            Email = email,
            Password = "Pass123*"
        });

        var login = await _client.PostAsJsonAsync("/api/v1/autenticacion/login", new
        {
            Email = email,
            Password = "Pass123*"
        });

        var payload = await login.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.access_token));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(payload.access_token);
        Assert.Equal("NosVemos.Auth", token.Issuer);
        Assert.Contains(token.Audiences, a => a == "NosVemos.Client");
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Email && c.Value == email.ToLowerInvariant());
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = $"wrong-pass-{Guid.NewGuid():N}@nosvemos.local";
        await _client.PostAsJsonAsync("/api/v1/autenticacion/registro", new
        {
            Email = email,
            Password = "Pass123*"
        });

        var login = await _client.PostAsJsonAsync("/api/v1/autenticacion/login", new
        {
            Email = email,
            Password = "BadPass999*"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    internal sealed record LoginResponse(string access_token, string token_type, int expires_in, string role);
}

public sealed class AuthFactory : WebApplicationFactory<Program>;
