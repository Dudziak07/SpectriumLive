using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace SpectriumLive.Wpf.Services;
public class CameraService
{
    private VideoCapture? videoCapture;
    private CancellationTokenSource? cancellationTokenSource;

    public ObservableCollection<string> GetAvailableCameras()
    {
        var cameras = new ObservableCollection<string>();

        for ( int i = 0; i < 10; i++ )
        {
            var cap = new VideoCapture(i);

            if ( cap.IsOpened() )
            {
                cameras.Add($"Camera {i}");
                cap.Release();
            }
            else
            {
                break;
            }
        }
        return cameras;
    }

    public void StartCamera(int cameraIndex, Action<BitmapSource> onNewFrame)
    {
        StopCamera();
        videoCapture = new VideoCapture(cameraIndex);

        if ( !videoCapture.IsOpened() )
        {
            videoCapture.Dispose();
            return;
        }

        cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;

        Task.Run(async () =>
        {
            using var frame = new Mat();

            while ( !token.IsCancellationRequested )
            {
                videoCapture.Read(frame);

                if ( !frame.Empty() )
                {
                    var bitmapSource = frame.ToBitmapSource();
                    bitmapSource.Freeze();

                    App.Current.Dispatcher.Invoke(() => onNewFrame(bitmapSource));
                }
                await Task.Delay(33, token);
            }
        }, token);
    }

    public void StopCamera()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
        videoCapture?.Release();
        videoCapture?.Dispose();
        videoCapture = null;
    }
}
