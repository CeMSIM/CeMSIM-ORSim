using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Sonifier : MonoBehaviour
{
    public PulseDataNumberRenderer dataSource;
    public float currentValue = 0;
    private float lastValue = 0;
    private AudioPlayer audioPlayer;
    private int delayBuffer = 3;


    AudioMixerGroup mixer;

    // Start is called before the first frame update
    void Start()
    {
        audioPlayer = GetComponent<AudioPlayer>();
    }

    // Update is called once per frame
    void Update()
    {
        if(delayBuffer > 0)
        {
            delayBuffer--;
        }
        else
        {
            currentValue = dataSource.currentValue;
            if(audioPlayer.audioSource.pitch < 0.4f)
            {
                audioPlayer.audioSource.pitch = 0.4f;
            }
            if(Mathf.Abs(currentValue - lastValue) > .0001f)
            {
                Sonifiy();
            }
        }
        
        lastValue = currentValue;
        
    }

    public void AdjustPitch(float value)
    {
        audioPlayer.audioSource.pitch = value;
    }

    void Sonifiy()
    {
        float difference = 97 - currentValue;
        float normalizedValue = (currentValue / 97);
        if(currentValue < 97)
        {
           normalizedValue = normalizedValue - .2f;
        }
       AdjustPitch(normalizedValue);
    }
}
