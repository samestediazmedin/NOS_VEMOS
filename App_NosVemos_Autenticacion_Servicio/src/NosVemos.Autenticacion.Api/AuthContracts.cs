internal record RegisterRequest(string Email, string Password);

internal record LoginRequest(string Email, string Password);

internal record UserCredential(string Email, string Role);
