// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

struct Marker
{
    int id;
    Vector3 position; // vector
    Vector3 rotation; // rodrigues vector
};

struct CameraIntrinsics
{
    Vector2 focalLength;
    Vector2 principalPoint;
    int imageWidth;
    int imageHeight;
};