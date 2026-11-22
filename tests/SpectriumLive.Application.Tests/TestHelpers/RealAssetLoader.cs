using SpectriumLive.Core.Entities;
using StbImageSharp;

namespace SpectriumLive.Application.Tests.TestHelpers;

internal static class RealAssetLoader
{
    public static string? FindAssetsRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            string candidate = Path.Combine(dir.FullName, ".test-assets");
            if (Directory.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        return null;
    }

    public static IEnumerable<string> GetKodakFiles()
    {
        string? root = FindAssetsRoot();
        if (root is null)
            return [];

        string kodakDir = Path.Combine(root, "kodak");
        if (!Directory.Exists(kodakDir))
            return [];

        return Directory.EnumerateFiles(kodakDir, "*.png").OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
    }

    public static IEnumerable<string> GetHighResFiles()
    {
        string? root = FindAssetsRoot();
        if (root is null)
            return [];

        string highResDir = Path.Combine(root, "highres");
        if (!Directory.Exists(highResDir))
            return [];

        return Directory.EnumerateFiles(highResDir, "*.*")
            .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
    }

    public static ImageBuffer LoadAsImageBuffer(string path)
    {
        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        int pixelCount = image.Width * image.Height;
        var pixels = new PixelData[pixelCount];
        byte[] data = image.Data;

        for (int i = 0; i < pixelCount; i++)
        {
            int offset = i * 4;
            pixels[i] = new PixelData(data[offset], data[offset + 1], data[offset + 2], data[offset + 3]);
        }

        return new ImageBuffer(image.Width, image.Height, pixels);
    }
}
