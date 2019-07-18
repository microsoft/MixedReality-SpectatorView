#include "pch.h"
#include "SpectatorView.WinRTExtensions.h"

using namespace ABI::Windows::Perception::Spatial;

// Because Marshal.GetObjectForIUnknown does not work when using the .NET Native compiler with IInspectable
// objects, this method allows managed code to explicitly specify a type for the marshaller to query for.
// Example usage from managed code for marshalling a SpatialCoordinateSystem using this method:
//
//     [DllImport("SpectatorView.WinRTExtensions.dll", EntryPoint = "MarshalIInspectable")]
//     private static extern void GetSpatialCoordinateSystem(IntPtr nativePtr, out SpatialCoordinateSystem coordinateSystem);

extern "C" __declspec(dllexport) void __stdcall MarshalIInspectable(IUnknown* nativePtr, IUnknown** inspectable)
{
    *inspectable = nativePtr;
}