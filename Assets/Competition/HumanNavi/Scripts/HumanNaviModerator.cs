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
	public class HumanNaviModerator : MonoBehaviour, ITimeIsUpHandler, IStartTrialHandler, IGoToNextTrialHandler, IReceiveHumanNaviMsgHandler//, IGiveUpHandler
	{
		private const int SendingAreYouReadyInterval = 1000;

		private const string MsgAreYouReady     = "Are_you_ready?";
		private const string MsgTaskSucceeded   = "Task_succeeded";
		private const string MsgTaskFailed      = "Task_failed";
		private const string MsgTaskFinished    = "Task_finished";
		private const string MsgGoToNextSession   = "Go_to_next_session";
		private const string MsgMissionComplete = "Mission_complete";

		private const string ReasonTimeIsUp = "Time_is_up";
		private const string ReasonGiveUp = "Give_up";

		private const string MsgIamReady           = "I_am_ready";
		private const string MsgGetAvatarStatus = "Get_avatar_status";
		private const string MsgGetObjectStatus = "Get_object_status";
		private const string MsgConfirmSpeechState = "Confirm_speech_state";
		private const string MsgGiveUp             = "Give_up";

		private const string MsgRequest     = "Guidance_request";
		private const string MsgSpeechState = "Speech_state";

		private const string TagNameOfGraspables = "Graspables";
		private const string TagNameOfFurniture = "Furniture";

		private enum Step
		{
			Initialize,
			InitializePlayback,
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

		[HeaderAttribute("Avatar")]
		public GameObject avatar;
		public GameObject head;
		public GameObject body;
		public NewtonVR.NVRHand LeftHand;
		public NewtonVR.NVRHand rightHand;
		public GameObject cameraRig;
		public GameObject Ethan;
		public float heightThresholdForPoseReset = -0.5f;

		[HeaderAttribute("Panels for avatar")]
		public GameObject noticePanelForAvatar;
		public UnityEngine.UI.Text noticeTextForAvatar;

		[HeaderAttribute("Menu")]
		public Camera birdviewCamera;
		public GameObject startTrialPanel;
		public GameObject goToNextTrialPanel;

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
		private bool isDuringSession = false;

		private Dictionary<string, bool> receivedMessageMap;
		private bool isTargetAlreadyGrasped;
		private bool goNextSession = false;
		private bool isAllTaskFinished = false;
		private string interruptedReason;

		private StepTimer stepTimer;

		private HumanNaviPlaybackRecorder playbackRecorder;

		private Vector3 initialTargetObjectPosition;
		private Vector3 initialDestinationPosition;

		private string objectIdInLeftHand;
		private string objectIdInRightHand;

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
				this.receivedMessageMap.Add(MsgGetAvatarStatus, false);
				this.receivedMessageMap.Add(MsgGetObjectStatus, false);
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

				if (this.interruptedReason != string.Empty && this.step != Step.WaitForNextSession)
				{
					SIGVerseLogger.Info("Failed '" + this.interruptedReason + "'");
					this.SendPanelNotice("Failed\n" + interruptedReason.Replace('_', ' '), 100, PanelNoticeStatus.Red);
					this.TimeIsUp();
				}

				if (OVRInput.GetDown(OVRInput.RawButton.X) && this.isDuringSession)
				{
					if (!this.sessionManager.GetSeechRunState())
					{
						this.SendRosHumanNaviMessage(MsgRequest, "");
					}
				}

				if (this.avatar.transform.position.y < heightThresholdForPoseReset)
				{
					this.ResetAvatarTransform();
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
					case Step.SessionStart:
					{
						this.goToNextTrialPanel.SetActive(false);

						SIGVerseLogger.Info("Session start!");
						this.RecordEventLog("Session_start");

						this.scoreManager.TaskStart();

						this.SendPanelNotice("Session start!", 100, PanelNoticeStatus.Green);
						base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar("Session start!", 3.0f));

						this.isDuringSession = true;
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
						this.SendRosTaskInfoMessage(this.taskInfoForRobot);

						SIGVerseLogger.Info("Waiting for end of session");

						this.step++;

						break;
					}
					case Step.WaitForEndOfSession:
					{
						// for score (grasp)
						this.JudgeGraspingObject();
						//this.CheckHandInteraction(this.LeftHand);
						//this.CheckHandInteraction(this.rightHand);

						// for avatar status
						this.objectIdInLeftHand  = this.GetGraspingObjectId(this.LeftHand);
						this.objectIdInRightHand = this.GetGraspingObjectId(this.rightHand);

						// for penalty of distance between the robot and the target/destination
						this.JudgeDistanceFromTargetObject();
						this.JudgeDistanceFromDestination();

						break;
					}
					case Step.WaitForNextSession:
					{
						if(this.goNextSession)
						{
							SIGVerseLogger.Info("Go to next session");
							this.RecordEventLog("Go_to_next_session");

							this.SendRosHumanNaviMessage(MsgGoToNextSession, "");

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
			this.SetFurnitureToHumanNaviTaskInfo();
			this.SetDestinationToHumanNaviTaskInfo();

			this.waitingTime = 0.0f;

			this.interruptedReason = string.Empty;

			this.objectIdInLeftHand  = "";
			this.objectIdInRightHand = "";

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

			this.isDuringSession = false;
			this.interruptedReason = string.Empty;

			this.step = Step.WaitForNextSession;
		}

		private void ResetFlags()
		{
			this.receivedMessageMap[MsgIamReady] = false;
			this.isTargetAlreadyGrasped = false;
			this.goNextSession = false;
		}

		private void ResetAvatarTransform()
		{
#if HUMAN_NAVI_USING_FINAL_IK
			this.avatar.transform.position = this.initialAvatarPosition;
			this.avatar.transform.eulerAngles = this.initialAvatarRotation;
#else
			this.avatar.transform.position = this.initialAvatarPosition;
			this.avatar.transform.eulerAngles = this.initialAvatarRotation;
			this.cameraRig.transform.localPosition = Vector3.zero;
			this.cameraRig.transform.localRotation = Quaternion.identity;
			this.Ethan.transform.localPosition = Vector3.zero;
			this.Ethan.transform.localRotation = Quaternion.identity;
#endif
		}

		private void SetObjectListToHumanNaviTaskInfo()
		{
			// Get graspable objects
			List<GameObject> graspableObjects = GameObject.FindGameObjectsWithTag(TagNameOfGraspables).ToList<GameObject>();
			if (graspableObjects.Count == 0)
			{
				throw new Exception("Graspable object is not found.");
			}

			foreach (GameObject graspableObject in graspableObjects)
			{
				// transtrate the coordinate system of GameObject (left-handed, Z-axis:front, Y-axis:up) to ROS coodinate system (right-handed, X-axis:front, Z-axis:up)
				Vector3 positionInROS = this.ConvertCoorinateSystemUnityToROS_Position(graspableObject.transform.position);
				Quaternion orientationInROS = this.ConvertCoorinateSystemUnityToROS_Rotation(graspableObject.transform.rotation);

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

					SIGVerseLogger.Info("Non-target object : " + objInfo.name + " " + objInfo.position + " " + objInfo.orientation);
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
				Vector3 positionInROS = this.ConvertCoorinateSystemUnityToROS_Position(furnitureObject.transform.position);
				Quaternion orientationInROS = this.ConvertCoorinateSystemUnityToROS_Rotation(furnitureObject.transform.rotation);

				SIGVerse.RosBridge.human_navigation.HumanNaviObjectInfo objInfo = new SIGVerse.RosBridge.human_navigation.HumanNaviObjectInfo
				{
					name = furnitureObject.name.Substring(0, furnitureObject.name.Length - 3),
					position = positionInROS,
					orientation = orientationInROS
				};

				taskInfoForRobot.furniture.Add(objInfo);

				SIGVerseLogger.Info("Furniture : " + objInfo.name + " " + objInfo.position + " " + objInfo.orientation);
			}

		}

		private void SetDestinationToHumanNaviTaskInfo()
		{
			List<GameObject> destinations = GameObject.FindGameObjectsWithTag("Destination").ToList<GameObject>();
			if (destinations.Count == 0)
			{
				throw new Exception("Destination candidate is not found.");
			}

			if (!destinations.Any(obj => obj.name == this.currentTaskInfo.destination))
			{
				throw new Exception("Destination is not found.");
			}

			GameObject destination = destinations.Where(obj => obj.name == this.currentTaskInfo.destination).SingleOrDefault();

			taskInfoForRobot.destination.position = this.ConvertCoorinateSystemUnityToROS_Position(destination.transform.position);
			taskInfoForRobot.destination.orientation = this.ConvertCoorinateSystemUnityToROS_Rotation(destination.transform.rotation);
			taskInfoForRobot.destination.size = this.ConvertCoorinateSystemUnityToROS_Position(destination.GetComponent<BoxCollider>().size);
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

			this.TaskFinished();
		}

		private IEnumerator ShowGotoNextPanel(float waitTime = 1.0f)
		{
			yield return new WaitForSeconds(waitTime);
			this.goToNextTrialPanel.SetActive(true);
			this.ShowNoticeMessagePanelForAvatar("Waiting for the next session to start");
			//base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar("Waiting for the next session to start", 10.0f));
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

				if (humanNaviMsg.message == MsgConfirmSpeechState)
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
				bool isStarted = this.playbackRecorder.Record();

				if (!isStarted) { SIGVerseLogger.Warn("Cannot start the world playback recording"); }
			}
		}

		public void StopPlaybackRecord()
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

		private void ShowNoticeMessagePanelForAvatar(string text)
		{
			this.noticeTextForAvatar.text = text;
			this.noticePanelForAvatar.SetActive(true);
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

			avatarStatus.head.position = ConvertCoorinateSystemUnityToROS_Position(this.head.transform.position);
			avatarStatus.head.orientation = ConvertCoorinateSystemUnityToROS_Rotation(this.head.transform.rotation);
			avatarStatus.body.position = ConvertCoorinateSystemUnityToROS_Position(this.body.transform.position);
			avatarStatus.body.orientation = ConvertCoorinateSystemUnityToROS_Rotation(this.body.transform.rotation);
			avatarStatus.left_hand.position = ConvertCoorinateSystemUnityToROS_Position(this.LeftHand.transform.position);
			avatarStatus.left_hand.orientation = ConvertCoorinateSystemUnityToROS_Rotation(this.LeftHand.transform.rotation);
			avatarStatus.right_hand.position = ConvertCoorinateSystemUnityToROS_Position(this.rightHand.transform.position);
			avatarStatus.right_hand.orientation = ConvertCoorinateSystemUnityToROS_Rotation(this.rightHand.transform.rotation);
			avatarStatus.object_in_left_hand = this.objectIdInLeftHand == "" ? "" : this.objectIdInLeftHand.Substring(0, this.objectIdInLeftHand.Length - 3);
			avatarStatus.object_in_right_hand = this.objectIdInRightHand == "" ? "" : this.objectIdInRightHand.Substring(0, this.objectIdInRightHand.Length - 3);
			avatarStatus.is_target_object_in_left_hand = this.IsTargetObject(this.objectIdInLeftHand);
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

			List<GameObject> graspableObjects = GameObject.FindGameObjectsWithTag(TagNameOfGraspables).ToList<GameObject>();

			foreach (GameObject graspableObject in graspableObjects)
			{
				Vector3 positionInROS = this.ConvertCoorinateSystemUnityToROS_Position(graspableObject.transform.position);
				Quaternion orientationInROS = this.ConvertCoorinateSystemUnityToROS_Rotation(graspableObject.transform.rotation);

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

		private void CheckGraspOfObject(NewtonVR.NVRHand hand)
		{
			if (hand.HoldButtonDown && hand.IsInteracting)
			{
				if (hand.CurrentlyInteracting.tag == TagNameOfGraspables)
				{
					if (this.IsTargetObject(hand.CurrentlyInteracting.name))
					{
						if (!this.isTargetAlreadyGrasped)
						{
							SIGVerseLogger.Info("Target object is grasped");

							this.SendPanelNotice("Target object is grasped", 100, PanelNoticeStatus.Green);

							this.scoreManager.AddScore(Score.ScoreType.CorrectObjectIsGrasped);
							this.scoreManager.AddTimeScoreOfGrasp();

							this.isTargetAlreadyGrasped = true;
						}
					}
					else
					{
						//if (!this.isTargetAlreadyGrasped)
						{
							SIGVerseLogger.Info("Wrong object is grasped");

							this.SendPanelNotice("Wrong object is grasped", 100, PanelNoticeStatus.Red);

							//this.scoreManager.AddScore(Score.ScoreType.IncorrectObjectIsGrasped);
							this.scoreManager.ImposeTimePenalty(Score.TimePnaltyType.IncorrectObjectIsGrasped);
						}
					}
				}
			}
		}

		private string GetGraspingObjectId(NewtonVR.NVRHand hand)
		{
			string graspingObject = "";

			if (hand.HoldButtonPressed)
			{
				if (hand.IsInteracting)
				{
					if (hand.CurrentlyInteracting.tag == TagNameOfGraspables)
					{
						graspingObject = hand.CurrentlyInteracting.name;
					}
				}
			}

			return graspingObject;
		}

		private bool IsTargetObject(string objectLabel)
		{
			if (objectLabel == this.currentTaskInfo.target) return true;
			else                                            return false;
		}

		private void CheckGraspingStatus(NewtonVR.NVRHand hand)
		{
			if (hand.HoldButtonDown)
			{
				this.RecordEventLog("Interaction" + "\t" + hand.name + ".HoldButtonDown");
			}

			if (hand.HoldButtonUp)
			{
				if (hand.IsInteracting)
				{
					this.RecordEventLog("Interaction" + "\t" + hand.name + ".ReleaseObject");
				}
				else
				{
					this.RecordEventLog("Interaction" + "\t" + hand.name + ".HoldButtonUp");
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

			SIGVerseLogger.Info("Target is plasced on the destination.");
			this.RecordEventLog("Target is plasced on the destination.");

			this.scoreManager.AddScore(Score.ScoreType.TargetObjectInDestination);
			this.scoreManager.AddTimeScoreOfPlacement();

			this.SendRosHumanNaviMessage(MsgTaskSucceeded, "");
			this.SendPanelNotice("Task succeeded", 100, PanelNoticeStatus.Green);
			base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar("Task succeeded", 10.0f));

			this.TaskFinished();
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

				string strGiveup = "Give_up";
				this.SendRosHumanNaviMessage(MsgTaskFailed, strGiveup);
				this.SendPanelNotice(strGiveup, 100, PanelNoticeStatus.Red);
				base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar(strGiveup, 3.0f));

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
			//this.RecordEventLog("Task_start");

			this.startTrialPanel.SetActive(false);
			this.isCompetitionStarted = true;
		}

		public void OnGoToNextTrial()
		{
			this.goToNextTrialPanel.SetActive(false);
			this.goNextSession = true;
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

		private void RecordEventLog(string log)
		{
			// For recording
			ExecuteEvents.Execute<IPlaybackRosMessageHandler>
			(
				target: this.playbackManager,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnRecordEvent(log)
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