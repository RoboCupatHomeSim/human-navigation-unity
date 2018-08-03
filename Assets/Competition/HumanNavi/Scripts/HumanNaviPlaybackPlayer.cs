using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace SIGVerse.Competition.HumanNavigation
{
	[RequireComponent(typeof (HumanNaviPlaybackCommon))]
	public class HumanNaviPlaybackPlayer : TrialPlaybackPlayer
	{
		[HeaderAttribute("Handyman Objects")]
		public HumanNaviScoreManager scoreManager;

		[HeaderAttribute("Session Manager")]
		public HumanNaviSessionManager sessionManager;

		private GameObject sapi;

		private PlaybackGuidanceMessageEventController guidanceMessageController;

		protected override void Awake()
		{
			this.isPlay = HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypePlay;

			base.Awake();

			if (this.isPlay)
			{
				Transform robot = GameObject.FindGameObjectWithTag("Robot").transform;

				//robot.Find("CompetitionScripts").gameObject.SetActive(false);
				robot.Find("RosBridgeScripts")  .gameObject.SetActive(false);

				Transform moderator = GameObject.FindGameObjectWithTag("Moderator").transform;

				moderator.GetComponent<HumanNaviModerator>().enabled = false;
				moderator.GetComponent<HumanNaviPubMessage>().enabled = false;
				moderator.GetComponent<HumanNaviSubMessage>().enabled = false;
				moderator.GetComponent<HumanNaviPubTaskInfo>().enabled = false;
				moderator.GetComponent<HumanNaviPubAvatarPose>().enabled = false;

				robot.GetComponentInChildren<HumanNaviSubGuidanceMessage>().enabled = false;

				this.scoreManager.enabled = false;

				// Avatar
#if HUMAN_NAVI_PLAYBACK_AVATAR_WITH_FINAL_IK
				// Avatar (Final IK)
				Transform avatar = GameObject.FindGameObjectWithTag("Avatar").transform;
				avatar.transform.Find("ThirdPersonEthanWithAnimation").gameObject.SetActive(false);
				avatar.GetComponentInChildren<RootMotion.FinalIK.VRIK>().enabled = false;

				avatar.GetComponentInChildren<NewtonVR.NVRPlayer>().enabled = false;

#else
				// Avatar(Simple Oculus Ethan )
				Transform avatar = GameObject.FindGameObjectWithTag("Avatar").transform;
				avatar.GetComponentInChildren<NewtonVR.NVRPlayer>().enabled = false;
				avatar.GetComponentInChildren<OVRManager>().enabled = false;
				avatar.GetComponentInChildren<OVRCameraRig>().enabled = false;

				avatar.GetComponentInChildren<Animator>().enabled = false;
				avatar.GetComponentInChildren<SIGVerse.Human.VR.SimpleHumanVRController>().enabled = false;
				avatar.GetComponentInChildren<SIGVerse.Human.IK.SimpleIK>().enabled = false;

				avatar.GetComponentInChildren<Animator>().enabled = false;
				avatar.GetComponentInChildren<SIGVerse.Human.VR.SimpleHumanVRController>().enabled = false;
				avatar.GetComponentInChildren<SIGVerse.Human.IK.SimpleIK>().enabled = false;
#endif

				//UnityEngine.XR.XRSettings.enabled = false;

				//foreach(GameObject graspingCandidatePosition in GameObject.FindGameObjectsWithTag("GraspingCandidatesPosition"))
				//{
				//	graspingCandidatePosition.SetActive(false);
				//}

				this.timeLimit = HumanNaviConfig.Instance.configInfo.sessionTimeLimit;
			}
		}

		protected override void Start()
		{
			HumanNaviPlaybackCommon common = this.GetComponent<HumanNaviPlaybackCommon>();

			this.filePath = common.GetFilePath();

			//this.transformController   = new PlaybackTransformEventController  (common);  // Transform
			this.videoPlayerController = new PlaybackVideoPlayerEventController(common);  // Video Player

			this.taskInfoController = new PlaybackTaskInfoEventController(this.trialNumberText, this.timeLeftValText, this.taskMessageText);
			this.scoreController = new PlaybackScoreEventController(this.scoreText, this.totalText); // Score
			this.panelNoticeController = new PlaybackPanelNoticeEventController(this, this.mainMenu);      // Notice of a Panel
			this.collisionController = new PlaybackCollisionEventController(this.collisionEffect);       // Collision
			this.hsrCollisionController = new PlaybackHsrCollisionEventController(this.collisionEffect);    // HSR Collision
		}

		protected override void ReadData(string[] headerArray, string dataStr)
		{
			base.ReadData(headerArray, dataStr);

			this.guidanceMessageController.ReadEvents(headerArray, dataStr);
		}

		protected override void StartInitializing()
		{
			base.StartInitializing();

			this.guidanceMessageController.StartInitializingEvents();
		}

		protected override void UpdateIndexAndElapsedTime(float elapsedTime)
		{
			base.UpdateIndexAndElapsedTime(elapsedTime);

			this.guidanceMessageController.UpdateIndex(elapsedTime);
		}

		protected override void UpdateData()
		{
			base.UpdateData();

			this.guidanceMessageController.ExecutePassedAllEvents(this.elapsedTime, this.deltaTime);
		}

		protected override float GetTotalTime()
		{
			return Mathf.Max(
				base.GetTotalTime(),
				this.taskInfoController.GetTotalTime(),
				this.scoreController.GetTotalTime(),
				this.panelNoticeController.GetTotalTime(),
				this.collisionController.GetTotalTime(),
				this.hsrCollisionController.GetTotalTime(),
				this.guidanceMessageController.GetTotalTime()
			);
		}

		public override void OnReadFileButtonClick()
		{
			this.trialNo = int.Parse(this.trialNoInputField.text);

			string filePath = string.Format(Application.dataPath + HumanNaviPlaybackCommon.FilePathFormat, this.trialNo);

			this.sessionManager.ResetEnvironment(this.trialNo);

			HumanNaviPlaybackCommon common = this.GetComponent<HumanNaviPlaybackCommon>();

			common.SetPlaybackTargets();

			this.sapi = GameObject.FindGameObjectWithTag("Robot").GetComponentInChildren<SAPIVoiceSynthesisExternal>().gameObject;

			this.guidanceMessageController = new PlaybackGuidanceMessageEventController(this.sapi); // Guidance message
			this.transformController = new PlaybackTransformEventController(common);  // Transform

			this.Initialize(filePath);
		}
	}
}

