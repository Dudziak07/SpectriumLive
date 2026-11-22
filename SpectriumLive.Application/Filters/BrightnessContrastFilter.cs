using SpectriumLive.Application.Helpers;
using SpectriumLive.Core;
using SpectriumLive.Core.Entities;
using System.ComponentModel;

namespace SpectriumLive.Application.Filters;

public class BrightnessContrastFilter : IFilter, INotifyPropertyChanged
{
    public string Name => "Brightness / Contrast";
    public ImageProcessorGroup Group => ImageProcessorGroup.Color;
    public bool HasConfigurableProperties => true;

    private readonly byte[] _lut = new byte[256];

    private int _brightness;
    public int Brightness
    {
        get => _brightness;
        set
        {
            int clamped = Math.Clamp(value, -255, 255);
            if (_brightness != clamped)
            {
                _brightness = clamped;
                RebuildLut();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Brightness)));
            }
        }
    }

    private double _contrast = 1.0;
    public double Contrast
    {
        get => _contrast;
        set
        {
            double clamped = double.IsNaN(value) ? 1.0 : Math.Clamp(value, 0.0, 3.0);
            if (Math.Abs(_contrast - clamped) >= 1e-9)
            {
                _contrast = clamped;
                RebuildLut();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Contrast)));
            }
        }
    }

    public BrightnessContrastFilter()
    {
        RebuildLut();
    }

    private void RebuildLut()
    {
        for (int i = 0; i < 256; i++)
        {
            _lut[i] = (byte)Math.Clamp((int)Math.Round((i - 128) * _contrast + 128 + _brightness), 0, 255);
        }
    }

    public void Apply(ImageBuffer imageBuffer)
    {
        if (_brightness == 0 && Math.Abs(_contrast - 1.0) < 1e-6)
            return;

        PixelSimdHelper.ForEachSlice(imageBuffer.Pixels, _lut, static (slice, lut) =>
            PixelSimdHelper.ApplyLut(slice, lut));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
