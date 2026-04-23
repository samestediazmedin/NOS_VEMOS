internal record CrearUsuarioRequest(string Nombre, string Email);

internal record UsuarioResponse(Guid Id, string Nombre, string Email, bool Activo);
