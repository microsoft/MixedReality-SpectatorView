#if UNITY_EDITOR
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Microsoft.MixedReality.SpectatorView.Samples
{
    [InitializeOnLoad]
    public class SampleDependencyVerifier : MonoBehaviour
    {
        private const string FixSymlinksScript = "ResetSamples.ps1";
        static SampleDependencyVerifier()
        {
            List<FileInfo> brokenLinks = new List<FileInfo>();
            SearchForBrokenSymlink(new DirectoryInfo(Application.dataPath), brokenLinks);

            if (brokenLinks.Count > 0)
            {
#if UNITY_EDITOR_WIN 
                AttemptToFixOnWindows(brokenLinks);
#else
                EditorUtility.DisplayDialog("Broken Dependencies", $"Broken symbolic links detected, but automatic fix is not available for you development machine OS. Check GitHub for help, or file a new issue.\n\nBroken Symlinks:\n{string.Join("\n", brokenLinks)}", "Ok");
#endif
                Debug.LogError($"Broken Symlinks:\n{string.Join("\n", brokenLinks)}");
            }
        }

        private static void AttemptToFixOnWindows(List<FileInfo> brokenLinks)
        {
            FileInfo pathToResetSamples = new FileInfo(Path.GetFullPath(Path.Combine(Application.dataPath, @"..\..\..", @"tools\scripts", FixSymlinksScript)));
            if (!pathToResetSamples.Exists)
            {
                EditorUtility.DisplayDialog("Broken Dependencies", $"Broken symbolic links detected, but can't find the '{pathToResetSamples}' script to try and fix them. Check GitHub for help, or file a new issue.\n\nBroken Symlinks:\n{string.Join("\n", brokenLinks)}", "Ok");
            }
            else if (EditorUtility.DisplayDialog("Broken Dependencies", $"Broken symbolic links detected, would you like to run '{FixSymlinksScript}' to try and fix them?\n(Admin privileges are required to run the script: '{pathToResetSamples}'.)\n\nBroken Symlinks:\n{string.Join("\n", brokenLinks)}", "Yes", "No"))
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{pathToResetSamples}\"")
                {
                    Verb = "runas"
                };
                Process.Start(processStartInfo).WaitForExit();
                EditorUtility.DisplayDialog("Script Completed", "The reset script has completed, but you may need to restart Unity.", "Ok");
            }
        }

        private static void SearchForBrokenSymlink(DirectoryInfo startingDirectory, List<FileInfo> brokenLinks)
        {
            FileInfo[] files = startingDirectory.GetFiles("*", SearchOption.TopDirectoryOnly);
            foreach (FileInfo file in files)
            {
                if ((file.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                {
                    brokenLinks.Add(file);
                }
            }

            DirectoryInfo[] directories = startingDirectory.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo directory in directories)
            {
                if ((directory.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                {
                    SearchForBrokenSymlink(directory, brokenLinks);
                }
            }
        }
    }
}
#endif