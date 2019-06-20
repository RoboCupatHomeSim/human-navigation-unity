using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SIGVerse.Competition.HumanNavigation
{
	public class HumanNaviAvatarsViewCameraController : MonoBehaviour
	{
		public Camera avatarsViewCamera;

		private void LateUpdate()
		{
			this.transform.position = this.avatarsViewCamera.transform.position;
			this.transform.rotation = this.avatarsViewCamera.transform.rotation;
		}

	}
}
