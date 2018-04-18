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

					panelIdentifier.guidanceMessageText.text = "";

					if (panelIdentifier.panelType != GuidanceMessagePanelIdentifier.GuidanceMessagePanelType.OnGUI)
					{
						panelIdentifier.gameObject.SetActive(false);
					}
				}
			}
		}

		public void OnSpeakMessage(string message, string displayType = "All")
		{
			foreach (GuidanceMessagePanelIdentifier panelIdentifier in this.panelIdentifiers)
			{
				if (displayType == GuidanceMessageDisplayType.All.ToString())
				{
					panelIdentifier.gameObject.SetActive(true);
					panelIdentifier.guidanceMessageText.text = message;
				}
				else
				{
					switch (panelIdentifier.panelType)
					{
						case GuidanceMessagePanelIdentifier.GuidanceMessagePanelType.OnHMD:
						{
							if (displayType == GuidanceMessageDisplayType.AvatarOnly.ToString())
							{
								panelIdentifier.gameObject.SetActive(true);
								panelIdentifier.guidanceMessageText.text = message;
							}
							break;
						}
						case GuidanceMessagePanelIdentifier.GuidanceMessagePanelType.OnRobot:
						{
							if (displayType == GuidanceMessageDisplayType.RobotOnly.ToString())
							{
								panelIdentifier.gameObject.SetActive(true);
								panelIdentifier.guidanceMessageText.text = message;
							}
							break;
						}
					}
				}
			}
		}

		public void OnStopSpeaking()
		{
			foreach (GuidanceMessagePanelIdentifier panelIdentifier in this.panelIdentifiers)
			{
				switch (panelIdentifier.panelType)
				{
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

