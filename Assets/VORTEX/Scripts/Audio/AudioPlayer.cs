using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    [HideInInspector]
    public AudioSource audioSource;

    [Tooltip("Audio clip to be played for this vital.")]
    public AudioClip audioClip;

    [Range(0, 1)]
    public float volume;

    public bool isAudio3D = false;

    private void Awake() { Initialize(); }

    public virtual void Initialize() {}

    public virtual void Pause() {}

    public virtual void UnPause() {}
}
