using OpenCvSharp;
using SpectriumLive.Application.Contracts;
using SpectriumLive.Core.Entities;
using System.Collections.ObjectModel;

namespace SpectriumLive.Wpf.Services;

public class CameraService(ICameraDeviceEnumerator cameraEnumerator)
{
    private readonly ICameraDeviceEnumerator cameraEnumerator = cameraEnumerator;
    private VideoCapture? videoCapture;
    private CancellationTokenSource? cancellationTokenSource;
    private Task? cameraTask;

    public Task<ObservableCollection<CameraDevice>> GetAvailableCamerasAsync()
    {
        return Task.Run(() =>
        {
            var cameras = cameraEnumerator.GetAvailableCameras();
            return new ObservableCollection<CameraDevice>(cameras);
        });
    }

    public void StartCamera(int cameraIndex, Action<Mat> onNewFrame)
    {
        StopCamera();
        videoCapture = new VideoCapture(cameraIndex);

        if ( !videoCapture.IsOpened() )
        {
            videoCapture.Dispose();
            videoCapture = null;
            return;
        }

        cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;

        cameraTask = Task.Run(async () =>
        {
            using var frame = new Mat();

            while ( !token.IsCancellationRequested )
            {
                if ( videoCapture is null || !videoCapture.IsOpened() )
                    break;

                videoCapture.Read(frame);

                if ( !frame.Empty() )
                {
                    App.Current?.Dispatcher.Invoke(() => onNewFrame(frame));
                }
                else
                {
                    await Task.Delay(10, token);
                }
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
        catch ( OperationCanceledException )
        {
        }
        catch ( AggregateException ex ) when ( ex.InnerExceptions.OfType<TaskCanceledException>().Any() )
        {
        }

        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
        videoCapture?.Release();
        videoCapture?.Dispose();
        videoCapture = null;
        cameraTask = null;
    }
}
