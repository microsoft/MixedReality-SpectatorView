using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(AudioSource))]
public class AudioSystem : MonoBehaviour
{
    [SerializeField]
    private AudioClip collisionSound;
    private AudioSource source;
    private bool isDropped;

    private void Start()
    {
        isDropped = false;
        source = this.GetComponent<AudioSource>();
    }
    private void OnTriggerEnter(Collider collision)
    {
        if (Time.frameCount < 100) return;
        source.clip = collisionSound;
        if (!source.isPlaying && isDropped){
            source.volume = 1;
            source.Play();
            isDropped = false;
        }
    }

    public void BeingDropped() {
        isDropped = true;
    }
}
