// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class SceneSwitcher : MonoBehaviour
    {
        [Tooltip("List of scenes to toggle between.")]
        [SerializeField]
        private string[] scenes = new string[]
        {
            "SpectatorView.TestScene1",
            "SpectatorView.TestScene2"
        };

        [Tooltip("Amount of time to spend in each scene")]
        [SerializeField]
        private float sceneDurationInSeconds = 5.0f;

        private float timeUntilNextSceneTransition = 0.0f;
        private int currentSceneIndex = 0;
        private string lastLoadedScene = string.Empty;

        void Awake()
        {
            DontDestroyOnLoad(this);
        }

        void Start()
        {
            timeUntilNextSceneTransition = sceneDurationInSeconds;
            string currentSceneName = SceneManager.GetActiveScene().name;
            bool sceneFound = false;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i] == currentSceneName)
                {
                    currentSceneIndex = i;
                    sceneFound = true;
                    return;
                }
            }

            if (sceneFound)
            {
                Debug.LogWarning("Current scene was found in scene list. This may result in multiple instances of Spectator View getting created");
            }
        }

        void Update()
        {
            timeUntilNextSceneTransition -= Time.deltaTime;
            if (timeUntilNextSceneTransition < 0)
            {
                string nextSceneName = scenes[currentSceneIndex];
                Debug.Log($"Loading new scene: {nextSceneName}");
                SceneManager.LoadScene(nextSceneName, LoadSceneMode.Additive);

                if (lastLoadedScene != string.Empty)
                {
                    Debug.Log($"Unloading scene: {lastLoadedScene}");
                    SceneManager.UnloadSceneAsync(lastLoadedScene);
                }

                currentSceneIndex++;
                currentSceneIndex = currentSceneIndex >= scenes.Length ? 0 : currentSceneIndex;
                timeUntilNextSceneTransition = sceneDurationInSeconds;
                lastLoadedScene = nextSceneName;
            }
        }
    }
}

