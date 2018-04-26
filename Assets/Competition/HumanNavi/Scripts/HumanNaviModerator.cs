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

namespace SIGVerse.Competition.HumanNavigation
{
	public class HumanNaviModerator : MonoBehaviour, ITimeIsUpHandler, IStartTrialHandler, IGoToNextTrialHandler//, IGiveUpHandler
	{
		private const int SendingAreYouReadyInterval = 1000;

		private const string MsgAreYouReady     = "Are_you_ready?";
		private const string MsgTaskSucceeded   = "Task_succeeded";
		private const string MsgTaskFailed      = "Task_failed";
		private const string MsgTaskFinished    = "Task_finished";
		private const string MsgGoToNextTrial   = "Go_to_next_trial";
		private const string MsgMissionComplete = "Mission_complete";

		private const string ReasonTimeIsUp = "Time_is_up";
		private const string ReasonGiveUp = "Give_up";

		private const string MsgIamReady           = "I_am_ready";
		private const string MsgGetAvatarPose      = "Get_avatar_pose";
		private const string MsgConfirmSpeechState = "Confirm_speech_state";
		private const string MsgGiveUp             = "Give_up";

		private const string MsgRequest     = "Guidance_request";
		private const string MsgSpeechState = "Speech_state";

		private enum Step
		{
			Initialize,
			InitializePlayback,
			WaitForStart,
			TrialStart,
			WaitForIamReady,
			SendTaskInfo,
			WaitForEndOfSession,
			WaitForNextTrial,
		};

		//-----------------------------

		[HeaderAttribute("Score Manager")]
		public HumanNaviScoreManager scoreManager;

		[HeaderAttribute("Session Manager")]
		public HumanNaviSessionManager sessionManager;

		[HeaderAttribute("Avatar")]
		public GameObject avatar;
		public GameObject cameraRig;
		public GameObject Ethan;
		public GameObject head;
		public NewtonVR.NVRHand LeftHand;
		public NewtonVR.NVRHand rightHand;

		[HeaderAttribute("Panels for avatar")]
		public GameObject noticePanelForAvatar;
		public UnityEngine.UI.Text noticeTextForAvatar;

		[HeaderAttribute("Menu")]
		public Camera birdviewCamera;
		public GameObject startTrialPanel;
		public GameObject goToNextTrialPanel;


		[HeaderAttribute("ROS Message")]
		public HumanNaviPubMessage pubHumanNaviMessage;
		public HumanNaviPubTaskInfo pubTaskInfo;
		public HumanNaviPubAvatarPose pubAvatarPose;

		[HeaderAttribute("Scenario Logger")]
		public GameObject playbackManager;

		//-----------------------------

		private Vector3 initialAvatarPosition;
		private Vector3 initialAvatarRotation;

		private GameObject mainMenu;
		private PanelMainController panelMainController;

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
		private string interruptedReason;

		private StepTimer stepTimer;

		private HumanNaviPlaybackRecorder playbackRecorder;

		//-----------------------------

		private IRosConnection[] rosConnections;


		void Awake()
		{
			try
			{
				// Playback system
				this.playbackRecorder = this.playbackManager.GetComponent<HumanNaviPlaybackRecorder>();

				// Avatar 
				this.initialAvatarPosition = this.avatar.transform.position;
				this.initialAvatarRotation = this.avatar.transform.eulerAngles;

				// GUI
				this.mainMenu = GameObject.FindGameObjectWithTag("MainMenu");
				this.panelMainController = mainMenu.GetComponent<PanelMainController>();

				this.noticePanelForAvatar.SetActive(false);
				this.noticeTextForAvatar.text = "";

				// MessageMap
				this.receivedMessageMap = new Dictionary<string, bool>();
				this.receivedMessageMap.Add(MsgIamReady, false);
				this.receivedMessageMap.Add(MsgGetAvatarPose, false);
				this.receivedMessageMap.Add(MsgConfirmSpeechState, false);
				this.receivedMessageMap.Add(MsgGiveUp, false);

				// ROSBridge
				// (Should be read after the robot is instantiated (after Awake process of SessionManager))
				this.rosConnections = SIGVerseUtils.FindObjectsOfInterface<IRosConnection>();
				SIGVerseLogger.Info("ROS connection : count=" + this.rosConnections.Length);

				// Timer
				this.stepTimer = new StepTimer();
			}
			catch (Exception exception)
			{
				Debug.LogError(exception);
				SIGVerseLogger.Error(exception.Message);
				SIGVerseLogger.Error(exception.StackTrace);
				this.ApplicationQuitAfter1sec();
			}
		}

		void Start()
		{
			this.step = Step.Initialize;

			this.ShowStartTaskPanel();

			this.interruptedReason = string.Empty;
		}

		//void OnDestroy()
		//{
		//	this.CloseRosConnections();
		//}

		// Update is called once per frame
		void Update()
		{
			try
			{
				if (this.isAllTaskFinished){ 
					return;
				}

				if (this.interruptedReason != string.Empty && this.step != Step.WaitForNextTrial)
				{
					SIGVerseLogger.Info("Failed '" + this.interruptedReason + "'");
					this.SendPanelNotice("Failed\n" + interruptedReason.Replace('_', ' '), 100, PanelNoticeStatus.Red);
					this.TimeIsUp();
				}

				if (OVRInput.GetDown(OVRInput.RawButton.X) && this.isDuringTrial && HumanNaviConfig.Instance.configInfo.playbackType != WorldPlaybackCommon.PlaybackTypePlay)
				{
					this.SendRosHumanNaviMessage(MsgRequest, "");
					//this.RecordEvent("Guidance_message_is_requested");
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
					case Step.InitializePlayback: // Need to wait for an update to finish instantiating objects to record
					{
						this.InitializePlayback();
						this.step++;
						break;
					}
					case Step.WaitForStart:
					{
						if (this.stepTimer.IsTimePassed((int)this.step, 3000))
						{
							if (this.IsPlaybackInitialized() && this.IsConnectedToRos())
							{
								this.step++;
							}
						}

						break;
					}
					case Step.TrialStart:
					{
						SIGVerseLogger.Info("Trial start!");
						//this.RecordEvent("Trial_start");

						this.scoreManager.TaskStart();

						this.SendPanelNotice("Trial start!", 100, PanelNoticeStatus.Green);
						base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar("Trial start!", 3.0f));

						this.isDuringTrial = true;
						this.step++;

						break;
					}
					case Step.WaitForIamReady:
					{
						if (this.receivedMessageMap[MsgIamReady])
						{
							this.StartPlaybackRecord();
							this.step++;
							break;
						}

						this.SendMessageAtIntervals(MsgAreYouReady, "", SendingAreYouReadyInterval);

						break;
					}
					case Step.SendTaskInfo:
					{
						//this.RecordEventROSMessageSent("TaskInfo", "");
						this.SendRosTaskInfoMessage(this.taskInfoForRobot);

						SIGVerseLogger.Info("Waiting for end of trial");

						this.step++;

						break;
					}
					case Step.WaitForEndOfSession:
					{
						this.CheckHandInteraction(this.LeftHand);
						this.CheckHandInteraction(this.rightHand);

						break;
					}
					case Step.WaitForNextTrial:
					{
						if(this.goNextTrial)
						{
							SIGVerseLogger.Info("Go to next trial");
							//this.RecordEvent("Go_to_next_trial");

							this.SendRosHumanNaviMessage(MsgGoToNextTrial, "");

							this.step = Step.Initialize;
						}

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

		//-----------------------------

		private void ApplicationQuitAfter1sec()
		{
			this.CloseRosConnections();

			Thread.Sleep(1000);
			Application.Quit();
		}

		//-----------------------------

		private void PreProcess()
		{
			this.ResetFlags();

			HumanNaviConfig.Instance.InclementNumberOfTrials(HumanNaviConfig.Instance.configInfo.playbackType);

			this.panelMainController.SetTrialNumberText(HumanNaviConfig.Instance.numberOfTrials);
			SIGVerseLogger.Info("##### " + this.panelMainController.GetTrialNumberText() + " #####");

			this.panelMainController.SetTaskMessageText("");

			this.scoreManager.ResetTimeLeftText();

			this.sessionManager.ResetEnvironment();
			this.ResetAvatarTransform();

			this.ClearRosConnections();
			this.sessionManager.ResetRobot();

			this.currentTaskInfo = this.sessionManager.GetCurrentTaskInfo();

			this.taskInfoForRobot = new SIGVerse.RosBridge.human_navigation.HumanNaviTaskInfo();
			string currentEnvironmentName = this.sessionManager.GetCurrentEnvironment().name;
			this.taskInfoForRobot.environment_id = currentEnvironmentName.Substring(0, currentEnvironmentName.Length - 3);
			this.SetObjectListToHumanNaviTaskInfo();
			this.SetDestinationToHumanNaviTaskInfo();

			this.waitingTime = 0.0f;

			this.interruptedReason = string.Empty;

			SIGVerseLogger.Info("End of PreProcess");
		}

		private void PostProcess()
		{
			if (HumanNaviConfig.Instance.numberOfTrials == HumanNaviConfig.Instance.configInfo.maxNumberOfTrials)
			{
				this.SendRosHumanNaviMessage(MsgMissionComplete, "");
				this.SendPanelNotice("Mission complete", 100, PanelNoticeStatus.Green);
				base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar("Mission complete", 5.0f));

				this.CloseRosConnections();

				this.isAllTaskFinished = true;
			}
			else
			{
				this.SendRosHumanNaviMessage(MsgTaskFinished, "");

				SIGVerseLogger.Info("Waiting for next task");
				base.StartCoroutine(this.ShowGotoNextPanel(3.0f));
			}

			this.StopPlaybackRecord();

			this.isDuringTrial = false;
			this.interruptedReason = string.Empty;

			this.step = Step.WaitForNextTrial;
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
					taskInfoForRobot.target_object.name = graspableObject.name.Substring(0, graspableObject.name.Length - 3);
					taskInfoForRobot.target_object.position = positionInROS;
				}
				else
				{
					SIGVerse.RosBridge.human_navigation.HumanNaviObjectInfo objInfo = new SIGVerse.RosBridge.human_navigation.HumanNaviObjectInfo
					{
						name = graspableObject.name.Substring(0, graspableObject.name.Length - 3),
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
				this.SendRosHumanNaviMessage(MsgAreYouReady, "");
				this.waitingTime = 0.0f;
			}
		}

		public void TimeIsUp()
		{
			string strTimeup = "Time_is_up";
			this.SendRosHumanNaviMessage(MsgTaskFailed, strTimeup);
			this.SendPanelNotice(strTimeup, 100, PanelNoticeStatus.Red);
			base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar(strTimeup, 3.0f));

			//this.RecordEvent("Time_is_up");

			this.TaskFinished();
		}

		private IEnumerator ShowGotoNextPanel(float waitTime = 1.0f)
		{
			yield return new WaitForSeconds(waitTime);
			this.goToNextTrialPanel.SetActive(true);
			base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar("Waiting for the next trial to start", 3.0f));
		}

		private void TaskFinished()
		{
			this.scoreManager.TaskEnd();
			this.PostProcess();
		}

		public void OnReceiveROSMessage(RosBridge.human_navigation.HumanNaviMsg humanNaviMsg)
		{
			//this.RecordEventROSMessageReceived("HumanNaviMsg", humanNaviMsg.message + "\t" + humanNaviMsg.detail);

			if (this.receivedMessageMap.ContainsKey(humanNaviMsg.message))
			{
				this.receivedMessageMap[humanNaviMsg.message] = true;

				if (humanNaviMsg.message == MsgGiveUp)
				{
					this.OnGiveUp();
				}
				else if(humanNaviMsg.message == MsgGetAvatarPose)
				{
					RosBridge.human_navigation.HumanNaviAvatarPose avatarPose = new RosBridge.human_navigation.HumanNaviAvatarPose();

					avatarPose.head.position = ConvertCoorinateSystemUnityToROS_Position(this.head.transform.position);
					avatarPose.head.orientation = ConvertCoorinateSystemUnityToROS_Rotation(this.head.transform.rotation);
					avatarPose.left_hand.position = ConvertCoorinateSystemUnityToROS_Position(this.LeftHand.transform.position);
					avatarPose.left_hand.orientation = ConvertCoorinateSystemUnityToROS_Rotation(this.LeftHand.transform.rotation);
					avatarPose.right_hand.position = ConvertCoorinateSystemUnityToROS_Position(this.rightHand.transform.position);
					avatarPose.right_hand.orientation = ConvertCoorinateSystemUnityToROS_Rotation(this.rightHand.transform.rotation);

					this.SendRosAvatarPoseMessage(avatarPose);
				}
				else if (humanNaviMsg.message == MsgConfirmSpeechState)
				{
					if (this.isDuringTrial)
					{
						this.SendRosHumanNaviMessage(MsgSpeechState, this.sessionManager.GetSeechRunState());
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
				this.playbackRecorder.SetPlaybackTargets();
				this.playbackRecorder.Initialize(HumanNaviConfig.Instance.numberOfTrials);
				//this.playbackTransformRecorder.SetTargetTransforms();
				//this.playbackTransformRecorder.Initialize(HumanNaviConfig.Instance.numberOfTrials);

				//this.playbackEventRecorder.Initialize(HumanNaviConfig.Instance.numberOfTrials);
			}
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypePlay)
			{
				//this.playbackTransformPlayer.Initialize(HumanNaviConfig.Instance.configInfo.playbackTrialNum);
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
				//bool isStarted = false;
				bool isStarted = this.playbackRecorder.Record();
				//isStarted |= this.playbackTransformRecorder.Record();
				//isStarted |= this.playbackEventRecorder.Record();

				if (!isStarted) { SIGVerseLogger.Warn("Cannot start the world playback recording"); }
			}
		}

		public void StopPlaybackRecord()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypeRecord)
			{
				//bool isStopped = false;
				bool isStopped = this.playbackRecorder.Stop();
				//isStopped |= this.playbackTransformRecorder.Stop();
				//isStopped |= this.playbackEventRecorder.Stop();

				if (!isStopped) { SIGVerseLogger.Warn("Cannot stop the world playback recording"); }
			}
		}

		//private void RecordEventObjectGrasped(string objectName, string whichHandUsed)
		//{
		//	ExecuteEvents.Execute<IHumanNaviRecoderHandler>
		//	(
		//		target: this.playbackManager,
		//		eventData: null,
		//		functor: (reciever, eventData) => reciever.OnObjectGrasped(objectName, whichHandUsed)
		//	);
		//}

		//private void RecordEvent(string detail)
		//{
		//	ExecuteEvents.Execute<IEventRecoderHandler>
		//	(
		//		target: this.playbackManager,
		//		eventData: null,
		//		functor: (reciever, eventData) => reciever.OnEventOccured(detail)
		//	);
		//}

		//private void RecordEventRosMessageSent(string messageType, string message)
		//{
		//	ExecuteEvents.Execute<IEventRecoderHandler>
		//	(
		//		target: this.playbackSystem,
		//		eventData: null,
		//		functor: (reciever, eventData) => reciever.OnROSMessageSent(messageType, message)
		//	);
		//}

		//private void RecordEventROSMessageReceived(string messageType, string message)
		//{
		//	ExecuteEvents.Execute<IEventRecoderHandler>
		//	(
		//		target: this.playbackSystem,
		//		eventData: null,
		//		functor: (reciever, eventData) => reciever.OnROSMessageReceived(messageType, message)
		//	);
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

		private IEnumerator ShowNoticeMessagePanelForAvatar(string text, float waitTime = 1.0f)
		{
			this.noticeTextForAvatar.text = text;
			this.noticePanelForAvatar.SetActive(true);

			yield return new WaitForSeconds(waitTime);

			this.noticePanelForAvatar.SetActive(false);
		}

		private void SendRosHumanNaviMessage(string message, string detail)
		{
			//this.RecordEventRosMessageSent("HumanNaviMsg", message + "\t" + detail);

			ExecuteEvents.Execute<IRosHumanNaviMessageSendHandler>
			(
				target: this.gameObject, 
				eventData: null, 
				functor: (reciever, eventData) => reciever.OnSendRosHumanNaviMessage(message, detail)
			);
		}

		private void SendRosAvatarPoseMessage(RosBridge.human_navigation.HumanNaviAvatarPose avatarPose)
		{
			//this.RecordEventRosMessageSent("AvatarPose", "");

			ExecuteEvents.Execute<IROSAvatarPoseSendHandler>
			(
				target: this.gameObject,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnSendROSAvatarPoseMessage(avatarPose)
			);
		}

		private void SendRosTaskInfoMessage(RosBridge.human_navigation.HumanNaviTaskInfo taskInfo)
		{
			//this.RecordEventRosMessageSent("TaskInfo", "");

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

						//this.RecordEventObjectGrasped(whichHandUsed, this.currentTaskInfo.target);

						if (hand.CurrentlyInteracting.name == this.currentTaskInfo.target)
						{
							if (!this.isTargetAlreadyGrasped)
							{
								SIGVerseLogger.Info("Target object is grasped");
								//this.RecordEvent("Target_object_is_grasped");

								this.SendPanelNotice("Target object is grasped", 100, PanelNoticeStatus.Green);

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
								//this.RecordEvent("Wrong_object_is_grasped");

								this.SendPanelNotice("Wrong object is grasped", 100, PanelNoticeStatus.Red);

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
			//this.RecordEvent("Target_object_placed_on_the_destination");

			this.scoreManager.AddScore(Score.Type.TargetObjectInDestination);
			this.scoreManager.AddTimeScoreOfPlacement();

			this.SendRosHumanNaviMessage(MsgTaskSucceeded, "");
			this.SendPanelNotice("Task succeeded", 100, PanelNoticeStatus.Green);
			base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar("Task succeeded", 3.0f));

			this.TaskFinished();
		}

		public void OnTimeIsUp()
		{
			this.interruptedReason = HumanNaviModerator.ReasonTimeIsUp;
		}

		public void OnGiveUp()
		{
			if (this.isDuringTrial)
			{
				this.interruptedReason = HumanNaviModerator.ReasonGiveUp;

				string strGiveup = "Give_up";
				this.SendRosHumanNaviMessage(MsgTaskFailed, strGiveup);
				this.SendPanelNotice(strGiveup, 100, PanelNoticeStatus.Red);
				base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar(strGiveup, 3.0f));

				//this.RecordEvent("Give_up");

				this.panelMainController.giveUpPanel.SetActive(false);


				this.TaskFinished();
			}
			else
			{
				SIGVerseLogger.Warn("It is a timing not allowed to give up.");
			}
		}

		public void OnStartTrial()
		{
			SIGVerseLogger.Info("Task start!");
			//this.RecordEvent("Task_start");

			this.startTrialPanel.SetActive(false);
			this.isCompetitionStarted = true;
		}

		public void OnGoToNextTrial()
		{
			this.goToNextTrialPanel.SetActive(false);
			this.goNextTrial = true;
		}

		public void ShowStartTaskPanel()
		{
			this.startTrialPanel.SetActive(true);
		}

		private void SendPanelNotice(string message, int fontSize, Color color)
		{
			PanelNoticeStatus noticeStatus = new PanelNoticeStatus(message, fontSize, color, 3.0f);

			// For changing the notice of a panel
			ExecuteEvents.Execute<IPanelNoticeHandler>
			(
				target: this.mainMenu,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnPanelNoticeChange(noticeStatus)
			);

			// For recording
			ExecuteEvents.Execute<IPanelNoticeHandler>
			(
				target: this.playbackManager,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnPanelNoticeChange(noticeStatus)
			);
		}


		public bool IsConnectedToRos()
		{
			foreach (IRosConnection rosConnection in this.rosConnections)
			{
				if (!rosConnection.IsConnected())
				{
					return false;
				}
			}
			return true;
		}

		public void ClearRosConnections()
		{
			foreach (IRosConnection rosConnection in this.rosConnections)
			{
				rosConnection.Clear();
			}

			SIGVerseLogger.Info("Clear ROS connections");
		}

		public void CloseRosConnections()
		{
			foreach (IRosConnection rosConnection in this.rosConnections)
			{
				rosConnection.Close();
			}

			SIGVerseLogger.Info("Close ROS connections");
		}


	}
}