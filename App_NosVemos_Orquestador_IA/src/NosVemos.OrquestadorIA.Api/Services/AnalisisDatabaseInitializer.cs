namespace NosVemos.OrquestadorIA.Api.Services;

internal static class AnalisisDatabaseInitializer
{
    public static void Initialize(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AnalisisDbContext>();
        db.Database.EnsureCreated();
    }
}
