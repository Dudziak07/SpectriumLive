using SpectriumLive.Application.Helpers;
using SpectriumLive.Core.Entities;
using System.ComponentModel;

namespace SpectriumLive.Application.Filters.Convolution;

public class GaussianBlurFilter : ConvolutionFilterBase, INotifyPropertyChanged
{
    public override string Name => "Gaussian Blur";
    public override bool HasConfigurableProperties => true;

    private double sigma = 1.4;
    private int kernelSize = 5;
    private double[,] kernel;
    private float[] kernel1D;
    private double factor;

    public double Sigma
    {
        get => sigma;
        set
        {
            if (Math.Abs(sigma - value) > 0.001)
            {
                sigma = Math.Clamp(value, 0.1, 10.0);
                RegenerateKernel();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sigma)));
            }
        }
    }

    public int KernelSize
    {
        get => kernelSize;
        set
        {
            int newSize = value % 2 == 0 ? value + 1 : value;
            newSize = Math.Clamp(newSize, 3, 15);
            
            if (kernelSize != newSize)
            {
                kernelSize = newSize;
                RegenerateKernel();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KernelSize)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected override double[,] Kernel => kernel;
    protected override double Factor => factor;

    public GaussianBlurFilter()
    {
        kernel = new double[5, 5];
        kernel1D = new float[5];
        factor = 1.0;
        RegenerateKernel();
    }

    public override void Apply(ImageBuffer imageBuffer) =>
        ConvolutionSimdHelper.ApplySeparableGaussianBlur(imageBuffer, kernel1D);

    private void RegenerateKernel()
    {
        kernel = GenerateGaussianKernel(kernelSize, sigma);
        factor = CalculateFactor(kernel);
        kernel1D = GenerateGaussianKernel1D(kernelSize, sigma);
    }

    private static float[] GenerateGaussianKernel1D(int size, double sigma)
    {
        var raw = new double[size];
        int center = size / 2;
        double twoSigma2 = 2.0 * sigma * sigma;
        double sum = 0.0;

        for (int i = 0; i < size; i++)
        {
            int d = i - center;
            double v = Math.Exp(-(d * d) / twoSigma2);
            raw[i] = v;
            sum += v;
        }

        double invSum = sum > 0.0 ? 1.0 / sum : 1.0;
        var result = new float[size];
        for (int i = 0; i < size; i++)
            result[i] = (float)(raw[i] * invSum);

        return result;
    }

    private static double[,] GenerateGaussianKernel(int size, double sigma)
    {
        var kernel = new double[size, size];
        int center = size / 2;
        double sigma2 = sigma * sigma;
        double normalizationFactor = 1.0 / (2 * Math.PI * sigma2);

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int dx = x - center;
                int dy = y - center;
                
                kernel[y, x] = normalizationFactor * Math.Exp(-(dx * dx + dy * dy) / (2 * sigma2));
            }

        return kernel;
    }

    private static double CalculateFactor(double[,] kernel)
    {
        double sum = 0;
        int size = kernel.GetLength(0);
        
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                sum += kernel[y, x];

        return sum > 0 ? 1.0 / sum : 1.0;
    }
}
