using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Valve.VR;

namespace SIGVerse.Competition.HumanNavigation
{
	public class HumanNaviSteamVRActionInitializer : MonoBehaviour
	{
		void Awake()
		{
			SteamVR_Actions.sigverse.Activate(SteamVR_Input_Sources.Any);
		}
	}
}

