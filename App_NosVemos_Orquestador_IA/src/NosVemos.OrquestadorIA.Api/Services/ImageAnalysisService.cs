using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NosVemos.OrquestadorIA.Api.Services;

internal sealed class ImageAnalysisService
{
    public (double Brightness, double Contrast) Analyze(Image<Rgba32> image)
    {
        var values = new List<double>(image.Width * image.Height);

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var p = row[x];
                    var luminance = 0.2126 * p.R + 0.7152 * p.G + 0.0722 * p.B;
                    values.Add(luminance);
                }
            }
        });

        var mean = values.Average();
        var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
        var stdev = Math.Sqrt(variance);
        return (mean, stdev);
    }

    public string GetRiskLevel(double brightness, double contrast)
    {
        if (brightness < 55 || contrast < 28)
        {
            return "Alto";
        }

        if (brightness < 90 || contrast < 40)
        {
            return "Medio";
        }

        return "Bajo";
    }

    public string GetRecommendation(string risk, string? context)
    {
        var area = string.IsNullOrWhiteSpace(context) ? "general" : context;
        return risk switch
        {
            "Alto" => $"Se recomienda revision inmediata del caso en modulo {area}.",
            "Medio" => $"Mantener seguimiento y nueva captura en 24 horas para {area}.",
            _ => $"Sin alertas criticas en {area}; continuar monitoreo regular."
        };
    }

    public double[] ExtractFeatureVector(Image<Rgba32> image)
    {
        const int gridRows = 4;
        const int gridCols = 4;
        const int bins = 16;
        var cellBrightness = new double[gridRows * gridCols];
        var cellColorDiff = new double[gridRows * gridCols];
        var counts = new int[gridRows * gridCols];
        var histogram = new double[bins];

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                var rowBucket = Math.Min(gridRows - 1, y * gridRows / accessor.Height);
                for (var x = 0; x < row.Length; x++)
                {
                    var p = row[x];
                    var colBucket = Math.Min(gridCols - 1, x * gridCols / row.Length);
                    var bucket = rowBucket * gridCols + colBucket;
                    var luminance = (0.2126 * p.R + 0.7152 * p.G + 0.0722 * p.B) / 255.0;
                    cellBrightness[bucket] += luminance;
                    cellColorDiff[bucket] += (p.R - p.B) / 255.0;
                    counts[bucket] += 1;

                    var bin = Math.Min(bins - 1, (int)Math.Floor(luminance * bins));
                    histogram[bin] += 1;
                }
            }
        });

        for (var i = 0; i < counts.Length; i++)
        {
            if (counts[i] == 0)
            {
                continue;
            }

            cellBrightness[i] /= counts[i];
            cellColorDiff[i] /= counts[i];
        }

        var totalHistogram = histogram.Sum();
        if (totalHistogram > 0)
        {
            for (var i = 0; i < histogram.Length; i++)
            {
                histogram[i] /= totalHistogram;
            }
        }

        return [.. cellBrightness, .. cellColorDiff, .. histogram];
    }
}
