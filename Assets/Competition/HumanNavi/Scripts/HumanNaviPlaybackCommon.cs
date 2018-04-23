using System.Collections.Generic;
using UnityEngine;
using SIGVerse.ToyotaHSR;
using SIGVerse.Common;
using UnityEngine.Video;

namespace SIGVerse.Competition.HumanNavigation
{
	public class HumanNaviPlaybackCommon : TrialPlaybackCommon
	{
		public const string FilePathFormat = "/../SIGVerseConfig/HumanNavi/Playback{0:D2}.dat";

		public List<string> KeywordsOfAvatarPartsPathToIgnore;

		// Events
		public const string DataTypeHumanNaviGuidanceMessageEvent = "GuidanceMessage";
		public const string DataTypeHumanNaviObjectGrasped = "ObjectGrasped";
		public const string DataTypeHumanNaviObjectPlaced = "ObjectPlaced";
		public const string DataTypeHumanNaviGuidanceRequested = "GuidanceRequested";
		public const string DataTypeHumanNaviROSMessageSent = "ROSMessageSent";
		public const string DataTypeHumanNaviROSMessageReceived = "ROSMessageReceived";
		public const string DataTypeHumanNaviEvent = "HumanNaviEvent";

		protected override void Awake()
		{
			// Video Player
			this.targetVideoPlayers = new List<VideoPlayer>();

			//base.Awake();

			//// Robot
			//Transform robot = GameObject.FindGameObjectWithTag("Robot").transform;

			//this.targetTransforms.Add(robot);

			//this.targetTransforms.Add(SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.BaseFootPrintName));
			//this.targetTransforms.Add(SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.ArmLiftLinkName));
			//this.targetTransforms.Add(SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.ArmFlexLinkName));
			//this.targetTransforms.Add(SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.ArmRollLinkName));
			//this.targetTransforms.Add(SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.WristFlexLinkName));
			//this.targetTransforms.Add(SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.WristRollLinkName));
			//this.targetTransforms.Add(SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.HeadPanLinkName));
			//this.targetTransforms.Add(SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.HeadTiltLinkName));
			//this.targetTransforms.Add(SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.TorsoLiftLinkName));
			//this.targetTransforms.Add(SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.HandLProximalLinkName));
			//this.targetTransforms.Add(SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.HandRProximalLinkName));
		}

		public void SetPlaybackTargets()
		{
			//HumanNaviPlaybackCommon common = this.GetComponent<HumanNaviPlaybackCommon>();

			this.targetTransforms = new List<Transform>();

			// Robot
			Transform robot = GameObject.FindGameObjectWithTag("Robot").transform;

			this.targetTransforms.Add(robot);
			this.targetTransforms.Add(SIGVerse.Common.SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.BaseFootPrintName));
			this.targetTransforms.Add(SIGVerse.Common.SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.ArmLiftLinkName));
			this.targetTransforms.Add(SIGVerse.Common.SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.ArmFlexLinkName));
			this.targetTransforms.Add(SIGVerse.Common.SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.ArmRollLinkName));
			this.targetTransforms.Add(SIGVerse.Common.SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.WristFlexLinkName));
			this.targetTransforms.Add(SIGVerse.Common.SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.WristRollLinkName));
			this.targetTransforms.Add(SIGVerse.Common.SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.HeadPanLinkName));
			this.targetTransforms.Add(SIGVerse.Common.SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.HeadTiltLinkName));
			this.targetTransforms.Add(SIGVerse.Common.SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.TorsoLiftLinkName));
			this.targetTransforms.Add(SIGVerse.Common.SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.HandLProximalLinkName));
			this.targetTransforms.Add(SIGVerse.Common.SIGVerseUtils.FindTransformFromChild(robot, HSRCommon.HandRProximalLinkName));

			// Avatar
			Transform moderator = GameObject.FindGameObjectWithTag("Avatar").transform;

			Transform[] avatarTransforms = moderator.GetComponentsInChildren<Transform>(true);

			foreach (Transform avatarTransform in avatarTransforms)
			{
				bool isTagergetTransform = true;
				foreach (string keyword in KeywordsOfAvatarPartsPathToIgnore)
				{
					if (SIGVerse.Common.SIGVerseUtils.GetHierarchyPath(avatarTransform).Contains(keyword)) { isTagergetTransform = false; }
				}
				if (isTagergetTransform)
				{
					this.targetTransforms.Add(avatarTransform);
				}
			}

			// Additional
			foreach (string playbackTargetTag in this.playbackTargetTags)
			{
				GameObject[] playbackTargetObjects = GameObject.FindGameObjectsWithTag(playbackTargetTag);

				foreach (GameObject playbackTargetObject in playbackTargetObjects)
				{
					this.targetTransforms.Add(playbackTargetObject.transform);
				}
			}
		}


	}
}

