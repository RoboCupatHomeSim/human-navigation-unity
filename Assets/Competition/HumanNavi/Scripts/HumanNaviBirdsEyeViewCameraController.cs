using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SIGVerse.Competition.HumanNavigation
{
	public class HumanNaviBirdsEyeViewCameraController : MonoBehaviour
	{
		[HeaderAttribute("Tracking Target")]
		public GameObject target;

		[HeaderAttribute("Limit for Flip")]
		public float limitAngleFromFront = 180.0f;

		private float cameraPosY;
		private float cameraRotY;
		private Vector3 relativePosition;
		private float initialTargetRotY;
		private bool isFliped;

		// Use this for initialization
		void Start()
		{
			this.cameraPosY = this.transform.position.y;
			this.cameraRotY = this.transform.eulerAngles.y;
			this.relativePosition = this.target.transform.position - this.transform.position;
			this.initialTargetRotY = this.target.transform.eulerAngles.y;

			this.isFliped = false;
		}

		private void LateUpdate()
		{
			Vector3 newCameraPosition = new Vector3();
			Vector3 newCameraRotation = new Vector3();

			float relativeAngleFromCamera = this.target.transform.eulerAngles.y - this.initialTargetRotY;
			if (relativeAngleFromCamera < -180) { relativeAngleFromCamera += 360.0f; }
			if (relativeAngleFromCamera >  180) { relativeAngleFromCamera -= 360.0f; }

			if (!this.isFliped)
			{
				newCameraPosition = this.target.transform.position - this.relativePosition;
				newCameraRotation = new Vector3(this.transform.eulerAngles.x, this.cameraRotY, this.transform.eulerAngles.z);

				if (Mathf.Abs(relativeAngleFromCamera) > this.limitAngleFromFront)
				{
					this.isFliped = true;
				}
			}
			else
			{
				newCameraPosition = new Vector3(this.target.transform.position.x + this.relativePosition.x,	this.target.transform.position.y, this.target.transform.position.z + this.relativePosition.z);
				newCameraRotation = new Vector3(this.transform.eulerAngles.x, this.cameraRotY - 180.0f, this.transform.eulerAngles.z);

				if (Mathf.Abs(relativeAngleFromCamera) < (180.0f - this.limitAngleFromFront))
				{
					this.isFliped = false;
				}
			}

			newCameraPosition.y = this.cameraPosY;
			this.transform.position = newCameraPosition;
			this.transform.eulerAngles = newCameraRotation;
		}

		public void SetTarget(GameObject target)
		{
			this.target = target;
		}
	}
}
