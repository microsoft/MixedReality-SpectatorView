// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Microsoft.MixedReality.BuildTools.Unity
{
    /// <summary>
    /// Cross platform command line player build tools
    /// </summary>
    public static class UnityPlayerBuildTools
    {
        /// <summary>
        /// Starts the build process
        /// </summary>
        /// <param name="buildInfo"></param>
        /// <returns>The <see href="https://docs.unity3d.com/ScriptReference/Build.Reporting.BuildReport.html">BuildReport</see> from Unity's <see href="https://docs.unity3d.com/ScriptReference/BuildPipeline.html">BuildPipeline</see></returns>
        public static BuildReport BuildUnityPlayer(IBuildInfo buildInfo)
        {
            Debug.Log($"\n\nRunning Build: \n{buildInfo.ToString()}\n\n");

            // Restore these old defines post building
            string oldDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildInfo.BuildTargetGroup);
            Debug.Log($"Found pre-existing scripting define symbols: {oldDefines}");

            // Clear any existing preprocessor directives in the player settings
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildInfo.BuildTargetGroup, string.Empty);
            if (buildInfo.Defines != null &&
                buildInfo.Defines != string.Empty)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildInfo.BuildTargetGroup, buildInfo.Defines);
                Debug.Log($"Preprocessor directives were defined for the current player: {buildInfo.Defines}\n");
            }

            var oldColorSpace = PlayerSettings.colorSpace;
            if (buildInfo.ColorSpace.HasValue)
            {
                PlayerSettings.colorSpace = buildInfo.ColorSpace.Value;
            }

            if (buildInfo.ScriptingBackend.HasValue)
            {
                PlayerSettings.SetScriptingBackend(buildInfo.BuildTargetGroup, buildInfo.ScriptingBackend.Value);
#if !UNITY_2019_1_OR_NEWER
                // When building the .NET backend, also build the C# projects, as the
                // intent of this build process is to prove that it's possible build
                // a solution where the local dev loop can be accomplished in the
                // generated C# projects.
                if (buildInfo.ScriptingBackend == ScriptingImplementation.WinRTDotNET)
                {
                    EditorUserBuildSettings.wsaGenerateReferenceProjects = true;
                }
#endif
            }

            string unityBuildOutputDirectory = $"{buildInfo.OutputDirectory}/Unity";
            BuildReport buildReport = default;
            try
            {
                buildReport = BuildPipeline.BuildPlayer(
                    buildInfo.Scenes.ToArray(),
                    unityBuildOutputDirectory,
                    buildInfo.BuildTarget,
                    buildInfo.BuildOptions);
            }
            catch (Exception e)
            {
                Debug.LogError($"{e.Message}\n{e.StackTrace}");
            }

            PlayerSettings.colorSpace = oldColorSpace;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildInfo.BuildTargetGroup, oldDefines);

            return buildReport;
        }

        /// <summary>
        /// Force Unity To Write Project Files
        /// </summary>
        public static void SyncSolution()
        {
            var syncVs = Type.GetType("UnityEditor.SyncVS,UnityEditor");
            var syncSolution = syncVs.GetMethod("SyncSolution", BindingFlags.Public | BindingFlags.Static);
            syncSolution.Invoke(null, null);
        }

        /// <summary>
        /// Start a build using Unity's command line.
        /// </summary>
        public static async void StartCommandLineBuild()
        {
            // We don't need stack traces on all our logs. Makes things a lot easier to read.
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Debug.Log($"Invoking Unity Command Line Build");

            bool success = true;
            try
            {
                SyncSolution();
                string[] arguments = Environment.GetCommandLineArgs();
                var buildInfo = BuildInfo.ParseBuildCommandLine(arguments) as IBuildInfo;
                var buildResult = BuildUnityPlayer(buildInfo);
                success = buildResult.summary.result == BuildResult.Succeeded;
            }
            catch (Exception e)
            {
                Debug.LogError($"Build Failed!\n{e.Message}\n{e.StackTrace}");
                success = false;
            }

            Debug.Log($"Exiting command line build... Build success? {success}");
            EditorApplication.Exit(success ? 0 : 1);
        }

        public static void ConfirmEditorCompiles()
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Debug.Log("Able to access static function through the editor.");
            bool success = true;
            string oldDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            try
            {
                string[] arguments = Environment.GetCommandLineArgs();
                var buildInfo = BuildInfo.ParseBuildCommandLine(arguments) as IBuildInfo;
                // Clear any existing preprocessor directives in the player settings
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildInfo.BuildTargetGroup, string.Empty);
                if (buildInfo.Defines != null &&
                    buildInfo.Defines != string.Empty)
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(buildInfo.BuildTargetGroup, buildInfo.Defines);
                    Debug.Log($"Preprocessor directives were defined for the current player: {buildInfo.Defines}\n");
                }
                SyncSolution();
                success = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Editor Compilation Failed!\n{e.Message}\n{e.StackTrace}");
                success = false;
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, oldDefines);
            Debug.Log($"Exiting command line build... Build success? {success}");
            EditorApplication.Exit(success ? 0 : 1);
        }

        /// <summary>
        /// Get the Unity Project Root Path.
        /// </summary>
        /// <returns>The full path to the project's root.</returns>
        public static string GetProjectPath()
        {
            return Path.GetDirectoryName(Path.GetFullPath(Application.dataPath));
        }
    }
}