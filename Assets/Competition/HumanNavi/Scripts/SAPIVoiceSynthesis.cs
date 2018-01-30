using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SpeechLib;
using UnityEngine.UI;

public class SAPIVoiceSynthesis : MonoBehaviour
{
	//[HeaderAttribute("GUI")]
	//public Text guidanceMessageText;

	[HeaderAttribute("SAPI")]
	public string language = "409";
	public string gender   = "Female";

	[HeaderAttribute("Guidance message text")]
	public string guidanceMessageTextName = "GuidanceMessageText"; ///// TODO /////


	private SpVoice voice;

	bool isSpeaking = false;

	private Text guidanceMessageText;

	// Use this for initialization
	void Awake()
	{
		this.voice = new SpVoice
		{
			Volume = 100, // Volume (no xml)
			Rate = 0      //   Rate (no xml)
		};

		SpObjectTokenCategory tokenCat = new SpObjectTokenCategory();
		tokenCat.SetId(SpeechLib.SpeechStringConstants.SpeechCategoryVoices, false);

		// Select tokens by specifying a semicolon-separated attribute.
		//     (Language:409=English (United States)/411=Japanese, Gender:Male/Female ... )
		ISpeechObjectTokens tokens = tokenCat.EnumerateTokens("Language=" + this.language + "; Gender=" + this.gender, null);

		this.voice.Voice = tokens.Item(0);

		//this.scene_recorder = GameObject.Find(PlaybackSystemParam.WorldRecorderName);
		guidanceMessageText = GameObject.Find(guidanceMessageTextName).GetComponent<Text>();
	}

	public void Start()
	{
		this.guidanceMessageText.text = "";
	}

	//void Update()
	//{
	//}

	public bool SpeakMessage(string msg)
	{
		if (this.voice.Status.RunningState == SpeechRunState.SRSEIsSpeaking)
		{
			return false;
		}

		this.voice.Speak(msg, SpeechVoiceSpeakFlags.SVSFlagsAsync);

		return true;
	}

	public void SetLanguage(string language, string gender)
	{
		if      (language == "English")  { language = "409"; }
		else if (language == "Japanese") { language = "411"; }
		else { return; }

		SpObjectTokenCategory tokenCat = new SpObjectTokenCategory();
		tokenCat.SetId(SpeechLib.SpeechStringConstants.SpeechCategoryVoices, false);
		ISpeechObjectTokens tokens = tokenCat.EnumerateTokens("Language=" + language + "; Gender=" + gender, null);

		this.voice.Voice = tokens.Item(0);
	}

	public bool IsSpeeching()
	{
		if (this.voice.Status.RunningState == SpeechRunState.SRSEIsSpeaking) return true;
		else                                                                 return false;
	}

	public void OnReceiveROSStringMessage(SIGVerse.ROSBridge.std_msgs.String stringMsg)
	{
		this.guidanceMessageText.text = stringMsg.data;
		this.SpeakMessage(stringMsg.data);
	}
}
