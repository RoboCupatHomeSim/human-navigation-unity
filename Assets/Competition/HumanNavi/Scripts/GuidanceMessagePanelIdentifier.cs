using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SIGVerse.Competition.HumanNavigation
{
	public class GuidanceMessagePanelIdentifier : MonoBehaviour
	{
		public enum GuidanceMessagePanelType
		{
			OnHMD,
			OnGUI,
			OnRobot,
		};

		public GuidanceMessagePanelType panelType;

		public Text guidanceMessageText;
	}
}

