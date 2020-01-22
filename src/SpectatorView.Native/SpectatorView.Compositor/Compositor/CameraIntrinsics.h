#pragma once

struct Vector2
{
    float x;
    float y;
};

struct Vector3
{
    float x;
    float y;
    float z;
};

struct Pose
{
    Vector3 position;
    Vector3 rotation;
};

struct CameraIntrinsics
{
    Vector2 focalLength;
    Vector2 principalPoint;
    int imageWidth;
    int imageHeight;
};