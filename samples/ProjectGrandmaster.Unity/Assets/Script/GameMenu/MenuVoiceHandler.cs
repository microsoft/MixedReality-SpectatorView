using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    public class MenuVoiceHandler : MonoBehaviour
    {
        public void ToggleMenuOn()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        public void ToggleMenuOff()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }
    }
}