using SpectriumLive.Core.Entities;

namespace SpectriumLive.Benchmarks;

public enum QuickResolution
{
    Tiny_64x64,
    Sd_640x480,
    FullHd_1920x1080,
    Uhd4K_3840x2160
}

public enum PowerOfTwoResolution
{
    P16_16x16,
    P32_32x32,
    P64_64x64,
    P128_128x128,
    P256_256x256,
    P512_512x512,
    P1024_1024x1024,
    P2048_2048x2048,
    P4096_4096x4096
}

public enum StandardFormatResolution
{
    Hd_1280x720,
    Fhd_1920x1080,
    Wuxga_1920x1200,
    Dci2K_2048x1080,
    Qhd2K_2560x1440,
    Wqxga_2560x1600,
    Uhd3K_2880x1620,
    Uhd4K_3840x2160
}

public enum PortraitFormatResolution
{
    PhoneHd_720x1280,
    PhoneFhd_1080x1920,
    Phone20_9_1080x2400,
    PhonePro_1290x2796,
    Phone4K_2160x3840,
    Portrait12MP_3000x4000
}

public enum PhotoScientificResolution
{
    Thermal_640x512,
    Kodak_768x512,
    MachineVision_1920x1200,
    Photo12MP_4000x3000,
    Photo24MP_6000x4000
}

public enum ExtremeResolution
{
    Uhd8K_7680x4320,
    Square8K_8192x8192,
    Photo45MP_8192x5464,
    Uhd16K_15360x8640,
    Square16K_16384x16384
}

internal static class BenchmarkImageData
{
    public static (int Width, int Height) GetDimensions(QuickResolution resolution) => resolution switch
    {
        QuickResolution.Tiny_64x64 => (64, 64),
        QuickResolution.Sd_640x480 => (640, 480),
        QuickResolution.FullHd_1920x1080 => (1920, 1080),
        QuickResolution.Uhd4K_3840x2160 => (3840, 2160),
        _ => throw new ArgumentOutOfRangeException(nameof(resolution))
    };

    public static (int Width, int Height) GetDimensions(PowerOfTwoResolution resolution) => resolution switch
    {
        PowerOfTwoResolution.P16_16x16 => (16, 16),
        PowerOfTwoResolution.P32_32x32 => (32, 32),
        PowerOfTwoResolution.P64_64x64 => (64, 64),
        PowerOfTwoResolution.P128_128x128 => (128, 128),
        PowerOfTwoResolution.P256_256x256 => (256, 256),
        PowerOfTwoResolution.P512_512x512 => (512, 512),
        PowerOfTwoResolution.P1024_1024x1024 => (1024, 1024),
        PowerOfTwoResolution.P2048_2048x2048 => (2048, 2048),
        PowerOfTwoResolution.P4096_4096x4096 => (4096, 4096),
        _ => throw new ArgumentOutOfRangeException(nameof(resolution))
    };

    public static (int Width, int Height) GetDimensions(StandardFormatResolution resolution) => resolution switch
    {
        StandardFormatResolution.Hd_1280x720 => (1280, 720),
        StandardFormatResolution.Fhd_1920x1080 => (1920, 1080),
        StandardFormatResolution.Wuxga_1920x1200 => (1920, 1200),
        StandardFormatResolution.Dci2K_2048x1080 => (2048, 1080),
        StandardFormatResolution.Qhd2K_2560x1440 => (2560, 1440),
        StandardFormatResolution.Wqxga_2560x1600 => (2560, 1600),
        StandardFormatResolution.Uhd3K_2880x1620 => (2880, 1620),
        StandardFormatResolution.Uhd4K_3840x2160 => (3840, 2160),
        _ => throw new ArgumentOutOfRangeException(nameof(resolution))
    };

    public static (int Width, int Height) GetDimensions(PortraitFormatResolution resolution) => resolution switch
    {
        PortraitFormatResolution.PhoneHd_720x1280 => (720, 1280),
        PortraitFormatResolution.PhoneFhd_1080x1920 => (1080, 1920),
        PortraitFormatResolution.Phone20_9_1080x2400 => (1080, 2400),
        PortraitFormatResolution.PhonePro_1290x2796 => (1290, 2796),
        PortraitFormatResolution.Phone4K_2160x3840 => (2160, 3840),
        PortraitFormatResolution.Portrait12MP_3000x4000 => (3000, 4000),
        _ => throw new ArgumentOutOfRangeException(nameof(resolution))
    };

    public static (int Width, int Height) GetDimensions(PhotoScientificResolution resolution) => resolution switch
    {
        PhotoScientificResolution.Thermal_640x512 => (640, 512),
        PhotoScientificResolution.Kodak_768x512 => (768, 512),
        PhotoScientificResolution.MachineVision_1920x1200 => (1920, 1200),
        PhotoScientificResolution.Photo12MP_4000x3000 => (4000, 3000),
        PhotoScientificResolution.Photo24MP_6000x4000 => (6000, 4000),
        _ => throw new ArgumentOutOfRangeException(nameof(resolution))
    };

    public static (int Width, int Height) GetDimensions(ExtremeResolution resolution) => resolution switch
    {
        ExtremeResolution.Uhd8K_7680x4320 => (7680, 4320),
        ExtremeResolution.Square8K_8192x8192 => (8192, 8192),
        ExtremeResolution.Photo45MP_8192x5464 => (8192, 5464),
        ExtremeResolution.Uhd16K_15360x8640 => (15360, 8640),
        ExtremeResolution.Square16K_16384x16384 => (16384, 16384),
        _ => throw new ArgumentOutOfRangeException(nameof(resolution))
    };

    public static ImageBuffer CreateRealisticBuffer(int width, int height)
    {
        var pixels = new PixelData[width * height];

        Parallel.For(0, height, y =>
        {
            uint state = (uint)(0x12345678u ^ (y * 2654435761u));
            byte baseG = (byte)((y * 255) / Math.Max(1, height - 1));
            int rowOffset = y * width;

            for (int x = 0; x < width; x++)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;

                byte baseR = (byte)((x * 255) / Math.Max(1, width - 1));
                byte noise = (byte)(state & 0x3F);
                byte r = (byte)((baseR + noise) & 0xFF);
                byte g = (byte)((baseG + ((state >> 8) & 0x3F)) & 0xFF);
                byte b = (byte)(((baseR ^ baseG) + ((state >> 16) & 0x3F)) & 0xFF);

                pixels[rowOffset + x] = new PixelData(r, g, b, 255);
            }
        });

        return new ImageBuffer(width, height, pixels);
    }
}
