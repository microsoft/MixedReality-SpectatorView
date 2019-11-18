using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This script is attached to the tile objects and is used to change their colour
namespace Microsoft.MixedReality.USYD.HighlightObjects
{
    public class ActivateHighlight : MonoBehaviour
    {
        private MeshRenderer mr;
        private Color startColor;
        private Color highlightColor;

        // Start is called before the first frame update
        void Start()
        {
            mr = GetComponent<MeshRenderer>();
            if (!mr)
            {
                Debug.LogError("Mesh Renderer not found");
            }
            else
            {
                ChangeStartColour();
                highlightColor = new Color(50f / 255f, 180f / 255f, 80f / 255f, 130f / 255f);
            }
            
        }

        // Update is called once per frame
        public void HighlightOn()
        {
            mr.material.color = highlightColor;
        }

        public void HighlightOff()
        {
            mr.material.color = startColor;
        }

        public void ChangeStartColour()
        {
            startColor = mr.material.color;
        }
    }
}