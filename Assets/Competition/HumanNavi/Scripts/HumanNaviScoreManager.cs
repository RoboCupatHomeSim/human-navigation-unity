using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using SIGVerse.Common;
using SIGVerse.ToyotaHSR;

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
			Time,
			CollisionEnter,
		}

		public static int GetScore(Type scoreType)
		{
			switch(scoreType)
			{
				case Score.Type.CorrectObjectIsGrasped    : { return +20; }
				case Score.Type.IncorrectObjectIsGrasped  : { return -10; }
				case Score.Type.TargetObjectInDestination : { return +20; }
				case Score.Type.Time                      : { return +30; }
				case Score.Type.CollisionEnter            : { return -10; }
			}

			throw new Exception("Illegal score type. Type = " + (int)scoreType + ", method name=(" + System.Reflection.MethodBase.GetCurrentMethod().Name + ")");
		}
	}

	public class HumanNaviScoreManager : MonoBehaviour, IHSRCollisionHandler
	{
		private const string TimeFormat = "#####0";
		private const float DefaultTimeScale = 1.0f;

		public HumanNaviModerator moderator;

		[HeaderAttribute("Task status")]
		public Text challengeInfoText;
		public Text taskMessageText;

		//[HeaderAttribute("Buttons")]
		//public Button startButton;

		[HeaderAttribute("Time left")]
		[TooltipAttribute("seconds")]
		public int timeLimit = 600;
		public int timeLimitForGrasp = 150;
		public int timeLimitForPlacement = 150;

		public Text timeLeftValueText;

		[HeaderAttribute("Score")]
		public Text scoreValText;
		public Text totalValText;
		//---------------------------------------------------

//		private float timeLeft;
		private float elapsedTime;
		private float elapsedTimeForGrasp;
		private float elapsedTimeForPlacement;

		private int score;

		//private List<int> scores;

		//private int totalScore = 0;

		private bool isRunning;


		// Use this for initialization
		void Start()
		{
			//this.timeLeft = (float)this.timeLimit;
			this.elapsedTime = 0.0f;

			//this.timeLeftValueText.text = this.timeLeft.ToString(TimeFormat);
			this.SetTimeValText();
			this.scoreValText.text = "0";

			this.totalValText.text = HumanNaviConfig.Instance.GetTotalScore().ToString();
			//this.totalScore = 0;

			this.isRunning = false;
		}

		// Update is called once per frame
		void Update()
		{
			if (this.isRunning)
			{
				//	this.timeLeft = Mathf.Max(0.0f, this.timeLeft-Time.deltaTime);

				//	this.timeLeftValueText.text = this.timeLeft.ToString(TimeFormat);

				//	if(this.timeLeft == 0.0f)
				//	{
				//		this.moderator.TimeUp();
				//	}

				//this.timeLeft = Mathf.Max(0.0f, this.timeLeft - Time.deltaTime);
				this.elapsedTime += Time.deltaTime;

				//this.timeLeftValueText.text = this.timeLeft.ToString(TimeFormat);
				this.SetTimeValText();

				//if (this.timeLeft == 0.0f)
				if (this.elapsedTime > this.timeLimit)
				{
					this.moderator.TimeIsUp();
				}
			}
		}

		public void AddScore(Score.Type scoreType)
		{
			//this.scores[this.scores.Count - 1] = Mathf.Clamp(this.scores[this.scores.Count - 1] + Score.GetScore(scoreType), Score.MinScore, Score.MaxScore);
			//this.scoreValText.text = this.scores[this.scores.Count - 1].ToString(timeFormat);
			this.score = Mathf.Clamp(this.score + Score.GetScore(scoreType), Score.MinScore, Score.MaxScore);
			this.scoreValText.text = this.score.ToString(TimeFormat);

			//SIGVerseLogger.Info("Score add [" + Score.GetScore(scoreType) + "], Challenge " + this.scores.Count + " Score=" + this.scores[this.scores.Count - 1]);
			SIGVerseLogger.Info("Score (grasp) add [" + Score.GetScore(scoreType) + "], Challenge " + HumanNaviConfig.Instance.numberOfTrials + " Score=" + this.score);
		}

		public void AddTimeScore(float elspsedTime, float timeLimit)
		{
			int timeScore = Mathf.FloorToInt(Score.GetScore(Score.Type.Time) * ((timeLimit - elapsedTime) / timeLimit));
			this.score = Mathf.Clamp(this.score + timeScore, Score.MinScore, Score.MaxScore);
			this.scoreValText.text = this.score.ToString(TimeFormat);

			SIGVerseLogger.Info("Score (time) add [" + timeScore + "], Challenge " + HumanNaviConfig.Instance.numberOfTrials + " Score=" + this.score);
		}

		public void AddTimeScore()
		{
			this.AddTimeScore(this.elapsedTime, this.timeLimit);
		}
		public void AddTimeScoreOfGrasp()
		{
			this.elapsedTimeForGrasp = this.elapsedTime;

			this.AddTimeScore(this.elapsedTimeForGrasp, this.timeLimitForGrasp);
		}
		public void AddTimeScoreOfPlacement()
		{
			this.elapsedTimeForGrasp = this.elapsedTime - this.elapsedTimeForGrasp;
			this.AddTimeScore(this.elapsedTimeForGrasp, this.timeLimitForPlacement);
		}


		public void TaskStart()
		{
			this.score = 0;

			this.scoreValText.text = this.score.ToString("###0");

			this.isRunning = true;
		}

		public void TaskEnd()
		{
			HumanNaviConfig.Instance.AddScore(this.score);

			this.totalValText.text = HumanNaviConfig.Instance.GetTotalScore().ToString();

			SIGVerseLogger.Info("Total Score=" + HumanNaviConfig.Instance.GetTotalScore());

			HumanNaviConfig.Instance.RecordScoreInFile();

			this.isRunning = false;
		}

		public void ResetTimeText()
		{
			//this.timeLeft = (float)this.timeLimit;
			//this.timeLeft = Mathf.Max(0.0f, this.timeLeft - Time.deltaTime);

			this.elapsedTime = 0.0f;
			this.SetTimeValText();
		}

		public void SetTimeValText()
		{
			this.timeLeftValueText.text = this.elapsedTime.ToString(TimeFormat) + " / " + this.timeLimit.ToString(TimeFormat);
		}

		public void SetTaskMessageText(string taskMessage)
		{
			this.taskMessageText.text = taskMessage;
		}

		public void SetChallengeInfoText()
		{
			int numberOfTrials = HumanNaviConfig.Instance.numberOfTrials;

			string ordinal;

			if (numberOfTrials == 11 || numberOfTrials == 12 || numberOfTrials == 13)
			{
				ordinal = "th";
			}
			else
			{
				if (numberOfTrials % 10 == 1)
				{
					ordinal = "st";
				}
				else if (numberOfTrials % 10 == 2)
				{
					ordinal = "nd";
				}
				else if (numberOfTrials % 10 == 3)
				{
					ordinal = "rd";
				}
				else
				{
					ordinal = "th";
				}
			}

			this.challengeInfoText.text = numberOfTrials + ordinal + " challenge";

			SIGVerseLogger.Info("###### " + this.GetChallengeInfoText() + " #####");
		}

		public bool CheckIsRunning()
		{
			return this.isRunning;
		}

		public string GetChallengeInfoText()
		{
			return this.challengeInfoText.text;
		}

		public void OnHsrCollisionEnter(Collision collision, float collisionVelocity, float effectScale)
		{
			this.AddScore(Score.Type.CollisionEnter);
		}
	}
}
