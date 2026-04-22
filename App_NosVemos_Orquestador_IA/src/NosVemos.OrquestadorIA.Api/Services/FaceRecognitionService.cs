using System.Collections.Concurrent;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NosVemos.OrquestadorIA.Api.Services;

internal sealed class FaceRecognitionService
{
    private readonly ConcurrentDictionary<string, List<double[]>> _profiles = new(StringComparer.OrdinalIgnoreCase);

    public int Train(string userId, Image<Rgba32> image)
    {
        var embedding = BuildEmbedding(image);
        var samples = _profiles.GetOrAdd(userId, _ => []);
        lock (samples)
        {
            samples.Add(embedding);
            return samples.Count;
        }
    }

    public (string? UserId, double Confidence) Identify(Image<Rgba32> image)
    {
        var probe = BuildEmbedding(image);
        var bestUser = default(string);
        var bestScore = -1.0;

        foreach (var (userId, samples) in _profiles)
        {
            double score;
            lock (samples)
            {
                if (samples.Count == 0)
                {
                    continue;
                }

                score = samples.Max(sample => CosineSimilarity(probe, sample));
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestUser = userId;
            }
        }

        if (bestScore < 0)
        {
            return (null, 0);
        }

        return (bestUser, Math.Round((bestScore + 1) / 2, 4));
    }

    public bool HasProfile(string userId) => _profiles.ContainsKey(userId);

    public int GetSampleCount(string userId)
    {
        if (!_profiles.TryGetValue(userId, out var samples))
        {
            return 0;
        }

        lock (samples)
        {
            return samples.Count;
        }
    }

    private static double[] BuildEmbedding(Image<Rgba32> image)
    {
        const int target = 16;
        var values = new double[target * target];

        for (var y = 0; y < target; y++)
        {
            for (var x = 0; x < target; x++)
            {
                var srcX = x * image.Width / target;
                var srcY = y * image.Height / target;
                var p = image[srcX, srcY];
                var luminance = (0.2126 * p.R + 0.7152 * p.G + 0.0722 * p.B) / 255.0;
                values[y * target + x] = luminance;
            }
        }

        var mean = values.Average();
        var norm = Math.Sqrt(values.Sum(v => Math.Pow(v - mean, 2)));
        if (norm <= 0)
        {
            return values;
        }

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = (values[i] - mean) / norm;
        }

        return values;
    }

    private static double CosineSimilarity(double[] a, double[] b)
    {
        var sum = 0.0;
        for (var i = 0; i < a.Length; i++)
        {
            sum += a[i] * b[i];
        }

        return Math.Clamp(sum, -1.0, 1.0);
    }
}
