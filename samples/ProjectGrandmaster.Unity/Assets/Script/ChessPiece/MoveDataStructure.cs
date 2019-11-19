// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Contributed by USYD Team - Hrithvik (Jacob) Sood, John Tran, Tom Derrick, Aydin Ucan, Aayush Jindal

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    public class MoveDataStructure : MonoBehaviour
    {
        static List<bool> eliminated;
        static List<GameObject> eliminatedObjects;
        static List<GameObject> pieceMoved;
        static List<int> previousXPosition;
        static List<int> previousZPosition;
        static List<int> currentXPosition;
        static List<int> currentZPosition;
        static List<List<string>> checkPath;
        static List<bool> check;
        static List<bool> pawnPromoted;

        static int count;
        
        void Awake()
        {
            eliminated = new List<bool>();
            eliminatedObjects = new List<GameObject>();
            pieceMoved = new List<GameObject>();
            previousXPosition = new List<int>();
            previousZPosition = new List<int>();
            currentXPosition = new List<int>();
            currentZPosition = new List<int>();
            checkPath = new List<List<string>>();
            check = new List<bool>();
            pawnPromoted = new List<bool>();
            count = 0;
        }

        public static int GetCount()
        {
            return count;
        }

        public static void Move(bool pieceEliminated, GameObject eliminatedObject, GameObject piece, string originalPos, string newPos)
        {
            eliminated.Add(pieceEliminated);
            if (pieceEliminated)
            {
                eliminatedObjects.Add(eliminatedObject);
            }
            else
            {
                eliminatedObjects.Add(null);
            }
            pieceMoved.Add(piece);
            previousXPosition.Add((int)char.GetNumericValue(originalPos[0]));
            previousZPosition.Add((int)char.GetNumericValue(originalPos[2]));
            currentXPosition.Add((int)char.GetNumericValue(newPos[0]));
            currentZPosition.Add((int)char.GetNumericValue(newPos[2]));

            // by default, pawnPromoted = false
            pawnPromoted.Add(false);
            // by default, check = false
            check.Add(false);
            count++;
        }

        public static void Clear()
        {
            eliminated.Clear();
            eliminatedObjects.Clear();
            pieceMoved.Clear();
            previousXPosition.Clear();
            previousZPosition.Clear();
            currentXPosition.Clear();
            currentZPosition.Clear();
            checkPath.Clear();
            check.Clear();
            pawnPromoted.Clear();
            count = 0;
        }

        public static void AddCheckPath(List<string> path)
        {
            checkPath.Add(path);
        }

        public static List<string> GetCheckPath()
        {
            int index = checkPath.Count - 1;
            return checkPath[index];
        }

        public static void RemoveLastPath()
        {
            // Remove from list
            int index = checkPath.Count - 1;
            checkPath.RemoveAt(index);
        }

        public static ArrayList Undo()
        {
            int index = count - 1;
            ArrayList undo = new ArrayList();
            undo.Add(eliminated[index]);
            undo.Add(eliminatedObjects[index]);
            undo.Add(pieceMoved[index]);
            undo.Add(previousXPosition[index]);
            undo.Add(previousZPosition[index]);
            undo.Add(currentXPosition[index]);
            undo.Add(currentZPosition[index]);
            undo.Add(check[index]);
            undo.Add(pawnPromoted[index]);

            eliminated.RemoveAt(index);
            eliminatedObjects.RemoveAt(index);
            pieceMoved.RemoveAt(index);
            previousXPosition.RemoveAt(index);
            previousZPosition.RemoveAt(index);
            currentXPosition.RemoveAt(index);
            currentZPosition.RemoveAt(index);
            check.RemoveAt(index);
            pawnPromoted.RemoveAt(index);
            count--;

            return undo;
        }

        public static void Check()
        {
            check[count - 1] = true;
        }

        public static void Promoted()
        {
            pawnPromoted[count - 1] = true;
        }

        public static bool GetLastCheck()
        {
            if (count - 1 < 0)
            {
                return false;
            }

            return check[count - 1];
        }

        public static bool OpponentKilled()
        {
            return eliminated[count - 1];
        }

        public static int GetPreviousX()
        {
            return previousXPosition[count - 1];
        }

        public static int GetPreviousZ()
        {
            return previousZPosition[count - 1];
        }

        public static int GetCurrentX()
        {
            return currentXPosition[count - 1];
        }

        public static int GetCurrentZ()
        {
            return currentZPosition[count - 1];
        }

        public static GameObject GetPieceEliminated()
        {
            return eliminatedObjects[count - 1];
        }

        public static GameObject GetPieceMoved()
        {
            return pieceMoved[count - 1];
        }

        public static List<GameObject> GetAllEliminated()
        {
            return eliminatedObjects;
        }
    }
}