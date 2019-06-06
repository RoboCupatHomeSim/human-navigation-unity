using System.Collections.Generic;
using UnityEngine;
using SIGVerse.ToyotaHSR;
using UnityEngine.EventSystems;
using System.Text.RegularExpressions;

namespace SIGVerse.Competition.HumanNavigation
{
	//public interface IHumanNaviRecoderHandler : IEventSystemHandler
	//{
	//	//void OnObjectGrasped(string objectName, string whichHandUsed);
	//	//void OnObjectPlaced(string objectName);
	//	//void OnGuidanceRequested();
	//	//void OnROSMessageSent(string message);
	//	//void OnROSMessageReceived(string message);
	//	//void OnEventOccured(string detail);

	//	//void OnROSMessageSent(string messageType, string message);
	//	//void OnROSMessageReceived(string messageType, string message);
	//}

	public interface IPlaybackRosMessageHandler : IEventSystemHandler
	{
		void OnSendRosMessage(SIGVerse.RosBridge.human_navigation.HumanNaviMsg message);
		void OnReceiveRosMessage(SIGVerse.RosBridge.human_navigation.HumanNaviMsg message);
	}

	public interface IRecordEventHandler : IEventSystemHandler
	{
		void OnRecordEvent(string log);
	}

	[RequireComponent(typeof (HumanNaviPlaybackCommon))]
	public class HumanNaviPlaybackRecorder : TrialPlaybackRecorder, ISpeakGuidanceMessageHandler, IRecordEventHandler, IPlaybackRosMessageHandler/*, IReceiveHumanNaviMsgHandler, IRosTaskInfoSendHandler, IRosAvatarStatusSendHandler*/
	{
		//public List<string> KeywordsOfAvatarPartsPathToIgnore;

		protected override void Awake()
		{
			this.isRecord = HumanNaviConfig.Instance.configInfo.playbackType == HumanNaviPlaybackCommon.PlaybackTypeRecord;

			base.Awake();
		}

		protected override void Start()
		{
			//base.Start();
		}

		public bool Initialize(int numberOfTrials)
		{
			string filePath = string.Format(Application.dataPath + HumanNaviPlaybackCommon.FilePathFormat, numberOfTrials);

			return this.Initialize(filePath);
		}

		protected override void StartInitializing()
		{
			base.StartInitializing();
		}

		public void SetPlaybackTargets()
		{
			HumanNaviPlaybackCommon common = this.GetComponent<HumanNaviPlaybackCommon>();

			common.SetPlaybackTargets();

			this.filePath = common.GetFilePath();

			this.isReplayVideoPlayers = common.IsReplayVideoPlayers();

			this.targetTransforms = common.GetTargetTransforms();   // Transform
			this.targetVideoPlayers = common.GetTargetVideoPlayers(); // Video Player
		}

		public void OnSpeakGuidanceMessage(GuidanceMessageStatus guidanceMessageStatus)
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType != HumanNaviPlaybackCommon.PlaybackTypeRecord) // for demo mode
			{
				return;
			}

			base.AddDataLine(PlaybackGuidanceMessageEventController.GetDataLine(this.GetHeaderElapsedTime(), guidanceMessageStatus));
		}

		public void OnSendRosMessage(SIGVerse.RosBridge.human_navigation.HumanNaviMsg message)
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType != WorldPlaybackCommon.PlaybackTypeRecord) // for demo mode
			{
				return;
			}
			if (this.step != Step.Recording)
			{
				return;
			}

			base.AddDataLine(GetDataLine(this.GetHeaderElapsedTime(), message, HumanNaviPlaybackCommon.DataTypeHumanNaviROSMessageSent));
		}
		public void OnSendRosAvatarStatusMessage(SIGVerse.RosBridge.human_navigation.HumanNaviAvatarStatus avatarStatus)
		{
			base.AddDataLine(GetDataLine(this.GetHeaderElapsedTime(), avatarStatus, HumanNaviPlaybackCommon.DataTypeHumanNaviROSMessageSent));
		}
		public void OnSendRosTaskInfoMessage(SIGVerse.RosBridge.human_navigation.HumanNaviTaskInfo taskInfo)
		{
			base.AddDataLine(GetDataLine(this.GetHeaderElapsedTime(), taskInfo, HumanNaviPlaybackCommon.DataTypeHumanNaviROSMessageSent));
		}

		public void OnReceiveRosMessage(SIGVerse.RosBridge.human_navigation.HumanNaviMsg humanNaviMsg)
		{
			base.AddDataLine(GetDataLine(this.GetHeaderElapsedTime(), humanNaviMsg, HumanNaviPlaybackCommon.DataTypeHumanNaviROSMessageReceived));
		}


		private string GetDataLine(string elapsedTime, SIGVerse.RosBridge.human_navigation.HumanNaviMsg message, string dataType)
		{
			string dataLine = elapsedTime + "," + dataType;
			dataLine += "\t" + "HumanNaviMsg";
			dataLine += "\t" + Regex.Escape(message.message);
			dataLine += "\t" + Regex.Escape(message.detail);

			return dataLine;
		}

		private string GetDataLine(string elapsedTime, SIGVerse.RosBridge.human_navigation.HumanNaviAvatarStatus avatarStatus, string dataType)
		{
			string dataLine = elapsedTime + "," + dataType;
			dataLine += "\t" + "HumanNaviAvatarPose";
			dataLine += "\t" + "head[position:" + avatarStatus.head.position.ToString() + ", orientation:" + avatarStatus.head.orientation.ToString() + "]";
			dataLine += "\t" + "body[position:" + avatarStatus.body.position.ToString() + ", orientation:" + avatarStatus.body.orientation.ToString() + "]";
			dataLine += "\t" + "left_hand[position:" + avatarStatus.left_hand.position.ToString() + ", orientation:" + avatarStatus.left_hand.orientation.ToString() + "]";
			dataLine += "\t" + "right_hand[position:" + avatarStatus.right_hand.position.ToString() + ", orientation:" + avatarStatus.right_hand.orientation.ToString() + "]";
			dataLine += "\t" + "object_in_left_hand:" + avatarStatus.object_in_left_hand;
			dataLine += "\t" + "object_in_right_hand:" + avatarStatus.object_in_right_hand;
			dataLine += "\t" + "is_target_object_in_left_hand:" + avatarStatus.is_target_object_in_left_hand.ToString();
			dataLine += "\t" + "is_target_object_in_right_hand:" + avatarStatus.is_target_object_in_right_hand.ToString();

			return dataLine;
		}

		private string GetDataLine(string elapsedTime, SIGVerse.RosBridge.human_navigation.HumanNaviTaskInfo taskInfo, string dataType)
		{
			string dataLine = elapsedTime + "," + dataType;
			dataLine += "\t" + "HumanNaviTaskInfo";
			dataLine += "\t" + taskInfo.environment_id;
			dataLine += "\t" + "target_object:" + taskInfo.target_object.name + ", " + taskInfo.target_object.position.ToString();
			dataLine += "\t" + "destination:" + taskInfo.destination.ToString();

			dataLine += "\t non_target_objects:";
			for(int i = 0; i < taskInfo.non_target_objects.Count; i++)
			{
				if (i != 0)
				{
					dataLine += ", ";
				}
				dataLine += taskInfo.non_target_objects[i].name + taskInfo.non_target_objects[i].position.ToString();
			}

			return dataLine;
		}

		public void OnRecordEvent(string log)
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType != WorldPlaybackCommon.PlaybackTypeRecord) // for demo mode
			{
				return;
			}

			string dataLine = elapsedTime + "," + HumanNaviPlaybackCommon.DataTypeHumanNaviEvent;
			dataLine += "\t" + log;
			base.AddDataLine(dataLine);
		}

		public float GetElapsedTime()
		{
			return this.elapsedTime;
		}

		//public void OnObjectGrasped(string objectName, string whichHandUsed)
		//{
		//	if (this.step == Step.Recording)
		//	{
		//		string dataLine = string.Empty;
		//		dataLine += this.GetHeaderElapsedTime();
		//		dataLine += "\t" + HumanNaviPlaybackCommon.DataTypeHumanNaviObjectGrasped;
		//		dataLine += "\t" + objectName;
		//		dataLine += "\t" + whichHandUsed;

		//		this.dataLines.Add(dataLine);
		//	}
		//}

		//public void OnEventOccured(string detail)
		//{
		//	if (this.step == Step.Recording)
		//	{
		//		string dataLine = string.Empty;
		//		dataLine += this.GetHeaderElapsedTime();
		//		dataLine += "\t" + HumanNaviPlaybackCommon.DataTypeHumanNaviEvent;
		//		dataLine += "\t" + detail;

		//		this.dataLines.Add(dataLine);
		//	}
		//}
	}
}
