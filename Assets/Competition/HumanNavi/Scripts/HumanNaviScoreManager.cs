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

		public enum Type
		{
			CorrectObjectIsGrasped,
			IncorrectObjectIsGrasped,
			TargetObjectInDestination,
			CompletionTime,
			CollisionEnter,
			OverSpeechCount,
		}

		public static int GetScore(Type scoreType)
		{
			switch(scoreType)
			{
				case Score.Type.CorrectObjectIsGrasped    : { return +20; }
				case Score.Type.IncorrectObjectIsGrasped  : { return -10; }
				case Score.Type.TargetObjectInDestination : { return +20; }
				case Score.Type.CompletionTime            : { return +30; }
				case Score.Type.CollisionEnter            : { return -10; }
				case Score.Type.OverSpeechCount           : { return  -3; }
			}

			throw new Exception("Illegal score type. Type = " + (int)scoreType + ", method name=(" + System.Reflection.MethodBase.GetCurrentMethod().Name + ")");
		}
	}

	public class HumanNaviScoreManager : MonoBehaviour, IHSRCollisionHandler, ISpeakGuidanceMessageHandler
	{
		private const string TimeFormat = "#####0";
		private const float DefaultTimeScale = 1.0f;

		[HeaderAttribute("Time left")]
		[TooltipAttribute("seconds")]
		public int timeLimit = 300;
		public int timeLimitForGrasp = 150;
		public int timeLimitForPlacement = 150;

		[HeaderAttribute("Speec Count")]
		public int LimitOfSpeechCount = 10;

		public List<GameObject> scoreNotificationDestinations;

		public List<string> timeIsUpDestinationTags;

		//---------------------------------------------------

		private float elapsedTimeForGrasp;
		private float elapsedTimeForPlacement;

		private GameObject mainMenu;

		private int score;
		private PanelMainController panelMainController;

		private List<GameObject> timeIsUpDestinations;

		private float timeLeft;

		private bool isRunning;

		private int speechCount;

		void Awake()
		{
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
		}

		// Use this for initialization
		void Start()
		{
			this.UpdateScoreText(0, HumanNaviConfig.Instance.GetTotalScore());

			this.timeLeft = (float)timeLimit;

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

		public void AddScore(Score.Type scoreType)
		{
			int additionalScore = Score.GetScore(scoreType);

			this.score = Mathf.Clamp(this.score + additionalScore, Score.MinScore, Score.MaxScore);

			this.UpdateScoreText(this.score);

			SIGVerseLogger.Info("Score (grasp) add [" + Score.GetScore(scoreType) + "], Challenge " + HumanNaviConfig.Instance.numberOfTrials + " Score=" + this.score);

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

		public void AddTimeScore(float elapsedTime, float timeLimit)
		{
			int additionalScore = Mathf.FloorToInt(Score.GetScore(Score.Type.CompletionTime) * ((timeLimit - elapsedTime) / timeLimit));

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

		public void AddTimeScoreOfGrasp()
		{
			this.elapsedTimeForGrasp = this.timeLimit - this.timeLeft;
			this.AddTimeScore(this.elapsedTimeForGrasp, this.timeLimitForGrasp);
		}

		public void AddTimeScoreOfPlacement()
		{
			this.elapsedTimeForPlacement = (this.timeLimit - this.timeLeft) - this.elapsedTimeForGrasp;
			this.AddTimeScore(this.elapsedTimeForPlacement, this.timeLimitForPlacement);
		}

		//---------------------------------------------------

		public void TaskStart()
		{
			this.isRunning = true;

			this.speechCount = 0;

			this.score = 0;

			this.UpdateScoreText(this.score);
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

		public void OnHsrCollisionEnter(Collision collision, float collisionVelocity, float effectScale)
		{
			this.AddScore(Score.Type.CollisionEnter);
		}

		public void OnSpeakGuidanceMessage(GuidanceMessageStatus guidanceMessageStatus)
		{
			this.speechCount++;

			if(this.speechCount > this.LimitOfSpeechCount)
			{
				this.AddScore(Score.Type.OverSpeechCount);
			}
		}
	}
}
