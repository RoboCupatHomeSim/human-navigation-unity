using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SIGVerse.Common;

namespace SIGVerse.Competition.HumanNavigation
{

	public class HumanNaviSessionManager : MonoBehaviour
	{
		[HeaderAttribute("Environment")]
		public List<GameObject> environments;

		[HeaderAttribute("Robot")]
		public GameObject robot;
		public string robotName;

		[HeaderAttribute("Camera Controller")]
		public HumanNaviBirdsEyeViewCameraController birdsEyeViewCameraControllerForRobot;

		private List<SIGVerse.Competition.HumanNavigation.TaskInfo> taskInfoList;

		private GameObject currentEnvironment;
		private GameObject currentRobot;

		private SAPIVoiceSynthesisExternal tts;

		private void Awake()
		{
			this.currentEnvironment = null;

			this.ClearExistingEnvironments();    // Environments
			this.ClearExistingRobots();          // Robot

			this.SetDefaultEnvironment();
			this.ResetRobot();

			this.InitializeTaskInfo();
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
			this.currentRobot.name = this.robotName;
			this.currentRobot.SetActive(true);

			this.tts = this.currentRobot.transform.Find("CompetitionScripts").GetComponent<SAPIVoiceSynthesisExternal>();

			this.birdsEyeViewCameraControllerForRobot.SetTarget(this.currentRobot.transform.Find("odom/base_footprint").gameObject);
		}

		public void InitializeTaskInfo()
		{
			this.taskInfoList = new List<SIGVerse.Competition.HumanNavigation.TaskInfo>();

			foreach (TaskInfo info in HumanNaviConfig.Instance.configInfo.taskInfoList)
			{
				taskInfoList.Add(info);
				SIGVerseLogger.Info("Environment_ID: " + info.environment + ", Target_object_name: " + info.target + ", Destination: " + info.destination);

				GameObject environment = this.environments.Where(obj => obj.name == info.environment).SingleOrDefault();
				if (environment == null)
				{
					SIGVerseLogger.Error("Environment not found.");
				}

				//// TODO: should be modified (sometimes error is occured)
				//Transform[] transformInChildren = environment.GetComponentsInChildren<Transform>();
				//if (transformInChildren.Where(obj => obj.gameObject.name == info.target).SingleOrDefault() == null)
				//{
				//	SIGVerseLogger.Error("Target object not found.");
				//}

				//if (transformInChildren.Where(obj => obj.gameObject.name == info.destination).SingleOrDefault() == null)
				//{
				//	SIGVerseLogger.Error("Destination not found.");
				//}


				/////
				// TODO: check duplication of object_id in a environment
				//Debug.Log(this.environmentPrefabs.Where(obj => obj.name == info.environment).SingleOrDefault());
				/////
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

		public GameObject GetCurrentEnvironment()
		{
			return this.currentEnvironment;
		}

		public string GetSeechRunStateMsgString()
		{
			if (this.tts.IsSpeaking()) { return "Is_speaking"; }
			else                       { return "Is_not_speaking"; }
		}

		public bool GetSeechRunState()
		{
			return this.tts.IsSpeaking();
		}

		public float GetDistanceFromRobot(Vector3 targetPosition)
		{
			Vector3 currentRobotPosition = this.currentRobot.transform.Find("odom/base_footprint").gameObject.transform.position;
			Vector2 robotPosition2D = new Vector2(currentRobotPosition.x, currentRobotPosition.z);
			Vector2 targetPosition2D = new Vector2(targetPosition.x, targetPosition.z);

			return (robotPosition2D - targetPosition2D).magnitude;
		}
	}
}