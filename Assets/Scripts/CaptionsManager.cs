using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;

public class CaptionsManager : MonoBehaviour
{
    public Text captions;

    bool isCaptionsOn = true;

    //KeywordRecognizer keywordRecognizer = null;
    //Dictionary<string, System.Action> keywords = new Dictionary<string, System.Action>();

    // Use this for initialization
    void Start()
    {
        //keywords.Add("Captions On", () =>
        //{
        //    isCaptionsOn = true;
        //    captions.text = "captions are now on";
        //});

        //keywords.Add("Captions Off", () =>
        //{
        //    isCaptionsOn = false;
        //    captions.text = "captions are now off";
        //});

        //keywords.Add("Focused", () =>
        //{
        //    var focusObject = GazeGestureManager.Instance.FocusedObject;
        //    if (focusObject != null)
        //    {
        //        // Call the OnDrop method on just the focused object.
        //        focusObject.SendMessage("OnCommand");
        //    }
        //});

        // Tell the KeywordRecognizer about our keywords.
        //keywordRecognizer = new KeywordRecognizer(keywords.Keys.ToArray());

        //// Register a callback for the KeywordRecognizer and start recognizing!
        //keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;
        //keywordRecognizer.Start();
    }

    //private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    //{
    //    System.Action keywordAction;
    //    if (keywords.TryGetValue(args.text, out keywordAction))
    //    {
    //        keywordAction.Invoke();
    //    }
    //}

    public void SetCaptionsText(string message)
    {
        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
        {
            // Display captions if they are enabled
            captions.text = (isCaptionsOn) ? message : "";
        }, false);
    }

    //public void ToggleKeywordRecognizer(bool state)
    //{
    //    if (state)
    //    {
    //        if (!keywordRecognizer.IsRunning)
    //            keywordRecognizer.Start();
    //    }
    //     else
    //    {
    //        if (keywordRecognizer.IsRunning)
    //            keywordRecognizer.Stop();
    //    }
    //    Debug.Log("Keyword Recognizer is now " + ((keywordRecognizer.IsRunning) ? "on" : "off"));
    //}
}
