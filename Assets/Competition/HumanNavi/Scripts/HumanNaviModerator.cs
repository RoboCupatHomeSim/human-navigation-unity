using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using SIGVerse.Common;
using SIGVerse.RosBridge;
using System.Threading;

//using SIGVerse.RosBridge.HumanNavigation;

namespace SIGVerse.Competition.HumanNavigation
{
	public class HumanNaviModerator : MonoBehaviour
	{
		private const int SendingAreYouReadyInterval = 1000;

		private const string MsgAreYouReady     = "Are_you_ready?";
		private const string MsgTaskSucceeded   = "Task_succeeded";
		private const string MsgTaskFailed      = "Task_failed";
		private const string MsgTaskFinished    = "Task_finished";
		private const string MsgGoToNextTrial   = "Go_to_next_trial";
		private const string MsgMissionComplete = "Mission_complete";

		private const string MsgIamReady           = "I_am_ready";
		private const string MsgGetAvatarPose      = "Get_avatar_pose";
		private const string MsgConfirmSpeechState = "Confirm_speech_state";

		private const string MsgRequest     = "Guidance_request";
		private const string MsgSpeechState = "Speech_state";

		private enum Step
		{
			Initialize,
			InitializePlayback,
			WaitForStart,
			WaitForIamReady,
			TrialStart,
			WaitTrialFinished,
			WaitNextTrial,
			Playback,
		};

		//-----------------------------

		[HeaderAttribute("Score Manager")]
		public HumanNaviScoreManager scoreManager;

		[HeaderAttribute("Avatar")]
		public GameObject avatar;
		public GameObject cameraRig;
		public GameObject Ethan;
		public GameObject head;
		public NewtonVR.NVRHand LeftHand;
		public NewtonVR.NVRHand rightHand;

		[HeaderAttribute("Robot")]
		public GameObject robotPrefab;

		[HeaderAttribute("Environment")]
		public List<GameObject> environmentPrefabs;

		[HeaderAttribute("Panels for avatar")]
		public GameObject noticePanelForAvatar;
		public UnityEngine.UI.Text noticeTextForAvatar;

		//[HeaderAttribute("Effect")]
		//public GameObject badEffect;
		//public GameObject goodEffect;

		[HeaderAttribute("Menu")]
		public HumanNaviMenu humanNaviMenu;
		public Camera birdviewCamera;

		[HeaderAttribute("ROS Message")]
		public HumanNaviPubMessage pubHumanNaviMessage;
		public HumanNaviPubTaskInfo pubTaskInfo;
		public HumanNaviPubAvatarPose pubAvatarPose;

		[HeaderAttribute("Scenario Logger")]
		public GameObject playbackSystem;

		//-----------------------------

		private GameObject robot;
		private GameObject environment;

		private Vector3 initialAvatarPosition;
		private Vector3 initialAvatarRotation;

		private List<SIGVerse.Competition.HumanNavigation.TaskInfo> taskInfoList;
		private SIGVerse.RosBridge.human_navigation.HumanNaviTaskInfo taskInfoForRobot;
		private SIGVerse.Competition.HumanNavigation.TaskInfo currentTaskInfo;

		private Step step;

		private float waitingTime;

		private bool isCompetitionStarted = false;
		private bool isDuringTrial = false;

		private Dictionary<string, bool> receivedMessageMap;
		private bool isTargetAlreadyGrasped;
		private bool goNextTrial = false;
		private bool isAllTaskFinished = false;

		//private GameObject worldRecorder;
		private HumanNaviPlaybackTransformRecorder playbackTransformRecorder;
		private HumanNaviPlaybackTransformPlayer playbackTransformPlayer;
		private HumanNaviPlaybackEventRecorder playbackEventRecorder;


		void Awake()
		{
			try
			{
				this.taskInfoList = new List<SIGVerse.Competition.HumanNavigation.TaskInfo>();

				List<GameObject> existingEnvironments = GameObject.FindGameObjectsWithTag("Environment").ToList<GameObject>();
				foreach(GameObject existingEnvironment in existingEnvironments)
				{
					existingEnvironment.SetActive(false);
				}

				if (HumanNaviConfig.Instance.configInfo.playbackType != WorldPlaybackCommon.PlaybackTypePlay) ///// TODO
				{
					List<GameObject> existingRobots = GameObject.FindGameObjectsWithTag("Robot").ToList<GameObject>();
					foreach (GameObject existingRobot in existingRobots)
					{
						existingRobot.SetActive(false);
					}
				}
				this.ResetRobot();

				this.playbackTransformRecorder = this.playbackSystem.GetComponent<HumanNaviPlaybackTransformRecorder>();
				this.playbackTransformPlayer   = this.playbackSystem.GetComponent<HumanNaviPlaybackTransformPlayer>();
				this.playbackEventRecorder     = this.playbackSystem.GetComponent<HumanNaviPlaybackEventRecorder>();


				foreach (TaskInfo info in HumanNaviConfig.Instance.configInfo.taskInfo)
				{
					taskInfoList.Add(info);
					SIGVerseLogger.Info("Environment_ID: " + info.environment + ", Target_object_name: " + info.target + ", Destination: " + info.destination);
				}

				/////
				// TODO: check duplication of object_id in a environment
				//Debug.Log(this.environmentPrefabs.Where(obj => obj.name == info.environment).SingleOrDefault());
				/////

				this.initialAvatarPosition = this.avatar.transform.position;
				this.initialAvatarRotation = this.avatar.transform.eulerAngles;

				this.noticePanelForAvatar.SetActive(false);
				this.noticeTextForAvatar.text = "";

				this.receivedMessageMap = new Dictionary<string, bool>();
				this.receivedMessageMap.Add(MsgIamReady, false);
				this.receivedMessageMap.Add(MsgGetAvatarPose, false);
				this.receivedMessageMap.Add(MsgConfirmSpeechState, false);
			}
			catch (Exception exception)
			{
				Debug.LogError(exception);
				SIGVerseLogger.Error(exception.Message);
				SIGVerseLogger.Error(exception.StackTrace);
				Application.Quit();
			}
		}

		void Start()
		{
			this.environment = null;

			this.step = Step.Initialize;

			this.SetDefaultEnvironment();

			this.humanNaviMenu.ShowStartTaskPanel();
		}

		// Update is called once per frame
		void Update()
		{
			try
			{
				if (this.isAllTaskFinished){ 
					return;
				}

				if (OVRInput.GetDown(OVRInput.RawButton.X) && this.isDuringTrial && HumanNaviConfig.Instance.configInfo.playbackType != WorldPlaybackCommon.PlaybackTypePlay)
				{
					this.SendROSHumanNaviMessage(MsgRequest, "");
					this.RecordEvent("Guidance_message_is_requested");
				}

				switch (this.step)
				{
					case Step.Initialize:
					{
						if (this.isCompetitionStarted)
						{
							this.PreProcess();
							this.step++;
						}

						break;
					}
					case Step.InitializePlayback:
					{
						this.InitializePlayback();
						this.step++;
						break;
					}
					case Step.WaitForStart:
					{
						if(this.IsPlaybackInitialized())
						{
							this.step++;
						}

						break;
					}
					case Step.WaitForIamReady:
					{
						if(HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypePlay)
						{
							this.StartPlaybackPlay();
							this.step = Step.Playback; // TODO
						}

						if (this.receivedMessageMap[MsgIamReady])
						{
							this.StartPlaybackRecord();
							this.step++;
							break;
						}

						this.SendMessageAtIntervals(MsgAreYouReady, "", SendingAreYouReadyInterval);

						break;
					}
					case Step.TrialStart:
					{
						SIGVerseLogger.Info("Trial start!");
						this.RecordEvent("Trial_start");

						string textForGUI = "Task start!";
						base.StartCoroutine(this.ShowNoticeMessagePanel(textForGUI, 3.0f));
						base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar(textForGUI, 3.0f));

						//this.pubTaskInfo.SendROSMessage(this.taskInfoForRobot);
						//this.RecordEventROSMessageSent("TaskInfo", "");
						this.SendRosTaskInfoMessage(this.taskInfoForRobot);

						this.scoreManager.TaskStart();
						
						//this.DoorOpen();

						SIGVerseLogger.Info("Waiting for end of trial");

						this.isDuringTrial = true;
						this.step++;

						break;
					}
					case Step.WaitTrialFinished:
					{
						this.CheckHandInteraction(this.LeftHand);
						this.CheckHandInteraction(this.rightHand);

						break;
					}
					case Step.WaitNextTrial:
					{
						if(this.goNextTrial)
						{
							SIGVerseLogger.Info("Go to next trial");
							this.RecordEvent("Go_to_next_trial");

							//if (HumanNaviConfig.Instance.configInfo.playbackType != HumanNaviPlaybackParam.PlaybackTypePlay) // TODO: delete
							this.SendROSHumanNaviMessage(MsgGoToNextTrial, "");

							this.step = Step.Initialize;
						}

						break;
					}
					case Step.Playback:
					{
						break;
					}
				}
			}
			catch (Exception exception)
			{
				Debug.LogError(exception);
				SIGVerseLogger.Error(exception.Message);
				SIGVerseLogger.Error(exception.StackTrace);
				this.ApplicationQuitAfter1sec();
			}
		}

		private void ApplicationQuitAfter1sec()
		{
			Thread.Sleep(1000);
			Application.Quit();
		}


		private void PreProcess()
		{
			this.ResetFlags();

			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypePlay)
			{
				HumanNaviConfig.Instance.numberOfTrials = HumanNaviConfig.Instance.configInfo.playbackTrialNum;
			}
			else
			{
				HumanNaviConfig.Instance.InclementNumberOfTrials(HumanNaviConfig.Instance.configInfo.playbackType);
			}

			this.scoreManager.SetChallengeInfoText();
			this.scoreManager.ResetTimeText();

			this.ResetEnvironment();
			this.ResetAvatarTransform();
			this.ResetRobot();

			this.currentTaskInfo = taskInfoList[HumanNaviConfig.Instance.numberOfTrials - 1];

			this.taskInfoForRobot = new SIGVerse.RosBridge.human_navigation.HumanNaviTaskInfo();
			this.taskInfoForRobot.environment_id = this.environment.name;
			this.SetObjectListToHumanNaviTaskInfo();
			this.SetDestinationToHumanNaviTaskInfo();

			this.waitingTime = 0.0f;

			SIGVerseLogger.Info("End of PreProcess");
		}

		private void PostProcess()
		{
			if (HumanNaviConfig.Instance.numberOfTrials == HumanNaviConfig.Instance.configInfo.maxNumberOfTrials)
			{
				this.SendROSHumanNaviMessage(MsgMissionComplete, "");
				base.StartCoroutine(this.ShowNoticeMessagePanel("Mission complete", 5.0f));
				base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar("Mission complete", 5.0f));

				this.isAllTaskFinished = true;
			}
			else
			{
				this.SendROSHumanNaviMessage(MsgTaskFinished, "");

				SIGVerseLogger.Info("Waiting for next task");
				base.StartCoroutine(this.ShowGotoNextPanel(3.0f));
			}

			this.StopPlayback();

			this.isDuringTrial = false;
			this.step = Step.WaitNextTrial;
		}

		private void ResetFlags()
		{
			this.receivedMessageMap[MsgIamReady] = false;
			this.isTargetAlreadyGrasped = false;
			this.goNextTrial = false;
		}

		private void ResetAvatarTransform()
		{
			this.avatar.transform.position = this.initialAvatarPosition;
			this.avatar.transform.eulerAngles = this.initialAvatarRotation;
			this.cameraRig.transform.localPosition = Vector3.zero;
			this.cameraRig.transform.localRotation = Quaternion.identity;
			this.Ethan.transform.localPosition = Vector3.zero;
			this.Ethan.transform.localRotation = Quaternion.identity;
		}

		private void ResetRobot()
		{
			///// TODO
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypePlay)
			{
				return;
			}

			if (this.robot != null)
			{
				Destroy(this.robot);
			}
			this.robot = MonoBehaviour.Instantiate(this.robotPrefab);
			this.robot.name = this.robotPrefab.name;
			this.robot.SetActive(true);
		}

		private void SetDefaultEnvironment()
		{
			if (this.environment != null)
			{
				Destroy(this.environment);
			}
			this.environment = MonoBehaviour.Instantiate(this.environmentPrefabs.Where(obj => obj.name == "Default_Environment").SingleOrDefault());
			this.environment.name = "Default_Environment";
			this.environment.SetActive(true);
		}

		private void ResetEnvironment()
		{
			if (this.environment != null)
			{
				Destroy(this.environment);
			}
			this.environment = MonoBehaviour.Instantiate(this.environmentPrefabs.Where(obj => obj.name == this.taskInfoList[HumanNaviConfig.Instance.numberOfTrials - 1].environment).SingleOrDefault());
			this.environment.name = this.environmentPrefabs.Where(obj => obj.name == this.taskInfoList[HumanNaviConfig.Instance.numberOfTrials - 1].environment).SingleOrDefault().name;
			this.environment.SetActive(true);
		}

		private void SetObjectListToHumanNaviTaskInfo()
		{
			// Get grasping candidates
			List<GameObject> graspableObjects = GameObject.FindGameObjectsWithTag("Graspables").ToList<GameObject>();
			if (graspableObjects.Count == 0)
			{
				throw new Exception("Graspable object is not found.");
			}

			foreach (GameObject graspableObject in graspableObjects)
			{
				// transtrate the coordinate system of GameObject (left-handed, Z-axis:front, Y-axis:up) to ROS coodinate system (right-handed, X-axis:front, Z-axis:up)
				Vector3 positionInROS = this.ConvertCoorinateSystemUnityToROS_Position(graspableObject.transform.position);

				if (graspableObject.name == currentTaskInfo.target)
				{
					taskInfoForRobot.target_object.name = graspableObject.name;
					taskInfoForRobot.target_object.position = positionInROS;
				}
				else
				{
					SIGVerse.RosBridge.human_navigation.HumanNaviObjectInfo objInfo = new SIGVerse.RosBridge.human_navigation.HumanNaviObjectInfo
					{
						name = graspableObject.name,
						position = positionInROS
					};

					taskInfoForRobot.objects_info.Add(objInfo);

					SIGVerseLogger.Info("Object : " + objInfo.name + " " + objInfo.position);
				}
			}
			SIGVerseLogger.Info("Target object : " + taskInfoForRobot.target_object.name + " " + taskInfoForRobot.target_object.position);

			if (taskInfoForRobot.target_object.name == "")
			{
				throw new Exception("Target object is not found.");
			}
		}

		private void SetDestinationToHumanNaviTaskInfo()
		{
			List<GameObject> destinations = GameObject.FindGameObjectsWithTag("Destination").ToList<GameObject>();
			if (destinations.Count == 0)
			{
				throw new Exception("Destination candidate is not found.");
			}

			foreach (GameObject destination in destinations)
			{
				if (destination.name == this.currentTaskInfo.destination)
				{
					Vector3 conterOfCollider = destination.GetComponent<BoxCollider>().center;
					taskInfoForRobot.destination = this.ConvertCoorinateSystemUnityToROS_Position(destination.transform.position + conterOfCollider);
				}
			}
			SIGVerseLogger.Info("Destination : " + taskInfoForRobot.destination);

			//if (taskInfoForRobot.destination == null)
			//{
			//	throw new Exception("Destination is not found.");
			//}
		}

		private void SendMessageAtIntervals(string message, string detail, int interval_ms = 1000)
		{
			this.waitingTime += UnityEngine.Time.deltaTime;

			if (this.waitingTime > interval_ms * 0.001)
			{
				this.SendROSHumanNaviMessage(MsgAreYouReady, "");
				this.waitingTime = 0.0f;
			}
		}

//		private void DoorOpen()
//		{
//			GameObject doorWay = GameObject.Find("doorway"); // TODO: should be modified
//			doorWay.transform.localPosition += doorWay.transform.right * (-0.9f);
////			this.addCommandLog("Info", "door_open");
//		}

		public void TimeIsUp()
		{
			string strTimeup = "Time is up";
			this.SendROSHumanNaviMessage(MsgTaskFailed, strTimeup);
			base.StartCoroutine(this.ShowNoticeMessagePanel(strTimeup, 3.0f));
			base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar(strTimeup, 3.0f));

			this.RecordEvent("Time_is_up");

			this.TaskFinished();
		}

		public void GiveUp()
		{
			string strGiveup = "Give up";
			this.SendROSHumanNaviMessage(MsgTaskFailed, strGiveup);
			base.StartCoroutine(this.ShowNoticeMessagePanel(strGiveup, 3.0f));
			base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar(strGiveup, 3.0f));

			this.RecordEvent("Give_up");

			this.TaskFinished();
		}

		public void StartTask()
		{
			SIGVerseLogger.Info("Task start!");
			this.RecordEvent("Task_start");

			this.humanNaviMenu.startTaskPanel.SetActive(false);
			this.isCompetitionStarted = true;
		}

		public void GoToNextTrial()
		{
			this.humanNaviMenu.goToNextTrialPanel.SetActive(false);
			this.goNextTrial = true;
		}

		private IEnumerator ShowGotoNextPanel(float waitTime = 1.0f)
		{
			yield return new WaitForSeconds(waitTime);
			this.humanNaviMenu.ShowGoToNextPanel();
			base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar("Waiting for the next trial to start", 3.0f));
		}

		private void TaskFinished()
		{
			this.scoreManager.TaskEnd();
			this.PostProcess();
		}

		public void OnReceiveROSMessage(RosBridge.human_navigation.HumanNaviMsg humanNaviMsg)
		{
			this.RecordEventROSMessageReceived("HumanNaviMsg", humanNaviMsg.message + "\t" + humanNaviMsg.detail);

			if (this.receivedMessageMap.ContainsKey(humanNaviMsg.message))
			{
				this.receivedMessageMap[humanNaviMsg.message] = true;

				if(humanNaviMsg.message == MsgGetAvatarPose)
				{
					RosBridge.human_navigation.HumanNaviAvatarPose avatarPose = new RosBridge.human_navigation.HumanNaviAvatarPose();

					avatarPose.head.position = ConvertCoorinateSystemUnityToROS_Position(this.head.transform.position);
					avatarPose.head.orientation = ConvertCoorinateSystemUnityToROS_Rotation(this.head.transform.rotation);
					avatarPose.left_hand.position = ConvertCoorinateSystemUnityToROS_Position(this.LeftHand.transform.position);
					avatarPose.left_hand.orientation = ConvertCoorinateSystemUnityToROS_Rotation(this.LeftHand.transform.rotation);
					avatarPose.right_hand.position = ConvertCoorinateSystemUnityToROS_Position(this.rightHand.transform.position);
					avatarPose.right_hand.orientation = ConvertCoorinateSystemUnityToROS_Rotation(this.rightHand.transform.rotation);

					//this.pubAvatarPose.SendROSMessage(avatarPose);
					this.SendRosAvatarPoseMessage(avatarPose);
				}
				else if (humanNaviMsg.message == MsgConfirmSpeechState)
				{
					if (this.robot != null && this.isDuringTrial)
					{
						string detail;
						if (this.robot.transform.Find("CompetitionScripts").GetComponent<SAPIVoiceSynthesis>().IsSpeeching()) { detail = "Is_speaking"; }
						else                                                                                                  { detail = "Is_not_speaking"; }
						this.SendROSHumanNaviMessage(MsgSpeechState, detail);
					}
				}
			}
			else
			{
				SIGVerseLogger.Warn("Received Illegal message : " + humanNaviMsg.message);
			}
		}

		private Vector3 ConvertCoorinateSystemUnityToROS_Position(Vector3 unityPosition)
		{
			return new Vector3(unityPosition.z, -unityPosition.x, unityPosition.y);
		}
		private Quaternion ConvertCoorinateSystemUnityToROS_Rotation(Quaternion unityQuaternion)
		{
			return new Quaternion(-unityQuaternion.z, unityQuaternion.x, -unityQuaternion.y, unityQuaternion.w);
		}

		private void InitializePlayback()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypeRecord)
			{
				this.playbackTransformRecorder.SetTargetTransforms();
				this.playbackTransformRecorder.Initialize(HumanNaviConfig.Instance.numberOfTrials);

				this.playbackEventRecorder.Initialize(HumanNaviConfig.Instance.numberOfTrials);
			}
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypePlay)
			{
				this.playbackTransformPlayer.Initialize(HumanNaviConfig.Instance.configInfo.playbackTrialNum);
			}
		}

		private bool IsPlaybackInitialized()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypeRecord)
			{
				///// TODO

				//if (!this.playbackTransformRecorder.IsInitialized()) { return false; }
				//if (!this.playbackEventRecorder.IsInitialized())     { return false; }
			}
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypePlay)
			{
				//if (!this.playbackTransformPlayer.IsInitialized()) { return false; }
				return true; // TODO
			}

			return true;
		}

		public void StartPlaybackRecord()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypeRecord)
			{
				bool isStarted = false;
				isStarted |= this.playbackTransformRecorder.Record();
				isStarted |= this.playbackEventRecorder.Record();

				if (!isStarted) { SIGVerseLogger.Warn("Cannot start the world playback recording"); }
			}
		}

		public void StartPlaybackPlay()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypePlay)
			{
				bool isStarted = this.playbackTransformPlayer.Play();

				if (!isStarted) { SIGVerseLogger.Warn("Cannot start the world playback playing"); }
			}
		}

		public void StopPlayback()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypeRecord)
			{
				bool isStopped = false;
				isStopped |= this.playbackTransformRecorder.Stop();
				isStopped |= this.playbackEventRecorder.Stop();

				if (!isStopped) { SIGVerseLogger.Warn("Cannot stop the world playback recording"); }
			}
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypePlay)
			{
				bool isStopped = this.playbackTransformPlayer.Stop();

				if (!isStopped) { SIGVerseLogger.Warn("Cannot stop the world playback playing"); }
			}
		}

		private void RecordEventObjectGrasped(string objectName, string whichHandUsed)
		{
			ExecuteEvents.Execute<IEventRecoderHandler>
			(
				target: this.playbackSystem,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnObjectGrasped(objectName, whichHandUsed)
			);
		}

		private void RecordEvent(string detail)
		{
			ExecuteEvents.Execute<IEventRecoderHandler>
			(
				target: this.playbackSystem,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnEventOccured(detail)
			);
		}

		private void RecordEventRosMessageSent(string messageType, string message)
		{
			ExecuteEvents.Execute<IEventRecoderHandler>
			(
				target: this.playbackSystem,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnROSMessageSent(messageType, message)
			);
		}

		private void RecordEventROSMessageReceived(string messageType, string message)
		{
			ExecuteEvents.Execute<IEventRecoderHandler>
			(
				target: this.playbackSystem,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnROSMessageReceived(messageType, message)
			);
		}



		//private void StartRecording()
		//{
		//	if (HumanNaviConfig.Instance.configInfo.playbackType == HumanNaviPlaybackParam.PlaybackTypeRecord)
		//	{
		//		ExecuteEvents.Execute<IPlaybackDataHandler>
		//		(
		//			target: this.worldRecorder,
		//			eventData: null,
		//			functor: (reciever, eventData) => reciever.OnRecord(HumanNaviConfig.Instance.numberOfTrials)
		//		);
		//	}
		//}

		//private void StopRecording()
		//{
		//	if (HumanNaviConfig.Instance.configInfo.playbackType == HumanNaviPlaybackParam.PlaybackTypeRecord)
		//	{
		//		ExecuteEvents.Execute<IPlaybackDataHandler>
		//		(
		//			target: this.worldRecorder,
		//			eventData: null,
		//			functor: (reciever, eventData) => reciever.OnStop()
		//		);
		//	}
		//}

		//private void StartPlaying()
		//{
		//	if (HumanNaviConfig.Instance.configInfo.playbackType == HumanNaviPlaybackParam.PlaybackTypePlay)
		//	{
		//		ExecuteEvents.Execute<IPlaybackPlayerDataHandler>
		//		(
		//			target: this.worldRecorder,
		//			eventData: null,
		//			functor: (reciever, eventData) => reciever.OnPlay()
		//			//functor: (reciever, eventData) => reciever.OnPlay(HumanNaviConfig.Instance.numberOfTrials)
		//		);

		//		this.step = Step.WaitForTaskFinished;
		//	}
		//}

		//private void StopPlaying()
		//{
		//	if (HumanNaviConfig.Instance.configInfo.playbackType == HumanNaviPlaybackParam.PlaybackTypePlay)
		//	{
		//		ExecuteEvents.Execute<IPlaybackPlayerDataHandler>
		//		(
		//			target: this.worldRecorder,
		//			eventData: null,
		//			functor: (reciever, eventData) => reciever.OnStop()
		//		);
		//	}

		//	this.step = Step.WaitForNextTask;
		//}

		//private void addCommandLog(string name, string data)
		//{
		//	if (HumanNaviConfig.Instance.configInfo.playbackType == HumanNaviPlaybackParam.PlaybackTypeRecord)
		//	{
		//		ExecuteEvents.Execute<IPlaybackDataHandler>
		//		(
		//			target: this.worldRecorder,
		//			eventData: null,
		//			functor: (reciever, eventData) => reciever.addEventLog(name, data)
		//		);
		//	}
		//}


		private IEnumerator ShowNoticeMessagePanel(string text, float waitTime = 1.0f)
		{
			this.humanNaviMenu.noticeText.text = text;
			this.humanNaviMenu.noticePanel.SetActive(true);

			yield return new WaitForSeconds(waitTime);

			this.humanNaviMenu.noticePanel.SetActive(false);
		}

		private IEnumerator ShowNoticeMessagePanelForAvatar(string text, float waitTime = 1.0f)
		{
			this.noticeTextForAvatar.text = text;
			this.noticePanelForAvatar.SetActive(true);

			yield return new WaitForSeconds(waitTime);

			this.noticePanelForAvatar.SetActive(false);
		}

		private void SendROSHumanNaviMessage(string message, string detail)
		{
			this.RecordEventRosMessageSent("HumanNaviMsg", message + "\t" + detail);

			ExecuteEvents.Execute<IRosHumanNaviMessageSendHandler>
			(
				target: this.gameObject, 
				eventData: null, 
				functor: (reciever, eventData) => reciever.OnSendROSHumanNaviMessage(message, detail)
			);
		}

		private void SendRosAvatarPoseMessage(RosBridge.human_navigation.HumanNaviAvatarPose avatarPose)
		{
			this.RecordEventRosMessageSent("AvatarPose", "");

			ExecuteEvents.Execute<IROSAvatarPoseSendHandler>
			(
				target: this.gameObject,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnSendROSAvatarPoseMessage(avatarPose)
			);
		}

		private void SendRosTaskInfoMessage(RosBridge.human_navigation.HumanNaviTaskInfo taskInfo)
		{
			this.RecordEventRosMessageSent("TaskInfo", "");

			ExecuteEvents.Execute<IRosTaskInfoSendHandler>
			(
				target: this.gameObject,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnSendRosTaskInfoMessage(taskInfo)
			);
		}

		private void CheckHandInteraction(NewtonVR.NVRHand hand)
		{
			if (hand.IsInteracting)
			{
				if (hand.HoldButtonDown)
				{
					if (hand.CurrentlyInteracting.tag == "Graspables")
					{
						//this.addEventLog("Interaction", hand.name + ".IsGrasping: " + hand.CurrentlyInteracting + hand.CurrentlyInteracting.transform.position);

						string whichHandUsed = string.Empty;
						if      (hand.IsLeft)  { whichHandUsed = "LeftHand";  }
						else if (hand.IsRight) { whichHandUsed = "RightHand"; }

						this.RecordEventObjectGrasped(whichHandUsed, this.currentTaskInfo.target);


						if (hand.CurrentlyInteracting.name == this.currentTaskInfo.target)
						{
							if (!this.isTargetAlreadyGrasped)
							{
								SIGVerseLogger.Info("Target object is grasped");
								this.RecordEvent("Target_object_is_grasped");

								base.StartCoroutine(this.ShowNoticeMessagePanel("Target object is grasped", 3.0f));

								this.scoreManager.AddScore(Score.Type.CorrectObjectIsGrasped);
								this.scoreManager.AddTimeScoreOfGrasp();

								this.isTargetAlreadyGrasped = true;
							}
						}
						else
						{
							if (!this.isTargetAlreadyGrasped)
							{
								SIGVerseLogger.Info("Wrong object is grasped");
								this.RecordEvent("Wrong_object_is_grasped");

								base.StartCoroutine(this.ShowNoticeMessagePanel("Wrong object is grasped", 3.0f));

								this.scoreManager.AddScore(Score.Type.IncorrectObjectIsGrasped);
							}
						}
					}
				}
			}
			else if (hand.HoldButtonDown)
			{
//				this.addEventLog("Interaction", hand.name + ".HoldButtonDown");
			}
			else if (hand.IsHovering)
			{
//				this.addEventLog("Interaction", hand.name + ".IsHovering");
			}
			else
			{
//				this.addEventLog("Interaction", hand.name + ".Nothing");
			}

		}


		public string GetTargetObjectName()
		{
			return this.currentTaskInfo.target;
		}

		public string GetDestinationName()
		{
			return this.currentTaskInfo.destination;
		}

		public bool IsTargetAlreadyGrasped()
		{
			return this.isTargetAlreadyGrasped;
		}

		public void TargetPlacedOnDestination()
		{
			SIGVerseLogger.Info("Target is plasced on the destination.");
			this.RecordEvent("Target_object_placed_on_the_destination");

			this.scoreManager.AddScore(Score.Type.TargetObjectInDestination);
			this.scoreManager.AddTimeScoreOfPlacement();

			this.SendROSHumanNaviMessage(MsgTaskSucceeded, "");
			base.StartCoroutine(this.ShowNoticeMessagePanel("Task succeeded", 3.0f));
			base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar("Task succeeded", 3.0f));

			this.TaskFinished();
		}
	}
}