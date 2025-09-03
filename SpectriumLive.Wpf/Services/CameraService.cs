using OpenCvSharp;
using System.Collections.ObjectModel;

namespace SpectriumLive.Wpf.Services;
public class CameraService
{
    private VideoCapture? videoCapture;
    private CancellationTokenSource? cancellationTokenSource;
    private Task? cameraTask;

    public static ObservableCollection<string> GetAvailableCameras()
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

    public void StartCamera(int cameraIndex, Action<Mat> onNewFrame)
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

        cameraTask = Task.Run(async () =>
        {
            using var frame = new Mat();

            while ( !token.IsCancellationRequested )
            {
                videoCapture.Read(frame);

                if ( !frame.Empty() )
                {
                    App.Current.Dispatcher.Invoke(() => onNewFrame(frame));
                }
                await Task.Delay(16, token);
            }
        }, token);
    }

    public void StopCamera()
    {
        cancellationTokenSource?.Cancel();

        try
        {
            cameraTask?.Wait(500);
        }
        catch ( OperationCanceledException ) { }

        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
        videoCapture?.Release();
        videoCapture?.Dispose();
        videoCapture = null;
        cameraTask = null;
    }
}
