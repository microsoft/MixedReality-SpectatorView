﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;

namespace Microsoft.MixedReality.USYD.Solvers
{
    public class BoardSolvers : MonoBehaviour
    {
        // To prevent enabling twice
        static bool enabled;

        private SolverHandler handler;
        private Solver currentSolver;

        private TrackedObjectType trackedType = TrackedObjectType.Head;

        public void SetSurfaceMagnetism()
        {
            // Already on
            if (enabled)
            {
                return;
            }
            AddSolver<SurfaceMagnetism>();

            var surfaceMagnetism = currentSolver as SurfaceMagnetism;
            surfaceMagnetism.SurfaceNormalOffset = -0.207f;
            surfaceMagnetism.ClosestDistance = 0.3f;
            surfaceMagnetism.MaxRaycastDistance = 4;
            surfaceMagnetism.CurrentOrientationMode = SurfaceMagnetism.OrientationMode.SurfaceNormal;
            handler.GoalRotation = new Quaternion(0, 0, 0, 1);
            Solver.SmoothTo(transform.rotation, handler.GoalRotation, Time.deltaTime, 1);

            // Magnetise all layers except these
            LayerMask[] mask = new LayerMask[6]
            {
                LayerMask.GetMask("Default"),
                LayerMask.GetMask("TransparentFX"),
                LayerMask.GetMask("Water"),
                LayerMask.GetMask("UI"),
                LayerMask.GetMask("PostProcessing"),
                LayerMask.GetMask("Spatial Awareness")
            };
            
            surfaceMagnetism.MagneticSurfaces = mask;
            enabled = true;
        }

        private void AddSolver<T>() where T : Solver
        {
            currentSolver = gameObject.AddComponent<T>();
            handler = GetComponent<SolverHandler>();
            RefreshSolverHandler();
        }

        private void RefreshSolverHandler()
        {
            if (handler != null)
            {
                handler.TrackedTargetType = trackedType;
                handler.TrackedHandness = Handedness.Both;
            }
        }

        public void DestroySolver()
        {
            if (currentSolver != null)
            {
                DestroyImmediate(currentSolver);
                currentSolver = null;
            }

            if (handler != null)
            {
                DestroyImmediate(handler);
                handler = null;
            }

            enabled = false;
        }
    }
}