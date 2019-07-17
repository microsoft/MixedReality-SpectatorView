#include "pch.h"
#include "SpectatorView.WinRTExtensions.h"

using namespace ABI::Windows::Perception::Spatial;

extern "C" __declspec(dllexport) void __stdcall GetSpatialCoordinateSystem(IUnknown* nativePtr, SpatialCoordinateSystem** coordinateSystem)
{
    *coordinateSystem = nullptr;

    if (nativePtr != nullptr)
    {
        nativePtr->QueryInterface(IID_ISpatialCoordinateSystem, (void**) coordinateSystem);
    }
}