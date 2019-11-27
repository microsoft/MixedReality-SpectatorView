// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Contributed by USYD Team - Hrithvik (Jacob) Sood, John Tran, Tom Derrick, Aydin Ucan, Aayush Jindal

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.SpectatorView.ProjectGrandmaster;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    public class Menu : MonoBehaviour
    {
        private bool pause;
        private GameObject[] mainOptions { get; set; }

        public GameObject visual;
        public GameObject homeMenu;
        public GameObject surfaceMagnetism;
        public GameObject gameSettings;
        public GameObject settingMenu;
        public GameObject volume;
        public GameObject game;

        public List<GameObject> buttons;

        private Vector3 originalSpot;
        private Vector3 displacement = new Vector3(0, 0, 0.02f);

        private GameObject currentlyOpenedMenu;

        private GameObject manager;
        private BoardSolvers solvers;
        private BoardInformation boardInfo;

        private float startY = 0.05f;

        void Awake()
        {
            manager = GameObject.Find("GameManager");
            boardInfo = manager.GetComponent<BoardInformation>();
            solvers = game.GetComponent<BoardSolvers>();

            pause = false;

            currentlyOpenedMenu = homeMenu;
            originalSpot = homeMenu.transform.localPosition;
        }

        /// <summary>
        /// Displays the menu.
        /// </summary>
        public void display()
        {
            visual.SetActive(true);
            homeMenu.SetActive(true);
            currentlyOpenedMenu = homeMenu;
        }

        /// <summary>
        /// Closes the menu.
        /// </summary>
        public void close()
        {
            visual.SetActive(false);
            currentlyOpenedMenu.SetActive(false);

            // destroy surface magnetism solver 
            solvers.DestroySolver();
        }

        /// <summary>
        /// Starts new game.
        /// </summary>
        public void NewGame()
        {
            boardInfo.ResetState();
            pause = true;
            // remove menu from vision
            close();
        }

        /// <summary>
        /// Resumes the game.
        /// </summary>
        public void Resume()
        {
            pause = true;
            // remove menu from vision
            close();
        }

        /// <summary>
        /// Goes to the settings menu page.
        /// </summary>
        public void Settings()
        {
            if (currentlyOpenedMenu == homeMenu)
            {
                Motion(settingMenu);
            }
        }

        /// <summary>
        /// Goes to the volume menu page
        /// </summary>
        public void Volume()
        {
            Motion(volume);
        }

        /// <summary>
        /// Goes to the previous menu page
        /// </summary>
        public void Back()
        {
            if (currentlyOpenedMenu == volume)
            {
                Motion(settingMenu);
            }
            else if (currentlyOpenedMenu == surfaceMagnetism)
            {
                Motion(settingMenu);
            }
            else if (currentlyOpenedMenu == gameSettings)
            {
                Motion(settingMenu);
            }
            else if (currentlyOpenedMenu == settingMenu)
            {
                Motion(homeMenu);
            }
        }

        /// <summary>
        /// Called when going to the next menu page
        /// </summary>
        /// <param name="page"> The new menu page the player is being navigated to. </param>
        private void Motion(GameObject page)
        {
            // Remove button colliders during animation
            RemoveColliders();

            page.transform.localPosition += displacement;
            page.SetActive(true);
            StartCoroutine(SetToPosition(page, page.transform.localPosition - (displacement * 1.5f)));
        }

        IEnumerator SetToPosition(GameObject page, Vector3 moveFront)
        {
            float time = 0;
            float duration = 1f;
            currentlyOpenedMenu.SetActive(false);

            Vector3 startPosition = page.transform.localPosition;
            // move front
            while (time <= duration)
            {
                time += Time.deltaTime;
                float blend = Mathf.Clamp01(time / duration);

                page.transform.localPosition = Vector3.Lerp(startPosition, moveFront, blend);

                yield return null;
            }

            time = 0;
            duration = 0.5f;
            startPosition = page.transform.localPosition;
            // Move back
            while (time <= duration)
            {
                time += Time.deltaTime;
                float blend = Mathf.Clamp01(time / duration);

                page.transform.localPosition = Vector3.Lerp(startPosition, originalSpot, blend);

                yield return null;
            }

            currentlyOpenedMenu = page;
            EnableColliders();
        }

        public void SurfaceMagnetismMenu()
        {
            if (currentlyOpenedMenu == homeMenu)
            {
                Motion(surfaceMagnetism);
            }
        }

        public void GameSettingsMenu()
        {
            if (currentlyOpenedMenu == settingMenu)
            {
                Motion(gameSettings);
            }
        }

        /// <summary>
        /// Disable box colliders during animation to avoid accidental press
        /// </summary>
        public void RemoveColliders()
        {
            foreach (GameObject button in buttons)
            {
                button.GetComponent<Collider>().enabled = false;
            }
        }

        /// <summary>
        /// Enable box colliders after animation
        /// </summary>
        public void EnableColliders()
        {
            foreach (GameObject button in buttons)
            {
                button.GetComponent<Collider>().enabled = true;
            }
        }

        /// <summary>
        /// Allow player to align the board with the surface
        /// </summary>
        public void SurfaceMagnetism()
        {
            if (currentlyOpenedMenu == surfaceMagnetism)
            {
                solvers.SetSurfaceMagnetism();
            }
        }

    }
}