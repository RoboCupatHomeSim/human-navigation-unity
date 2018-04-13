using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SIGVerse.Competition.HumanNavigation
{
	public interface IStartTrialHandler : IEventSystemHandler
	{
		void OnStartTrial();
	}

	public class PanelStartTrialController : MonoBehaviour
	{
		public GameObject startTrialPanel;

		public List<string> startTrialDestinationTags;

		private List<GameObject> startTrialDestinations;

		void Awake()
		{
			this.startTrialDestinations = new List<GameObject>();

			foreach (string startTrialDestinationTag in this.startTrialDestinationTags)
			{
				GameObject[] giveUpDestinationArray = GameObject.FindGameObjectsWithTag(startTrialDestinationTag);

				foreach (GameObject giveUpDestination in giveUpDestinationArray)
				{
					this.startTrialDestinations.Add(giveUpDestination);
				}
			}

			this.startTrialPanel.SetActive(false);
		}

		//void Start()
		//{
		//}
		
		public void OnStartButtonClick()
		{
			foreach (GameObject startTrialDestination in this.startTrialDestinations)
			{
				ExecuteEvents.Execute<IStartTrialHandler>
				(
					target: startTrialDestination,
					eventData: null,
					functor: (reciever, eventData) => reciever.OnStartTrial()
				);
			}
		}
	}
}

