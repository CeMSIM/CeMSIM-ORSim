using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlarmPlayer : AudioPlayer
{
    public PulseDataNumberRenderer valueToMonitor;
    public float valueToTriggerAlarmAt = 0.0f;
    public bool higher;
    public float delayBetweenDings = 0.5f;
    private float timeSinceDing = 0.0f;
    private int delayBuffer = 3;
    bool paused = false;

    // Update is called once per frame
    void Update()
    {
        HandleAlarm();
        
        if(Input.GetKeyUp(KeyCode.Space) && !paused)
        {
            Mute();
        }
    }

    public override void Initialize()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = isAudio3D ? 1 : 0;
    }

    private void HandleAlarm()
    {
        if(delayBuffer > 0)
        {
            delayBuffer--;
        }
        else if(timeSinceDing > delayBetweenDings && !paused)
        {
            bool above = valueToMonitor.currentValue > valueToTriggerAlarmAt ? true : false;

            if((higher && above) || (!higher && !above))
            {
                if(!audioSource.isPlaying)
                {
                    audioSource.PlayOneShot(audioClip);
                    timeSinceDing = 0; 
                }
            }
        }
        timeSinceDing+=Time.deltaTime;
    }
    public override void Pause()
    {
        audioSource.Pause();
        paused = true;
    }

    public override void UnPause()
    {
        audioSource.Pause();
        paused = false;
    }

    public void Mute()
    {
        Pause();
        StartCoroutine(UnMuteAfterDelay());
    }

    IEnumerator UnMuteAfterDelay()
    {
        yield return new WaitForSeconds(60);
        UnPause();
    }
}
