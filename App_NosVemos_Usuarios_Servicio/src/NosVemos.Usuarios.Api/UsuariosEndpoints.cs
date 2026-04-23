using Microsoft.EntityFrameworkCore;

internal static class UsuariosEndpoints
{
    public static void MapUsuariosEndpoints(this WebApplication app)
    {
        var usuarios = app.MapGroup("/api/v1/usuarios").RequireAuthorization();

        usuarios.MapGet("", async (UsuariosDbContext db) =>
        {
            var users = await db.Usuarios
                .Select(x => new UsuarioResponse(x.Id, x.Nombre, x.Email, x.Activo))
                .ToListAsync();
            return Results.Ok(users);
        });

        usuarios.MapGet("/{id:guid}", async (Guid id, UsuariosDbContext db) =>
        {
            var user = await db.Usuarios
                .Where(x => x.Id == id)
                .Select(x => new UsuarioResponse(x.Id, x.Nombre, x.Email, x.Activo))
                .FirstOrDefaultAsync();
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        usuarios.MapPost("", async (CrearUsuarioRequest request, UsuariosDbContext db) =>
        {
            var entity = new Usuario
            {
                Id = Guid.NewGuid(),
                Nombre = request.Nombre,
                Email = request.Email,
                Activo = true
            };
            db.Usuarios.Add(entity);
            await db.SaveChangesAsync();

            var created = new UsuarioResponse(entity.Id, entity.Nombre, entity.Email, entity.Activo);
            return Results.Created($"/api/v1/usuarios/{created.Id}", created);
        });
    }
}
