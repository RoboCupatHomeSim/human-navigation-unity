using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SIGVerse.Competition.HumanNavigation
{
	public class GuidanceMessagePanelController : MonoBehaviour, ISpeakGuidanceMessageHandler, IStopSpeakingHandler
	{
		[HeaderAttribute("Retrieval objects to find panelIdentifier script")]
		public List<string> retrievalObjectNames;

		private List<GuidanceMessagePanelIdentifier> panelIdentifiers;

		void Awake()
		{
			this.panelIdentifiers = new List<GuidanceMessagePanelIdentifier>();

			//GuidanceMessagePanelIdentifier[] panelIdentifiersArray = Resources.FindObjectsOfTypeAll<GuidanceMessagePanelIdentifier>();
			//GuidanceMessagePanelIdentifier[] panelIdentifiersArray;
			
			foreach (string retrievalObjectName in this.retrievalObjectNames)
			{
				GameObject retrievalObject = GameObject.Find(retrievalObjectName);

				if (retrievalObject!=null && retrievalObject.activeSelf)
				{
					GuidanceMessagePanelIdentifier panelIdentifier = retrievalObject.GetComponentInChildren<GuidanceMessagePanelIdentifier>(true);

					this.panelIdentifiers.Add(panelIdentifier);

					panelIdentifier.guidanceMessageText.text = "";

					if (panelIdentifier.panelType != GuidanceMessagePanelIdentifier.GuidanceMessagePanelType.OnGUI)
					{
						panelIdentifier.gameObject.SetActive(false);
					}
				}
			}
		}

		public void OnSpeakGuidanceMessage(GuidanceMessageStatus guidanceMessageStatus)
		{
			foreach (GuidanceMessagePanelIdentifier panelIdentifier in this.panelIdentifiers)
			{
				if (guidanceMessageStatus.DisplayType == GuidanceMessageDisplayType.All.ToString())
				{
					panelIdentifier.gameObject.SetActive(true);
					panelIdentifier.guidanceMessageText.text = guidanceMessageStatus.Message;
				}
				else
				{
					switch (panelIdentifier.panelType)
					{
						case GuidanceMessagePanelIdentifier.GuidanceMessagePanelType.OnHMD:
						{
							if (guidanceMessageStatus.DisplayType == GuidanceMessageDisplayType.AvatarOnly.ToString())
							{
								panelIdentifier.gameObject.SetActive(true);
								panelIdentifier.guidanceMessageText.text = guidanceMessageStatus.Message;
							}
							break;
						}
						case GuidanceMessagePanelIdentifier.GuidanceMessagePanelType.OnRobot:
						{
							if (guidanceMessageStatus.DisplayType == GuidanceMessageDisplayType.RobotOnly.ToString())
							{
								panelIdentifier.gameObject.SetActive(true);
								panelIdentifier.guidanceMessageText.text = guidanceMessageStatus.Message;
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

