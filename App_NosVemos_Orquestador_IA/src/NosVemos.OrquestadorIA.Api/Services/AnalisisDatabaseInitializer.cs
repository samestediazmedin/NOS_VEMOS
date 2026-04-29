using Microsoft.EntityFrameworkCore;

namespace NosVemos.OrquestadorIA.Api.Services;

internal static class AnalisisDatabaseInitializer
{
    public static void Initialize(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AnalisisDbContext>();
        db.Database.EnsureCreated();

        if (db.Database.IsSqlServer())
        {
            db.Database.ExecuteSqlRaw(
                """
                IF OBJECT_ID(N'[BiometricProfiles]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [BiometricProfiles] (
                        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                        [UserId] nvarchar(128) NOT NULL,
                        [UserName] nvarchar(256) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NOT NULL
                    );
                    CREATE UNIQUE INDEX [IX_BiometricProfiles_UserId] ON [BiometricProfiles]([UserId]);
                END
                """);

            db.Database.ExecuteSqlRaw(
                """
                IF OBJECT_ID(N'[BiometricSamples]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [BiometricSamples] (
                        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                        [ProfileId] uniqueidentifier NOT NULL,
                        [Angle] nvarchar(32) NOT NULL,
                        [Quality] int NOT NULL,
                        [FeatureVector] nvarchar(max) NOT NULL,
                        [CapturedAt] datetime2 NOT NULL,
                        CONSTRAINT [FK_BiometricSamples_BiometricProfiles_ProfileId]
                            FOREIGN KEY ([ProfileId]) REFERENCES [BiometricProfiles]([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_BiometricSamples_ProfileId] ON [BiometricSamples]([ProfileId]);
                END
                """);
        }
    }
}
