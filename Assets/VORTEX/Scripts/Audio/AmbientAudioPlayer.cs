using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmbientAudioPlayer : AudioPlayer
{
    public override void Initialize()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.clip = audioClip;
        audioSource.loop = true;
        audioSource.spatialBlend = isAudio3D ? 1 : 0;
        audioSource.Play();
    }
    public override void  Pause()
    {
        audioSource.Pause();
    }

    public override void UnPause()
    {
        audioSource.Play();
    }
}
