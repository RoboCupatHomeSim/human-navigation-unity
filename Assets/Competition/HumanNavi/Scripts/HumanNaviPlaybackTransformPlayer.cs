using UnityEngine;
using System.Collections.Generic;
using SIGVerse.ToyotaHSR;

namespace SIGVerse.Competition.HumanNavigation
{
	[RequireComponent(typeof (HumanNaviPlaybackTransformCommon))]
	public class HumanNaviPlaybackTransformPlayer : WorldPlaybackPlayer
	{
		protected override void Awake()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypePlay)
			{
				Transform robot = GameObject.FindGameObjectWithTag("Robot").transform;
				robot.Find("CompetitionScripts").gameObject.SetActive(false);
				robot.Find("RosBridgeScripts").gameObject.SetActive(false);
				robot.GetComponent<HSRCollisionDetector>().enabled = false;
				robot.GetComponent<SetNotificationDestination>().enabled = false;

				Transform moderator = GameObject.FindGameObjectWithTag("Moderator").transform;
				moderator.GetComponent<HumanNaviPubTaskInfo>().enabled = false;
				moderator.GetComponent<HumanNaviPubMessage>().enabled = false;
				moderator.GetComponent<HumanNaviPubAvatarPose>().enabled = false;
				moderator.GetComponent<HumanNaviSubMessage>().enabled = false;

				Transform avatar = GameObject.FindGameObjectWithTag("Avatar").transform;
				avatar.GetComponentInChildren<NewtonVR.NVRPlayer>().enabled = false;
				avatar.GetComponentInChildren<OVRManager>().enabled = false;
				avatar.GetComponentInChildren<OVRCameraRig>().enabled = false;

				UnityEngine.XR.XRSettings.enabled = false;

				GameObject mainMenu = GameObject.FindGameObjectWithTag("MainMenu");
				mainMenu.GetComponentInChildren<HumanNaviScoreManager>().enabled = false;
			}
			else
			{
				this.enabled = false;
			}
		}

		protected override void Start()
		{
		}

		public bool Initialize(int numnberOfTrial)
		{
			string filePath = string.Format(Application.dataPath + HumanNaviPlaybackTransformCommon.FilePathFormat, numnberOfTrial);

			return base.Initialize(filePath);
		}

		//// Use this for initialization
		//void Start()
		//{
		//	WorldPlaybackCommon common = this.GetComponent<HumanNaviPlaybackTransformCommon>();

		//	this.targetTransforms = common.GetTargetTransforms();

		//	foreach (Transform targetTransform in targetTransforms)
		//	{
		//		this.targetObjectsPathMap.Add(HumanNaviPlaybackTransformCommon.GetLinkPath(targetTransform), targetTransform);
		//	}
		//}

		protected override void StartInitializing()
		{
			///// TODO

			//this.SetTargetTransforms();
			//HumanNaviPlaybackTransformCommon common = this.GetComponent<HumanNaviPlaybackTransformCommon>();

			//foreach (Transform targetTransform in this.targetTransforms)
			//{
			//	this.targetObjectsPathMap.Add(WorldPlaybackCommon.GetLinkPath(targetTransform), targetTransform);
			//}

			//this.playingTransformQue = new Queue<UpdatingTransformList>();
			//this.transformOrder = new List<Transform>();


			//base.StartInitializing();
		}


		public void SetTargetTransforms()
		{
			///// TODO

			//HumanNaviPlaybackTransformCommon common = this.GetComponent<HumanNaviPlaybackTransformCommon>();

			//this.targetTransforms = new List<Transform>();

			//// Robot
			//Transform robot = GameObject.FindGameObjectWithTag("Robot").transform;

			//this.targetTransforms.Add(robot);

			//this.targetTransforms.Add(HSRCommon.FindGameObjectFromChild(robot, HSRCommon.BaseFootPrintName));
			//this.targetTransforms.Add(HSRCommon.FindGameObjectFromChild(robot, HSRCommon.ArmLiftLinkName));
			//this.targetTransforms.Add(HSRCommon.FindGameObjectFromChild(robot, HSRCommon.ArmFlexLinkName));
			//this.targetTransforms.Add(HSRCommon.FindGameObjectFromChild(robot, HSRCommon.ArmRollLinkName));
			//this.targetTransforms.Add(HSRCommon.FindGameObjectFromChild(robot, HSRCommon.WristFlexLinkName));
			//this.targetTransforms.Add(HSRCommon.FindGameObjectFromChild(robot, HSRCommon.WristRollLinkName));
			//this.targetTransforms.Add(HSRCommon.FindGameObjectFromChild(robot, HSRCommon.HeadPanLinkName));
			//this.targetTransforms.Add(HSRCommon.FindGameObjectFromChild(robot, HSRCommon.HeadTiltLinkName));
			//this.targetTransforms.Add(HSRCommon.FindGameObjectFromChild(robot, HSRCommon.TorsoLiftLinkName));
			//this.targetTransforms.Add(HSRCommon.FindGameObjectFromChild(robot, HSRCommon.HandLProximalLinkName));
			//this.targetTransforms.Add(HSRCommon.FindGameObjectFromChild(robot, HSRCommon.HandRProximalLinkName));

			//// Avatar
			//Transform moderator = GameObject.FindGameObjectWithTag("Avatar").transform;

			//Transform[] avatarTransforms = moderator.GetComponentsInChildren<Transform>(true);

			//foreach (Transform avatarTransform in avatarTransforms)
			//{
			//	this.targetTransforms.Add(avatarTransform);
			//}

			//// Additional
			//foreach (string playbackTargetTag in common.playbackTargetTags)
			//{
			//	GameObject[] playbackTargetObjects = GameObject.FindGameObjectsWithTag(playbackTargetTag);

			//	foreach (GameObject playbackTargetObject in playbackTargetObjects)
			//	{
			//		this.targetTransforms.Add(playbackTargetObject.transform);
			//	}
			//}

		}

	}
}

