using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SIGVerse.Common;

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

	public class SAPIVoiceSynthesisExternal : MonoBehaviour, IPlaybackGuidanceMessageHandler
	{
		[HeaderAttribute("SAPI")]
		public string path = "/../TTS/ConsoleSimpleTTS.exe";
		public string language = "409";
		public string gender = "Female";

		[HeaderAttribute("Guidance message param")]
		public int maxCharcters = 400;

		private List<GameObject> notificationDestinations;

		bool isSpeaking;

		private System.Diagnostics.Process process;

		// Use this for initialization
		void Awake()
		{
			this.process = new System.Diagnostics.Process();

			this.process.StartInfo.FileName = Application.dataPath + this.path;

			//this.process.EnableRaisingEvents = true;
			//this.process.Exited += new System.EventHandler(ProcessExit);

			//this.process.StartInfo.RedirectStandardOutput = true;
			//this.process.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(OutputHandler);

			//this.process.StartInfo.RedirectStandardError = true;
			//this.process.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(ErrorOutputHanlder);

			this.process.StartInfo.CreateNoWindow = true;
			this.process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

			SIGVerseLogger.Info("Text-To-Speech: " + this.process.StartInfo.FileName);

			this.ResetNotificationDestinations();

			this.isSpeaking = false;
		}

		//public void Start()
		//{
		//}

		void Update()
		{
			if (this.isSpeaking && this.process.HasExited)
			{
				foreach (GameObject destination in this.notificationDestinations)
				{
					ExecuteEvents.Execute<IStopSpeakingHandler>
					(
						target: destination,
						eventData: null,
						functor: (reciever, eventData) => reciever.OnStopSpeaking()
					);
				}

				this.isSpeaking = false;
			}
		}

		public bool SpeakMessage(string message, string displayType)
		{
			if (this.isSpeaking)
			{
				SIGVerseLogger.Info("Text-To-Speech: isSpeaking");
				return false;
			}

			string truncatedMessage;
			if (message.Length > maxCharcters)
			{
				truncatedMessage = message.Substring(0, maxCharcters);
				SIGVerseLogger.Info("Length of guidance message is over " + this.maxCharcters.ToString() + " charcters.");
			}
			else
			{
				truncatedMessage = message;
			}

			// speak
			string settings = "Language=" + this.language + "; Gender=" + this.gender;
			this.process.StartInfo.Arguments = "\"" + truncatedMessage + "\" \"" + settings + "\"";


			// For recording
			foreach (GameObject destination in this.notificationDestinations)
			{
				ExecuteEvents.Execute<ISpeakGuidanceMessageHandler>
				(
					target: destination,
					eventData: null,
					functor: (reciever, eventData) => reciever.OnSpeakGuidanceMessage(new GuidanceMessageStatus(message, displayType))
				);
			}

			this.process.Start();

			this.isSpeaking = true;

			return true;
		}

		public bool IsSpeaking()
		{
			return this.isSpeaking;
		}

		public void OnReceiveROSHumanNaviGuidanceMessage(SIGVerse.RosBridge.human_navigation.HumanNaviGuidanceMsg guidanceMsg)
		{
			this.SpeakMessage(guidanceMsg.message, guidanceMsg.display_type);
		}

		public void OnPlaybackGuidanceMessage(GuidanceMessageStatus guidanceMessageStatus)
		{
			this.SpeakMessage(guidanceMessageStatus.Message, guidanceMessageStatus.DisplayType);
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
			this.notificationDestinations.Add(GameObject.FindObjectOfType<HumanNaviScoreManager>().gameObject);
			this.notificationDestinations.Add(GameObject.FindObjectOfType<GuidanceMessagePanelController>().gameObject);
			this.notificationDestinations.Add(GameObject.FindObjectOfType<HumanNaviPlaybackCommon>().gameObject);
		}
	}
}
