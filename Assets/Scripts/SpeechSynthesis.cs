using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

public class SpeechSynthesis : MonoBehaviour
{
    /// <summary>
    /// List of all voices currently implemented in this sample. This may not include all the
    /// voices supported by the Cognitive Services Text-to-Speech API. Please visit the following
    /// link to get the most up-to-date list of supported languages:
    /// https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/bingvoiceoutput
    /// Don't forget to edit ConvertVoiceNametoString() below if you add more values to this enum.
    /// </summary>
    public enum VoiceName
    {
        enAUCatherine,
        enAUHayleyRUS,
        enCALinda,
        enCAHeatherRUS,
        enGBSusanApollo,
        enGBHazelRUS,
        enGBGeorgeApollo,
        enIESean,
        enINHeeraApollo,
        enINPriyaRUS,
        enINRaviApollo,
        enUSZiraRUS,
        enUSJessaRUS,
        enUSJessaNeural,
        enUSBenjaminRUS,
        enUSGuyNeural,
        deATMichael,
        deCHKarsten,
        deDEHedda,
        deDEHeddaRUS,
        deDEStefanApollo,
        deDEKatjaNeural,
        esESLauraApollo,
        esESHelenaRUS,
        esESPabloApollo,
        esMXHildaRUS,
        esMXRaulApollo,
        frCACaroline,
        frCAHarmonieRUS,
        frCHGuillaume,
        frFRJulieApollo,
        frFRHortenseRUS
    }

    [Tooltip("Cognitive Services Speech API Key")]
    public string SpeechServiceAPIKey = string.Empty;
    [Tooltip("Cognitive Services Speech Service Region.")]
    public string SpeechServiceRegion = string.Empty;

    [Tooltip("The audio source where speech will be played.")]
    public AudioSource audioSource = null;

    public VoiceName voiceName = VoiceName.enUSJessaRUS;
    public int VoicePitch = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Speech synthesis to pull audio output stream.
    public void SpeakWithSDKPlugin(string message)
    {
        //Synthesize cortana = new Synthesize();
        SpeechSynthesizer synthesizer;

        // Creates an instance of a speech config with specified subscription key and service region.
        // Replace with your own subscription key and service region (e.g., "westus").
        var config = SpeechConfig.FromSubscription(SpeechServiceAPIKey, SpeechServiceRegion);
        config.SpeechSynthesisLanguage = GetVoiceLocale(voiceName);
        config.SpeechSynthesisVoiceName = ConvertVoiceNametoString(voiceName);

        // Creates an audio out stream.
        var stream = AudioOutputStream.CreatePullStream();
        // Creates a speech synthesizer using audio stream output.
        var streamConfig = AudioConfig.FromStreamOutput(stream);
        synthesizer = new SpeechSynthesizer(config, streamConfig);
        Task<SpeechSynthesisResult> Speaking = synthesizer.SpeakTextAsync(message);

        // We can't await the task without blocking the main Unity thread, so we'll call a coroutine to
        // monitor completion and play audio when it's ready.
        StartCoroutine(WaitAndPlayRoutineSDK(Speaking));
    }

    private IEnumerator WaitAndPlayRoutineSDK(Task<SpeechSynthesisResult> speakTask)
    {
        // Yield control back to the main thread as long as the task is still running
        while (!speakTask.IsCompleted)
        {
            yield return null;
        }

        var result = speakTask.Result;
        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
        {
            var audiodata = result.AudioData;
            Debug.Log($"Speech synthesized for text and the audio was written to output stream.");

            int sampleCount = 0;
            int frequency = 16000;
            var unityData = FixedRAWAudioToUnityAudio(audiodata, 1, 16, out sampleCount);

            // Convert data to a Unity audio clip
            Debug.Log($"Converting audio data of size {unityData.Length} to Unity audio clip with {sampleCount} samples at frequency {frequency}.");
            var clip = ToClip("Speech", unityData, sampleCount, frequency);

            // Set the source on the audio clip
            audioSource.clip = clip;

            Debug.Log($"Trigger playback of audio clip on AudioSource.");
            // Play audio
            audioSource.Play();
        }
        else if (result.Reason == ResultReason.Canceled)
        {
            var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
            Debug.Log($"CANCELED: Reason={cancellation.Reason}");

            if (cancellation.Reason == CancellationReason.Error)
            {
                Debug.Log($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                Debug.Log($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                Debug.Log($"CANCELED: Did you update the subscription info?");
            }
        }
    }

    /// <summary>
    /// Dynamically creates an <see cref="AudioClip"/> that represents raw Unity audio data.
    /// </summary>
    /// <param name="name"> The name of the dynamically generated clip.</param>
    /// <param name="audioData">Raw Unity audio data.</param>
    /// <param name="sampleCount">The number of samples in the audio data.</param>
    /// <param name="frequency">The frequency of the audio data.</param>
    /// <returns>The <see cref="AudioClip"/>.</returns>
    private static AudioClip ToClip(string name, float[] audioData, int sampleCount, int frequency)
    {
        var clip = AudioClip.Create(name, sampleCount, 1, frequency, false);
        clip.SetData(audioData, 0);
        return clip;
    }

    /// <summary>
    /// Converts raw WAV data into Unity formatted audio data.
    /// </summary>
    /// <param name="wavAudio">The raw WAV data.</param>
    /// <param name="sampleCount">The number of samples in the audio data.</param>
    /// <param name="frequency">The frequency of the audio data.</param>
    /// <returns>The Unity formatted audio data. </returns>
    private static float[] FixedRAWAudioToUnityAudio(byte[] wavAudio, int channelCount, int resolution, out int sampleCount)
    {
        // Pos is now positioned to start of actual sound data.
        int bytesPerSample = resolution / 8; // e.g. 2 bytes per sample (16 bit sound mono)
        sampleCount = wavAudio.Length / bytesPerSample;
        if (channelCount == 2) { sampleCount /= 2; }  // 4 bytes per sample (16 bit stereo)
        Debug.Log($"Audio data contains {sampleCount} samples. Starting conversion");

        // Allocate memory (supporting left channel only)
        var unityData = new float[sampleCount];

        int pos = 0;
        try
        {
            // Write to double array/s:
            int i = 0;
            while (pos < wavAudio.Length)
            {
                unityData[i] = BytesToFloat(wavAudio[pos], wavAudio[pos + 1]);
                pos += 2;
                if (channelCount == 2)
                {
                    pos += 2;
                }
                i++;
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"Error occurred converting audio data to float array of size {wavAudio.Length} at position {pos}.");
        }

        return unityData;
    }

    /// <summary>
    /// Converts two bytes to one float in the range -1 to 1.
    /// </summary>
    /// <param name="firstByte">The first byte.</param>
    /// <param name="secondByte"> The second byte.</param>
    /// <returns>The converted float.</returns>
    private static float BytesToFloat(byte firstByte, byte secondByte)
    {
        // Convert two bytes to one short (little endian)
        short s = (short)((secondByte << 8) | firstByte);

        // Convert to range from -1 to (just below) 1
        return s / 32768.0F;
    }

    /// <summary>
    /// Converts an array of bytes to an integer.
    /// </summary>
    /// <param name="bytes"> The byte array.</param>
    /// <param name="offset"> An offset to read from.</param>
    /// <returns>The converted int.</returns>
    private static int BytesToInt(byte[] bytes, int offset = 0)
    {
        int value = 0;
        for (int i = 0; i < 4; i++)
        {
            value |= ((int)bytes[offset + i]) << (i * 8);
        }
        return value;
    }

    public string GetVoiceLocale(VoiceName voicename)
    {
        return ConvertVoiceNametoString(voicename).Substring(46, 5);
    }

    /// <summary>
    /// Converts a specific VoioceName enum option into its string counterpart as expected
    /// by the API when building the SSML string that is sent to Cognitive Services.
    /// Make sure that each option in the enum is included in the switch below.
    /// </summary>
    /// <param name="voicename"></param>
    /// <returns></returns>
    public string ConvertVoiceNametoString(VoiceName voicename)
    {
        switch (voicename)
        {
            case VoiceName.enAUCatherine:
                return "Microsoft Server Speech Text to Speech Voice (en-AU, Catherine)";
            case VoiceName.enAUHayleyRUS:
                return "Microsoft Server Speech Text to Speech Voice (en-AU, HayleyRUS)";
            case VoiceName.enCALinda:
                return "Microsoft Server Speech Text to Speech Voice (en-CA, Linda)";
            case VoiceName.enCAHeatherRUS:
                return "Microsoft Server Speech Text to Speech Voice (en-CA, HeatherRUS)";
            case VoiceName.enGBSusanApollo:
                return "Microsoft Server Speech Text to Speech Voice (en-GB, Susan, Apollo)";
            case VoiceName.enGBHazelRUS:
                return "Microsoft Server Speech Text to Speech Voice (en-GB, HazelRUS)";
            case VoiceName.enGBGeorgeApollo:
                return "Microsoft Server Speech Text to Speech Voice (en-GB, George, Apollo)";
            case VoiceName.enIESean:
                return "Microsoft Server Speech Text to Speech Voice (en-IE, Sean)";
            case VoiceName.enINHeeraApollo:
                return "Microsoft Server Speech Text to Speech Voice (en-IN, Heera, Apollo)";
            case VoiceName.enINPriyaRUS:
                return "Microsoft Server Speech Text to Speech Voice (en-IN, PriyaRUS)";
            case VoiceName.enINRaviApollo:
                return "Microsoft Server Speech Text to Speech Voice (en-IN, Ravi, Apollo)";
            case VoiceName.enUSZiraRUS:
                return "Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)";
            case VoiceName.enUSJessaRUS:
                return "Microsoft Server Speech Text to Speech Voice (en-US, JessaRUS)";
            case VoiceName.enUSJessaNeural:
                return "Microsoft Server Speech Text to Speech Voice (en-US, JessaNeural)";
            case VoiceName.enUSBenjaminRUS:
                return "Microsoft Server Speech Text to Speech Voice (en-US, BenjaminRUS)";
            case VoiceName.enUSGuyNeural:
                return "Microsoft Server Speech Text to Speech Voice (en-US, GuyNeural)";
            case VoiceName.deATMichael:
                return "Microsoft Server Speech Text to Speech Voice (de-AT, Michael)";
            case VoiceName.deCHKarsten:
                return "Microsoft Server Speech Text to Speech Voice (de-CH, Karsten)";
            case VoiceName.deDEHedda:
                return "Microsoft Server Speech Text to Speech Voice (de-DE, Hedda)";
            case VoiceName.deDEHeddaRUS:
                return "Microsoft Server Speech Text to Speech Voice (de-DE, HeddaRUS)";
            case VoiceName.deDEStefanApollo:
                return "Microsoft Server Speech Text to Speech Voice (de-DE, Stefan, Apollo)";
            case VoiceName.deDEKatjaNeural:
                return "Microsoft Server Speech Text to Speech Voice (de-DE, KatjaNeural)";
            case VoiceName.esESHelenaRUS:
                return "Microsoft Server Speech Text to Speech Voice (es-ES, HelenaRUS)";
            case VoiceName.esESLauraApollo:
                return "Microsoft Server Speech Text to Speech Voice (es-ES, Laura, Apollo)";
            case VoiceName.esESPabloApollo:
                return "Microsoft Server Speech Text to Speech Voice (es-ES, Pablo, Apollo)";
            case VoiceName.esMXHildaRUS:
                return "Microsoft Server Speech Text to Speech Voice (es-MX, HildaRUS)";
            case VoiceName.esMXRaulApollo:
                return "Microsoft Server Speech Text to Speech Voice (es-MX, Raul, Apollo)";
            case VoiceName.frCACaroline:
                return "Microsoft Server Speech Text to Speech Voice (fr-CA, Caroline)";
            case VoiceName.frCAHarmonieRUS:
                return "Microsoft Server Speech Text to Speech Voice (fr-CA, HarmonieRUS)";
            case VoiceName.frCHGuillaume:
                return "Microsoft Server Speech Text to Speech Voice (fr-CH, Guillaume)";
            case VoiceName.frFRJulieApollo:
                return "Microsoft Server Speech Text to Speech Voice (fr-FR, Julie, Apollo)";
            case VoiceName.frFRHortenseRUS:
                return "Microsoft Server Speech Text to Speech Voice (fr-FR, HortenseRUS)";
            default:
                return "Microsoft Server Speech Text to Speech Voice (en-US, JessaRUS)";
        }
    }
}
