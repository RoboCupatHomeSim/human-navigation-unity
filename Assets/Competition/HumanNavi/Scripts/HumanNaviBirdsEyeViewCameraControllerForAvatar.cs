using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SIGVerse.Competition.HumanNavigation
{
	public class HumanNaviBirdsEyeViewCameraControllerForAvatar : HumanNaviBirdsEyeViewCameraController
	{
		[HeaderAttribute("Tracking Target")]
		public GameObject targetForSimpleIK;
		public GameObject targetForFinalIK;

		protected override void Awake()
		{
#if ENABLE_VRIK
			this.SetTarget(targetForFinalIK);
#else
			this.SetTarget(targetForSimpleIK);
#endif
		}
	}
}
