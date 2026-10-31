using SpectriumLive.Core.Entities;

namespace SpectriumLive.Application.Contracts;

public interface ICameraDeviceEnumerator
{
    IReadOnlyList<CameraDevice> GetAvailableCameras();
}

