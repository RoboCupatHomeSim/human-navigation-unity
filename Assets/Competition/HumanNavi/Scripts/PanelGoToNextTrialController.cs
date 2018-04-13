using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SIGVerse.Competition.HumanNavigation
{
	public interface IGoToNextTrialHandler : IEventSystemHandler
	{
		void OnGoToNextTrial();
	}

	public class PanelGoToNextTrialController : MonoBehaviour
	{
		public GameObject goToNextTrialPanel;

		public List<string> goToNextTrialDestinationTags;

		private List<GameObject> goToNextTrialDestinations;


		void Awake()
		{
			this.goToNextTrialDestinations = new List<GameObject>();

			foreach (string goToNextTrialDestinationTag in this.goToNextTrialDestinationTags)
			{
				GameObject[] giveUpDestinationArray = GameObject.FindGameObjectsWithTag(goToNextTrialDestinationTag);

				foreach (GameObject giveUpDestination in giveUpDestinationArray)
				{
					this.goToNextTrialDestinations.Add(giveUpDestination);
				}
			}

			this.goToNextTrialPanel.SetActive(false);
		}

		//void Start()
		//{
		//}
		
		public void OnGoToNextButtonClick()
		{
			foreach (GameObject goToNextTrialDestination in this.goToNextTrialDestinations)
			{
				ExecuteEvents.Execute<IGoToNextTrialHandler>
				(
					target: goToNextTrialDestination,
					eventData: null,
					functor: (reciever, eventData) => reciever.OnGoToNextTrial()
				);
			}
		}
	}
}

