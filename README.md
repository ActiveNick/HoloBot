# HoloBot
Take your bots beyond Skype, Slack, Microsoft Teams and Facebook and bring them into the real world with Mixed Reality. Why simply type-chat with a bot when you could actually look at them, talk to them and listen to their answers?

HoloBot is reusable Windows Mixed Reality Unity project for [Microsoft HoloLens](http://hololens.com) that acts as a holographic client for a chatbot. This 3D app lets you interact with a floating robot head using speech recognition, getting answers spoken back by the "bot" using Text-to-Speech. The commands sent to the bot are powered by the [Microsoft Bot Framework](https://dev.botframework.com/) and [LUIS](https://www.microsoft.com/cognitive-services/en-us/language-understanding-intelligent-service-luis) from [Microsoft Cognitive Services](https://www.microsoft.com/cognitive-services).

- **Unity version:** 5.6.2f1 (release)
- **HoloToolkit version:** 1.5.8

To get started with HoloLens & Windows Mixed Reality development, visit the [Windows Mixed Reality Dev Center](https://developer.microsoft.com/en-us/windows/mixed-reality). The HoloLens Developer Kit is available for sale in several countries at http://hololens.com.

## Features
- Hovering bot head (aka HoloBot) with looping ambient sound.
- Tap & hold the HoloBot to drag it to a different location, drop the hold to place.
- Gaze at the HoloBot to trigger the speech recognizer, you will hear a "ping" sound.
- Speak "commands" to HoloBot using natural language. HoloBot has only been tested with English for now.
- The HoloBot speaks back to you using Speech Synthesis (aka Text-to-Speech, or TTS).
- All sounds and speech use spatial sound that originate from the HoloBot's location in the room.
- The "brain" of HoloBot can be any public bot built with the [Microsoft Bot Framework](https://dev.botframework.com/). Build your Bot using C# or Node. See bot integration instructions below.

## Video Demonstration
[![ScreenShot](Screenshots/HoloBot-YouTube-Titlepage.PNG)](https://youtu.be/f_5rT3IeusM)

The bot demonstrated in this video is [The Maker Show Bot, also found here on GitHub](https://github.com/ActiveNick/TheMakerShowBot). Feel free to fork the code and plug HoloBot to your own chatbot.

## Instructions / Implementation Notes
- The HoloBot model and sounds come from the [Holographic Academy](https://developer.microsoft.com/en-us/windows/holographic/academy) tutorial: [Holograms 240: Sharing Holograms].(https://developer.microsoft.com/en-us/windows/holographic/holograms_240).
- HoloBot has finally been upgraded to a recent version of the HoloToolkit for Unity. See above for the specific version.
- Uses the InputManager from [HoloToolkit for Unity](https://github.com/microsoft/HoloToolkit-Unity) (prefab) for Gaze & Gesture management.
- Now using the **Hand Draggable** script instead omn custom **Tap to Place**.
- **MicrophoneManager.cs** now implements **IFocusable** for Gaze events (enter/leave), which triggers the speech recording. 
- Uses Text to Speech Manager from [HoloToolkit for Unity](https://github.com/microsoft/HoloToolkit-Unity) (Utilities scripts).
- Make sure to copy a UWP build of **Newtonsoft.Json.dll** in the **/Plugins** folder of the HoloBot Unity project.
- Edit the Inspector settings for the **Newtonsoft.Json.dll** plugin as follows:

![All](Screenshots/PluginSettings.PNG)

## Connecting your Bot to HoloBot
- Create and register your bot as per the intructions at https://dev.botframework.com. Bots can be built with C# & ASP.NET WebAPI or Javascript & Node.js. Since HoloBot uses free natural language dictation, it is highly recommended that your bot support NLP via the [Language Understanding Intelligent Service](https://www.microsoft.com/cognitive-services/en-us/language-understanding-intelligent-service-luis) (LUIS) from [Microsoft Cognitive Services](https://www.microsoft.com/cognitive-services).
- From the Bot Connector portal, enable the Direct Line channel on your bot, and enable version 3.0 of the Direct Line API.
- Generate and copy your Direct Line secret (aka API key)
- Open **BotService.cs** in the **/Scripts** folder of the HoloBot Unity project and paste your Direct Line secret in the **_APIKEY** private string

![All](Screenshots/HoloBot-MakerShow-01.PNG)

## Acknowledgments
I want to offer special thanks to the following people who have helped me in building this project:
- [Jarez Bienz](https://github.com/jbienzms), for the Text-to-Speech component that he wrote for the HoloToolkit for Unity, and for his help in integrating my UWP Bot Framework code into Unity.
- [Kat Haris](https://github.com/KatVHarris), for her awesome Unity skills and helping me with audio sources triggers.
- Vanessa Arnauld & Sara Nagy, for being incredible holographic "enablers" :)
- The whole Microsoft Holographic Academy team & mentors - especially Pat - for their awesome training, resources, patience and help.

## Follow Me
* Twitter: [@ActiveNick](http://twitter.com/ActiveNick)
* Blog: [AgeofMobility.com](http://AgeofMobility.com)
* SlideShare: [http://www.slideshare.net/ActiveNick](http://www.slideshare.net/ActiveNick)
