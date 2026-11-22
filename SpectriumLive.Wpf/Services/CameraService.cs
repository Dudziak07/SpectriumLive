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
    private int isProcessingFrame;

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
                    if ( Interlocked.CompareExchange(ref isProcessingFrame, 1, 0) == 0 )
                    {
                        try
                        {
                            onNewFrame(frame);
                        }
                        finally
                        {
                            Volatile.Write(ref isProcessingFrame, 0);
                        }
                    }
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
        Volatile.Write(ref isProcessingFrame, 0);
    }
}
