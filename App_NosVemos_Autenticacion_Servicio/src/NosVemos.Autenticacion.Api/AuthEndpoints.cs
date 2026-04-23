using Microsoft.EntityFrameworkCore;

internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app, string jwtKey)
    {
        app.MapPost("/api/v1/autenticacion/registro", async (RegisterRequest request, AuthDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { message = "Email y password son obligatorios." });
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var exists = await db.Usuarios.AnyAsync(x => x.Email == normalizedEmail);
            if (exists)
            {
                return Results.Conflict(new { message = "El usuario ya existe." });
            }

            db.Usuarios.Add(new UsuarioAuth
            {
                Email = normalizedEmail,
                Password = AuthPasswordService.Hash(request.Password),
                Role = "Usuario"
            });
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/usuarios/{normalizedEmail}", new { Email = normalizedEmail, Rol = "Usuario" });
        });

        app.MapPost("/api/v1/autenticacion/login", async (LoginRequest request, AuthDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { message = "Email y password son obligatorios." });
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await db.Usuarios.FirstOrDefaultAsync(x => x.Email == normalizedEmail);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            if (!AuthPasswordService.Verify(request.Password, user.Password))
            {
                return Results.Unauthorized();
            }

            if (!AuthPasswordService.IsBcryptHash(user.Password))
            {
                user.Password = AuthPasswordService.Hash(request.Password);
                await db.SaveChangesAsync();
            }

            var token = AuthTokenService.BuildToken(new UserCredential(user.Email, user.Role), jwtKey);
            return Results.Ok(new { access_token = token, token_type = "Bearer", expires_in = 3600, role = user.Role });
        });
    }
}
