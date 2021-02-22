﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VitalsAudioPlayer : AudioPlayer
{
    [Tooltip("Object containing <PulseDataNumberRenderer> to pull desired vitals data from.")]
    public GameObject vital;

    private float audioPlayRate = 0;    //determined based on 60 seconds / (currentValue of vital)
    private float currentValue = 0;
    private float lastValue = 0;
    
    private float timer = 0;
    private bool paused = false;


    void Update()
    {
        if (!paused)
        {
            audioSource.volume = volume;
            Play();
        }
    }

    void SetPlayRate()
    {
        currentValue = vital.GetComponent<PulseDataNumberRenderer>().currentValue;
        if(currentValue == lastValue)
        {
            return;
        }
        else
        {
            audioPlayRate = 60 / currentValue;
        }
    }

    void Play()
    {
        SetPlayRate();

        if (timer < audioPlayRate)
        {
            timer += Time.deltaTime;
        }
        else
        {
            audioSource.PlayOneShot(audioClip);
            timer = 0;
        }

        lastValue = currentValue;
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
