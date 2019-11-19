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
        bool pause;
        GameObject[] mainOptions { get; set; }

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

        GameObject manager;
        BoardSolvers solvers;
        BoardInformation boardInfo;

        float startY = 0.05f;

        void Awake()
        {
            manager = GameObject.Find("GameManager");
            boardInfo = manager.GetComponent<BoardInformation>();
            solvers = game.GetComponent<BoardSolvers>();

            pause = false;

            currentlyOpenedMenu = homeMenu;
            originalSpot = homeMenu.transform.localPosition;
        }

        public void display()
        {
            visual.SetActive(true);
            homeMenu.SetActive(true);
            currentlyOpenedMenu = homeMenu;
        }

        public void close()
        {
            visual.SetActive(false);
            currentlyOpenedMenu.SetActive(false);

            // destroy surface magnetism solver 
            solvers.DestroySolver();
        }
        public void NewGame()
        {
            boardInfo.ResetState();
            pause = true;
            // remove menu from vision
            close();
        }

        public void Resume()
        {
            pause = true;
            // remove menu from vision
            close();
        }

        public void Settings()
        {
            Motion(settingMenu);
        }

        public void Volume()
        {
            Motion(volume);
        }

        public void Back()
        {
            if (currentlyOpenedMenu == volume) { Motion(settingMenu); }
            else if (currentlyOpenedMenu == surfaceMagnetism) { Motion(settingMenu); }
            else if (currentlyOpenedMenu == gameSettings) { Motion(settingMenu); }
            else if (currentlyOpenedMenu == settingMenu) { Motion(homeMenu); }
        }

        // called when going to the next menu page
        private void Motion(GameObject piece)
        {
            // Remove button colliders during animation
            RemoveColliders();

            piece.transform.localPosition += displacement;
            piece.SetActive(true);
            StartCoroutine(SetToPosition(piece, piece.transform.localPosition - (displacement * 1.5f)));
        }

        IEnumerator SetToPosition(GameObject piece, Vector3 moveFront)
        {
            float time = 0;
            float duration = 1f;
            currentlyOpenedMenu.SetActive(false);

            Vector3 startPosition = piece.transform.localPosition;
            // move front
            while (time <= duration)
            {
                time += Time.deltaTime;
                float blend = Mathf.Clamp01(time / duration);

                piece.transform.localPosition = Vector3.Lerp(startPosition, moveFront, blend);

                yield return null;
            }

            time = 0;
            duration = 0.5f;
            startPosition = piece.transform.localPosition;
            // Move back
            while (time <= duration)
            {
                time += Time.deltaTime;
                float blend = Mathf.Clamp01(time / duration);

                piece.transform.localPosition = Vector3.Lerp(startPosition, originalSpot, blend);

                yield return null;
            }

            currentlyOpenedMenu = piece;
            EnableColliders();
        }

        public void SurfaceMagnetismMenu()
        {
            Motion(surfaceMagnetism);
        }

        public void GameSettingsMenu()
        {
            Motion(gameSettings); 
        }

        // Disable box colliders during animation to avoid accidental press
        public void RemoveColliders()
        {
            foreach (GameObject button in buttons)
            {
                button.GetComponent<Collider>().enabled = false;
            }
        }
    
        // Enable box colliders after animation
        public void EnableColliders()
        {
            foreach (GameObject button in buttons)
            {
                button.GetComponent<Collider>().enabled = true;
            }
        }

        // Allow player to align the board with the surface
        public void SurfaceMagnetism()
        {
            solvers.SetSurfaceMagnetism();
        }

    }
}