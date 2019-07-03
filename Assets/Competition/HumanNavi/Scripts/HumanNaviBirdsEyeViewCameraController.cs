using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SIGVerse.Competition.HumanNavigation
{
	public class HumanNaviBirdsEyeViewCameraController : HumanNaviBaseBirdsEyeViewCameraController
	{
		[HeaderAttribute("Tracking Target")]
		public GameObject target;

		protected override void Awake()
		{
			this.SetTrackingTarget(this.target);
		}
	}
}
