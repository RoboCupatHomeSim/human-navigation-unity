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
using UnityEngine.SceneManagement;

using Valve.VR;
using Valve.VR.InteractionSystem;

namespace SIGVerse.Competition.HumanNavigation
{
	public interface IReachMaxWrongObjectGraspCountHandler : IEventSystemHandler
	{
		void OnReachMaxWrongObjectGraspCount();
	}


	public class HumanNaviModerator : MonoBehaviour, ITimeIsUpHandler, IStartTrialHandler, IGoToNextTrialHandler, IReceiveHumanNaviMsgHandler, ISendSpeechResultHandler, IReachMaxWrongObjectGraspCountHandler
	{
		private const int SendingAreYouReadyInterval = 1000;

		private const string MsgAreYouReady     = "Are_you_ready?";
		private const string MsgTaskSucceeded   = "Task_succeeded";
		private const string MsgTaskFailed      = "Task_failed";
		private const string MsgTaskFinished    = "Task_finished";
		private const string MsgGoToNextSession = "Go_to_next_session";
		private const string MsgMissionComplete = "Mission_complete";

		private const string ReasonTimeIsUp = "Time_is_up";
		private const string ReasonGiveUp   = "Give_up";
		private const string ReasonReachMaxWrongObjectGraspCount = "Reach_max_wrong_object_grasp_count";

		private const string MsgIamReady        = "I_am_ready";
		private const string MsgGetAvatarStatus = "Get_avatar_status";
		private const string MsgGetObjectStatus = "Get_object_status";
		private const string MsgGetSpeechState  = "Get_speech_state";
		private const string MsgGiveUp          = "Give_up";

		private const string MsgRequest      = "Guidance_request";
		private const string MsgSpeechState  = "Speech_state";
		private const string MsgSpeechResult = "Speech_result";

		private const string TagNameOfGraspables  = "Graspables";
		private const string TagNameOfFurniture   = "Furniture";
		private const string TagNameOfDestination = "Destination";

		private enum Step
		{
			Initialize,
			WaitForStart,
			SessionStart,
			WaitForIamReady,
			SendTaskInfo,
			WaitForEndOfSession,
			WaitForNextSession,
		};

		//-----------------------------

		[HeaderAttribute("Score Manager")]
		public HumanNaviScoreManager scoreManager;

		[HeaderAttribute("Session Manager")]
		public HumanNaviSessionManager sessionManager;

//		[HeaderAttribute("Avatar")]
//		public float heightThresholdForPoseReset = -0.5f;

		[HeaderAttribute("Avatar for SimpleIK")]
		public GameObject avatarForSimpleIK;
		public GameObject headForSimpleIK;
		public GameObject bodyForSimpleIK;
//		public NewtonVR.NVRHand LeftHandForSimpleIK;
//		public NewtonVR.NVRHand rightHandForSimpleIK;
		public Hand LeftHandForSimpleIK;
		public Hand rightHandForSimpleIK;
		public GameObject noticePanelForSimpleIKAvatar;
		public UnityEngine.UI.Text noticeTextForSimpleIKAvatar;

		[HeaderAttribute("Avatar for FinalIK")]
		public GameObject avatarForFinalIK;
		public GameObject headForFinalIK;
		public GameObject bodyForFinalIK;
//		public NewtonVR.NVRHand LeftHandForFinalIK;
//		public NewtonVR.NVRHand rightHandForFinalIK;
		public Hand LeftHandForFinalIK;
		public Hand rightHandForFinalIK;
		public GameObject noticePanelForFinalIKAvatar;
		public UnityEngine.UI.Text noticeTextForFinalIKAvatar;

		[HeaderAttribute("Menu")]
		public Camera birdviewCamera;
		public GameObject startTrialPanel;
		public GameObject goToNextTrialPanel;

		[HeaderAttribute("Scenario Logger")]
		public GameObject playbackManager;

		//-----------------------------

		private GameObject avatar;
		private GameObject head;
		private GameObject body;
//		private NewtonVR.NVRHand LeftHand;
//		private NewtonVR.NVRHand rightHand;
		private Hand LeftHand;
		private Hand rightHand;
		private GameObject noticePanelForAvatar;
		private UnityEngine.UI.Text noticeTextForAvatar;

		private Vector3 initialAvatarPosition;
		private Vector3 initialAvatarRotation;

		private GameObject mainMenu;
		private PanelMainController panelMainController;

		private SIGVerse.RosBridge.human_navigation.HumanNaviTaskInfo taskInfoForRobot;
		private HumanNavigation.TaskInfo currentTaskInfo;

		private Step step;

		private float waitingTime;

		private bool isCompetitionStarted = false;
		private bool isDuringSession = false;

		private Dictionary<string, bool> receivedMessageMap;
		private bool isTargetAlreadyGrasped = false;
		private bool isAllTaskFinished = false;
		private string interruptedReason;

		private StepTimer stepTimer;

		private HumanNaviPlaybackRecorder playbackRecorder;

		private Vector3 initialTargetObjectPosition;
		private Vector3 initialDestinationPosition;

		private string objectIdInLeftHand;
		private string objectIdInRightHand;

		private string objectIdInLeftHandPreviousFrame;
		private string objectIdInRightHandPreviousFrame;

		private List<string> alreadyGraspedObjects;

		private int numberOfSession;

		private List<GameObject> graspableObjects;

		private Dictionary<SteamVR_Input_Sources, GameObject> preAttachedObjectMap;

		//private int countWrongObjectsGrasp;

		//-----------------------------

		private IRosConnection[] rosConnections = new IRosConnection[] { };

		//-----------------------------

		private bool isPracticeMode = false;

		void Awake()
		{
			try
			{
#if ENABLE_VRIK
				this.avatar    = this.avatarForFinalIK;
				this.head      = this.headForFinalIK;
				this.body      = this.bodyForFinalIK;
				this.LeftHand  = this.LeftHandForFinalIK;
				this.rightHand = this.rightHandForFinalIK;
				this.noticePanelForAvatar = this.noticePanelForFinalIKAvatar;
				this.noticeTextForAvatar  = this.noticeTextForFinalIKAvatar;
#else
				this.avatar    = this.avatarForSimpleIK;
				this.head      = this.headForSimpleIK;
				this.body      = this.bodyForSimpleIK;
				this.LeftHand  = this.LeftHandForSimpleIK;
				this.rightHand = this.rightHandForSimpleIK;
				this.noticePanelForAvatar = this.noticePanelForSimpleIKAvatar;
				this.noticeTextForAvatar  = this.noticeTextForSimpleIKAvatar;
#endif
				// Practice mode
				if (HumanNaviConfig.Instance.configInfo.executionMode == (int)ExecutionMode.Practice)
				{
					this.isPracticeMode = true;
				}

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
				this.receivedMessageMap.Add(MsgGetAvatarStatus, false);
				this.receivedMessageMap.Add(MsgGetObjectStatus, false);
				this.receivedMessageMap.Add(MsgGetSpeechState, false);
				this.receivedMessageMap.Add(MsgGiveUp, false);

				this.preAttachedObjectMap = new Dictionary<SteamVR_Input_Sources, GameObject>();
				this.preAttachedObjectMap.Add(SteamVR_Input_Sources.LeftHand,  null);
				this.preAttachedObjectMap.Add(SteamVR_Input_Sources.RightHand, null);

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

			this.panelMainController.SetTeamNameText("Team: " + HumanNaviConfig.Instance.configInfo.teamName);

			this.ShowStartTaskPanel();

			this.InitializeMainPanel();

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
				if (this.isAllTaskFinished)
				{ 
					return;
				}

				if (this.interruptedReason != string.Empty && this.step != Step.WaitForNextSession)
				{
					SIGVerseLogger.Info("Failed '" + this.interruptedReason + "'");
					this.SendPanelNotice("Failed\n" + interruptedReason.Replace('_', ' '), 100, PanelNoticeStatus.Red);
					this.TimeIsUp();
				}

				// Giveup for practice mode
				if ( this.isPracticeMode &&(
					(SteamVR_Actions.sigverse_PressThumbstick.GetStateDown(SteamVR_Input_Sources.LeftHand)  && SteamVR_Actions.sigverse_PressNearButton.GetStateDown(SteamVR_Input_Sources.LeftHand)  && SteamVR_Actions.sigverse_PressFarButton.GetStateDown(SteamVR_Input_Sources.LeftHand)) ||
					(SteamVR_Actions.sigverse_PressThumbstick.GetStateDown(SteamVR_Input_Sources.RightHand) && SteamVR_Actions.sigverse_PressNearButton.GetStateDown(SteamVR_Input_Sources.RightHand) && SteamVR_Actions.sigverse_PressFarButton.GetStateDown(SteamVR_Input_Sources.RightHand)) ||
					(Input.GetKeyDown(KeyCode.Escape))
				)){
					this.OnGiveUp();
				}

				if (SteamVR_Actions.sigverse_PressNearButton.GetStateDown(SteamVR_Input_Sources.LeftHand) && this.isDuringSession)
				{
					if (this.isPracticeMode)
					{
						this.sessionManager.SpeakGuidanceMessageForPractice(HumanNaviConfig.Instance.configInfo.guidanceMessageForPractice);
					}
					else
					{
						//if (!this.sessionManager.GetTTSRuningState())
						{
							this.SendRosHumanNaviMessage(MsgRequest, "");
						}
					}
				}

				//if (this.avatar.transform.position.y < heightThresholdForPoseReset)
				//{
				//	this.ResetAvatarTransform();
				//}

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
					case Step.SessionStart:
					{
						this.goToNextTrialPanel.SetActive(false);

						this.scoreManager.TaskStart();
						this.StartPlaybackRecord();

						SIGVerseLogger.Info("Session start!");
						this.RecordEventLog("Session_start");

						this.SendPanelNotice("Session start!", 100, PanelNoticeStatus.Green);
						base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar("Session start!", 3.0f));

						this.isDuringSession = true;
						this.step++;

						break;
					}
					case Step.WaitForIamReady:
					{
						// For Practice
						if (this.isPracticeMode)
						{
							this.sessionManager.ResetNotificationDestinationsOfTTS();

							this.step++;
							break;
						}

						if (this.receivedMessageMap[MsgIamReady])
						{
							//this.StartPlaybackRecord();
							this.step++;
							break;
						}

						this.SendMessageAtIntervals(MsgAreYouReady, "", SendingAreYouReadyInterval);

						break;
					}
					case Step.SendTaskInfo:
					{
						if (!this.isPracticeMode)
						{
							this.SendRosTaskInfoMessage(this.taskInfoForRobot);
						}

						if (this.isPracticeMode) // first instruction for practice mode (TODO: this code should be in more appropriate position)
						{
							this.sessionManager.SpeakGuidanceMessageForPractice(HumanNaviConfig.Instance.configInfo.guidanceMessageForPractice);
						}

						SIGVerseLogger.Info("Waiting for end of session");

						this.step++;

						break;
					}
					case Step.WaitForEndOfSession:
					{
						// for score (grasp)
						this.JudgeGraspingObject();

						// for log (grasp)
						this.CheckGraspingStatus(this.LeftHand);
						this.CheckGraspingStatus(this.rightHand);

						// for avatar status
						this.objectIdInLeftHandPreviousFrame = this.objectIdInLeftHand;
						this.objectIdInRightHandPreviousFrame = this.objectIdInRightHand;
						this.objectIdInLeftHand  = this.GetGraspingObjectId(this.LeftHand);
						this.objectIdInRightHand = this.GetGraspingObjectId(this.rightHand);

						// for penalty of distance between the robot and the target/destination
						this.JudgeDistanceFromTargetObject();
						this.JudgeDistanceFromDestination();

						break;
					}
					case Step.WaitForNextSession:
					{
						if(this.stepTimer.IsTimePassed((int)this.step, 5000))
						{
							//this.step = Step.Initialize;
							if (!this.IsPlaybackFinished()) { break; }

							SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
			StartCoroutine(this.CloseRosConnections());

			Thread.Sleep(1000);
			Application.Quit();
		}

		//-----------------------------

		private void InitializeMainPanel()
		{
			this.panelMainController.SetTeamNameText("Team: " + HumanNaviConfig.Instance.configInfo.teamName);

			if (this.isPracticeMode)
			{
				HumanNaviConfig.Instance.numberOfTrials = 1;
			}
			else
			{
				HumanNaviConfig.Instance.InclementNumberOfTrials(HumanNaviConfig.Instance.configInfo.playbackType);
			}

			this.numberOfSession = HumanNaviConfig.Instance.numberOfTrials;

			//this.panelMainController.SetTrialNumberText(HumanNaviConfig.Instance.numberOfTrials);
			this.panelMainController.SetTrialNumberText(this.numberOfSession);
			SIGVerseLogger.Info("##### " + this.panelMainController.GetTrialNumberText() + " #####");

			this.panelMainController.SetTaskMessageText("");

			this.scoreManager.ResetTimeLeftText();
		}

		private void PreProcess()
		{
			this.sessionManager.ChangeEnvironment(this.numberOfSession);
			this.ResetAvatarTransform();

			this.sessionManager.ActivateRobot();

			if (!this.isPracticeMode)
			{
				this.rosConnections = SIGVerseUtils.FindObjectsOfInterface<IRosConnection>();
				SIGVerseLogger.Info("ROS connection : count=" + this.rosConnections.Length);
			}
			else
			{
				SIGVerseLogger.Info("No ROS connection (Practice mode)");
			}

			//this.currentTaskInfo = this.sessionManager.GetCurrentTaskInfo();
			this.currentTaskInfo = this.sessionManager.GetTaskInfo(this.numberOfSession);

			this.taskInfoForRobot = new SIGVerse.RosBridge.human_navigation.HumanNaviTaskInfo();
			string environmentName = this.sessionManager.GetEnvironment().name;
			this.taskInfoForRobot.environment_id = environmentName.Substring(0, environmentName.Length - 3);
			this.SetObjectListToHumanNaviTaskInfo();
			this.SetFurnitureToHumanNaviTaskInfo();
			this.SetDestinationToHumanNaviTaskInfo();

			this.waitingTime = 0.0f;

			this.interruptedReason = string.Empty;

			this.objectIdInLeftHand  = "";
			this.objectIdInRightHand = "";
			this.objectIdInLeftHandPreviousFrame  = ""; // Tentative Code
			this.objectIdInRightHandPreviousFrame = ""; // Tentative Code

			this.alreadyGraspedObjects = new List<string>();

			this.InitializePlayback();

			//this.countWrongObjectsGrasp = 0;

//			SIGVerseLogger.Info("End of PreProcess");
		}

		private void PostProcess()
		{
			if (HumanNaviConfig.Instance.numberOfTrials == HumanNaviConfig.Instance.configInfo.maxNumberOfTrials)
			{
				this.SendRosHumanNaviMessage(MsgMissionComplete, "");

				StartCoroutine(this.StopTime());

				StartCoroutine(this.CloseRosConnections());

				StartCoroutine(this.DisplayEndMessage());

				this.isAllTaskFinished = true;
			}
			else
			{
				this.SendRosHumanNaviMessage(MsgTaskFinished, "");

				SIGVerseLogger.Info("Go to next session");
				//this.RecordEventLog("Go_to_next_session");

				this.SendRosHumanNaviMessage(MsgGoToNextSession, "");

				StartCoroutine(this.ClearRosConnections());
			}

			this.StopPlaybackRecord();

			this.isDuringSession = false;
			this.interruptedReason = string.Empty;

			this.step = Step.WaitForNextSession;
		}

		private void ResetAvatarTransform()
		{

#if ENABLE_VRIK
			this.avatar.transform.GetComponent<HumanNaviAvatarController>().StartInitializing();
			this.avatar.transform.position = this.initialAvatarPosition;
			this.avatar.transform.eulerAngles = this.initialAvatarRotation;
			this.avatar.transform.Find("ThirdPersonEthan").localPosition = Vector3.zero;
			this.avatar.transform.Find("ThirdPersonEthanWithAnimation").localPosition = Vector3.zero;
#else
			this.avatar.transform.position = this.initialAvatarPosition;
			this.avatar.transform.eulerAngles = this.initialAvatarRotation;
			this.avatar.transform.Find("[CameraRig]").localPosition = Vector3.zero;
			this.avatar.transform.Find("[CameraRig]").localRotation = Quaternion.identity;
			this.avatar.transform.Find("Ethan").localPosition = Vector3.zero;
			this.avatar.transform.Find("Ethan").localRotation = Quaternion.identity;
#endif
		}

		private void SetObjectListToHumanNaviTaskInfo()
		{
			// Get graspable objects
			this.graspableObjects = GameObject.FindGameObjectsWithTag(TagNameOfGraspables).ToList<GameObject>();
			if (this.graspableObjects.Count == 0)
			{
				throw new Exception("Graspable object is not found.");
			}

			foreach (GameObject graspableObject in this.graspableObjects)
			{
				// transtrate the coordinate system of GameObject (left-handed, Z-axis:front, Y-axis:up) to ROS coodinate system (right-handed, X-axis:front, Z-axis:up)
				Vector3    positionInROS    = this.ConvertCoordinateSystemUnityToROS_Position(graspableObject.transform.position);
				Quaternion orientationInROS = this.ConvertCoordinateSystemUnityToROS_Rotation(graspableObject.transform.rotation);

				if (graspableObject.name == currentTaskInfo.target)
				{
					taskInfoForRobot.target_object.name = graspableObject.name.Substring(0, graspableObject.name.Length - 3);
					taskInfoForRobot.target_object.position = positionInROS;
					taskInfoForRobot.target_object.orientation = orientationInROS;

					// for penalty
					this.initialTargetObjectPosition = graspableObject.transform.position;
				}
				else
				{
					SIGVerse.RosBridge.human_navigation.HumanNaviObjectInfo objInfo = new SIGVerse.RosBridge.human_navigation.HumanNaviObjectInfo
					{
						name = graspableObject.name.Substring(0, graspableObject.name.Length - 3),
						position = positionInROS,
						orientation = orientationInROS
					};

					taskInfoForRobot.non_target_objects.Add(objInfo);

//					SIGVerseLogger.Info("Non-target object : " + objInfo.name + " " + objInfo.position + " " + objInfo.orientation);
				}
			}
			SIGVerseLogger.Info("Target object : " + taskInfoForRobot.target_object.name + " " + taskInfoForRobot.target_object.position + " " + taskInfoForRobot.target_object.orientation);

			if (taskInfoForRobot.target_object.name == "")
			{
				throw new Exception("Target object is not found.");
			}
		}

		private void SetFurnitureToHumanNaviTaskInfo()
		{
			// Get furniture
			List<GameObject> furnitureObjects = GameObject.FindGameObjectsWithTag(TagNameOfFurniture).ToList<GameObject>();
			if (furnitureObjects.Count == 0)
			{
				throw new Exception("Furniture is not found.");
			}

			foreach (GameObject furnitureObject in furnitureObjects)
			{
				// transtrate the coordinate system of GameObject (left-handed, Z-axis:front, Y-axis:up) to ROS coodinate system (right-handed, X-axis:front, Z-axis:up)
				Vector3    positionInROS    = this.ConvertCoordinateSystemUnityToROS_Position(furnitureObject.transform.position);
				Quaternion orientationInROS = this.ConvertCoordinateSystemUnityToROS_Rotation(furnitureObject.transform.rotation);

				SIGVerse.RosBridge.human_navigation.HumanNaviObjectInfo objInfo = new SIGVerse.RosBridge.human_navigation.HumanNaviObjectInfo
				{
					name = furnitureObject.name.Substring(0, furnitureObject.name.Length - 3),
					position = positionInROS,
					orientation = orientationInROS
				};

				taskInfoForRobot.furniture.Add(objInfo);

//				SIGVerseLogger.Info("Furniture : " + objInfo.name + " " + objInfo.position + " " + objInfo.orientation);
			}
		}

		private void SetDestinationToHumanNaviTaskInfo()
		{
			List<GameObject> destinations = GameObject.FindGameObjectsWithTag(TagNameOfDestination).ToList<GameObject>();
			if (destinations.Count == 0)
			{
				throw new Exception("Destination candidate is not found.");
			}

			if (!destinations.Any(obj => obj.name == this.currentTaskInfo.destination))
			{
				throw new Exception("Destination is not found.");
			}

			GameObject destination = destinations.Where(obj => obj.name == this.currentTaskInfo.destination).SingleOrDefault();

			taskInfoForRobot.destination.position    = this.ConvertCoordinateSystemUnityToROS_Position(destination.transform.position);
			taskInfoForRobot.destination.orientation = this.ConvertCoordinateSystemUnityToROS_Rotation(destination.transform.rotation);
			taskInfoForRobot.destination.size        = this.ConvertCoordinateSystemUnityToROS_Position(destination.GetComponent<BoxCollider>().size);
			// TODO: size parameter depends on the scale of parent object (for now, scale of all parent objects should be scale = (1,1,1))

			// for penalty
			this.initialDestinationPosition = destination.transform.position;

			SIGVerseLogger.Info("Destination : " + taskInfoForRobot.destination);
		}

		private void SendMessageAtIntervals(string message, string detail, int interval_ms = 1000)
		{
			this.waitingTime += UnityEngine.Time.deltaTime;

			if (this.waitingTime > interval_ms * 0.001)
			{
				this.SendRosHumanNaviMessage(message, detail);
				this.waitingTime = 0.0f;
			}
		}

		private void TimeIsUp()
		{
			this.SendRosHumanNaviMessage(MsgTaskFailed, ReasonTimeIsUp);
			this.SendPanelNotice("Time is up", 100, PanelNoticeStatus.Red);
			base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar("Time is up", 3.0f));

			this.TaskFinished();
		}

		private void TaskFinished()
		{
			this.scoreManager.TaskEnd();
			this.PostProcess();
		}

		public void OnReceiveRosMessage(RosBridge.human_navigation.HumanNaviMsg humanNaviMsg)
		{
			if (!this.isDuringSession)
			{
				SIGVerseLogger.Warn("Illegal timing [session is not started]");
				return;
			}

			if (this.receivedMessageMap.ContainsKey(humanNaviMsg.message))
			{
				if (humanNaviMsg.message == MsgIamReady)
				{
					if(this.step != Step.WaitForIamReady)
					{
						SIGVerseLogger.Warn("Illegal timing [message : " + humanNaviMsg.message + ", step:" + this.step + "]");
						return;
					}
				}

				if (humanNaviMsg.message == MsgGetAvatarStatus)
				{
					this.SendRosAvatarStatusMessage();
				}

				if (humanNaviMsg.message == MsgGetObjectStatus)
				{
					this.SendRosObjectStatusMessage();
				}

				if (humanNaviMsg.message == MsgGetSpeechState)
				{
					this.SendRosHumanNaviMessage(MsgSpeechState, this.sessionManager.GetSeechRunStateMsgString());
				}

				if (humanNaviMsg.message == MsgGiveUp)
				{
					this.OnGiveUp();
				}

				this.receivedMessageMap[humanNaviMsg.message] = true;
			}
			else
			{
				SIGVerseLogger.Warn("Received Illegal message [message: " + humanNaviMsg.message +"]");
			}
		}

		public void OnSendSpeechResult(string speechResult)
		{
			this.SendRosHumanNaviMessage(MsgSpeechResult, speechResult);
		}

		private Vector3 ConvertCoordinateSystemUnityToROS_Position(Vector3 unityPosition)
		{
			return new Vector3(unityPosition.z, -unityPosition.x, unityPosition.y);
		}
		private Quaternion ConvertCoordinateSystemUnityToROS_Rotation(Quaternion unityQuaternion)
		{
			return new Quaternion(-unityQuaternion.z, unityQuaternion.x, -unityQuaternion.y, unityQuaternion.w);
		}

		private void InitializePlayback()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypeRecord)
			{
				this.playbackRecorder.SetPlaybackTargets();
				//this.playbackRecorder.Initialize(HumanNaviConfig.Instance.numberOfTrials);
				this.playbackRecorder.Initialize(this.numberOfSession);
			}
		}

		private bool IsPlaybackInitialized()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypeRecord)
			{
				if (!this.playbackRecorder.IsInitialized()) { return false; }
			}

			return true;
		}

		private bool IsPlaybackFinished()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == HumanNaviPlaybackCommon.PlaybackTypeRecord)
			{
				if (!this.playbackRecorder.IsFinished()) { return false; }
			}

			//if (HumanNaviConfig.Instance.configInfo.playbackType == HumanNaviPlaybackCommon.PlaybackTypePlay)
			//{
			//	if (!this.playbackPlayer.IsFinished()) { return false; }
			//}

			return true;
		}

		private void StartPlaybackRecord()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypeRecord)
			{
				bool isStarted = this.playbackRecorder.Record();

				if (!isStarted) { SIGVerseLogger.Warn("Cannot start the world playback recording"); }
			}
		}

		private void StopPlaybackRecord()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypeRecord)
			{
				bool isStopped = this.playbackRecorder.Stop();

				if (!isStopped) { SIGVerseLogger.Warn("Cannot stop the world playback recording"); }
			}
		}

		private IEnumerator ShowNoticeMessagePanelForAvatar(string text, float waitTime = 1.0f)
		{
			this.noticeTextForAvatar.text = text;
			this.noticePanelForAvatar.SetActive(true);

			yield return new WaitForSeconds(waitTime);

			this.noticePanelForAvatar.SetActive(false);
		}

		private void SendRosHumanNaviMessage(string message, string detail)
		{
			ExecuteEvents.Execute<IRosHumanNaviMessageSendHandler>
			(
				target: this.gameObject, 
				eventData: null, 
				functor: (reciever, eventData) => reciever.OnSendRosHumanNaviMessage(message, detail)
			);

			ExecuteEvents.Execute<IPlaybackRosMessageHandler>
			(
				target: this.playbackManager,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnSendRosMessage(new SIGVerse.RosBridge.human_navigation.HumanNaviMsg(message, detail))
			);
		}

		private void SendRosAvatarStatusMessage()
		{
			RosBridge.human_navigation.HumanNaviAvatarStatus avatarStatus = new RosBridge.human_navigation.HumanNaviAvatarStatus();

			avatarStatus.head.position          = ConvertCoordinateSystemUnityToROS_Position(this.head.transform.position);
			avatarStatus.head.orientation       = ConvertCoordinateSystemUnityToROS_Rotation(this.head.transform.rotation);
			avatarStatus.body.position          = ConvertCoordinateSystemUnityToROS_Position(this.body.transform.position);
			avatarStatus.body.orientation       = ConvertCoordinateSystemUnityToROS_Rotation(this.body.transform.rotation);
			avatarStatus.left_hand.position     = ConvertCoordinateSystemUnityToROS_Position(this.LeftHand.transform.position);
			avatarStatus.left_hand.orientation  = ConvertCoordinateSystemUnityToROS_Rotation(this.LeftHand.transform.rotation);
			avatarStatus.right_hand.position    = ConvertCoordinateSystemUnityToROS_Position(this.rightHand.transform.position);
			avatarStatus.right_hand.orientation = ConvertCoordinateSystemUnityToROS_Rotation(this.rightHand.transform.rotation);
			avatarStatus.object_in_left_hand    = this.objectIdInLeftHand  == "" ? "" : this.objectIdInLeftHand .Substring(0, this.objectIdInLeftHand .Length - 3);
			avatarStatus.object_in_right_hand   = this.objectIdInRightHand == "" ? "" : this.objectIdInRightHand.Substring(0, this.objectIdInRightHand.Length - 3);
			avatarStatus.is_target_object_in_left_hand  = this.IsTargetObject(this.objectIdInLeftHand);
			avatarStatus.is_target_object_in_right_hand = this.IsTargetObject(this.objectIdInRightHand);

			ExecuteEvents.Execute<IRosAvatarStatusSendHandler>
			(
				target: this.gameObject,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnSendRosAvatarStatusMessage(avatarStatus)
			);

			ExecuteEvents.Execute<IRosAvatarStatusSendHandler>
			(
				target: this.playbackManager,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnSendRosAvatarStatusMessage(avatarStatus)
			);
		}

		private void SendRosObjectStatusMessage()
		{
			RosBridge.human_navigation.HumanNaviObjectStatus objectStatus = new RosBridge.human_navigation.HumanNaviObjectStatus();

			foreach (GameObject graspableObject in this.graspableObjects)
			{
				Vector3    positionInROS    = this.ConvertCoordinateSystemUnityToROS_Position(graspableObject.transform.position);
				Quaternion orientationInROS = this.ConvertCoordinateSystemUnityToROS_Rotation(graspableObject.transform.rotation);

				if (graspableObject.name == currentTaskInfo.target)
				{
					objectStatus.target_object.name = graspableObject.name.Substring(0, graspableObject.name.Length - 3);
					objectStatus.target_object.position = positionInROS;
					objectStatus.target_object.orientation = orientationInROS;
				}
				else
				{
					SIGVerse.RosBridge.human_navigation.HumanNaviObjectInfo objInfo = new SIGVerse.RosBridge.human_navigation.HumanNaviObjectInfo
					{
						name = graspableObject.name.Substring(0, graspableObject.name.Length - 3),
						position = positionInROS,
						orientation = orientationInROS
					};

					objectStatus.non_target_objects.Add(objInfo);
				}
			}

			ExecuteEvents.Execute<IRosObjectStatusSendHandler>
			(
				target: this.gameObject,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnSendRosObjectStatusMessage(objectStatus)
			);

			ExecuteEvents.Execute<IRosObjectStatusSendHandler>
			(
				target: this.playbackManager,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnSendRosObjectStatusMessage(objectStatus)
			);
		}

		private void SendRosTaskInfoMessage(RosBridge.human_navigation.HumanNaviTaskInfo taskInfo)
		{
			ExecuteEvents.Execute<IRosTaskInfoSendHandler>
			(
				target: this.gameObject,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnSendRosTaskInfoMessage(taskInfo)
			);

			ExecuteEvents.Execute<IRosTaskInfoSendHandler>
			(
				target: this.playbackManager,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnSendRosTaskInfoMessage(taskInfo)
			);
		}

		private void JudgeGraspingObject()
		{
			this.CheckGraspOfObject(this.LeftHand);
			this.CheckGraspOfObject(this.rightHand);
		}

//		private void CheckGraspOfObject(NewtonVR.NVRHand hand)
		private void CheckGraspOfObject(Hand hand)
		{
			if (this.preAttachedObjectMap[hand.handType] == hand.currentAttachedObject) { return; }

			this.preAttachedObjectMap[hand.handType] = hand.currentAttachedObject;

//			if (hand.HoldButtonDown && hand.IsInteracting)
			if (hand.currentAttachedObject != null)
			{
				if (hand.currentAttachedObject.tag == TagNameOfGraspables)
				{
					if (this.IsTargetObject(hand.currentAttachedObject.name))
					{
						if (!this.isTargetAlreadyGrasped)
						{
							SIGVerseLogger.Info("Target object is grasped" + "\t" + this.GetElapsedTimeText());

							this.SendPanelNotice("Target object is grasped", 100, PanelNoticeStatus.Green);

							this.scoreManager.AddScore(Score.ScoreType.CorrectObjectIsGrasped);
							//this.scoreManager.AddTimeScoreOfGrasp();
							this.scoreManager.AddTimeScore();

							this.isTargetAlreadyGrasped = true;
						}

						this.RecordEventLog("Object_Is_Grasped" + "\t" + "Target_Object" + "\t" + hand.currentAttachedObject.name);
					}
					else
					{
						//if (!this.isTargetAlreadyGrasped)
						{
							if (!this.alreadyGraspedObjects.Contains(hand.currentAttachedObject.name))
							{
								SIGVerseLogger.Info("Wrong object is grasped [new]" + "\t" + this.GetElapsedTimeText() + "\t" + this.GetElapsedTimeText());

								this.SendPanelNotice("Wrong object is grasped", 100, PanelNoticeStatus.Red);

								this.scoreManager.AddScore(Score.ScoreType.IncorrectObjectIsGrasped);
								//this.scoreManager.ImposeTimePenalty(Score.TimePnaltyType.IncorrectObjectIsGrasped);

								//this.countWrongObjectsGrasp++;

								this.alreadyGraspedObjects.Add(hand.currentAttachedObject.name);
							}
							else
							{
								SIGVerseLogger.Info("Wrong object is grasped [already grasped]" + "\t" + this.GetElapsedTimeText() + "\t" + this.GetElapsedTimeText());
							}

							this.RecordEventLog("Object_Is_Grasped" + "\t" + "Wrong_Object" + "\t" + hand.currentAttachedObject.name);
						}
					}
				}
				else// if (hand.CurrentlyInteracting.tag != "Untagged")
				{
					SIGVerseLogger.Info("Object_Is_Grasped" + "\t" + "Others" + "\t" + hand.currentAttachedObject.name + "\t" + this.GetElapsedTimeText());
					this.RecordEventLog("Object_Is_Grasped" + "\t" + "Others" + "\t" + hand.currentAttachedObject.name);
				}
			}
		}

//		private string GetGraspingObjectId(NewtonVR.NVRHand hand)
		private string GetGraspingObjectId(Hand hand)
		{
			string graspingObject = "";

			if(hand.currentAttachedObject != null)
			{
				if (hand.currentAttachedObject.tag == TagNameOfGraspables)
				{
					graspingObject = hand.currentAttachedObject.name;
				}
			}

			//if (hand.HoldButtonPressed)
			//{
			//	if (hand.IsInteracting)
			//	{
			//		if (hand.CurrentlyInteracting.tag == TagNameOfGraspables)
			//		{
			//			graspingObject = hand.CurrentlyInteracting.name;
			//		}
			//	}
			//}

			return graspingObject;
		}

		private bool IsTargetObject(string objectLabel)
		{
			if (objectLabel == this.currentTaskInfo.target) return true;
			else                                            return false;
		}

//		private void CheckGraspingStatus(NewtonVR.NVRHand hand)
		private void CheckGraspingStatus(Hand hand)
		{
//			if (hand.HoldButtonDown)
			if (SteamVR_Actions.sigverse_PressMiddle.GetStateDown(hand.handType))
			{
				SIGVerseLogger.Info("HandInteraction" + "\t" + "HoldButtonDown" + "\t" + hand.name + "\t" + this.GetElapsedTimeText());
				this.RecordEventLog("HandInteraction" + "\t" + "HoldButtonDown" + "\t" + hand.name);
			}

//			if (hand.HoldButtonUp)
			if (SteamVR_Actions.sigverse_PressMiddle.GetStateUp(hand.handType))
			{
				string objectInhand = "";
				if      (hand.handType==SteamVR_Input_Sources.LeftHand) { objectInhand = this.objectIdInLeftHandPreviousFrame; }
				else if (hand.handType==SteamVR_Input_Sources.RightHand){ objectInhand = this.objectIdInRightHandPreviousFrame; }

				if(objectInhand != "")
				{
					SIGVerseLogger.Info("HandInteraction" + "\t" + "ReleaseObject" + "\t" + hand.name + "\t" + this.GetElapsedTimeText());
					this.RecordEventLog("HandInteraction" + "\t" + "ReleaseObject" + "\t" + hand.name + "\t" + objectInhand);
				}
				else
				{
					SIGVerseLogger.Info("HandInteraction" + "\t" + "HoldButtonUp" + "\t" + hand.name + "\t" + this.GetElapsedTimeText());
					this.RecordEventLog("HandInteraction" + "\t" + "HoldButtonUp" + "\t" + hand.name);
				}
			}
		}

		private void JudgeDistanceFromTargetObject()
		{
			if (!this.scoreManager.IsAlreadyGivenDistancePenaltyForTargetObject())
			{
				float distanceFromTargetObject = this.sessionManager.GetDistanceFromRobot(this.initialTargetObjectPosition);
				if (distanceFromTargetObject < this.scoreManager.limitDistanceFromTarget)
				{
					this.scoreManager.AddDistancePenaltyForTargetObject();
				}
			}
		}
		private void JudgeDistanceFromDestination()
		{
			if (!this.scoreManager.IsAlreadyGivenDistancePenaltyForDestination())
			{
				float distanceFromDestination = this.sessionManager.GetDistanceFromRobot(this.initialDestinationPosition);
				if (distanceFromDestination < this.scoreManager.limitDistanceFromTarget)
				{
					this.scoreManager.AddDistancePenaltyForDestination();
				}
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
			if(!this.isDuringSession)
			{
				return;
			} 

			SIGVerseLogger.Info("Target is plasced on the destination." + "\t" + this.GetElapsedTimeText());
			this.RecordEventLog("Target is plasced on the destination.");

			this.scoreManager.AddScore(Score.ScoreType.TargetObjectInDestination);
			//this.scoreManager.AddTimeScoreOfPlacement();
			this.scoreManager.AddTimeScore();

			this.SendRosHumanNaviMessage(MsgTaskSucceeded, "");
			this.SendPanelNotice("Task succeeded", 100, PanelNoticeStatus.Green);
			base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar("Task succeeded", 10.0f));

			this.TaskFinished();
		}

		public bool IsTargetGrasped()
		{
			bool isGraspedByLeftHand  = this.LeftHand .currentAttachedObject != null && this.IsTargetObject(this.LeftHand .currentAttachedObject.name);
			bool isGraspedByRightHand = this.rightHand.currentAttachedObject != null && this.IsTargetObject(this.rightHand.currentAttachedObject.name);

			return isGraspedByLeftHand || isGraspedByRightHand;
		}

		public void OnTimeIsUp()
		{
			this.interruptedReason = HumanNaviModerator.ReasonTimeIsUp;
		}

		public void OnGiveUp()
		{
			if (this.isDuringSession)
			{
				this.interruptedReason = HumanNaviModerator.ReasonGiveUp;

				this.SendRosHumanNaviMessage(MsgTaskFailed, ReasonGiveUp);
				this.SendPanelNotice("Give up", 100, PanelNoticeStatus.Red);
				base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar("Give up", 3.0f));

				this.panelMainController.giveUpPanel.SetActive(false);

				this.TaskFinished();
			}
			else
			{
				SIGVerseLogger.Warn("It is a timing not allowed to give up.");
			}
		}

		public void OnReachMaxWrongObjectGraspCount()
		{
			this.interruptedReason = HumanNaviModerator.ReasonReachMaxWrongObjectGraspCount;

			string strReason = "Reach_max_wrong_object_grasp_count";
			this.SendRosHumanNaviMessage(MsgTaskFailed, strReason);
			this.SendPanelNotice("Reach max wrong object grasp count", 100, PanelNoticeStatus.Red);
			base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar("Reach max wrong object grasp count", 3.0f));

			this.TaskFinished();
		}

		public void OnStartTrial()
		{
			SIGVerseLogger.Info("Task start!");
			//this.RecordEventLog("Task_start");

			this.startTrialPanel.SetActive(false);
			this.isCompetitionStarted = true;
		}

		public void OnGoToNextTrial()
		{
			this.goToNextTrialPanel.SetActive(false);

			this.isCompetitionStarted = true; // for practice mode
		}

		private void ShowStartTaskPanel()
		{
			if (this.isPracticeMode)
			{
				this.goToNextTrialPanel.SetActive(true);
			}
			else
			{
				this.startTrialPanel.SetActive(true);
			}
		}

		private void SendPanelNotice(string message, int fontSize, Color color, bool shouldSendToPlaybackManager = true)
		{
			PanelNoticeStatus noticeStatus = new PanelNoticeStatus(message, fontSize, color, 3.0f);

			// For changing the notice of a panel
			ExecuteEvents.Execute<IPanelNoticeHandler>
			(
				target: this.mainMenu,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnPanelNoticeChange(noticeStatus)
			);

			if (shouldSendToPlaybackManager)
			{
				// For recording
				ExecuteEvents.Execute<IPanelNoticeHandler>
				(
					target: this.playbackManager,
					eventData: null,
					functor: (reciever, eventData) => reciever.OnPanelNoticeChange(noticeStatus)
				);
			}
		}

		public IEnumerator DisplayEndMessage()
		{
			yield return new WaitForSecondsRealtime(7);

			string endMessage = "All sessions have ended";

			SIGVerseLogger.Info(endMessage);

			this.SendPanelNotice(endMessage, 80, PanelNoticeStatus.Blue, false);
		}


		private void RecordEventLog(string log)
		{
			// For recording
			ExecuteEvents.Execute<IRecordEventHandler>
			(
				target: this.playbackManager,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnRecordEvent(log)
			);
		}

		private string GetElapsedTimeText()
		{
			if (HumanNaviConfig.Instance.configInfo.playbackType == WorldPlaybackCommon.PlaybackTypeRecord)
			{
				return Math.Round(this.playbackRecorder.GetElapsedTime(), 4, MidpointRounding.AwayFromZero).ToString();
			}
			else
			{
				return Math.Round(this.scoreManager.GetElapsedTime(), 4, MidpointRounding.AwayFromZero).ToString();
			}
		}

		private bool IsConnectedToRos()
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

		private IEnumerator ClearRosConnections()
		{
			yield return new WaitForSecondsRealtime(1.5f);

			foreach (IRosConnection rosConnection in this.rosConnections)
			{
				//Debug.Log(rosConnection.ToString());
				rosConnection.Clear();
			}

			SIGVerseLogger.Info("Clear ROS connections");
		}

		private IEnumerator CloseRosConnections()
		{
			yield return new WaitForSecondsRealtime(1.5f);

			foreach (IRosConnection rosConnection in this.rosConnections)
			{
				rosConnection.Close();
			}

			SIGVerseLogger.Info("Close ROS connections");
		}
		private IEnumerator StopTime()
		{
			yield return new WaitForSecondsRealtime(1.0f);

			Time.timeScale = 0.0f;
		}
	}
}
