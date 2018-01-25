using UnityEngine;
using UnityEngine.UI;

public class CaptionsManager : MonoBehaviour
{
    public Text captions;

    bool isCaptionsOn = true;

    void Start()
    {
        
    }

    public void SetCaptionsText(string message)
    {
        //UnityEngine.WSA.Application.InvokeOnAppThread(() =>
        //{
            // Display captions if they are enabled
            captions.text = (isCaptionsOn) ? message : "";
        //}, false);
    }

}
