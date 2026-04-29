using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NosVemos.OrquestadorIA.Api.Services;

internal sealed class BiometricRecognitionService(ImageAnalysisService analysisService)
{
    private const int MaxSamplesPerUser = 24;

    public async Task<BiometricEnrollmentResult> EnrollAsync(
        AnalisisDbContext db,
        string userId,
        string userName,
        string angle,
        int quality,
        Image<Rgba32> image,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var normalizedUserId = Normalize(userId, "usuario");
        var normalizedUserName = Normalize(userName, normalizedUserId);
        var normalizedAngle = Normalize(angle, "frontal");
        var normalizedQuality = Math.Clamp(quality, 0, 100);
        var vector = analysisService.ExtractFeatureVector(image);
        var encoded = EncodeVector(vector);

        var profile = await db.BiometricProfiles.Include(x => x.Samples)
            .FirstOrDefaultAsync(x => x.UserId == normalizedUserId, ct);

        if (profile is null)
        {
            profile = new BiometricProfileEntity
            {
                Id = Guid.NewGuid(),
                UserId = normalizedUserId,
                UserName = normalizedUserName,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.BiometricProfiles.Add(profile);
        }
        else
        {
            profile.UserName = normalizedUserName;
            profile.UpdatedAt = now;
        }

        var sample = new BiometricSampleEntity
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            Angle = normalizedAngle,
            Quality = normalizedQuality,
            FeatureVector = encoded,
            CapturedAt = now
        };
        db.BiometricSamples.Add(sample);

        var samplesToTrim = await db.BiometricSamples
            .Where(x => x.ProfileId == profile.Id)
            .OrderByDescending(x => x.CapturedAt)
            .Skip(MaxSamplesPerUser - 1)
            .ToListAsync(ct);
        if (samplesToTrim.Count > 0)
        {
            db.BiometricSamples.RemoveRange(samplesToTrim);
        }

        await db.SaveChangesAsync(ct);

        var sampleCount = await db.BiometricSamples.CountAsync(x => x.ProfileId == profile.Id, ct);
        return new BiometricEnrollmentResult(profile.UserId, profile.UserName, sampleCount);
    }

    public async Task<BiometricRecognitionResult?> RecognizeAsync(AnalisisDbContext db, Image<Rgba32> image, double threshold, CancellationToken ct)
    {
        var query = await db.BiometricProfiles
            .AsNoTracking()
            .Include(x => x.Samples)
            .Where(x => x.Samples.Count > 0)
            .ToListAsync(ct);

        if (query.Count == 0)
        {
            return null;
        }

        var input = analysisService.ExtractFeatureVector(image);
        var best = query
            .Select(profile =>
            {
                var score = profile.Samples
                    .Select(sample => Similarity(input, DecodeVector(sample.FeatureVector)))
                    .DefaultIfEmpty(0)
                    .Max();
                return new { profile.UserId, profile.UserName, Score = score };
            })
            .OrderByDescending(x => x.Score)
            .First();

        var confidence = Math.Round(best.Score, 4);
        return new BiometricRecognitionResult(best.UserId, best.UserName, confidence, confidence >= threshold);
    }

    public async Task<BiometricRecognitionResult?> VerifyAgainstUserAsync(
        AnalisisDbContext db,
        Image<Rgba32> image,
        string expectedUserId,
        double threshold,
        CancellationToken ct)
    {
        var normalizedExpected = Normalize(expectedUserId, string.Empty);
        if (string.IsNullOrWhiteSpace(normalizedExpected))
        {
            return null;
        }

        var profile = await db.BiometricProfiles
            .AsNoTracking()
            .Include(x => x.Samples)
            .FirstOrDefaultAsync(x => x.UserId.ToLower() == normalizedExpected.ToLower(), ct);

        if (profile is null || profile.Samples.Count == 0)
        {
            return null;
        }

        var input = analysisService.ExtractFeatureVector(image);
        var score = profile.Samples
            .Select(sample => Similarity(input, DecodeVector(sample.FeatureVector)))
            .DefaultIfEmpty(0)
            .Max();

        var confidence = Math.Round(score, 4);
        return new BiometricRecognitionResult(profile.UserId, profile.UserName, confidence, confidence >= threshold);
    }

    private static string Normalize(string? value, string fallback)
    {
        var normalized = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string EncodeVector(double[] vector)
    {
        return string.Join(';', vector.Select(x => x.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)));
    }

    private static double[] DecodeVector(string encoded)
    {
        return encoded
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => double.TryParse(part, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0)
            .ToArray();
    }

    private static double Similarity(double[] a, double[] b)
    {
        if (a.Length == 0 || b.Length == 0)
        {
            return 0;
        }

        var size = Math.Min(a.Length, b.Length);
        double dot = 0;
        double normA = 0;
        double normB = 0;
        for (var i = 0; i < size; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA <= 0 || normB <= 0)
        {
            return 0;
        }

        var cosine = dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
        return Math.Clamp((cosine + 1) / 2, 0, 1);
    }
}

internal sealed record BiometricEnrollmentResult(string UserId, string UserName, int SampleCount);
internal sealed record BiometricRecognitionResult(string UserId, string UserName, double Confidence, bool IsMatch);
