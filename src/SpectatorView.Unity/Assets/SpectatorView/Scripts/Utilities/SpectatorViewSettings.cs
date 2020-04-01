using UnityEditor;

namespace Microsoft.MixedReality.SpectatorView
{
    public class SpectatorViewSettings
    {
        public const string SettingsDirectory = "Generated.SpectatorView.Settings";

        /// <summary>
        /// Returns a string for the given asset name and extension in the project's Settings directory.
        /// </summary>
        /// <param name="assetName">Asset name</param>
        /// <param name="assetExtension">Asset extension</param>
        /// <returns>Asset path</returns>
        public static string GetSettingsPath(string assetName, string assetExtension)
        {
            EnsureSettingsDirectoryExists();
            return $"Assets/{SettingsDirectory}/Resources/{assetName}{assetExtension}";
        }

        private static void EnsureSettingsDirectoryExists()
        {
#if UNITY_EDITOR
            if (!AssetDatabase.IsValidFolder($"Assets/{SettingsDirectory}"))
            {
                AssetDatabase.CreateFolder("Assets", $"{SettingsDirectory}");
            }
            if (!AssetDatabase.IsValidFolder($"Assets/{SettingsDirectory}/Resources"))
            {
                AssetDatabase.CreateFolder($"Assets/{SettingsDirectory}", "Resources");
            }
#endif
        }
    }
}