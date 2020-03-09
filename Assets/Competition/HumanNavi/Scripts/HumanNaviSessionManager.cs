using System.Collections;
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
		[HeaderAttribute("Robot")]
		public GameObject robot;

		[HeaderAttribute("Environment")]
		public GameObject defaultEnvironment;
		public List<GameObject> environments;

		private Transform robotBaseFootprint;

		private GameObject targetEnvironment;

		private List<SIGVerse.Competition.HumanNavigation.TaskInfo> taskInfoList;

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

					robot.transform.Find("RosBridgeScripts").gameObject.SetActive(false);
					robot.GetComponentInChildren<HumanNaviSubGuidanceMessage>().enabled = false;

					foreach (Camera camera in robot.transform.GetComponentsInChildren<Camera>())
					{
						camera.gameObject.SetActive(false);
					}
				}

				this.robot.SetActive(false);
				this.robotBaseFootprint = SIGVerseUtils.FindTransformFromChild(this.robot.transform, "base_footprint");

				this.InitializeEnvironments();

				this.tts = this.robot.transform.Find("CompetitionScripts").GetComponent<SAPIVoiceSynthesisExternal>();

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

		public void InitializeEnvironments()
		{
			this.defaultEnvironment.SetActive(true);

			foreach (GameObject environment in this.environments)
			{
				environment.SetActive(false);
			}
		}

		public void ActivateRobot()
		{
			this.robot.SetActive(true);
		}

		public void InitializeTaskInfo()
		{
			//Check for duplicates
			List<string> duplicateNames = this.environments.GroupBy(obj => obj.name).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

			if(duplicateNames.Count > 0)
			{
				throw new Exception("There are multiple environments with the same name. e.g. " + duplicateNames[0]); 
			}


			this.taskInfoList = new List<SIGVerse.Competition.HumanNavigation.TaskInfo>();

			foreach (TaskInfo taskInfo in HumanNaviConfig.Instance.configInfo.taskInfoList)
			{
				taskInfoList.Add(taskInfo);

				SIGVerseLogger.Info("Environment_ID: " + taskInfo.environment + ", Target_object_name: " + taskInfo.target + ", Destination: " + taskInfo.destination);

				GameObject environment = this.environments.Where(obj => obj.name == taskInfo.environment).SingleOrDefault();

				if (environment == null)
				{
					throw new Exception("Environment does not exist. name=" + taskInfo.environment);
				}

				Transform[] transformInChildren = environment.GetComponentsInChildren<Transform>();

				try
				{
					if(transformInChildren.Where(obj => obj.gameObject.name == taskInfo.target).SingleOrDefault()==null)
					{
						throw new Exception("Target object does not exist.");
					}
				}
				catch(InvalidOperationException)
				{
					throw new Exception("There are multiple target objects.");
				}

				try
				{
					if (transformInChildren.Where(obj => obj.gameObject.name == taskInfo.destination).SingleOrDefault() == null)
					{
						throw new Exception("Destination does not exist.");
					}
				}
				catch (InvalidOperationException)
				{
					throw new Exception("There are multiple destinations.");
				}
			}
		}

		public void ChangeEnvironment(int numberOfSession)
		{
			this.defaultEnvironment.SetActive(false);

			foreach(GameObject environment in this.environments)
			{
				environment.SetActive(false);
			}

			this.targetEnvironment = this.environments.Where(obj => obj.name == this.taskInfoList[numberOfSession - 1].environment).SingleOrDefault();
			this.targetEnvironment.SetActive(true);

			//Check for duplicates
			List<string> duplicateGraspableNames = GameObject.FindGameObjectsWithTag("Graspables").ToList().GroupBy(obj => obj.name).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

			if (duplicateGraspableNames.Count > 0)
			{
				throw new Exception("There are multiple graspables with the same name. e.g. " + duplicateGraspableNames[0]);
			}

			List<string> duplicateDestinationNames = GameObject.FindGameObjectsWithTag("Destination").ToList().GroupBy(obj => obj.name).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

			if (duplicateDestinationNames.Count > 0)
			{
				throw new Exception("There are multiple destination with the same name. e.g. " + duplicateDestinationNames[0]);
			}
		}

		public TaskInfo GetTaskInfo(int numberOfSession)
		{
			return taskInfoList[numberOfSession - 1];
		}

		public GameObject GetEnvironment()
		{
			return this.targetEnvironment;
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
			Vector2 robotPosition2D = new Vector2(this.robotBaseFootprint.position.x, this.robotBaseFootprint.position.z);
			Vector2 targetPosition2D = new Vector2(targetPosition.x, targetPosition.z);

			return (robotPosition2D - targetPosition2D).magnitude;
		}

		public void ResetNotificationDestinationsOfTTS()
		{
			this.tts.ResetNotificationDestinations();
		}
	}
}