using SIGVerse.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SIGVerse.Competition.HumanNavigation
{
	public class HumanNaviPlacementChecker : MonoBehaviour
	{
		private const float WaitingTime = 1.0f;

		private bool targetEnterd = false;
		private bool targetStabled = false;
		private bool targetPlaced = false;

		private float EndOfWaitingTime;

		private HumanNaviModerator moderator; // TODO: should be modified

		private Rigidbody targetRigidbody;

		void Start ()
		{
			// TODO: should be modified
			this.moderator = GameObject.Find("Moderator").GetComponent<HumanNaviModerator>(); 
		}

		private void Update()
		{
			if(this.targetPlaced){ return; }
			if (!this.targetEnterd){ return; }
			if (this.targetRigidbody.gameObject.name != this.moderator.GetTargetObjectName()) { return; }

			if (this.targetRigidbody.IsSleeping())
			{
				if (!this.targetStabled)
				{
					this.targetStabled = true;
					this.EndOfWaitingTime = Time.time + WaitingTime;
				}
				else
				{
					if(Time.time > this.EndOfWaitingTime)
					{
						this.targetPlaced = true;
						this.moderator.TargetPlacedOnDestination();
					}
				}
			}

		}

		public void ResetFlags()
		{
			this.targetRigidbody = null;
			this.targetEnterd = false;
			this.targetStabled = false;
			this.targetPlaced = false;
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!this.moderator.IsTargetAlreadyGrasped()){ return; }
			if (this.name != this.moderator.GetDestinationName()){ return; }

			if (other.attachedRigidbody == null) { return; }

			if (other.attachedRigidbody.gameObject.name != this.moderator.GetTargetObjectName()) { return; }

			this.targetRigidbody = other.attachedRigidbody;
			this.targetEnterd = true;
		}

		private void OnTriggerExit(Collider other)
		{
			if (this.targetEnterd)
			{
				if (other.gameObject.name == this.moderator.GetTargetObjectName())
				{
					this.ResetFlags();
				}
			}

		}
	}
}


