using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using SIGVerse.Common;
using SIGVerse.ToyotaHSR;
using UnityEngine.EventSystems;

namespace SIGVerse.Competition.HumanNavigation
{
	public static class Score
	{
		public const int MaxScore = +999;
		public const int MinScore = -999;

		public enum ScoreType
		{
			CorrectObjectIsGrasped,
			IncorrectObjectIsGrasped,
			TargetObjectInDestination,
			CompletionTime,
			CollisionEnter,
			OverSpeechCount,
			DistancePenaltyForTargetObject,
			DistancePenaltyForDestination,
		}

		public enum TimePnaltyType
		{
			IncorrectObjectIsGrasped,
			OverSpeechCount,
		}

		public static int GetScore(ScoreType scoreType)
		{
			switch(scoreType)
			{
				case Score.ScoreType.CorrectObjectIsGrasped         : { return +20; }
				case Score.ScoreType.IncorrectObjectIsGrasped       : { return  -5; }
				case Score.ScoreType.TargetObjectInDestination      : { return +20; }
				case Score.ScoreType.CompletionTime                 : { return +60; }
				case Score.ScoreType.CollisionEnter                 : { return -10; }
				case Score.ScoreType.OverSpeechCount                : { return  -3; }
				case Score.ScoreType.DistancePenaltyForTargetObject : { return -40; }
				case Score.ScoreType.DistancePenaltyForDestination  : { return -40; }
			}

			throw new Exception("Illegal score type. ScoreType = " + (int)scoreType + ", method name=(" + System.Reflection.MethodBase.GetCurrentMethod().Name + ")");
		}

		public static int GetTimePenalty(TimePnaltyType penaltyType)
		{
			switch (penaltyType)
			{
				case Score.TimePnaltyType.IncorrectObjectIsGrasped: { return -25; }
				case Score.TimePnaltyType.OverSpeechCount:          { return -15; }
			}

			throw new Exception("Illegal time penalty type. PenaltyType = " + (int)penaltyType + ", method name=(" + System.Reflection.MethodBase.GetCurrentMethod().Name + ")");
		}
	}

	public class HumanNaviScoreManager : MonoBehaviour, IRobotCollisionHandler, ISpeakGuidanceMessageHandler
	{
		private const string TimeFormat = "#####0";
		private const float DefaultTimeScale = 1.0f;

		[HeaderAttribute("Time left")]
		[TooltipAttribute("seconds")]
		public int timeLimitForGrasp = 150;
		public int timeLimitForPlacement = 150;

		[HeaderAttribute("Speech Count")]
		public int LimitOfSpeechCount = 15;

		[HeaderAttribute("Distance from Target")]
		public float limitDistanceFromTarget = 3.0f;

		[HeaderAttribute("Max Count of Wrong Object Grasp")]
		public int maxWrongObjectGraspCount = -1;

		public List<GameObject> scoreNotificationDestinations;

		public List<string> timeIsUpDestinationTags;

		public List<string> reachMaxWrongObjectGraspCountDestinationTags;

		//---------------------------------------------------

		private int timeLimit;

		private float elapsedTimeForGrasp;
		private float elapsedTimeForPlacement;

		private GameObject mainMenu;

		private int score;
		private PanelMainController panelMainController;

		private List<GameObject> timeIsUpDestinations;

		private float timeLeft;

		private bool isRunning;

		private int speechCount;

		private List<GameObject> reachMaxWrongObjectGraspCountDestinations;

		private bool isAlreadyGivenDistancePenaltyForTargetObject;
		private bool isAlreadyGivenDistancePenaltyForDestination;

		private int wrongObjectGraspCount;

		void Awake()
		{
			this.timeLimit = HumanNaviConfig.Instance.configInfo.sessionTimeLimit;

			this.mainMenu = GameObject.FindGameObjectWithTag("MainMenu");

			this.panelMainController = this.mainMenu.GetComponent<PanelMainController>();

			this.timeIsUpDestinations = new List<GameObject>();
			foreach (string timeIsUpDestinationTag in this.timeIsUpDestinationTags)
			{
				GameObject[] timeIsUpDestinationArray = GameObject.FindGameObjectsWithTag(timeIsUpDestinationTag);

				foreach (GameObject timeIsUpDestination in timeIsUpDestinationArray)
				{
					this.timeIsUpDestinations.Add(timeIsUpDestination);
				}
			}

			this.reachMaxWrongObjectGraspCountDestinations = new List<GameObject>();
			foreach (string reachMaxWrongObjectGraspCountDestinationTag in this.reachMaxWrongObjectGraspCountDestinationTags)
			{
				GameObject[] reachMaxWrongObjectGraspCountDestinationArray = GameObject.FindGameObjectsWithTag(reachMaxWrongObjectGraspCountDestinationTag);

				foreach (GameObject reachMaxWrongObjectGraspCountDestination in reachMaxWrongObjectGraspCountDestinationArray)
				{
					this.reachMaxWrongObjectGraspCountDestinations.Add(reachMaxWrongObjectGraspCountDestination);
				}
			}

		}

		// Use this for initialization
		void Start()
		{
			this.UpdateScoreText(0, HumanNaviConfig.Instance.GetTotalScore());

			this.timeLeft = (float)this.timeLimit;

			this.panelMainController.SetTimeLeft(this.timeLeft);

			this.isRunning = false;
		}

		// Update is called once per frame
		void Update()
		{
			if (this.isRunning)
			{
				this.timeLeft = Mathf.Max(0.0f, this.timeLeft - Time.deltaTime);
				this.panelMainController.SetTimeLeft(this.timeLeft);

				if (this.timeLeft == 0.0f)
				{
					foreach (GameObject timeIsUpDestination in this.timeIsUpDestinations)
					{
						ExecuteEvents.Execute<ITimeIsUpHandler>
						(
							target: timeIsUpDestination,
							eventData: null,
							functor: (reciever, eventData) => reciever.OnTimeIsUp()
						);
					}
				}
			}
		}

		//---------------------------------------------------

		public void AddScore(Score.ScoreType scoreType)
		{
			if (!this.isRunning)
			{
				return;
			}

			int additionalScore = Score.GetScore(scoreType);

			this.score = Mathf.Clamp(this.score + additionalScore, Score.MinScore, Score.MaxScore);

			this.UpdateScoreText(this.score);

			SIGVerseLogger.Info("Score (" + scoreType.ToString() + ") add [" + Score.GetScore(scoreType) + "], Challenge " + HumanNaviConfig.Instance.numberOfTrials + " Score=" + this.score);

			// Send the Score Notification
			ScoreStatus scoreStatus = new ScoreStatus(additionalScore, this.score, HumanNaviConfig.Instance.GetTotalScore());

			foreach (GameObject scoreNotificationDestination in this.scoreNotificationDestinations)
			{
				ExecuteEvents.Execute<IScoreHandler>
				(
					target: scoreNotificationDestination,
					eventData: null,
					functor: (reciever, eventData) => reciever.OnScoreChange(scoreStatus)
				);
			}

			if(scoreType == Score.ScoreType.IncorrectObjectIsGrasped)
			{
				this.wrongObjectGraspCount++;
			}

			if (this.maxWrongObjectGraspCount > 0)
			{
				if (this.wrongObjectGraspCount >= this.maxWrongObjectGraspCount)
				{
					foreach (GameObject reachMaxWrongObjectGraspCountDestination in this.reachMaxWrongObjectGraspCountDestinations)
					{
						ExecuteEvents.Execute<IReachMaxWrongObjectGraspCountHandler>
						(
							target: reachMaxWrongObjectGraspCountDestination,
							eventData: null,
							functor: (reciever, eventData) => reciever.OnReachMaxWrongObjectGraspCount()
						);
					}
				}
			}
		}

		public void AddTimeScore()
		{
			int additionalScore = Mathf.FloorToInt(Score.GetScore(Score.ScoreType.CompletionTime) * (this.timeLeft / this.timeLimit));

			this.score = Mathf.Clamp(this.score + additionalScore, Score.MinScore, Score.MaxScore);
			this.UpdateScoreText(this.score);

			SIGVerseLogger.Info("Score (time) add [" + additionalScore + "], Challenge " + HumanNaviConfig.Instance.numberOfTrials + " Score=" + this.score);

			// Send the Score Notification
			ScoreStatus scoreStatus = new ScoreStatus(additionalScore, this.score, HumanNaviConfig.Instance.GetTotalScore());

			foreach (GameObject scoreNotificationDestination in this.scoreNotificationDestinations)
			{
				ExecuteEvents.Execute<IScoreHandler>
				(
					target: scoreNotificationDestination,
					eventData: null,
					functor: (reciever, eventData) => reciever.OnScoreChange(scoreStatus)
				);
			}
		}

		//public void AddTimeScore(float elapsedTime, float timeLimit)
		//{
		//	int additionalScore = Mathf.FloorToInt(Score.GetScore(Score.ScoreType.CompletionTime) * ((timeLimit - elapsedTime) / timeLimit));

		//	this.score = Mathf.Clamp(this.score + additionalScore, Score.MinScore, Score.MaxScore);
		//	this.UpdateScoreText(this.score);

		//	SIGVerseLogger.Info("Score (time) add [" + additionalScore + "], Challenge " + HumanNaviConfig.Instance.numberOfTrials + " Score=" + this.score);

		//	// Send the Score Notification
		//	ScoreStatus scoreStatus = new ScoreStatus(additionalScore, this.score, HumanNaviConfig.Instance.GetTotalScore());

		//	foreach (GameObject scoreNotificationDestination in this.scoreNotificationDestinations)
		//	{
		//		ExecuteEvents.Execute<IScoreHandler>
		//		(
		//			target: scoreNotificationDestination,
		//			eventData: null,
		//			functor: (reciever, eventData) => reciever.OnScoreChange(scoreStatus)
		//		);
		//	}
		//}

		//public void AddTimeScoreOfGrasp()
		//{
		//	this.elapsedTimeForGrasp = this.timeLimit - this.timeLeft;
		//	this.AddTimeScore(this.elapsedTimeForGrasp, this.timeLimitForGrasp);
		//}

		//public void AddTimeScoreOfPlacement()
		//{
		//	this.elapsedTimeForPlacement = (this.timeLimit - this.timeLeft) - this.elapsedTimeForGrasp;
		//	this.AddTimeScore(this.elapsedTimeForPlacement, this.timeLimitForPlacement);
		//}

		public void ImposeTimePenalty(Score.TimePnaltyType penaltyType)
		{
			if (!this.isRunning)
			{
				return;
			}

			// event (send notification) for playback [TODO]

			this.timeLeft = Mathf.Max(0.0f, this.timeLeft + Score.GetTimePenalty(penaltyType));
		}

		//---------------------------------------------------

		public bool IsAlreadyGivenDistancePenaltyForTargetObject()
		{
			return this.isAlreadyGivenDistancePenaltyForTargetObject;
		}
		public bool IsAlreadyGivenDistancePenaltyForDestination()
		{
			return this.isAlreadyGivenDistancePenaltyForDestination;
		}

		public void AddDistancePenaltyForTargetObject()
		{
			this.isAlreadyGivenDistancePenaltyForTargetObject = true;
			this.AddScore(Score.ScoreType.DistancePenaltyForTargetObject);
		}
		public void AddDistancePenaltyForDestination()
		{
			this.isAlreadyGivenDistancePenaltyForDestination = true;
			this.AddScore(Score.ScoreType.DistancePenaltyForDestination);
		}


		//---------------------------------------------------

		public void TaskStart()
		{
			this.isRunning = true;

			this.speechCount = 0;

			this.score = 0;

			this.wrongObjectGraspCount = 0;

			this.UpdateScoreText(this.score);

			this.isAlreadyGivenDistancePenaltyForTargetObject = false;
			this.isAlreadyGivenDistancePenaltyForDestination  = false;
		}

		public void TaskEnd()
		{
			HumanNaviConfig.Instance.AddScore(this.score);

			this.UpdateScoreText(this.score, HumanNaviConfig.Instance.GetTotalScore());

			SIGVerseLogger.Info("Total Score=" + HumanNaviConfig.Instance.GetTotalScore());

			HumanNaviConfig.Instance.RecordScoreInFile();

			this.isRunning = false;
		}

		//---------------------------------------------------

		public void ResetTimeLeftText()
		{
			this.timeLeft = (float)timeLimit;
			this.panelMainController.SetTimeLeft(this.timeLeft);
		}

		private void UpdateScoreText(float score)
		{
			ExecuteEvents.Execute<IPanelScoreHandler>
			(
				target: this.mainMenu,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnScoreChange(score)
			);
		}

		private void UpdateScoreText(float score, float total)
		{
			ExecuteEvents.Execute<IPanelScoreHandler>
			(
				target: this.mainMenu,
				eventData: null,
				functor: (reciever, eventData) => reciever.OnScoreChange(score, total)
			);
		}

		//---------------------------------------------------

		public void OnRobotCollisionEnter(Collision collision, float collisionVelocity, float effectScale)
		{
			this.AddScore(Score.ScoreType.CollisionEnter);
		}

		public void OnSpeakGuidanceMessage(GuidanceMessageStatus guidanceMessageStatus)
		{
			this.speechCount++;

			if(this.speechCount > this.LimitOfSpeechCount)
			{
				this.AddScore(Score.ScoreType.OverSpeechCount);
				//this.ImposeTimePenalty(Score.TimePnaltyType.OverSpeechCount);
			}
		}

		public float GetElapsedTime()
		{
			return this.timeLimit - this.timeLeft;
		}
	}
}
