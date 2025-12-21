using SpectriumLive.Application.Filters;
using SpectriumLive.Core.Entities;
using Xunit;

namespace SpectriumLive.Application.Tests.Filters;

public class BinaryFilterTests
{
    private const byte Threshold = 128;

    [Fact]
    public void Apply_OnDarkPixel_ReturnsBlackPixel()
    {
        // Arrange
        var filter = new BinaryFilter();
        var darkPixel = new PixelData(100, 130, 140, 255);
        var image = new ImageBuffer(1, 1, [darkPixel]);

        // Act
        filter.Apply(image);

        // Assert
        var resultPixel = image.Pixels[0];

        Assert.Equal(0, resultPixel.R);
        Assert.Equal(0, resultPixel.G);
        Assert.Equal(0, resultPixel.B);
        Assert.Equal(255, resultPixel.A);
    }

    [Fact]
    public void Apply_OnLightPixel_ReturnsWhitePixel()
    {
        // Arrange
        var filter = new BinaryFilter();
        var lightPixel = new PixelData(100, 150, 200, 255);
        var image = new ImageBuffer(1, 1, [lightPixel]);

        // Act
        filter.Apply(image);

        // Assert
        var resultPixel = image.Pixels[0];

        Assert.Equal(255, resultPixel.R);
        Assert.Equal(255, resultPixel.G);
        Assert.Equal(255, resultPixel.B);
        Assert.Equal(255, resultPixel.A);
    }

    [Fact]
    public void Apply_ManualMethod_OnDarkPixel_ReturnsBlackPixel()
    {
        // Arrange
        var filter = new BinaryFilter { SelectedMethod = BinarizationMethod.Manual, ManualThreshold = 128 };
        var darkPixel = new PixelData(100, 130, 140, 255);
        var image = new ImageBuffer(1, 1, [darkPixel]);

        // Act
        filter.Apply(image);

        // Assert
        var resultPixel = image.Pixels[0];

        Assert.Equal(0, resultPixel.R);
        Assert.Equal(0, resultPixel.G);
        Assert.Equal(0, resultPixel.B);
        Assert.Equal(255, resultPixel.A);
    }

    [Fact]
    public void Apply_ManualMethod_OnLightPixel_ReturnsWhitePixel()
    {
        // Arrange
        var filter = new BinaryFilter { SelectedMethod = BinarizationMethod.Manual, ManualThreshold = 128 };
        var lightPixel = new PixelData(100, 150, 200, 255);
        var image = new ImageBuffer(1, 1, [lightPixel]);

        // Act
        filter.Apply(image);

        // Assert
        var resultPixel = image.Pixels[0];

        Assert.Equal(255, resultPixel.R);
        Assert.Equal(255, resultPixel.G);
        Assert.Equal(255, resultPixel.B);
        Assert.Equal(255, resultPixel.A);
    }

    [Fact]
    public void Apply_ManualMethod_CustomThreshold_AppliesCorrectly()
    {
        // Arrange
        var filter = new BinaryFilter { SelectedMethod = BinarizationMethod.Manual, ManualThreshold = 50 };
        var pixel = new PixelData(60, 60, 60, 255); // Gray ~60
        var image = new ImageBuffer(1, 1, [pixel]);

        // Act
        filter.Apply(image);

        // Assert
        var resultPixel = image.Pixels[0];
        Assert.Equal(255, resultPixel.R); // Above threshold 50
    }

    [Fact]
    public void Apply_PercentBlackMethod_CalculatesThresholdFromHistogram()
    {
        // Arrange
        var filter = new BinaryFilter { SelectedMethod = BinarizationMethod.PercentBlack, PercentBlack = 0.5 };
        var pixels = new PixelData[]
        {
            new(0, 0, 0, 255),       // Black
            new(128, 128, 128, 255), // Mid gray
            new(255, 255, 255, 255), // White
            new(64, 64, 64, 255)     // Dark gray
        };
        var image = new ImageBuffer(2, 2, pixels);

        // Act
        filter.Apply(image);

        // Assert - At 50% percent black, threshold should split the image
        Assert.True(filter.CalculatedThreshold >= 0 && filter.CalculatedThreshold <= 255);
    }

    [Fact]
    public void Apply_MeanIterativeMethod_CalculatesThreshold()
    {
        // Arrange
        var filter = new BinaryFilter { SelectedMethod = BinarizationMethod.MeanIterative };
        var pixels = new PixelData[]
        {
            new(0, 0, 0, 255),
            new(50, 50, 50, 255),
            new(200, 200, 200, 255),
            new(255, 255, 255, 255)
        };
        var image = new ImageBuffer(2, 2, pixels);

        // Act
        filter.Apply(image);

        // Assert
        Assert.True(filter.CalculatedThreshold > 0 && filter.CalculatedThreshold < 255);
    }

    [Fact]
    public void Apply_OtsuMethod_CalculatesOptimalThreshold()
    {
        // Arrange
        var filter = new BinaryFilter { SelectedMethod = BinarizationMethod.Otsu };
        var pixels = new PixelData[]
        {
            new(10, 10, 10, 255),
            new(20, 20, 20, 255),
            new(230, 230, 230, 255),
            new(240, 240, 240, 255)
        };
        var image = new ImageBuffer(2, 2, pixels);

        // Act
        filter.Apply(image);

        // Assert - Otsu should find a threshold and apply binarization
        Assert.True(filter.CalculatedThreshold >= 0 && filter.CalculatedThreshold <= 255);
        
        // All pixels should be either black (0) or white (255) after binarization
        foreach (var pixel in image.Pixels)
        {
            Assert.True(pixel.R == 0 || pixel.R == 255);
            Assert.True(pixel.G == 0 || pixel.G == 255);
            Assert.True(pixel.B == 0 || pixel.B == 255);
        }
    }

    [Fact]
    public void HasConfigurableProperties_ReturnsTrue()
    {
        var filter = new BinaryFilter();
        Assert.True(filter.HasConfigurableProperties);
    }

    [Fact]
    public void ManualThreshold_ClampedTo0_255()
    {
        var filter = new BinaryFilter();
        
        filter.ManualThreshold = -10;
        Assert.Equal(0, filter.ManualThreshold);
        
        filter.ManualThreshold = 300;
        Assert.Equal(255, filter.ManualThreshold);
    }

    [Fact]
    public void PercentBlack_ClampedTo0_1()
    {
        var filter = new BinaryFilter();
        
        filter.PercentBlack = -0.5;
        Assert.Equal(0.0, filter.PercentBlack);
        
        filter.PercentBlack = 1.5;
        Assert.Equal(1.0, filter.PercentBlack);
    }
}
