using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SIGVerse.Competition.HumanNavigation
{
	public class GuidanceMessagePanelController : MonoBehaviour, ISpeakMessageHandler, IStopSpeakingHandler
	{
		private List<GuidanceMessagePanelIdentifier> panelIdentifiers;

		void Awake()
		{
			this.panelIdentifiers = new List<GuidanceMessagePanelIdentifier>();

			GuidanceMessagePanelIdentifier[] panelIdentifiersArray = Resources.FindObjectsOfTypeAll<GuidanceMessagePanelIdentifier>();

			foreach (GuidanceMessagePanelIdentifier panelIdentifier in panelIdentifiersArray)
			{
				if (panelIdentifier.transform.root.gameObject.activeSelf)
				{
					this.panelIdentifiers.Add(panelIdentifier);

					if (panelIdentifier.panelType != GuidanceMessagePanelIdentifier.GuidanceMessagePanelType.OnGUI)
					{
						panelIdentifier.gameObject.SetActive(false);
					}
				}
			}
		}

		public void OnSpeakMessage(string message)
		{
			foreach (GuidanceMessagePanelIdentifier panelIdentifier in this.panelIdentifiers)
			{
				switch (panelIdentifier.panelType)
				{
					case GuidanceMessagePanelIdentifier.GuidanceMessagePanelType.OnGUI:
					{
						panelIdentifier.guidanceMessageText.text = "";
						break;
					}
					case GuidanceMessagePanelIdentifier.GuidanceMessagePanelType.OnHMD:
					{
						Debug.Log("GuidanceMessagePanelController: " + panelIdentifier.gameObject);
						panelIdentifier.gameObject.SetActive(true);
						break;
					}
					case GuidanceMessagePanelIdentifier.GuidanceMessagePanelType.OnRobot:
					{
						panelIdentifier.gameObject.SetActive(true);
						break;
					}
				}

				panelIdentifier.guidanceMessageText.text = message;
			}
		}

		public void OnStopSpeaking()
		{
			foreach (GuidanceMessagePanelIdentifier panelIdentifier in this.panelIdentifiers)
			{
				switch (panelIdentifier.panelType)
				{
					case GuidanceMessagePanelIdentifier.GuidanceMessagePanelType.OnGUI:
					{
						break;
					}
					case GuidanceMessagePanelIdentifier.GuidanceMessagePanelType.OnHMD:
					{
						panelIdentifier.gameObject.SetActive(false);
						break;
					}
					case GuidanceMessagePanelIdentifier.GuidanceMessagePanelType.OnRobot:
					{
						panelIdentifier.gameObject.SetActive(false);
						break;
					}
				}
			}
		}

	}
}

