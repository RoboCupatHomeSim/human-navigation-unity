using System.Collections.Generic;
using UnityEngine;
using SIGVerse.ToyotaHSR;
using UnityEngine.EventSystems;

namespace SIGVerse.Competition.HumanNavigation
{
	public interface IHumanNaviRecoderHandler : IEventSystemHandler
	{
		//void OnObjectGrasped(string objectName, string whichHandUsed);
		//void OnObjectPlaced(string objectName);
		//void OnGuidanceRequested();
		//void OnROSMessageSent(string message);
		//void OnROSMessageReceived(string message);
		//void OnEventOccured(string detail);

		//void OnROSMessageSent(string messageType, string message);
		//void OnROSMessageReceived(string messageType, string message);
	}

	[RequireComponent(typeof (HumanNaviPlaybackCommon))]
	public class HumanNaviPlaybackRecorder : TrialPlaybackRecorder, ISpeakGuidanceMessageHandler
	{
		public List<string> KeywordsOfAvatarPartsPathToIgnore;

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
			this.dataLines.Add(PlaybackGuidanceMessageEventController.GetDataLine(this.GetHeaderElapsedTime(), guidanceMessageStatus));
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
