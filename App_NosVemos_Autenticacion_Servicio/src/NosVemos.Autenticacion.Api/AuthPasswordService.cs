internal static class AuthPasswordService
{
    public static string Hash(string rawPassword)
    {
        return BCrypt.Net.BCrypt.HashPassword(rawPassword);
    }

    public static bool Verify(string inputPassword, string storedPassword)
    {
        if (IsBcryptHash(storedPassword))
        {
            return BCrypt.Net.BCrypt.Verify(inputPassword, storedPassword);
        }

        return inputPassword == storedPassword;
    }

    public static bool IsBcryptHash(string value)
    {
        return value.StartsWith("$2a$") || value.StartsWith("$2b$") || value.StartsWith("$2y$");
    }
}
