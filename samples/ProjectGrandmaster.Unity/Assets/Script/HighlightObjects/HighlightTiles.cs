using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.SpectatorView.ProjectGrandmaster;

//This script is attached to pieces to change the colour of tiles they can move to.
//The methods should be called when the players hand moves ontop of the piece.

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    public class HighlightTiles : MonoBehaviour
    {
        private PieceInformation pi;
        private List<string> possibleMoves;
        private GameObject chessboard;
        private List<GameObject> activeTiles = new List<GameObject>();
        public BoardInformation bi;


        // Start is called before the first frame update
        void Start()
        {
            pi = GetComponent<PieceInformation>();
            if (!pi) { Debug.LogError("PieceInformation script not found"); }

            chessboard = GameObject.Find("Chessboard");
            if (!chessboard) { Debug.LogError("chessboard gameobject not found"); }
        }

        //Highlights tiles that this piece can move to
        //Used by Interactable script events
        public void TilesOn()
        {
            //Look for possible moves
            pi.GetMoves();
            possibleMoves = pi.GetPossibleMoves();

            if (possibleMoves == null || bi.GetTurn() != (int)pi.colour)
            {
                return;
            }

            //Swap the corresponding tiles with highlighted version
            foreach (string item in possibleMoves)
            {
                int x = (int)char.GetNumericValue(item[0]);
                int z = (int)char.GetNumericValue(item[2]);
                GameObject tile = chessboard.transform.GetChild(z).gameObject.transform.GetChild(x).gameObject;
                activeTiles.Add(tile);

                tile.GetComponent<ActivateHighlight>().HighlightOn();
            }
        }

        //Goes through the active tiles and turns highlight off
        //Used by Interactable script events
        public void TilesOff()
        {
            if (possibleMoves== null)
                return;

            foreach (GameObject tile in activeTiles)
            {
                tile.GetComponent<ActivateHighlight>().HighlightOff();
            }

            possibleMoves.Clear();
        }

        //changes the int for board position to the letter position used in editor
        //delete if not referenced once finished
        public char NumToLetter(int num)
        {
            return ((char)(num + 'A'));
        }
    }
}