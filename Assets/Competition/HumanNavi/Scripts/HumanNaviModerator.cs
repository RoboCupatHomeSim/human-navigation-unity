using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using SIGVerse.Common;
using SIGVerse.ROSBridge;
using System.Threading;

//using SIGVerse.ROSBridge.HumanNavigation;

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

		private const string MsgIamReady      = "I_am_ready";
		private const string MsgGetAvatarPose = "Get_avatar_pose";

		private const string MsgRequest = "Guidance_request";

		private enum Step
		{
			Initialize,
			WaitForIamReady,
			TrialStart,
			WaitTrialFinished,
			WaitNextTrial
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

		[HeaderAttribute("Effect")]
		public GameObject badEffect;
		public GameObject goodEffect;
		//public GameObject messagePanelForAvatar;

		[HeaderAttribute("Menu")]
		public HumanNaviMenu humanNaviMenu;
		public Camera birdviewCamera;

		[HeaderAttribute("ROS Message")]
		public HumanNaviPubMessage pubHumanNaviMessage;
		public HumanNaviPubTaskInfo pubTaskInfo;
		public HumanNaviPubAvatarPose pubAvatarPose;

		//-----------------------------

		private GameObject robot;
		private GameObject environment;

		private Vector3 initialAvatarPosition;
		private Vector3 initialAvatarRotation;

		private List<SIGVerse.Competition.HumanNavigation.TaskInfo> taskInfoList;
		private SIGVerse.ROSBridge.human_navigation.HumanNaviTaskInfo taskInfoForRobot;
		private SIGVerse.Competition.HumanNavigation.TaskInfo currentTaskInfo;

		private Step step;

		private float waitingTime;

		private bool isCompetitionStarted = false;
		private bool isDuringTrial = false;
		private Dictionary<string, bool> receivedMessageMap;
		private bool isTargetAlreadyGrasped;
		private bool goNextTrial = false;
		private bool isAllTaskFinished = false;

		private GameObject worldRecorder;

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

				List<GameObject> existingRobots = GameObject.FindGameObjectsWithTag("Robot").ToList<GameObject>();
				foreach (GameObject existingRobot in existingRobots)
				{
					existingRobot.SetActive(false);
				}
				this.ResetRobot();

//				this.worldRecorder = GameObject.Find(HumanNaviPlaybackParam.WorldRecorderName);

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
				if (this.isAllTaskFinished)
				{
					return;
				}

				if (OVRInput.GetDown(OVRInput.RawButton.X) && this.isDuringTrial)
				{
					this.SendROSHumanNaviMessage(MsgRequest, "");
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
					case Step.WaitForIamReady:
					{
						if (this.receivedMessageMap[MsgIamReady])
						{
							this.isDuringTrial = true;
							this.step++;
							break;
						}

						this.SendMessageAtIntervals(MsgAreYouReady, "", SendingAreYouReadyInterval);

						break;
					}
					case Step.TrialStart:
					{
						SIGVerseLogger.Info("Task start!");

						string textForGUI = "Task start!";
						base.StartCoroutine(this.ShowNoticeMessagePanel(textForGUI, 3.0f));
						base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar(textForGUI, 3.0f));

						//this.worldRecorder.GetComponentInChildren<HumanNaviPlaybackRecorder>().addCommadLog("test", "Task start!");
//						this.StartRecording();

//						this.addCommandLog("TaskMessage", "Task_start:" + this.scoreManager.GetChallengeInfoText());

						this.pubTaskInfo.SendROSMessage(this.taskInfoForRobot);
//						this.addCommandLog("SendROSMessage", "Task_Info");
//						this.addCommandLog("SendROSMessage", this.taskInfo.environment_id);
//						this.addCommandLog("SendROSMessage", this.taskInfo.target_object.name + this.taskInfo.target_object.position);

						this.scoreManager.TaskStart();
						
						//this.DoorOpen();

						SIGVerseLogger.Info("Waiting for end of trial");
//						this.addCommandLog("Info", "Waiting_for_end_of_trial");

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
							//this.addCommandLog("Info", "Going_to_next_trial");
							//if (HumanNaviConfig.Instance.configInfo.playbackType != HumanNaviPlaybackParam.PlaybackTypePlay) // TODO: delete
							this.SendROSHumanNaviMessage(MsgGoToNextTrial, "");

							//Destroy(this.environment);

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

		private void ApplicationQuitAfter1sec()
		{
			Thread.Sleep(1000);
			Application.Quit();
		}


		private void PreProcess()
		{
			this.ResetFlags();

			HumanNaviConfig.Instance.InclementNumberOfTrials(HumanNaviConfig.Instance.configInfo.playbackType);

			this.scoreManager.SetChallengeInfoText();
			this.scoreManager.ResetTimeText();

			this.ResetEnvironment();
			this.ResetAvatarTransform();
			this.ResetRobot();

			this.currentTaskInfo = taskInfoList[HumanNaviConfig.Instance.numberOfTrials - 1];

			this.taskInfoForRobot = new SIGVerse.ROSBridge.human_navigation.HumanNaviTaskInfo();
			this.taskInfoForRobot.environment_id = this.environment.name;
			this.SetObjectListToHumanNaviTaskInfo();
			this.SetDestinationToHumanNaviTaskInfo();

			this.waitingTime = 0.0f;

			//this.StartRecording();
			//			this.StartPlaying();

			SIGVerseLogger.Info("End of PreProcess");
		}

		private void PostProcess()
		{
			if (HumanNaviConfig.Instance.numberOfTrials == HumanNaviConfig.Instance.configInfo.maxNumberOfTrials)
			{
				this.SendROSHumanNaviMessage(MsgMissionComplete, "");
				base.StartCoroutine(this.ShowNoticeMessagePanel("Mission complete", 5.0f));
				base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar("Mission complete", 5.0f));

				//this.ShowMessageEffect(MessageEffect, this.birdviewCamera.gameObject, "Mission complete");
				//this.ShowMessageEffectForAvatar(messagePanelForAvatar, this.head.gameObject, "Mission complete");

				this.isAllTaskFinished = true;
			}
			else
			{
				this.SendROSHumanNaviMessage(MsgTaskFinished, "");

				SIGVerseLogger.Info("Waiting for next task");
				base.StartCoroutine(this.ShowGotoNextPanel(3.0f));
			}

//			this.StopRecording();
//			this.StopPlaying();

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
				//SIGVerseLogger.Error("Graspable object is not found.");
				throw new Exception("Graspable object is not found.");
			}

			foreach (GameObject graspableObject in graspableObjects)
			{
				// transtrate the coordinate system of GameObject (left-handed, Z-axis:front, Y-axis:up) to ROS coodinate system (right-handed, X-axis:front, Z-axis:up)
				Vector3 positionInROS = new Vector3(graspableObject.transform.position.z, -graspableObject.transform.position.x, graspableObject.transform.position.y);

				if (graspableObject.name == currentTaskInfo.target)
				{
					taskInfoForRobot.target_object.name = graspableObject.name;
					taskInfoForRobot.target_object.position = positionInROS;
				}
				else
				{
					SIGVerse.ROSBridge.human_navigation.HumanNaviObjectInfo objInfo = new SIGVerse.ROSBridge.human_navigation.HumanNaviObjectInfo
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
				//SIGVerseLogger.Error("Graspable object is not found.");
				throw new Exception("Destination candidate is not found.");
			}

			foreach (GameObject destination in destinations)
			{
				if (destination.name == this.currentTaskInfo.destination)
				{
					Vector3 conterOfCollider = destination.GetComponent<BoxCollider>().center;
					taskInfoForRobot.destination = destination.transform.position + conterOfCollider;
				}
			}
			SIGVerseLogger.Info("Destination : " + taskInfoForRobot.destination);

			if (taskInfoForRobot.destination == null)
			{
				throw new Exception("Destination is not found.");
			}
		}

		private void SendMessageAtIntervals(string message, string detail, int interval_ms = 1000)
		{
			this.waitingTime += UnityEngine.Time.deltaTime;

			if (this.waitingTime > interval_ms * 0.001)
			{
				this.SendROSHumanNaviMessage(MsgAreYouReady, "");
//				this.addCommandLog("SendROSMessage", MsgAreYouReady);
				this.waitingTime = 0.0f;
			}
		}

		private void DoorOpen()
		{
			GameObject doorWay = GameObject.Find("doorway"); // TODO: should be modified
			doorWay.transform.localPosition += doorWay.transform.right * (-0.9f);
//			this.addCommandLog("Info", "door_open");
		}

		public void TimeIsUp()
		{
			string strTimeup = "Time is up";
			this.SendROSHumanNaviMessage(MsgTaskFailed, strTimeup);
			base.StartCoroutine(this.ShowNoticeMessagePanel(strTimeup, 3.0f));
			base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar(strTimeup, 3.0f));
			//this.ShowMessageEffect(MessageEffect, this.birdviewCamera.gameObject, strTimeup);
			//this.ShowMessageEffectForAvatar(messagePanelForAvatar, this.head.gameObject, strTimeup);

			//			this.addCommandLog("Info", "Time_is up");

			this.TaskFinished();
		}

		public void GiveUp()
		{
			string strGiveup = "Give up";
			this.SendROSHumanNaviMessage(MsgTaskFailed, strGiveup);
			base.StartCoroutine(this.ShowNoticeMessagePanel(strGiveup, 3.0f));
			base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar(strGiveup, 3.0f));

			//this.ShowMessageEffect(MessageEffect, this.birdviewCamera.gameObject, strGiveup);
			//this.ShowMessageEffectForAvatar(messagePanelForAvatar, this.head.gameObject, strGiveup);

//			this.addCommandLog("Info", "Give up");

			this.TaskFinished();
		}

		public void StartTask()
		{
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

			//this.ShowMessageEffectForAvatar(messagePanelForAvatar, this.head.gameObject, "Waiting for next trial");
		}

		private void TaskFinished()
		{
			this.scoreManager.TaskEnd();
			this.PostProcess();
		}

		public void OnReceiveROSMessage(ROSBridge.human_navigation.HumanNaviMsg humanNaviMsg)
		{
			if (this.receivedMessageMap.ContainsKey(humanNaviMsg.message))
			{
				this.receivedMessageMap[humanNaviMsg.message] = true;

				if(humanNaviMsg.message == MsgGetAvatarPose)
				{
					ROSBridge.human_navigation.HumanNaviAvatarPose avatarPose = new ROSBridge.human_navigation.HumanNaviAvatarPose();

					avatarPose.head.position = ConvertCoorinateSystemUnityToROS_Position(this.head.transform.position);
					avatarPose.head.orientation = ConvertCoorinateSystemUnityToROS_Rotation(this.head.transform.rotation);
					avatarPose.left_hand.position = ConvertCoorinateSystemUnityToROS_Position(this.LeftHand.transform.position);
					avatarPose.left_hand.orientation = ConvertCoorinateSystemUnityToROS_Rotation(this.LeftHand.transform.rotation);
					avatarPose.right_hand.position = ConvertCoorinateSystemUnityToROS_Position(this.rightHand.transform.position);
					avatarPose.right_hand.orientation = ConvertCoorinateSystemUnityToROS_Rotation(this.rightHand.transform.rotation);

					this.pubAvatarPose.SendROSMessage(avatarPose);
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

		private void ShowEffects(GameObject effectPrefab, Vector3 objectPotition, Vector3 headRotation)
		{
			GameObject effect = MonoBehaviour.Instantiate(effectPrefab);
			effect.transform.position = objectPotition;
			//Vector3 relativePosition = (objectPotition - headPosition).normalized;
			//relativePosition.y = 0.0f;
			Vector3 direction = Vector3.Scale(headRotation, new Vector3(0, 1, 0));
			//Vector3 direction = Vector3.Scale(relativePosition / relativePosition.magnitude, Vector3.up);
			//Vector3 direction = relativePosition / relativePosition.magnitude;
			Debug.Log(direction);
			effect.transform.eulerAngles = direction;

			Destroy(effect, 3.0f);
		}

		//private void ShowMessageEffect(GameObject effectPrefab, GameObject camera, string message)
		//{
		//	GameObject effect = MonoBehaviour.Instantiate(effectPrefab);
		//	effect.GetComponentInChildren<TextMesh>().text = message;
		//	//effect.transform.position = camera.transform.position;
		//	effect.transform.position = camera.transform.position + camera.transform.forward * 0.5f - camera.transform.up * 0.025f;
		//	effect.transform.eulerAngles = camera.transform.eulerAngles;//Vector3.Scale(camera.transform.eulerAngles, new Vector3(0, 1, 0));

		//	Destroy(effect, 3.0f);
		//}

		private void ShowMessageEffectForAvatar(GameObject effectPrefab, GameObject camera, string message)
		{
			GameObject effect = MonoBehaviour.Instantiate(effectPrefab);
			effect.GetComponentInChildren<TextMesh>().text = message;
			//effect.transform.position = camera.transform.position;
			effect.transform.position = camera.transform.position + camera.transform.forward * 0.4f + camera.transform.up * 0.1f;
			effect.transform.eulerAngles = Vector3.Scale(camera.transform.eulerAngles, new Vector3(0, 1, 0));

			Destroy(effect, 3.0f);
		}

		private void SendROSHumanNaviMessage(string message, string detail)
		{
			ExecuteEvents.Execute<IRosSendHandler>
			(
				target: this.gameObject, 
				eventData: null, 
				functor: (reciever, eventData) => reciever.OnSendROSHumanNaviMessage(message, detail)
			);
		}

		private void CheckHandInteraction(NewtonVR.NVRHand hand)
		{
			if (hand.IsInteracting)
			{
//				this.addEventLog("Interaction", hand.name + ".IsInteracting: " + hand.CurrentlyInteracting + hand.CurrentlyInteracting.transform.position);

				if (hand.HoldButtonDown)
				{
//					this.addEventLog("Interaction", hand.name + ".HoldButtonDown");

					if (hand.CurrentlyInteracting.tag == "Graspables")
					{
//						this.addEventLog("Interaction", hand.name + ".IsGrasping: " + hand.CurrentlyInteracting + hand.CurrentlyInteracting.transform.position);

						if (hand.CurrentlyInteracting.name == this.currentTaskInfo.target)
						{
							if (!this.isTargetAlreadyGrasped)
							{
								SIGVerseLogger.Info("Target object is grasped");
//								this.addEventLog("Info", "Target_object_is_grasped");

								base.StartCoroutine(this.ShowNoticeMessagePanel("Target object is grasped", 3.0f));

								//ShowEffects(this.goodEffect, hand.CurrentlyInteracting.gameObject.transform.position, this.head.transform.eulerAngles);
								this.scoreManager.AddScore(Score.Type.CorrectObjectIsGrasped);
								this.scoreManager.AddTimeScoreOfGrasp();

								//this.showMessageEffect(MessageEffect, this.birdviewCamera.gameObject, "Task succeeded");


								this.isTargetAlreadyGrasped = true;
							}
						}
						else
						{
							if (!this.isTargetAlreadyGrasped)
							{
								SIGVerseLogger.Info("Incorrect object is grasped");
//								this.addEventLog("Info", "Incorrect_object_is_grasped");

								//ShowEffects(this.badEffect, this.rightHand.CurrentlyInteracting.gameObject.transform.position, this.head.transform.eulerAngles);
								//this.ShowMessageEffect(MessageEffect, this.birdviewCamera.gameObject, "Incorrect object is grasped");
								base.StartCoroutine(this.ShowNoticeMessagePanel("Incorrect object is grasped", 3.0f));

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
			Debug.Log("target is plasced on the destination.");

			this.scoreManager.AddScore(Score.Type.TargetObujectInDestination);
			this.scoreManager.AddTimeScoreOfPlacement();

			this.SendROSHumanNaviMessage(MsgTaskSucceeded, "");
			base.StartCoroutine(this.ShowNoticeMessagePanel("Task succeeded", 3.0f));
			base.StartCoroutine(this.ShowNoticeMessagePanelForAvatar("Task succeeded", 3.0f));

			//SIGVerseLogger.Info("Success '" + MsgTaskFinished + "'");

			this.TaskFinished();
		}
	}
}