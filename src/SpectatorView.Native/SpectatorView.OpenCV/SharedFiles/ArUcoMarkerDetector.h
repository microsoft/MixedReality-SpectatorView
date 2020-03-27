#pragma once
#include "pch.h"
#include "DataStructures.h"

class ArUcoMarkerDetector
{
public:
    ArUcoMarkerDetector();
    ~ArUcoMarkerDetector();
    bool DetectMarkers(
        unsigned char* imageData,
        int imageWidth,
        int imageHeight,
        float* focalLength,
        float* principalPoint,
        float* radialDistortion,
        int radialDistortionCount,
        float* tangentialDistortion,
        int tangentialDistortionCount,
        float markerSize,
        int arUcoMarkerDictionaryId);
    inline int GetDetectedMarkersCount() { return static_cast<int>(_detectedMarkers.size()); }
    inline int GetDetectedMarkerId(int index) { return _detectedMarkers[index].id; }
    bool GetDetectedMarkerIds(int* _detectedIds, int size);
    bool GetDetectedMarkerPose(int _detectedId, Vector3* position, Vector3* rotation);
    void Reset() { _detectedMarkers.clear(); }

private:
    std::unordered_map<int, Marker> _detectedMarkers;
};
