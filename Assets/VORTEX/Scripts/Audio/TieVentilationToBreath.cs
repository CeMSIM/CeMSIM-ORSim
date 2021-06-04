using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TieVentilationToBreath : AudioPlayer
{
    public bool paused = false;
    public AudioClip  inflate, deflate;

    public void Play(AudioClip clip)
    {
        if(clip.Equals(inflate))
        {
                audioSource.clip = inflate;
            if(!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else if(clip.Equals(deflate))
        {
            audioSource.clip = deflate;
            if(!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
    }

    public override void Initialize()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = isAudio3D ? 1 : 0;
    }

    public override void Pause()
    {
        audioSource.Pause();
        paused = true;
    }

    public override void UnPause()
    {
        audioSource.UnPause();
        paused = false;
    }
}
