using SpectriumLive.Application.Helpers;
using SpectriumLive.Core;
using SpectriumLive.Core.Entities;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace SpectriumLive.Application.Filters;

public enum BinarizationMethod
{
    Manual,
    PercentBlack,
    MeanIterative,
    Otsu
}

public class BinaryFilter : IFilter, INotifyPropertyChanged
{
    public string Name => "Binary";
    public ImageProcessorGroup Group => ImageProcessorGroup.Color;
    public bool HasConfigurableProperties => true;

    private int[]? lastHistogram;
    private int lastTotalPixels;
    private bool isSyncing;

    private BinarizationMethod selectedMethod = BinarizationMethod.Manual;
    public BinarizationMethod SelectedMethod
    {
        get => selectedMethod;
        set
        {
            if (selectedMethod != value)
            {
                selectedMethod = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedMethod)));
            }
        }
    }

    private int manualThreshold = 128;
    public int ManualThreshold
    {
        get => manualThreshold;
        set
        {
            int clamped = Math.Clamp(value, 0, 255);
            if (manualThreshold != clamped)
            {
                manualThreshold = clamped;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ManualThreshold)));
                SyncPercentFromThreshold(clamped);
            }
        }
    }

    private double percentBlack = 0.5;
    public double PercentBlack
    {
        get => percentBlack;
        set
        {
            double clamped = Math.Clamp(value, 0.0, 1.0);
            if (Math.Abs(percentBlack - clamped) > 0.0001)
            {
                percentBlack = clamped;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PercentBlack)));
                SyncThresholdFromPercent(clamped);
            }
        }
    }

    private int calculatedThreshold = 128;
    public int CalculatedThreshold
    {
        get => calculatedThreshold;
        private set
        {
            if (calculatedThreshold != value)
            {
                calculatedThreshold = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CalculatedThreshold)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SyncPercentFromThreshold(int threshold)
    {
        if (isSyncing || lastHistogram == null || lastTotalPixels == 0)
            return;

        isSyncing = true;
        try
        {
            double newPercent = CalculatePercentFromThreshold(lastHistogram, lastTotalPixels, threshold);
            if (Math.Abs(percentBlack - newPercent) > 0.0001)
            {
                percentBlack = newPercent;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PercentBlack)));
            }
        }
        finally
        {
            isSyncing = false;
        }
    }

    private void SyncThresholdFromPercent(double percent)
    {
        if (isSyncing || lastHistogram == null || lastTotalPixels == 0)
            return;

        isSyncing = true;
        try
        {
            int newThreshold = CalculateThresholdFromPercent(lastHistogram, lastTotalPixels, percent);
            if (manualThreshold != newThreshold)
            {
                manualThreshold = newThreshold;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ManualThreshold)));
            }
        }
        finally
        {
            isSyncing = false;
        }
    }

    private void SyncUIFromCalculatedThreshold(int threshold)
    {
        if (isSyncing || lastHistogram == null || lastTotalPixels == 0)
            return;

        isSyncing = true;
        try
        {
            if (manualThreshold != threshold)
            {
                manualThreshold = threshold;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ManualThreshold)));
            }

            double newPercent = CalculatePercentFromThreshold(lastHistogram, lastTotalPixels, threshold);
            if (Math.Abs(percentBlack - newPercent) > 0.0001)
            {
                percentBlack = newPercent;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PercentBlack)));
            }
        }
        finally
        {
            isSyncing = false;
        }
    }

    public void Apply(ImageBuffer imageBuffer)
    {
        lastHistogram = CalculateHistogram(imageBuffer);
        lastTotalPixels = imageBuffer.Pixels.Length;

        int threshold = SelectedMethod switch
        {
            BinarizationMethod.Manual => ManualThreshold,
            BinarizationMethod.PercentBlack => CalculateThresholdPercentBlack(lastHistogram, lastTotalPixels),
            BinarizationMethod.MeanIterative => CalculateThresholdMeanIterative(lastHistogram),
            BinarizationMethod.Otsu => CalculateThresholdOtsu(lastHistogram, lastTotalPixels),
            _ => ManualThreshold
        };

        CalculatedThreshold = threshold;
        SyncUIFromCalculatedThreshold(threshold);

        Parallel.For(0, imageBuffer.Pixels.Length, i =>
        {
            var pixel = imageBuffer.Pixels[i];
            var gray = ColorCalculator.ToGrayscale(pixel);
            byte finalValue = gray < threshold ? (byte)0 : (byte)255;
            imageBuffer.Pixels[i] = new(finalValue, finalValue, finalValue, pixel.A);
        });
    }

    private int[] CalculateHistogram(ImageBuffer imageBuffer)
    {
        int[] histogram = new int[256];
        object lockObj = new();

        Parallel.ForEach(
            Partitioner.Create(0, imageBuffer.Pixels.Length),
            () => new int[256],
            (range, _, localHist) =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    var gray = ColorCalculator.ToGrayscale(imageBuffer.Pixels[i]);
                    localHist[gray]++;
                }
                return localHist;
            },
            localHist =>
            {
                lock (lockObj)
                {
                    for (int i = 0; i < 256; i++)
                        histogram[i] += localHist[i];
                }
            });

        return histogram;
    }

    private int CalculateThresholdPercentBlack(int[] histogram, int totalPixels)
    {
        return CalculateThresholdFromPercent(histogram, totalPixels, PercentBlack);
    }

    private int CalculateThresholdFromPercent(int[] histogram, int totalPixels, double percent)
    {
        if (totalPixels == 0)
            return 128;

        long target = (long)(totalPixels * percent);
        long count = 0;

        for (int i = 0; i < 256; i++)
        {
            count += histogram[i];
            if (count >= target)
                return i;
        }

        return 255;
    }

    private double CalculatePercentFromThreshold(int[] histogram, int totalPixels, int threshold)
    {
        if (totalPixels == 0)
            return 0.5;

        long count = 0;
        int limit = Math.Clamp(threshold, 0, 255);

        for (int i = 0; i <= limit; i++)
            count += histogram[i];

        return (double)count / totalPixels;
    }

    private int CalculateThresholdMeanIterative(int[] histogram)
    {
        long sum = 0;
        long totalCount = 0;

        for (int i = 0; i < 256; i++)
        {
            sum += (long)i * histogram[i];
            totalCount += histogram[i];
        }

        if (totalCount == 0)
            return 128;

        int currentT = (int)(sum / totalCount);
        int newT;
        int iterations = 0;

        do
        {
            long sumBackground = 0, countBackground = 0;
            long sumForeground = 0, countForeground = 0;

            for (int i = 0; i < 256; i++)
            {
                if (i <= currentT)
                {
                    sumBackground += (long)i * histogram[i];
                    countBackground += histogram[i];
                }
                else
                {
                    sumForeground += (long)i * histogram[i];
                    countForeground += histogram[i];
                }
            }

            double meanBackground = countBackground == 0 ? 0 : (double)sumBackground / countBackground;
            double meanForeground = countForeground == 0 ? 255 : (double)sumForeground / countForeground;

            newT = (int)Math.Round((meanBackground + meanForeground) / 2.0);

            if (newT == currentT)
                break;

            currentT = newT;
            iterations++;

        } while (iterations < 100);

        return currentT;
    }

    private int CalculateThresholdOtsu(int[] histogram, int totalPixels)
    {
        if (totalPixels == 0)
            return 128;

        long sumTotal = 0;
        for (int i = 0; i < 256; i++)
            sumTotal += (long)i * histogram[i];

        long sumBackground = 0;
        long weightBackground = 0;
        double maxVariance = 0;
        int optimalThreshold = 0;

        for (int t = 0; t < 256; t++)
        {
            weightBackground += histogram[t];
            
            if (weightBackground == 0)
                continue;

            long weightForeground = totalPixels - weightBackground;
            
            if (weightForeground == 0)
                break;

            sumBackground += (long)t * histogram[t];

            double meanBackground = (double)sumBackground / weightBackground;
            double meanForeground = (double)(sumTotal - sumBackground) / weightForeground;

            double betweenVariance = (double)weightBackground * weightForeground 
                                   * (meanBackground - meanForeground) 
                                   * (meanBackground - meanForeground);

            if (betweenVariance > maxVariance)
            {
                maxVariance = betweenVariance;
                optimalThreshold = t;
            }
        }

        return optimalThreshold;
    }
}
