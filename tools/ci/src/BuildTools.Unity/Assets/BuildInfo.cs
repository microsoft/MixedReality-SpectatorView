// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Microsoft.MixedReality.BuildTools.Unity
{
    public class BuildInfo : IBuildInfo
    {
        /// <inheritdoc />
        public BuildTarget BuildTarget { get; }

        /// <inheritdoc />
        public BuildTargetGroup BuildTargetGroup { get; }

        /// <inheritdoc />
        public BuildOptions BuildOptions { get; }
             
        /// <inheritdoc />
        public int Architecture { get; }

        private string outputDirectory;

        /// <inheritdoc />
        public string OutputDirectory
        {
            get => outputDirectory;
            set => outputDirectory = value;
        }

        /// <inheritdoc />
        public IEnumerable<string> Scenes { get; set; }

        /// <inheritdoc />
        public ColorSpace? ColorSpace { get; set; }

        /// <inheritdoc />
        public ScriptingImplementation? ScriptingBackend { get; set; }

        /// <inheritdoc />
        public string Defines { get; set; }

        /// <inheritdoc />
        public string LogDirectory { get; set; }

        private BuildInfo()
        {
            BuildTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            BuildOptions = BuildOptions.None;
            Architecture = PlayerSettings.GetArchitecture(BuildTargetGroup);
            Scenes = new List<string>();
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Build Info:\n");
            builder.Append($"BuildTarget:{BuildTarget.ToString()}\n");
            builder.Append($"BuildTargetGroup:{BuildTargetGroup.ToString()}\n");
            builder.Append($"Architecture:{ArchitectureToString(Architecture)}\n");
            builder.Append($"Scenes:{Scenes.ToString()}\n");
            builder.Append($"Defines:{Defines}\n");
            builder.Append($"ScriptingBackend:{ScriptingBackend.ToString()}\n");
            builder.Append($"ColorSpace:{ColorSpace.ToString()}\n");
            builder.Append($"OutputDirectory:{OutputDirectory.ToString()}\n");
            builder.Append($"LogDirectory:{LogDirectory.ToString()}\n");
            return builder.ToString();
        }

        private static string ArchitectureToString(int architecture)
        {
            switch (architecture)
            {
                case 0: return "None";
                case 1: return "ARM64";
                case 2: return "Universal";
                default: return "Unknown";
            }
        }

        public static BuildInfo ParseBuildCommandLine(string[] arguments)
        {
            // The BuildTarget property will be set by flags when invoking Unity.exe from the command line.
            // The BuildTarget and BuildTargetGroup are therefore setup in the IBuildInfo constructor.
            BuildInfo buildInfo = new BuildInfo();

            for (int i = 0; i < arguments.Length; ++i)
            {
                switch (arguments[i])
                {
                    case "-sceneList":
                        buildInfo.Scenes = buildInfo.Scenes.Union(SplitList(arguments[++i]));
                        break;
                    case "-sceneListFile":
                        string path = arguments[++i];
                        if (File.Exists(path))
                        {
                            buildInfo.Scenes = buildInfo.Scenes.Union(SplitList(File.ReadAllText(path)));
                        }
                        else
                        {
                            Debug.LogWarning($"Scene list file at '{path}' does not exist.");
                        }
                        break;
                    case "-define":
                        buildInfo.Defines = arguments[++i];
                        break;
                    case "-buildOutput":
                        buildInfo.OutputDirectory = arguments[++i];
                        break;
                    case "-colorSpace":
                        buildInfo.ColorSpace = (ColorSpace)Enum.Parse(typeof(ColorSpace), arguments[++i]);
                        break;
                    case "-scriptingBackend":
                        buildInfo.ScriptingBackend = (ScriptingImplementation)Enum.Parse(typeof(ScriptingImplementation), arguments[++i]);
                        break;
                    case "-logDirectory":
                        buildInfo.LogDirectory = arguments[++i];
                        break;
                }
            }

            return buildInfo;
        }

        private static IEnumerable<string> SplitList(string list)
        {
            return from item in list.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                   select item.Trim();
        }
    }
}