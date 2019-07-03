using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SIGVerse.Competition.HumanNavigation
{
	public abstract class HumanNaviBaseBirdsEyeViewCameraController : MonoBehaviour
	{
		[HeaderAttribute("Limit for Flip")]
		public float limitAngleFromFront = 180.0f;

		protected GameObject trackingTarget; // Tracking Target

		protected float cameraPosY;
		protected float cameraRotY;
		protected Vector3 relativePosition;
		protected float initialTargetRotY;
		protected bool isFliped;

		protected virtual void Awake()
		{
		}

		// Use this for initialization
		protected virtual void Start()
		{
			this.cameraPosY = this.transform.position.y;
			this.cameraRotY = this.transform.eulerAngles.y;
			this.relativePosition = this.trackingTarget.transform.position - this.transform.position;
			this.initialTargetRotY = this.trackingTarget.transform.eulerAngles.y;

			this.isFliped = false;
		}

		protected virtual void LateUpdate()
		{
			Vector3 newCameraPosition = new Vector3();
			Vector3 newCameraRotation = new Vector3();

			float relativeAngleFromCamera = this.trackingTarget.transform.eulerAngles.y - this.initialTargetRotY;
			if (relativeAngleFromCamera < -180) { relativeAngleFromCamera += 360.0f; }
			if (relativeAngleFromCamera >  180) { relativeAngleFromCamera -= 360.0f; }

			if (!this.isFliped)
			{
				newCameraPosition = this.trackingTarget.transform.position - this.relativePosition;
				newCameraRotation = new Vector3(this.transform.eulerAngles.x, this.cameraRotY, this.transform.eulerAngles.z);

				if (Mathf.Abs(relativeAngleFromCamera) > this.limitAngleFromFront)
				{
					this.isFliped = true;
				}
			}
			else
			{
				newCameraPosition = new Vector3(this.trackingTarget.transform.position.x + this.relativePosition.x,	this.trackingTarget.transform.position.y, this.trackingTarget.transform.position.z + this.relativePosition.z);
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

		protected void SetTrackingTarget(GameObject target)
		{
			this.trackingTarget = target;
		}
	}
}
