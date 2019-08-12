//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//
using UnityEngine;
using UnityEngine.UI;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif
using HoloBot;

/// <summary>
/// SpeechRecognition class lets the user use Speech-to-Text to convert spoken words
/// into text strings. There is an optional mode that can be enabled to also translate
/// the text after the recognition returns results. Both modes also support interim
/// results (i.e. recognition hypotheses) that are returned in near real-time as the 
/// speaks in the microphone.
/// </summary>
public class SpeechRecognition : MonoBehaviour
{
    // Public fields in the Unity inspector
    [Tooltip("Unity UI Text component used to report potential errors on screen.")]
    public Text RecognizedText;
    [Tooltip("Unity UI Text component used to post recognition results on screen.")]
    public Text ErrorText;

    [Tooltip("SpeechSynthesis object used for text-to-speech playback.")]
    public SpeechSynthesis speechTTS;

    // Used to show live messages on screen, must be locked to avoid threading deadlocks since
    // the recognition events are raised in a separate thread
    private string recognizedString = "";
    private string errorString = "";
    // Status flag to make sure we don't start more than one reco job at a time
    private bool isRecognizing = false;

    // Speech recognition key, required
    [Tooltip("Connection string to Cognitive Services Speech.")]
    public string SpeechServiceAPIKey = string.Empty;
    [Tooltip("Region for your Cognitive Services Speech instance (must match the key).")]
    public string SpeechServiceRegion = "";

    // Cognitive Services Speech objects used for Speech Recognition
    private SpeechRecognizer recognizer;
    // The current language of origin is locked to English-US in this sample. Change this
    // to another region & language code to use a different origin language.
    // e.g. fr-fr, es-es, etc.
    string fromLanguage = "en-us";

    // Client object to communicate with the Bot Service
    private BotService tmsBot = new BotService();

    private bool micPermissionGranted = false;
#if PLATFORM_ANDROID
    // Required to manifest microphone permission on Android
    // https://docs.unity3d.com/Manual/android-manifest.html
    private Microphone mic;
#endif

    /// <summary>
    /// First thing to run in the MonoBehavior when the scene is loaded.
    /// Awake was made async so we can await the StartConversation Task.
    /// </summary>
    private async void Awake()
    {
        // IMPORTANT INFO BEFORE YOU CAN USE THIS SAMPLE:
        // Get your own Cognitive Services Speech subscription key for free at the following
        // link: https://docs.microsoft.com/azure/cognitive-services/speech-service/get-started.
        // Use the inspector fields to manually set these values with your subscription info.
        // If you prefer to manually set your Speech Service API Key and Region in code,
        // then uncomment the two lines below and set the values to your own.
        //SpeechServiceAPIKey = "YourSubscriptionKey";
        //SpeechServiceRegion = "YourServiceRegion";

        // Initialize the Bot Framework client before we can send requests in
        await tmsBot.StartConversation();
    }

    private void Start()
    {
        if (speechTTS == null)
        {
            UnityEngine.Debug.LogFormat("The SpeechManager is NotFiniteNumberException properly configured with a TTS object.");
            return;
        } else
        {
            speechTTS.SpeechServiceAPIKey = SpeechServiceAPIKey;
            speechTTS.SpeechServiceRegion = SpeechServiceRegion;
        }

#if PLATFORM_ANDROID
        // Request to use the microphone, cf.
        // https://docs.unity3d.com/Manual/android-RequestingPermissions.html
        recognizedString = "Waiting for microphone permission...";
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#else
        micPermissionGranted = true;
#endif
    }

    public bool IsReady {
        get
        {
            return micPermissionGranted;
        }
    }

    /// <summary>
    /// Attach to button component used to launch continuous recognition (with or without translation)
    /// </summary>
    public void StartRecognition(bool continuous = false)
    {
        errorString = "";
        if (micPermissionGranted)
        {
            if (continuous)
            {
                // Continuous recognition is currently not needed/supported in this application
            }
            else
            {
                StartSingleRecognition();
            }
        }
        else
        {
            recognizedString = "This app cannot function without access to the microphone.";
            errorString = "ERROR: Microphone access denied.";
            UnityEngine.Debug.LogFormat(errorString);
        }
    }

    /// <summary>
    /// Creates a class-level Speech Recognizer for a specific language using Azure credentials
    /// and hooks-up lifecycle & recognition events
    /// </summary>
    void CreateSpeechRecognizer()
    {
        // Make sure the developer has initialized the sample using their own Cognitive Services Speech API key
        if (SpeechServiceAPIKey.Length == 0 || SpeechServiceAPIKey == "YourSubscriptionKey")
        {
            recognizedString = "You forgot to obtain Cognitive Services Speech credentials and inserting them in this app." + Environment.NewLine +
                               "See the README file and/or the instructions in the Awake() function for more info before proceeding.";
            errorString = "ERROR: Missing service credentials";
            UnityEngine.Debug.LogFormat(errorString);
            return;
        }
        UnityEngine.Debug.LogFormat("Creating Speech Recognizer.");
        recognizedString = "Initializing speech recognition, please wait...";

        if (recognizer == null)
        {
            SpeechConfig config = SpeechConfig.FromSubscription(SpeechServiceAPIKey, SpeechServiceRegion);
            config.SpeechRecognitionLanguage = fromLanguage;
            recognizer = new SpeechRecognizer(config);

            if (recognizer != null)
            {
                // Subscribes to speech events.
                recognizer.Recognizing += RecognizingHandler;
                recognizer.Recognized += RecognizedHandler;
                recognizer.SpeechStartDetected += SpeechStartDetectedHandler;
                recognizer.SpeechEndDetected += SpeechEndDetectedHandler;
                recognizer.Canceled += CanceledHandler;
                recognizer.SessionStarted += SessionStartedHandler;
                recognizer.SessionStopped += SessionStoppedHandler;
            }
        }
        UnityEngine.Debug.LogFormat("CreateSpeechRecognizer exit");
    }

    /// <summary>
    /// Initiate a single speech recognition task from the default microphone.
    /// </summary>
    private async void StartSingleRecognition()
    {
        UnityEngine.Debug.LogFormat("Starting Single Speech Recognition.");
        CreateSpeechRecognizer();
        if (recognizer != null && !isRecognizing)
        {
            UnityEngine.Debug.LogFormat("Starting Speech Recognizer.");
            isRecognizing = true;
            recognizedString = "Listening...";
            UpdateUI();
            await recognizer.RecognizeOnceAsync().ConfigureAwait(false);
            UnityEngine.Debug.LogFormat("Speech Recognizer is now running.");
        }
        UnityEngine.Debug.LogFormat("Start Continuous Speech Recognition exit");
    }

    /// <summary>
    /// Speech session started in the recognizer.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    #region Speech Recognition event handlers
    private void SessionStartedHandler(object sender, SessionEventArgs e)
    {
        UnityEngine.Debug.LogFormat($"\n    Session started event. Event: {e.ToString()}.");
    }

    /// <summary>
    /// Speech session stopped in the recognizer
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SessionStoppedHandler(object sender, SessionEventArgs e)
    {
        UnityEngine.Debug.LogFormat($"\n    Session event. Event: {e.ToString()}.");
        UnityEngine.Debug.LogFormat($"Session Stop detected. Stop the recognition.");
        isRecognizing = false;
    }

    /// <summary>
    /// Event raised when the user starts talking.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SpeechStartDetectedHandler(object sender, RecognitionEventArgs e)
    {
        UnityEngine.Debug.LogFormat($"SpeechStartDetected received: offset: {e.Offset}.");
    }

    /// <summary>
    ///  Event raised when the user stops talking.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SpeechEndDetectedHandler(object sender, RecognitionEventArgs e)
    {
        UnityEngine.Debug.LogFormat($"SpeechEndDetected received: offset: {e.Offset}.");
        UnityEngine.Debug.LogFormat($"Speech end detected.");
        isRecognizing = false;
    }

    /// <summary>
    /// "Recognizing" events are fired every time we receive interim results during recognition
    /// (i.e. hypotheses). This increases perceived performance since the user gets feedback
    /// as they speak and don't have to wait for long before getting a response.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void RecognizingHandler(object sender, SpeechRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.RecognizingSpeech)
        {
            //UnityEngine.Debug.LogFormat($"HYPOTHESIS: Text={e.Result.Text}");     // disabled, too spammy
            recognizedString = $"I'm hearing: {Environment.NewLine}{e.Result.Text}";
            UnityDispatcher.InvokeOnAppThread(() => { UpdateUI(); });
        }
    }

    /// <summary>
    /// "Recognized" events are fired when the end of utterance was detected by the server
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void RecognizedHandler(object sender, SpeechRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.RecognizedSpeech)
        {
            UnityEngine.Debug.LogFormat($"RECOGNIZED: Text={e.Result.Text}");
            recognizedString = $"You said: {Environment.NewLine}{e.Result.Text}";
            UnityDispatcher.InvokeOnAppThread(() => { UpdateUI(); });
            // Send the recognized text as input to the bot framework via the DirectLine API
            SendBotRequestMessage(e.Result.Text);
        }
        else if (e.Result.Reason == ResultReason.NoMatch)
        {
            UnityEngine.Debug.LogFormat($"NOMATCH: Speech could not be recognized.");
        }
        RecognizerCleanup(false);
        isRecognizing = false;
    }

    /// <summary>
    /// Sends requests to the Bot Framework via the DirectLine v3 API.
    /// The specific bot that gets called gets configured via the DirectLine API key
    /// in the BotService class. This function runs in the background to insure the
    /// application isn;t blocked whiule we wait for the bot response.
    /// </summary>
    /// <param name="message"></param>
    private async void SendBotRequestMessage(string message)
    {
        string result = "I'm sorry, I'm not sure how to answer that";

        // sends the message to the bot and awaits a response.
        if (await tmsBot.SendMessage(message))
        {
            ConversationActitvities messages = await tmsBot.GetMessages();
            if (messages.activities.Length > 0)
            {
                result = "";
            }

            // Note that attachments (like cards) are still not supported
            for (int i = 1; i < messages.activities.Length; i++)
            {
                // We focus on the speak tag if the bot was speech-enabled.
                // Otherwise we'll just speak the default text instead.
                if (messages.activities[i].speak.Length > 0)
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
        recognizedString = result;
        // Use Text-to Speech to respond to the user
        UnityDispatcher.InvokeOnAppThread(() => { UpdateUI(); });
        speechTTS.SpeakWithSDKPlugin(result);
    }

    // "Canceled" events are fired if the server encounters some kind of error.
    // This is often caused by invalid subscription credentials.
    private void CanceledHandler(object sender, SpeechRecognitionCanceledEventArgs e)
    {
        UnityEngine.Debug.LogFormat($"CANCELED: Reason={e.Reason}");

        errorString = e.ToString();
        UnityDispatcher.InvokeOnAppThread(() => { UpdateUI(); });
        if (e.Reason == CancellationReason.Error)
        {
            UnityEngine.Debug.LogFormat($"CANCELED: ErrorDetails={e.ErrorDetails}");
            UnityEngine.Debug.LogFormat($"CANCELED: Did you update the subscription info?");
        }
        isRecognizing = false;
        RecognizerCleanup(false);
    }
    #endregion

    /// <summary>
    /// Extract the language code from the enum used to populate the droplists.
    /// Assumes that an underscore "_" is used as a separator in the enum name.
    /// </summary>
    /// <param name="languageListLabel"></param>
    /// <returns></returns>
    string ExtractLanguageCode(string languageListLabel)
    {
        return languageListLabel.Substring(0, languageListLabel.IndexOf("_"));
    }

    /// <summary>
    /// Main update loop: Runs every frame
    /// </summary>
    void Update()
    {
#if PLATFORM_ANDROID
        if (!micPermissionGranted && Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            micPermissionGranted = true;
        }
#endif
    }

    public void UpdateUI()
    {
        RecognizedText.text = recognizedString;
        ErrorText.text = errorString;
    }

    void OnDisable()
    {
        RecognizerCleanup(true);
    }

    /// <summary>
    /// Stops the recognition on the speech recognizer or translator as applicable.
    /// Important: Unhook all events & clean-up resources.
    /// </summary>
    public void RecognizerCleanup(bool dispose)
    {
        if (recognizer != null)
        {
            recognizer.Recognizing -= RecognizingHandler;
            recognizer.Recognized -= RecognizedHandler;
            recognizer.SpeechStartDetected -= SpeechStartDetectedHandler;
            recognizer.SpeechEndDetected -= SpeechEndDetectedHandler;
            recognizer.Canceled -= CanceledHandler;
            recognizer.SessionStarted -= SessionStartedHandler;
            recognizer.SessionStopped -= SessionStoppedHandler;
            if (dispose)
                recognizer.Dispose();
            recognizer = null;
            isRecognizing = false;
            UnityEngine.Debug.LogFormat("Speech Recognizer is now stopped.");
        }
    }
}
