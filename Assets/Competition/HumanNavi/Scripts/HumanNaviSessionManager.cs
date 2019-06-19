﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SIGVerse.Common;
using System;
using System.Threading;

namespace SIGVerse.Competition.HumanNavigation
{

	public class HumanNaviSessionManager : MonoBehaviour
	{
		[HeaderAttribute("Environment")]
		public List<GameObject> environments;

		[HeaderAttribute("Robot")]
		public GameObject robot;
		public string basefootprintPath = "odom/base_footprint_pos_noise/base_footprint_rigidbody/base_footprint_rot_noise/base_footprint";

		[HeaderAttribute("Camera Controller")]
		public HumanNaviBirdsEyeViewCameraController birdsEyeViewCameraControllerForRobot;

		private List<SIGVerse.Competition.HumanNavigation.TaskInfo> taskInfoList;

		private GameObject currentEnvironment;
		private GameObject currentRobot;

		private SAPIVoiceSynthesisExternal tts;

		private void Awake()
		{
			try
			{
				// For practice mode
				if (HumanNaviConfig.Instance.configInfo.executionMode == (int)ExecutionMode.Practice)
				{
					this.GetComponent<HumanNaviPubTaskInfo>().enabled = false;
					this.GetComponent<HumanNaviPubMessage>().enabled = false;
					this.GetComponent<HumanNaviSubMessage>().enabled = false;
					this.GetComponent<HumanNaviPubAvatarStatus>().enabled = false;
					this.GetComponent<HumanNaviPubObjectStatus>().enabled = false;

					GameObject robot = GameObject.Find("HSR-B"); // TODO
					robot.transform.Find("RosBridgeScripts").gameObject.SetActive(false); // TODO
					robot.GetComponentInChildren<HumanNaviSubGuidanceMessage>().enabled = false;

					foreach (Camera camera in robot.transform.GetComponentsInChildren<Camera>())
					{
						camera.gameObject.SetActive(false);
					}
				}

				this.currentEnvironment = null;

				this.ClearExistingEnvironments();    // Environments
				this.ClearExistingRobots();          // Robot

				this.SetDefaultEnvironment();
				this.ResetRobot();

				this.InitializeTaskInfo();
			}
			catch (Exception e)
			{
				SIGVerseLogger.Error(e.Message);
				this.ApplicationQuitAfter1sec();
			}
		}

		private void ApplicationQuitAfter1sec()
		{
			Thread.Sleep(1000);
			Application.Quit();
		}

		public void ClearExistingEnvironments()
		{
			foreach (GameObject existingEnvironment in GameObject.FindGameObjectsWithTag("Environment"))
			{
				existingEnvironment.SetActive(false);
			}
		}

		public void ClearExistingRobots()
		{
			List<GameObject> existingRobots = GameObject.FindGameObjectsWithTag("Robot").ToList<GameObject>();
			foreach (GameObject existingRobot in existingRobots)
			{
				existingRobot.SetActive(false);
			}
		}

		public void ResetRobot()
		{
			if (this.currentRobot != null)
			{
				this.currentRobot.SetActive(false); // For guidance message panel controller
				Destroy(this.currentRobot);
			}

			this.currentRobot = MonoBehaviour.Instantiate(this.robot);
			this.currentRobot.name = this.robot.name;
			this.currentRobot.SetActive(true);

			this.tts = this.currentRobot.transform.Find("CompetitionScripts").GetComponent<SAPIVoiceSynthesisExternal>();

			this.birdsEyeViewCameraControllerForRobot.SetTarget(this.currentRobot.transform.Find(this.basefootprintPath).gameObject);
		}

		public void InitializeTaskInfo()
		{
			this.taskInfoList = new List<SIGVerse.Competition.HumanNavigation.TaskInfo>();

			foreach (TaskInfo taskInfo in HumanNaviConfig.Instance.configInfo.taskInfoList)
			{
				taskInfoList.Add(taskInfo);
				SIGVerseLogger.Info("Environment_ID: " + taskInfo.environment + ", Target_object_name: " + taskInfo.target + ", Destination: " + taskInfo.destination);

				GameObject environment = this.environments.Where(obj => obj.name == taskInfo.environment).SingleOrDefault();

				if (environment == null)
				{
					throw new Exception("Environment does not exist or multiple exist.");
				}

				Transform[] transformInChildren = environment.GetComponentsInChildren<Transform>();

				if (transformInChildren.Where(obj => obj.gameObject.name == taskInfo.target).SingleOrDefault() == null)
				{
					throw new Exception("Target object does not exist or multiple exist.");
				}

				if (transformInChildren.Where(obj => obj.gameObject.name == taskInfo.destination).SingleOrDefault() == null)
				{
					throw new Exception("Destination does not exist or multiple exist.");
				}
			}
		}

		public void SetDefaultEnvironment()
		{
			if (this.currentEnvironment != null)
			{
				this.currentEnvironment.SetActive(false);
				MonoBehaviour.Destroy(this.currentEnvironment);
			}
			this.currentEnvironment = MonoBehaviour.Instantiate(this.environments.Where(obj => obj.name == "Default_Environment").SingleOrDefault());
			this.currentEnvironment.name = "Default_Environment";
			this.currentEnvironment.SetActive(true);
		}

		public void ResetEnvironment()
		{
			this.ResetEnvironment(HumanNaviConfig.Instance.numberOfTrials);
		}

		public void ResetEnvironment(int numberOfSession)
		{
			if (this.currentEnvironment != null)
			{
				this.currentEnvironment.SetActive(false);
				MonoBehaviour.Destroy(this.currentEnvironment);
			}
			this.currentEnvironment = MonoBehaviour.Instantiate(this.environments.Where(obj => obj.name == this.taskInfoList[numberOfSession - 1].environment).SingleOrDefault());
			this.currentEnvironment.name = this.taskInfoList[numberOfSession - 1].environment;
			this.currentEnvironment.SetActive(true);
		}

		public TaskInfo GetCurrentTaskInfo()
		{
			return taskInfoList[HumanNaviConfig.Instance.numberOfTrials - 1];
		}

		public TaskInfo GetCurrentTaskInfo(int numberOfSession)
		{
			return taskInfoList[numberOfSession - 1];
		}

		public GameObject GetCurrentEnvironment()
		{
			return this.currentEnvironment;
		}

		public string GetSeechRunStateMsgString()
		{
			if (this.tts.IsSpeaking()) { return "Is_speaking"; }
			else                       { return "Is_not_speaking"; }
		}

		public bool GetTTSRuningState()
		{
			return this.tts.IsSpeaking();
		}

		public void SpeakGuidanceMessageForPractice(string msg, string displeyType = "All")
		{
			RosBridge.human_navigation.HumanNaviGuidanceMsg guidanceMsg = new RosBridge.human_navigation.HumanNaviGuidanceMsg();
			guidanceMsg.message = msg;
			guidanceMsg.display_type = displeyType;
			this.tts.OnReceiveROSHumanNaviGuidanceMessage(guidanceMsg);
		}

		public float GetDistanceFromRobot(Vector3 targetPosition)
		{
			Vector3 currentRobotPosition = this.currentRobot.transform.Find(this.basefootprintPath).gameObject.transform.position;
			Vector2 robotPosition2D = new Vector2(currentRobotPosition.x, currentRobotPosition.z);
			Vector2 targetPosition2D = new Vector2(targetPosition.x, targetPosition.z);

			return (robotPosition2D - targetPosition2D).magnitude;
		}

		public void ResetNotificationDestinationsOfTTS()
		{
			this.tts.ResetNotificationDestinations();
		}
	}
}