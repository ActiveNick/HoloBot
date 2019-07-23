using Microsoft.MixedReality;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using Microsoft.MixedReality.Toolkit.Input;
using HoloBot;
using System;

/// <summary>
/// MicrophoneManager lets us capture audio from the user and feed into speech recognition
/// Make sure to enable the Microphone capability in the Windows 10 UWP Player Settings
/// </summary>
public class MicrophoneManager : MonoBehaviour, IMixedRealityFocusHandler
{
    //[Tooltip("A text area for the recognizer to display the recognized strings.")]
    //public Text DictationDisplay;

    //private DictationRecognizer dictationRecognizer;
    private StringBuilder textSoFar;

    public SpeechRecognition SpeechManager;

    // Use this string to cache the text currently displayed in the text box.
    //public Text captions;
    //public Animator animator;
    //public TextToSpeech MyTTS;        // Windows TTS was removed in MRTKv2, will be replaced with Cognitive Services Speech
    public CaptionsManager captionsManager;

    // Using an empty string specifies the default microphone. 
    private static string deviceName = string.Empty;
    //private int samplingRate;

    // Max recording duration (currently unused)
    private const int messageLength = 10;

    // Client object to communicate with the Bot Service
    private BotService tmsBot = new BotService();

    // Audio sources, including the one used for TTS playback
    // SelectedSource is used to play the audible Ping sound when speech recording starts
    private AudioSource[] audioSources;
    private AudioSource ttsAudioSrc;
    public AudioSource selectedSource;

    // Awake was made async so we can await the StartConversation Task
    // DO NOT call Task.Wait() from the main thread or things will lock-up once you await 
    // a call inside the task.
    // Use regular Awake in non-UWP or else the Unity editor compiler will complain
    async void Awake()
    {
        // Initialize the Bot Framework client before we can send requests in
        //await tmsBot.StartConversation();

        //animator = GetComponent<Animator>();

        // Create a new DictationRecognizer and assign it to dictationRecognizer variable.
        //dictationRecognizer = new DictationRecognizer();

        // Register for dictationRecognizer.DictationHypothesis and implement DictationHypothesis below
        // This event is fired while the user is talking. As the recognizer listens, it provides text of what it's heard so far.
        //dictationRecognizer.DictationHypothesis += DictationRecognizer_DictationHypothesis;

        // Register for dictationRecognizer.DictationResult and implement DictationResult below
        // This event is fired after the user pauses, typically at the end of a sentence. The full recognized string is returned here.
        //dictationRecognizer.DictationResult += DictationRecognizer_DictationResult;

        // Register for dictationRecognizer.DictationComplete and implement DictationComplete below
        // This event is fired when the recognizer stops, whether from Stop() being called, a timeout occurring, or some other error.
        //dictationRecognizer.DictationComplete += DictationRecognizer_DictationComplete;

        // Register for dictationRecognizer.DictationError and implement DictationError below
        // This event is fired when an error occurs.
        //dictationRecognizer.DictationError += DictationRecognizer_DictationError;

        // Loop through Audio Sources on this gameobject to find the empty one
        // that will be used for TTS playback
        audioSources = this.GetComponents<AudioSource>();
        foreach (AudioSource a in audioSources)
        {
            if (a.clip == null)
            {
                ttsAudioSrc = a; // Used for TTS playback
            }
            
            if ((a.clip != null) && (a.clip.name == "Ping"))
            {
                selectedSource = a; // Used to play a ping sound when speech recording starts
            }
        }
        // Query the maximum frequency of the default microphone. Use 'unused' to ignore the minimum frequency.
        //int unused;
        //Microphone.GetDeviceCaps(deviceName, out unused, out samplingRate);

        // Use this string to cache the text currently displayed in the text box.
        textSoFar = new StringBuilder();

        captionsManager.SetCaptionsText("");
    }

    void Update()
    {

    }

    /// <summary>
    /// Activate speech recognition only when the user looks straight at the bot
    /// </summary>
    public void OnFocusEnter(FocusEventData eventData)
    {
        // Don't activate speech recognition if the recognizer is already running
        if (SpeechManager.IsReady)
        {
            // Don't activate speech recognition if the speech synthesizer's audio source
            // is still in active playback mode
            if (!ttsAudioSrc.isPlaying)
            {
                //captionsManager.ToggleKeywordRecognizer(false);
                if (selectedSource != null)
                {
                    selectedSource.Play();
                }
                //animator.Play("Idle");
                //StartCoroutine(CoStartRecording());
                StartRecording();
            }
        }
    }

    public void OnFocusExit(FocusEventData eventData)
    {
        // Do nothing, let the user keep talking even if they look away
    }

    /// <summary>
    /// Turns on the dictation recognizer and begins recording audio from the default microphone.
    /// </summary>
    /// <returns>The audio clip recorded from the microphone.</returns>
    public void StartRecording()
    {
        // Start dictationRecognizer
        //dictationRecognizer.Start();
        SpeechManager.StartRecognition(false);

        //Debug.Log("Dictation Recognizer is now " + ((dictationRecognizer.Status == SpeechSystemStatus.Running) ? "on" : "off"));
    }

    /// <summary>
    /// Ends the recording session.
    /// </summary>
    public void StopRecording()
    {
        //// Check if dictationRecognizer.Status is Running and stop it if so
        //if (dictationRecognizer.Status == SpeechSystemStatus.Running)
        //{
        //    dictationRecognizer.Stop();
        //}

        //Debug.Log("Dictation Recognizer is now " + ((dictationRecognizer.Status == SpeechSystemStatus.Running) ? "on" : "off"));
    }

    /// <summary>
    /// This event is fired while the user is talking. As the recognizer listens, it provides text of what it's heard so far.
    /// </summary>
    /// <param name="text">The currently hypothesized recognition.</param>
    private void DictationRecognizer_DictationHypothesis(string text)
    {
        // Set DictationDisplay text to be textSoFar and new hypothesized text
        // Currently unused
    }

    /// <summary>
    /// This event is fired after the user pauses, typically at the end of a sentence. The full recognized string is returned here.
    /// </summary>
    /// <param name="text">The text that was heard by the recognizer.</param>
    /// <param name="confidence">A representation of how confident (rejected, low, medium, high) the recognizer is of this recognition.</param>
    private async void DictationRecognizer_DictationResult(string text, ConfidenceLevel confidence)
    {
        StopRecording();

        // Append textSoFar with latest text
        textSoFar.Append(text);

        // Set DictationDisplay text to be textSoFar as return by hypothesis
        //DictationDisplay.text = textSoFar.ToString();

        //UnityEngine.WSA.Application.InvokeOnAppThread(() =>
        //{
            // Display captions for the question
            captionsManager.SetCaptionsText(text);
        //}, false); 

        string msg = text;
        string result = "I'm sorry, I'm not sure how to answer that";

        if (await tmsBot.SendMessage(msg))
        {
            ConversationActitvities messages = await tmsBot.GetMessages();
            if(messages.activities.Length > 0) 
            {
                result = "";
            }

            // Note that attachments (like cards) are still not supported
            for (int i = 1; i < messages.activities.Length; i++)
            {
                // We focus on the speak tag if the bot was speech-enabled.
                // Otherwise we'll just speak the default text instead.
                if(messages.activities[i].speak.Length > 0)
                {
                    result += (messages.activities[i].speak + " ");
                } 
                else
                {
                    result += (messages.activities[i].text + " ");
                }
            }
        }

        //animator.Play("Happy");
        //MyTTS.StartSpeaking(result);

        // Display captions for the question
        captionsManager.SetCaptionsText(result);
    }

    ///// <summary>
    ///// This event is fired after the user pauses, typically at the end of a sentence. The full recognized string is returned here.
    ///// </summary>
    ///// <param name="text">The text that was heard by the recognizer.</param>
    ///// <param name="confidence">A representation of how confident (rejected, low, medium, high) the recognizer is of this recognition.</param>
    //private void DictationRecognizer_DictationResult(string text, ConfidenceLevel confidence)
    //{
    //    StopRecording();
    //    // Append textSoFar with latest text
    //    textSoFar.Append(text);

    //    captionsManager.SetCaptionsText(text);

    //    //animator.Play("Happy"); // TO DO: Need to fix, not working yet
    //    //MyTTS.StartSpeaking(text);        // Windows TTS was removed in MRTKv2, will be replaced with Cognitive Services Speech

    //    // Set DictationDisplay text to be textSoFar
    //    //DictationDisplay.text = textSoFar.ToString();
    //}

    ///// <summary>
    ///// This event is fired when the recognizer stops, whether from Stop() being called, a timeout occurring, or some other error.
    ///// Typically, this will simply return "Complete". In this case, we check to see if the recognizer timed out.
    ///// </summary>
    ///// <param name="cause">An enumerated reason for the session completing.</param>
    //private void DictationRecognizer_DictationComplete(DictationCompletionCause cause)
    //{
    //    // If Timeout occurs, the user has been silent for too long.
    //    // With dictation, the default timeout after a recognition is 20 seconds.
    //    // The default timeout with initial silence is 5 seconds.
    //    if (cause == DictationCompletionCause.TimeoutExceeded)
    //    {

    //    }
    //}

    ///// <summary>
    ///// This event is fired when an error occurs.
    ///// </summary>
    ///// <param name="error">The string representation of the error reason.</param>
    ///// <param name="hresult">The int representation of the hresult.</param>
    //private void DictationRecognizer_DictationError(string error, int hresult)
    //{

    //}
}