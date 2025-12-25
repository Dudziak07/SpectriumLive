using SpectriumLive.Application.Filters.Convolution;
using SpectriumLive.Core.Entities;
using Xunit;

namespace SpectriumLive.Application.Tests.Filters.Convolution;

public class GaussianBlurFilterTests
{
    [Fact]
    public void Apply_OnImage_AppliesBlurEffect()
    {
        // Arrange
        var filter = new GaussianBlurFilter();
        var pixels = new PixelData[25];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new PixelData(0, 0, 0, 255);
        pixels[12] = new PixelData(255, 255, 255, 255);
        var image = new ImageBuffer(5, 5, pixels);

        // Act
        filter.Apply(image);

        // Assert - center pixel should be blurred
        var centerPixel = image.Pixels[12];
        Assert.True(centerPixel.R > 0 && centerPixel.R < 255);
        Assert.Equal(centerPixel.R, centerPixel.G);
        Assert.Equal(centerPixel.R, centerPixel.B);
    }

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var filter = new GaussianBlurFilter();

        // Assert
        Assert.Equal(1.4, filter.Sigma);
        Assert.Equal(5, filter.KernelSize);
        Assert.True(filter.HasConfigurableProperties);
    }

    [Fact]
    public void Sigma_ClampsToValidRange()
    {
        // Arrange
        var filter = new GaussianBlurFilter();

        // Act & Assert - below minimum
        filter.Sigma = 0.01;
        Assert.Equal(0.1, filter.Sigma);

        // Act & Assert - above maximum
        filter.Sigma = 15.0;
        Assert.Equal(10.0, filter.Sigma);
    }

    [Theory]
    [InlineData(4, 5)]
    [InlineData(6, 7)]
    public void KernelSize_EvenValue_ConvertsToNextOdd(int inputSize, int expectedSize)
    {
        // Arrange
        var filter = new GaussianBlurFilter();

        // Act
        filter.KernelSize = inputSize;

        // Assert
        Assert.Equal(expectedSize, filter.KernelSize);
    }

    [Fact]
    public void PropertyChanged_RaisedOnSigmaChange()
    {
        // Arrange
        var filter = new GaussianBlurFilter();
        string? changedProperty = null;
        filter.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

        // Act
        filter.Sigma = 2.0;

        // Assert
        Assert.Equal(nameof(filter.Sigma), changedProperty);
    }

    [Fact]
    public void Apply_LargerSigma_ProducesMoreBlur()
    {
        // Arrange
        var pixels1 = Enumerable.Repeat(new PixelData(0, 0, 0, 255), 49).ToArray();
        var pixels2 = Enumerable.Repeat(new PixelData(0, 0, 0, 255), 49).ToArray();
        pixels1[24] = pixels2[24] = new PixelData(255, 255, 255, 255);

        var image1 = new ImageBuffer(7, 7, pixels1);
        var image2 = new ImageBuffer(7, 7, pixels2);

        var filterSmallSigma = new GaussianBlurFilter { Sigma = 0.5, KernelSize = 7 };
        var filterLargeSigma = new GaussianBlurFilter { Sigma = 2.0, KernelSize = 7 };

        // Act
        filterSmallSigma.Apply(image1);
        filterLargeSigma.Apply(image2);

        // Assert - larger sigma spreads light more, so center is darker
        Assert.True(image1.Pixels[24].R > image2.Pixels[24].R);
    }
}
