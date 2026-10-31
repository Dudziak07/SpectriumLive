using SpectriumLive.Application.Contracts;
using SpectriumLive.Core.Entities;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;

namespace SpectriumLive.Infrastructure.Services;

[SupportedOSPlatform("windows")]
public class DirectShowCameraEnumerator : ICameraDeviceEnumerator
{
    private static readonly Guid VideoInputDeviceCategory = new("860BB310-5D01-11D0-BD3B-00A0C911CE86");

    public IReadOnlyList<CameraDevice> GetAvailableCameras()
    {
        var devices = new List<CameraDevice>();

        if (!OperatingSystem.IsWindows())
            return devices;

        ICreateDevEnum? devEnum = null;
        IEnumMoniker? enumMoniker = null;

        try
        {
            devEnum = (ICreateDevEnum)new SystemDeviceEnum();
            var category = VideoInputDeviceCategory;
            int hr = devEnum.CreateClassEnumerator(ref category, out enumMoniker, 0);

            if (hr != 0 || enumMoniker is null)
                return devices;

            var monikers = new IMoniker[1];
            int index = 0;

            while (enumMoniker.Next(1, monikers, IntPtr.Zero) == 0)
            {
                var moniker = monikers[0];
                object? bagObj = null;

                try
                {
                    var bagGuid = typeof(IPropertyBag).GUID;
                    moniker.BindToStorage(null!, null, ref bagGuid, out bagObj);

                    if (bagObj is IPropertyBag propertyBag)
                    {
                        object value = string.Empty;
                        hr = propertyBag.Read("FriendlyName", ref value, IntPtr.Zero);

                        if (hr == 0 && value is string name && !string.IsNullOrWhiteSpace(name))
                        {
                            devices.Add(new CameraDevice(index++, name));
                        }
                    }
                }
                finally
                {
                    if (bagObj is not null)
                    {
                        Marshal.ReleaseComObject(bagObj);
                    }
                    Marshal.ReleaseComObject(moniker);
                }
            }
        }
        catch
        {
            // DirectShow enumeration failed, return empty list
        }
        finally
        {
            if (enumMoniker is not null)
            {
                Marshal.ReleaseComObject(enumMoniker);
            }
            if (devEnum is not null)
            {
                Marshal.ReleaseComObject(devEnum);
            }
        }

        return devices;
    }

    [ComImport, Guid("62be5d10-60eb-11d0-bd3b-00a0c911ce86")]
    private class SystemDeviceEnum;

    [ComImport, Guid("29840822-5B84-11D0-BD3B-00A0C911CE86"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ICreateDevEnum
    {
        [PreserveSig]
        int CreateClassEnumerator([In] ref Guid clsidDeviceClass, [Out] out IEnumMoniker? ppEnumMoniker, [In] int dwFlags);
    }

    [ComImport, Guid("55272A00-42CB-11CE-8135-00AA004BB851"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyBag
    {
        [PreserveSig]
        int Read([In, MarshalAs(UnmanagedType.LPWStr)] string pszPropName, [In, Out, MarshalAs(UnmanagedType.Struct)] ref object pVar, [In] IntPtr pErrorLog);

        [PreserveSig]
        int Write([In, MarshalAs(UnmanagedType.LPWStr)] string pszPropName, [In, MarshalAs(UnmanagedType.Struct)] ref object pVar);
    }
}
