using AForge.Video.DirectShow;
using OpenCvSharp;
using SpectriumLive.Core.Entities;
using System.Collections.ObjectModel;

namespace SpectriumLive.Wpf.Services;
public class CameraService
{
    private VideoCapture? videoCapture;
    private CancellationTokenSource? cancellationTokenSource;
    private Task? cameraTask;

    public Task<ObservableCollection<CameraDevice>> GetAvailableCamerasAsync()
    {
        return Task.Run(() =>
        {
            var cameras = new ObservableCollection<CameraDevice>();
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            for ( int i = 0; i < videoDevices.Count; i++ )
            {
                cameras.Add(new CameraDevice(i, videoDevices[i].Name));
            }
            return cameras;
        });
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
