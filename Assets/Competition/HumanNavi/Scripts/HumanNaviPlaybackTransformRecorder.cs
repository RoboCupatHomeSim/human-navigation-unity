using UnityEngine;

using System.Collections.Generic;
using SIGVerse.ToyotaHSR;


namespace SIGVerse.Competition.HumanNavigation
{
	[RequireComponent(typeof (HumanNaviPlaybackTransformCommon))]
	public class HumanNaviPlaybackTransformRecorder : WorldPlaybackRecorder
	{
		public List<string> KeywordsOfAvatarPartsPathToIgnore;

		protected override void Awake()
		{
			this.isRecord = HumanNaviConfig.Instance.configInfo.playbackType == HumanNaviPlaybackTransformCommon.PlaybackTypeRecord;

			base.Awake();
		}

		protected override void Start()
		{
		}

		public bool Initialize(int numberOfTrials)
		{
			string filePath = string.Format(Application.dataPath + HumanNaviPlaybackTransformCommon.FilePathFormat, numberOfTrials);

			return base.Initialize(filePath);
		}

		protected override void StartInitializing()
		{
			this.SetTargetTransforms();

			//base.StartInitializing();
		}

		public void SetTargetTransforms()
		{
			HumanNaviPlaybackTransformCommon common = this.GetComponent<HumanNaviPlaybackTransformCommon>();

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
				///// TODO
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
			foreach (string playbackTargetTag in common.playbackTargetTags)
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


