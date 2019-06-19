using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SIGVerse.Common;
using Google.Cloud.Translation.V2;

namespace SIGVerse.Competition.HumanNavigation
{
	public interface IPlaybackGuidanceMessageHandler : IEventSystemHandler
	{
		void OnPlaybackGuidanceMessage(GuidanceMessageStatus guidanceMessageStatus);
	}

	public interface ISpeakGuidanceMessageHandler : IEventSystemHandler
	{
		void OnSpeakGuidanceMessage(GuidanceMessageStatus guidanceMessageStatus);
	}

	public interface IStopSpeakingHandler : IEventSystemHandler
	{
		void OnStopSpeaking();
	}

	public interface ISendSpeechResultHandler : IEventSystemHandler
	{
		void OnSendSpeechResult(string speechResult);
	}

	public class SAPIVoiceSynthesisExternal : MonoBehaviour, IPlaybackGuidanceMessageHandler
	{
		private const string SpeechResultCancelled = "Cancelled";
		private const string SpeechResultStarted   = "Started";
		private const string SpeechResultFinished  = "Finished";

		[HeaderAttribute("SAPI")]
		public string path = "/../TTS/ConsoleSimpleTTS.exe";
		public string gender = "Female";

		[HeaderAttribute("Guidance message param")]
		public int maxCharactersForSourceLang = 1000;
		public int maxCharactersForTargetLang = 400;

		private List<GameObject> notificationDestinations;

		bool isSpeaking;

		private System.Diagnostics.Process speechProcess;

		TranslationClient translationClient;

		// Use this for initialization
		void Awake()
		{
			this.speechProcess = new System.Diagnostics.Process();

			this.speechProcess.StartInfo.FileName = Application.dataPath + this.path;

			//this.process.EnableRaisingEvents = true;
			//this.process.Exited += new System.EventHandler(ProcessExit);

			//this.process.StartInfo.RedirectStandardOutput = true;
			//this.process.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(OutputHandler);

			//this.process.StartInfo.RedirectStandardError = true;
			//this.process.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(ErrorOutputHanlder);

			this.speechProcess.StartInfo.CreateNoWindow = true;
			this.speechProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

			SIGVerseLogger.Info("Text-To-Speech: " + this.speechProcess.StartInfo.FileName);

			this.ResetNotificationDestinations();

			this.isSpeaking = false;

			try
			{
				this.translationClient = TranslationClient.Create();
			}
			catch (Exception)
			{
				this.translationClient = null;
			}
		}

		//public void Start()
		//{
		//}

		void Update()
		{
			if (this.isSpeaking && this.speechProcess.HasExited)
			{
				foreach (GameObject destination in this.notificationDestinations)
				{
					// For guidance message panel
					ExecuteEvents.Execute<IStopSpeakingHandler>
					(
						target: destination,
						eventData: null,
						functor: (reciever, eventData) => reciever.OnStopSpeaking()
					);

					// For send speech result (ROS message)
					ExecuteEvents.Execute<ISendSpeechResultHandler>
					(
						target: destination,
						eventData: null,
						functor: (reciever, eventData) => reciever.OnSendSpeechResult(SpeechResultFinished)
					);
				}

				this.isSpeaking = false;
			}
		}

		private bool SpeakMessage(string message, string displayType, string sourceLanguage, string targetLanguage)
		{
			if (this.isSpeaking)
			{
				SIGVerseLogger.Info("Text-To-Speech: isSpeaking");

				try
				{
					if (/*isTaskFinished &&*/ !this.speechProcess.HasExited)
					{
						this.speechProcess.Kill();
					}
				}
				catch (Exception)
				{
					SIGVerseLogger.Warn("Couldn't terminate the speech process, but do nothing.");
					// Do nothing even if an error occurs
				}

			}

			// Translation
			if ((sourceLanguage == string.Empty && targetLanguage != string.Empty) || (sourceLanguage != string.Empty && targetLanguage == string.Empty))
			{
				SIGVerseLogger.Error("Invalid language type. Source Language=" + sourceLanguage + ", Target Language="+ targetLanguage);
				return false;
			}
			
			if (sourceLanguage != string.Empty && targetLanguage != string.Empty)
			{
				if (message.Length > maxCharactersForSourceLang)
				{
					message.Substring(0, maxCharactersForSourceLang);
					SIGVerseLogger.Info("Length of guidance message(source lang) is over " + this.maxCharactersForSourceLang.ToString() + " charcters.");
				}

				if(this.translationClient!=null)
				{
					message = this.translationClient.TranslateText(message, targetLanguage, sourceLanguage).TranslatedText;
				}
				else
				{
					SIGVerseLogger.Warn("There is no environment for translation.");
				}
			}

			string truncatedMessage;

			if (message.Length > maxCharactersForTargetLang)
			{
				truncatedMessage = message.Substring(0, maxCharactersForTargetLang);
				SIGVerseLogger.Info("Length of guidance message(target lang) is over " + this.maxCharactersForTargetLang.ToString() + " charcters.");
			}
			else
			{
				truncatedMessage = message;
			}

			// speak
			string settings = "Language=" + HumanNaviConfig.Instance.ttsLanguageId + "; Gender=" + this.gender;

			this.speechProcess.StartInfo.Arguments = "\"" + truncatedMessage + "\" \"" + settings + "\"";

			SIGVerseLogger.Warn("Speech Message="+ this.speechProcess.StartInfo.Arguments);

			foreach (GameObject destination in this.notificationDestinations)
			{
				// For recording
				ExecuteEvents.Execute<ISpeakGuidanceMessageHandler>
				(
					target: destination,
					eventData: null,
					functor: (reciever, eventData) => reciever.OnSpeakGuidanceMessage(new GuidanceMessageStatus(message, displayType, sourceLanguage, targetLanguage))
				);

				// For send speech result (ROS message)
				ExecuteEvents.Execute<ISendSpeechResultHandler>
				(
					target: destination,
					eventData: null,
					functor: (reciever, eventData) => reciever.OnSendSpeechResult(SpeechResultStarted)
				);
			}

			this.speechProcess.Start();

			this.isSpeaking = true;

			return true;
		}

		public bool IsSpeaking()
		{
			return this.isSpeaking;
		}

		public void OnReceiveROSHumanNaviGuidanceMessage(SIGVerse.RosBridge.human_navigation.HumanNaviGuidanceMsg guidanceMsg)
		{
			this.SpeakMessage(guidanceMsg.message, guidanceMsg.display_type, guidanceMsg.source_language, guidanceMsg.target_language);
		}

		public void OnPlaybackGuidanceMessage(GuidanceMessageStatus guidanceMessageStatus)
		{
			this.SpeakMessage(guidanceMessageStatus.Message, guidanceMessageStatus.DisplayType, guidanceMessageStatus.SourceLanguage, guidanceMessageStatus.TargetLanguage);
		}

		//private void ProcessExit(object sender, System.EventArgs e)
		//{
		//	System.Diagnostics.Process proc = (System.Diagnostics.Process)sender;

		//	//Debug.Log("process exit");
		//	//this.isSpeaking = false;

		//	this.endOfSpeaking = true;

		//	proc.Kill();
		//}

		private void OutputHandler(object sender, System.Diagnostics.DataReceivedEventArgs args)
		{
			if (!string.IsNullOrEmpty(args.Data))
			{
				SIGVerseLogger.Info(args.Data);
			}
		}

		private void ErrorOutputHanlder(object sender, System.Diagnostics.DataReceivedEventArgs args)
		{
			if (!string.IsNullOrEmpty(args.Data))
			{
				SIGVerseLogger.Error(args.Data);
			}
		}

		public void ResetNotificationDestinations()
		{
			this.notificationDestinations = new List<GameObject>();
			this.notificationDestinations.Add(GameObject.FindObjectOfType<HumanNaviModerator>().gameObject);
			this.notificationDestinations.Add(GameObject.FindObjectOfType<HumanNaviScoreManager>().gameObject);
			this.notificationDestinations.Add(GameObject.FindObjectOfType<GuidanceMessagePanelController>().gameObject);
			this.notificationDestinations.Add(GameObject.FindObjectOfType<HumanNaviPlaybackCommon>().gameObject);
		}
	}
}
