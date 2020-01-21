#pragma once

struct Vector2
{
    float x;
    float y;
};

struct CameraIntrinsics
{
    Vector2 focalLength;
    Vector2 principalPoint;
    int imageWidth;
    int imageHeight;
};