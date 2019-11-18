// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using Microsoft.MixedReality.USYD.ChessPiece;

namespace Microsoft.MixedReality.USYD.AI
{
///<summary>
///Write what this does 
///</>summary>
    public class Setup : MonoBehaviour
    {
        private string position = "position startpos moves";
        PieceAction pAction;
        GameObject[,] board;
        
        /// <summary>
        /// AI's total thinking time in milliseconds
        /// </summary>
        private int moveTime = 30000; 

        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = Path.Combine(Application.streamingAssetsPath, "stockfish-10-win/Windows/stockfish_10_x64.exe"),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true
        };
        Process process;

        public void Start()
        {
            pAction = GetComponent<PieceAction>();

            ///<summary>
            /// Set up the game engine communication
            ///</summary>
            process = new Process();
            process.StartInfo = startInfo;
            process.OutputDataReceived += new DataReceivedEventHandler(ReceiveMessage);
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            SendMessageUCI("uci"); // Engine uses UCI
            SendMessageUCI("ucinewgame");
            ///<summary> Sets the number of principal variations to 3 </summary>
            SendMessageUCI("setoption name MultiPV value 3");
        }

        public void Moved(GameObject[,] board, string move = "c2c3")
        {
            this.board = board;
            position += " " + move;
            SendMessageUCI(position);
            SendMessageUCI("go movetime " + moveTime);
        }

        private void SendMessageUCI(string message)
        {
            process.StandardInput.WriteLine(message);
            process.StandardInput.Flush();
        }

        ///<summary>
        /// Gets the best move from the engine with 30 seconds of search (based on the moveTime variable)
        /// Once found, send move to BoardInformation
        ///</summary>
        private void ReceiveMessage(object sender, DataReceivedEventArgs e)
        {
            string message = e.Data;

            if (message.Contains("bestmove"))
            {
                string bestMove = message.Substring(9, 4);

                int z = (int)char.GetNumericValue(bestMove[1]) - 1; // index starts with 0
                int x = (int)bestMove[0] - 97; // a = 97                

                // new x and z positions
                int newZ = (int)char.GetNumericValue(bestMove[3]) - 1;
                int newX = (int)bestMove[2] - 97;

                UnityEngine.Debug.Log("x and z are " + newX + " " + newZ);
                MoveAI(x, z, newX, newZ);
            }
        }

        private void MoveAI(int x, int z, int newX, int newZ)
        {
            // Get game object in this position
            GameObject piece = board[z, x];
            UnityEngine.Debug.Log(piece.name);

            PieceInformation pInformation = piece.GetComponent<PieceInformation>();
            Vector3 newPosition = new Vector3(newX, 0, newZ);

            pAction.ChangePosition(piece, newPosition, 1);
        }
        /*
                public void NewGame()
                {
                    SendMessageUCI("ucinewgame");
                    File.Delete("../SaveGame/currentGame.txt");
                    currentGame = File.CreateText("../SaveGame/currentGame.txt");
                }*/
    }
}