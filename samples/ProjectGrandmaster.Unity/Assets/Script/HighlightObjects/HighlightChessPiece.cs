using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This script is attached to the chess pieces and is used to change their colour
//Used by Manipulation Handler script

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    public class HighlightChessPiece : MonoBehaviour
    {
        private MeshRenderer mr;
        private Color startColor;
        private Color TouchHighlightColor; //purple
        private Color GrabHighlightColor; //green

        // Start is called before the first frame update
        void Awake()
        {
            mr = GetComponent<MeshRenderer>();
            if (!mr)
            {
                Debug.LogError("Mesh Renderer not found");
            }
            else
            {
                ChangeStartColour();
                //TouchHighlightColor = new Color(240f / 255f, 145f / 255f, 255f / 255f, 255f / 255f);
                TouchHighlightColor = new Color(195f / 255f, 156f / 255f, 200f / 255f, 50f / 255f);
                GrabHighlightColor = new Color(155f / 255f, 90f / 255f, 170f / 255f, 130f / 255f);
                //new Color(63/175f, 190/255f, 180/255f, 0.4f); //blue highlight
            }
           
        }

        public void TouchHighlightOn()
        {
            mr.material.color = TouchHighlightColor;
        }

        public void GrabHighlightOn()
        {
            mr.material.color = GrabHighlightColor;
        }

        public void HighlightOff()
        {
            mr.material.color = startColor;
        }

        public void HighlightColour(Color color)
        {
            mr.material.color = color;
        }

        public void ChangeStartColour()
        {
            startColor = mr.material.color;
        }
    }
}