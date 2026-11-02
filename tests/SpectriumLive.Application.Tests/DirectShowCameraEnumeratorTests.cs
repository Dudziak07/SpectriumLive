using SpectriumLive.Infrastructure.Services;
using Xunit;

namespace SpectriumLive.Application.Tests;

public class DirectShowCameraEnumeratorTests
{
    [Fact]
    public void GetAvailableCameras_DoesNotThrow_AndReturnsValidList()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var enumerator = new DirectShowCameraEnumerator();
        var cameras = enumerator.GetAvailableCameras();

        Assert.NotNull(cameras);
    }
}
