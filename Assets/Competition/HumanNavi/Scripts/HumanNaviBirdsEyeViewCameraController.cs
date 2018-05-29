using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SIGVerse.Competition.HumanNavigation
{
	public class HumanNaviBirdsEyeViewCameraController : MonoBehaviour
	{
		public GameObject target;

		private float cameraPosY;
		private Vector3 relativePosition;

		// Use this for initialization
		void Start()
		{
			this.cameraPosY = this.transform.position.y;
			this.relativePosition = this.target.transform.position - this.transform.position;
		}

		private void LateUpdate()
		{
			Vector3 newCameraPosition = this.target.transform.position - this.relativePosition;
			newCameraPosition.y = this.cameraPosY;
			this.transform.position = newCameraPosition;
		}

		public void SetTarget(GameObject target)
		{
			this.target = target;
		}
	}
}
