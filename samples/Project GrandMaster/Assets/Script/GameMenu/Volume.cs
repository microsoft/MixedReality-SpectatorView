using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.USYD.GameMenu
{
    public class Volume : MonoBehaviour
    {
        public void setVolume(float x)
        {
            PlayerPrefs.SetFloat("Volume", x);
        }
        void Start()
        {
            PlayerPrefs.SetFloat("Volume", 1);
        }
        void Update()
        {
            AudioSource[] audioSrcs = FindObjectsOfType<AudioSource>();
            foreach (AudioSource audioSrc in audioSrcs)
            {
                audioSrc.volume = PlayerPrefs.GetFloat("Volume");
            }
        }
    }
}