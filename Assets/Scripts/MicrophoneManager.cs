using HoloToolkit;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using HoloBot;
using System;

/// <summary>
/// MicrophoneManager lets us capture audio from the user and feed into speech recognition
/// Make sure to enable the Microphone capability in the Windows 10 UWP Player Settings
/// </summary>
public class MicrophoneManager : MonoBehaviour, IFocusable
{
    //[Tooltip("A text area for the recognizer to display the recognized strings.")]
    //public Text DictationDisplay;

    private DictationRecognizer dictationRecognizer;
    private StringBuilder textSoFar;

    // Use this string to cache the text currently displayed in the text box.
    public Animator animator;
    public TextToSpeechManager MyTTS;
    public AudioSource selectedSource;
    //public Text captions;
    public CaptionsManager captionsManager;
    public Billboard billboard;

    // Using an empty string specifies the default microphone. 
    private static string deviceName = string.Empty;
    //private int samplingRate;
    private const int messageLength = 10;
    private BotService tmsBot = new BotService();
    private AudioSource[] audioSources;
    private AudioSource ttsAudioSrc;

    void Awake()
    {
        //animator = GetComponent<Animator>();
       
        // Create a new DictationRecognizer and assign it to dictationRecognizer variable.
        dictationRecognizer = new DictationRecognizer();

        // Register for dictationRecognizer.DictationHypothesis and implement DictationHypothesis below
        // This event is fired while the user is talking. As the recognizer listens, it provides text of what it's heard so far.
        dictationRecognizer.DictationHypothesis += DictationRecognizer_DictationHypothesis;

        // Register for dictationRecognizer.DictationResult and implement DictationResult below
        // This event is fired after the user pauses, typically at the end of a sentence. The full recognized string is returned here.
        dictationRecognizer.DictationResult += DictationRecognizer_DictationResult;

        // Register for dictationRecognizer.DictationComplete and implement DictationComplete below
        // This event is fired when the recognizer stops, whether from Stop() being called, a timeout occurring, or some other error.
        dictationRecognizer.DictationComplete += DictationRecognizer_DictationComplete;

        // Register for dictationRecognizer.DictationError and implement DictationError below
        // This event is fired when an error occurs.
        dictationRecognizer.DictationError += DictationRecognizer_DictationError;

        audioSources = this.GetComponents<AudioSource>();
        foreach (AudioSource a in audioSources)
        {
            if (a.clip == null)
            {
                ttsAudioSrc = a;
            }
            
            if ((a.clip != null) && (a.clip.name == "Ping"))
            {
                selectedSource = a;
            }
        }
        // Query the maximum frequency of the default microphone. Use 'unused' to ignore the minimum frequency.
        //int unused;
        //Microphone.GetDeviceCaps(deviceName, out unused, out samplingRate);

        // Use this string to cache the text currently displayed in the text box.
        textSoFar = new StringBuilder();

        captionsManager.SetCaptionsText("");

        //billboard.enabled = false;

#if WINDOWS_UWP
        var startTask = tmsBot.StartConversation();
        startTask.Wait();
        // startTask.Result;
#endif

    }

    void Update()
    {

        // Add condition to check if dictationRecognizer.Status is Running
        //if (!Microphone.IsRecording(deviceName) && dictationRecognizer.Status == SpeechSystemStatus.Running)
        //{
        //    // This acts like pressing the Stop button and sends the message to the Communicator.
        //    // If the microphone stops as a result of timing out, make sure to manually stop the dictation recognizer.
        //    // Look at the StopRecording function.
        //    SendMessage("RecordStop");
        //}
        //if (ttsAudioSrc.isPlaying)
        //{
        //    billboard.enabled = true;
        //}
        //else
        //{
        //    billboard.enabled = false;
        //}

    }

    /// <summary>
    /// Activate speech recognition only when the user looks straight at the bot
    /// </summary>
    public void OnFocusEnter()
    {
        // Don't activate speech recognition if the recognizer is already running
        if (dictationRecognizer.Status != SpeechSystemStatus.Running)
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

    //IEnumerator CoStartRecording()
    //{
    //    yield return new WaitForSeconds(1f);
    //    StartRecording();
    //}

    public void OnFocusExit()
    {
    //    StopRecording();
    //    //captionsManager.ToggleKeywordRecognizer(true);
    }

    /// <summary>
    /// Turns on the dictation recognizer and begins recording audio from the default microphone.
    /// </summary>
    /// <returns>The audio clip recorded from the microphone.</returns>
    public void StartRecording()
    {
        // Shutdown the PhraseRecognitionSystem. This controls the KeywordRecognizers
        //PhraseRecognitionSystem.Shutdown();
        //animator.Stop();

        // Start dictationRecognizer
        dictationRecognizer.Start();

        //DictationDisplay.text = "Dictation is starting. It may take time to display your text the first time, but begin speaking now...";

        // Start recording from the microphone for 10 seconds
        //return Microphone.Start(deviceName, false, messageLength, samplingRate);

        Debug.Log("Dictation Recognizer is now " + ((dictationRecognizer.Status == SpeechSystemStatus.Running) ? "on" : "off"));
    }

    /// <summary>
    /// Ends the recording session.
    /// </summary>
    public void StopRecording()
    {
        // Check if dictationRecognizer.Status is Running and stop it if so
        if (dictationRecognizer.Status == SpeechSystemStatus.Running)
        {
            dictationRecognizer.Stop();
        }

        //animator.Play("Idle");

        //Microphone.End(deviceName);

        //StartCoroutine("RestartSpeechSystem");

        Debug.Log("Dictation Recognizer is now " + ((dictationRecognizer.Status == SpeechSystemStatus.Running) ? "on" : "off"));
    }

    /// <summary>
    /// This event is fired while the user is talking. As the recognizer listens, it provides text of what it's heard so far.
    /// </summary>
    /// <param name="text">The currently hypothesized recognition.</param>
    private void DictationRecognizer_DictationHypothesis(string text)
    {
        // Set DictationDisplay text to be textSoFar and new hypothesized text
        // We don't want to append to textSoFar yet, because the hypothesis may have changed on the next event
        //DictationDisplay.text = textSoFar.ToString() + " " + text + "...";
    }

    // This event handler's code only works in UWP (i.e. HoloLens)
#if WINDOWS_UWP
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

        // Set DictationDisplay text to be textSoFar
        //DictationDisplay.text = textSoFar.ToString();

        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
        {
            // Display captions for the question
            captionsManager.SetCaptionsText(text);
        }, false); 

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
        MyTTS.SpeakText(result);

        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
        {
            // Display captions for the question
            captionsManager.SetCaptionsText(result);
        }, false);     
    }

#else

    /// <summary>
    /// This event is fired after the user pauses, typically at the end of a sentence. The full recognized string is returned here.
    /// </summary>
    /// <param name="text">The text that was heard by the recognizer.</param>
    /// <param name="confidence">A representation of how confident (rejected, low, medium, high) the recognizer is of this recognition.</param>
    private void DictationRecognizer_DictationResult(string text, ConfidenceLevel confidence)
    {
        StopRecording();
        // Append textSoFar with latest text
        textSoFar.Append(text);

        captionsManager.SetCaptionsText(text);

        //animator.Play("Happy"); // TO DO: Need to fix, not working yet
        MyTTS.SpeakText(text);

        // Set DictationDisplay text to be textSoFar
        //DictationDisplay.text = textSoFar.ToString();
    }
#endif
    
    /// <summary>
    /// This event is fired when the recognizer stops, whether from Stop() being called, a timeout occurring, or some other error.
    /// Typically, this will simply return "Complete". In this case, we check to see if the recognizer timed out.
    /// </summary>
    /// <param name="cause">An enumerated reason for the session completing.</param>
    private void DictationRecognizer_DictationComplete(DictationCompletionCause cause)
    {
        // If Timeout occurs, the user has been silent for too long.
        // With dictation, the default timeout after a recognition is 20 seconds.
        // The default timeout with initial silence is 5 seconds.
        if (cause == DictationCompletionCause.TimeoutExceeded)
        {
            //Microphone.End(deviceName);

            //DictationDisplay.text = "Dictation has timed out. Please press the record button again.";
            //SendMessage("ResetAfterTimeout");
        }
    }

    /// <summary>
    /// This event is fired when an error occurs.
    /// </summary>
    /// <param name="error">The string representation of the error reason.</param>
    /// <param name="hresult">The int representation of the hresult.</param>
    private void DictationRecognizer_DictationError(string error, int hresult)
    {
        // Set DictationDisplay text to be the error string
        //DictationDisplay.text = error + "\nHRESULT: " + hresult;
    }

    //private IEnumerator RestartSpeechSystem()
    //{
    //    while (dictationRecognizer != null && dictationRecognizer.Status == SpeechSystemStatus.Running)
    //    {
    //        yield return null;
    //    }
    //    if (PhraseRecognitionSystem.Status == SpeechSystemStatus.Stopped)
    //    {
    //        PhraseRecognitionSystem.Restart();
    //    }

    //    //keywordToStart.StartKeywordRecognizer();
    //    //captionsManager.ToggleKeywordRecognizer(true);
    //}
}