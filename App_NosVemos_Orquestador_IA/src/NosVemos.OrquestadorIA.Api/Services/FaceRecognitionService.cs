using System.Collections.Concurrent;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NosVemos.OrquestadorIA.Api.Services;

internal sealed class FaceRecognitionService
{
    private const int TargetSize = 32;
    private const int MaxSamplesPerUser = 20;
    private readonly ConcurrentDictionary<string, List<double[]>> _profiles = new(StringComparer.OrdinalIgnoreCase);

    public int Train(string userId, Image<Rgba32> image)
    {
        var embedding = BuildEmbedding(image);
        var samples = _profiles.GetOrAdd(userId, _ => []);
        lock (samples)
        {
            samples.Add(embedding);
            if (samples.Count > MaxSamplesPerUser)
            {
                samples.RemoveAt(0);
            }

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

                score = samples
                    .Select(sample => CosineSimilarity(probe, sample))
                    .OrderByDescending(x => x)
                    .Take(3)
                    .Average();
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
        var pixels = ExtractCenteredAndNormalizedGrid(image, TargetSize);
        var gradients = BuildGradientMagnitude(pixels, TargetSize);

        var embedding = new double[pixels.Length + gradients.Length];
        Array.Copy(pixels, 0, embedding, 0, pixels.Length);
        Array.Copy(gradients, 0, embedding, pixels.Length, gradients.Length);

        NormalizeInPlace(embedding);
        return embedding;
    }

    private static double[] ExtractCenteredAndNormalizedGrid(Image<Rgba32> image, int size)
    {
        var values = new double[size * size];
        var sourceSize = Math.Min(image.Width, image.Height);
        var offsetX = (image.Width - sourceSize) / 2.0;
        var offsetY = (image.Height - sourceSize) / 2.0;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var srcX = offsetX + ((x + 0.5) * sourceSize / size);
                var srcY = offsetY + ((y + 0.5) * sourceSize / size);
                var p = SampleBilinear(image, srcX, srcY);
                values[y * size + x] = (0.2126 * p.R + 0.7152 * p.G + 0.0722 * p.B) / 255.0;
            }
        }

        var mean = values.Average();
        var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Length;
        var stdev = Math.Sqrt(Math.Max(variance, 1e-12));

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = (values[i] - mean) / stdev;
        }

        return values;
    }

    private static double[] BuildGradientMagnitude(double[] source, int size)
    {
        var result = new double[source.Length];

        for (var y = 1; y < size - 1; y++)
        {
            for (var x = 1; x < size - 1; x++)
            {
                var idx = y * size + x;
                var gx = source[idx + 1] - source[idx - 1];
                var gy = source[idx + size] - source[idx - size];
                result[idx] = Math.Sqrt((gx * gx) + (gy * gy));
            }
        }

        NormalizeInPlace(result);
        return result;
    }

    private static void NormalizeInPlace(double[] values)
    {
        var norm = Math.Sqrt(values.Sum(v => v * v));
        if (norm <= 0)
        {
            return;
        }

        for (var i = 0; i < values.Length; i++)
        {
            values[i] /= norm;
        }
    }

    private static Rgba32 SampleBilinear(Image<Rgba32> image, double x, double y)
    {
        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var x1 = Math.Min(x0 + 1, image.Width - 1);
        var y1 = Math.Min(y0 + 1, image.Height - 1);
        x0 = Math.Clamp(x0, 0, image.Width - 1);
        y0 = Math.Clamp(y0, 0, image.Height - 1);

        var dx = x - x0;
        var dy = y - y0;

        var p00 = image[x0, y0];
        var p10 = image[x1, y0];
        var p01 = image[x0, y1];
        var p11 = image[x1, y1];

        var r = Interpolate(p00.R, p10.R, p01.R, p11.R, dx, dy);
        var g = Interpolate(p00.G, p10.G, p01.G, p11.G, dx, dy);
        var b = Interpolate(p00.B, p10.B, p01.B, p11.B, dx, dy);
        return new Rgba32((byte)r, (byte)g, (byte)b);
    }

    private static double Interpolate(double p00, double p10, double p01, double p11, double dx, double dy)
    {
        var top = p00 + ((p10 - p00) * dx);
        var bottom = p01 + ((p11 - p01) * dx);
        return top + ((bottom - top) * dy);
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
