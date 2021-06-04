using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("References")]
    public GameObject audioContainer;

    [Header("Debugging")]
    public bool paused = false;
    private bool playing = true;
    public bool startScenario = false;

    private AudioPlayer[] audioPlayers;
    // Start is called before the first frame update
    void Start()
    {
        audioPlayers = audioContainer.GetComponentsInChildren<AudioPlayer>();
    }

    public void Update()
    {
        #region Pause - Test Functionality
        //This section is only used for testing.
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (startScenario)
            {
                if (ScenarioManager.Instance.isRunning)
                    ScenarioManager.Instance.PauseScenario();
                else
                    ScenarioManager.Instance.UnPauseScenario();
            }
            else
            {
                if (!paused && playing)
                {
                    playing = false;
                    PauseAudio();
                }
                else if (!playing)
                {
                    playing = true;
                    UnPauseAudio();
                }

                paused = !paused;
            }
        }
        //paused = ScenarioManager.Instance.isRunning;
        #endregion  
    }

    public void PauseAudio()
    {
        foreach(AudioPlayer audioPlayer in audioPlayers)
        {
            audioPlayer.Pause();
        }
    }

    public void UnPauseAudio()
    {
        foreach (AudioPlayer audioPlayer in audioPlayers)
        {
            audioPlayer.UnPause();
        }
    }
}
