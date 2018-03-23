using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SIGVerse.Common;
using System.IO;
using UnityEngine.EventSystems;

namespace SIGVerse.Competition.HumanNavigation
{
	public interface IEventRecoderHandler : IEventSystemHandler
	{
		void OnObjectGrasped(string objectName, string whichHandUsed);
		void OnObjectPlaced(string objectName);
		void OnGuidanceRequested();
		void OnROSMessageSent(string message);
		void OnROSMessageReceived(string message);
		void OnEventOccured(string detail);

		void OnROSMessageSent(string messageType, string message);
		void OnROSMessageReceived(string messageType, string message);
	}

	[RequireComponent(typeof(HumanNaviPlaybackEventCommon))]
	public class HumanNaviPlaybackEventRecorder : WorldPlaybackRecorder, IEventRecoderHandler
	{
		void Awake()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == HumanNaviPlaybackEventCommon.PlaybackTypeRecord)
			{
			}
			else
			{
				this.enabled = false;
			}
		}

		protected override void Start()
		{
		}

		public bool Initialize(int numberOfTrials)
		{
			string filePath = string.Format(Application.dataPath + HumanNaviPlaybackEventCommon.FilePathFormat, numberOfTrials);

			return base.Initialize(filePath);
		}

		protected override void StartInitializing()
		{
			this.step = Step.Initializing;

			SIGVerseLogger.Info("Output Playback file Path=" + this.filePath);

			// File open
			StreamWriter streamWriter = new StreamWriter(this.filePath, false);

			//string definitionLine = string.Empty;

			//definitionLine += "0.0,"; // Elapsed time is dummy.

			//// Make header lines
			//definitionLine += "\t" + "VRDevice_Model";

			//definitionLine += "\t" + "CenterEye";
			//definitionLine += "\t" + "Head";
			//definitionLine += "\t" + "LeftEye";
			//definitionLine += "\t" + "LeftHand";
			//definitionLine += "\t" + "RightEye";
			//definitionLine += "\t" + "RightHand";

			//definitionLine += "\t" + "LTouch";
			//definitionLine += "\t" + "RTouch";

			//definitionLine += "\t" + "RawButton_A";
			//definitionLine += "\t" + "RawButton_B";
			//definitionLine += "\t" + "RawButton_X";
			//definitionLine += "\t" + "RawButton_Y";
			//definitionLine += "\t" + "RawButton_Start";
			//definitionLine += "\t" + "RawButton_LThumbstick";
			//definitionLine += "\t" + "RawButton_RThumbstick";
			//definitionLine += "\t" + "RawButton_LHandTrigger";
			//definitionLine += "\t" + "RawButton_RHandTrigger";
			//definitionLine += "\t" + "RawButton_LIndexTrigger";
			//definitionLine += "\t" + "RawButton_RIndexTrigger";
			//definitionLine += "\t" + "RawButton_LShoulder";
			//definitionLine += "\t" + "RawButton_RShoulder";

			//definitionLine += "\t" + "RawAxis1D_LHandTrigger";
			//definitionLine += "\t" + "RawAxis1D_RHandTrigger";
			//definitionLine += "\t" + "RawAxis1D_LIndexTrigger";
			//definitionLine += "\t" + "RawAxis1D_RIndexTrigger";

			//definitionLine += "\t" + "RawAxis2D_LThumbstick";
			//definitionLine += "\t" + "RawAxis2D_RThumbstick";

			//definitionLine += "\t" + "RawTouch_A";
			//definitionLine += "\t" + "RawTouch_B";
			//definitionLine += "\t" + "RawTouch_X";
			//definitionLine += "\t" + "RawTouch_Y";
			//definitionLine += "\t" + "RawTouch_LIndexTrigger";
			//definitionLine += "\t" + "RawTouch_RIndexTrigger";
			//definitionLine += "\t" + "RawTouch_LThumbRest";
			//definitionLine += "\t" + "RawTouch_RThumbRest";
			//definitionLine += "\t" + "RawTouch_LThumbstick";
			//definitionLine += "\t" + "RawTouch_RThumbstick";

			//definitionLine += "\t" + "RawNearTouch_LIndexTrigger";
			//definitionLine += "\t" + "RawNearTouch_RIndexTrigger";
			//definitionLine += "\t" + "RawNearTouch_LThumbButtons";
			//definitionLine += "\t" + "RawNearTouch_RThumbButtons";

			//streamWriter.WriteLine(definitionLine);

			streamWriter.Flush();
			streamWriter.Close();

			this.dataLines = new List<string>();

			this.step = Step.Initialized;
		}

		protected override void SaveData()
		{
			//if (1000.0 * (this.elapsedTime - this.previousRecordedTime) < recordInterval) { return; }

			////this.SaveEvent();

			//this.previousRecordedTime = this.elapsedTime;
		}

		//public void SaveEvent()
		//{

		//}

		public void OnObjectGrasped(string objectName, string whichHandUsed)
		{
			if (this.step == Step.Recording)
			{
				string dataLine = string.Empty;
				dataLine += Math.Round(this.elapsedTime, 4, MidpointRounding.AwayFromZero);
				dataLine += "\t" + HumanNaviPlaybackEventCommon.DataTypeHumanNaviObjectGrasped;
				dataLine += "\t" + objectName;
				dataLine += "\t" + whichHandUsed;

				this.dataLines.Add(dataLine);
			}
		}

		public void OnObjectPlaced(string objectName)
		{
			if (this.step == Step.Recording)
			{
				string dataLine = string.Empty;
				dataLine += Math.Round(this.elapsedTime, 4, MidpointRounding.AwayFromZero);
				dataLine += "\t" + HumanNaviPlaybackEventCommon.DataTypeHumanNaviObjectPlaced;
				dataLine += "\t" + objectName;

				this.dataLines.Add(dataLine);
			}
		}

		public void OnGuidanceRequested()
		{
			if (this.step == Step.Recording)
			{
				string dataLine = string.Empty;
				dataLine += Math.Round(this.elapsedTime, 4, MidpointRounding.AwayFromZero);
				dataLine += "\t" + HumanNaviPlaybackEventCommon.DataTypeHumanNaviGuidanceRequested;

				this.dataLines.Add(dataLine);
			}
		}

		public void OnROSMessageSent(string message)
		{
			if (this.step == Step.Recording)
			{
				string dataLine = string.Empty;
				dataLine += Math.Round(this.elapsedTime, 4, MidpointRounding.AwayFromZero);
				dataLine += "\t" + HumanNaviPlaybackEventCommon.DataTypeHumanNaviROSMessageSent;
				dataLine += "\t" + message;

				this.dataLines.Add(dataLine);
			}
		}

		public void OnROSMessageReceived(string message)
		{
			if (this.step == Step.Recording)
			{
				string dataLine = string.Empty;
				dataLine += Math.Round(this.elapsedTime, 4, MidpointRounding.AwayFromZero);
				dataLine += "\t" + HumanNaviPlaybackEventCommon.DataTypeHumanNaviROSMessageReceived;
				dataLine += "\t" + message;

				this.dataLines.Add(dataLine);
			}
		}

		public void OnEventOccured(string detail)
		{
			if (this.step == Step.Recording)
			{
				string dataLine = string.Empty;
				dataLine += Math.Round(this.elapsedTime, 4, MidpointRounding.AwayFromZero);
				dataLine += "\t" + HumanNaviPlaybackEventCommon.DataTypeHumanNaviEvent;
				dataLine += "\t" + detail;

				this.dataLines.Add(dataLine);
			}
		}

		public void OnROSMessageSent(string messageType, string message)
		{
			if (this.step == Step.Recording)
			{
				string dataLine = string.Empty;
				dataLine += Math.Round(this.elapsedTime, 4, MidpointRounding.AwayFromZero);
				dataLine += "\t" + HumanNaviPlaybackEventCommon.DataTypeHumanNaviROSMessageSent;
				dataLine += "\t" + messageType;
				dataLine += "\t" + message;

				this.dataLines.Add(dataLine);
			}
		}

		public void OnROSMessageReceived(string messageType, string message)
		{
			if (this.step == Step.Recording)
			{
				string dataLine = string.Empty;
				dataLine += Math.Round(this.elapsedTime, 4, MidpointRounding.AwayFromZero);
				dataLine += "\t" + HumanNaviPlaybackEventCommon.DataTypeHumanNaviROSMessageReceived;
				dataLine += "\t" + messageType;
				dataLine += "\t" + message;

				this.dataLines.Add(dataLine);
			}
		}
	}
}