using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;

/// <summary>
/// GazeManager is used to manage speech recognition triggers based on
/// Head Gaze (HL1, HL2, VR) or Eye Gaze (HL2)
/// </summary>
public class GazeManager : MonoBehaviour, IMixedRealityFocusHandler
{
    // Speech Manager object used to trigger speech recognition and handle requests
    public SpeechRecognition SpeechManager;

    // Audio sources
    // audioSrcTTS: Used for TTS playback (TTS currently disabled)
    // audioSrcPing Used to play the audible Ping sound when speech recognition is triggered
    private AudioSource[] audioSources;
    private AudioSource audioSrcTTS;
    private AudioSource audioSrcPing;

    void Awake()
    {
        // Used to trigger animations based on user input (CURRENTLY DISABLED)
        //animator = GetComponent<Animator>();

        // Loop through Audio Sources on this gameobject to find the empty one
        // that will be used for TTS playback
        audioSources = this.GetComponents<AudioSource>();
        foreach (AudioSource a in audioSources)
        {
            if (a.clip == null)
            {
                audioSrcTTS = a; // Used for TTS playback
            }
            
            if ((a.clip != null) && (a.clip.name == "Ping"))
            {
                audioSrcPing = a; // Used to play a ping sound when speech recording starts
            }
        }
    }

    //void Update()
    //{

    //}

    /// <summary>
    /// Activate speech recognition only when the user looks straight at the bot
    /// </summary>
    public void OnFocusEnter(FocusEventData eventData)
    {
        // Don't activate speech recognition if the SpeechManager is not initialized
        // properly or if it's already actively listening for user speech (i.e. recognizing)
        if (SpeechManager.IsReady)
        {
            // Don't activate speech recognition if the speech synthesizer's audio source
            // is still in active playback mode
            if (!audioSrcTTS.isPlaying)
            {
                if (audioSrcPing != null)
                {
                    audioSrcPing.Play();
                }
                //animator.Play("Idle");
                StartListening();
            }
        }
    }

    public void OnFocusExit(FocusEventData eventData)
    {
        // Do nothing, let the user keep talking even if they look away.
        // The speech recognizer automatically detects end of utterances and stops listening.
    }

    /// <summary>
    /// Turns on the speech recognizer via the Speech Manager.
    /// The speech recognizer automatically detects end of utterances and stops listening.
    /// </summary>
    public void StartListening()
    {
        // Start Speech Dictation Recognizer
        SpeechManager.StartRecognition();
    }
}