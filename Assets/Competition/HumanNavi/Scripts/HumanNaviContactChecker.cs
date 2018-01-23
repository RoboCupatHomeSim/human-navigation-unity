using SIGVerse.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SIGVerse.Competition.HumanNavigation
{
	public class HumanNaviContactChecker : MonoBehaviour
	{
		private const float WaitingTime = 1.0f;

		//private List<BoxCollider> triggers;

		//private GameObject targetObject = null;

		//private bool hasTargetCollided  = false;
		//private bool shouldCheck = false;

		private bool targetEnterd = false;
		private bool targetStabled = false;
		private bool targetPlaced = false;

		private float EndOfWaitingTime;

		private HumanNaviModerator moderator; // TODO: should be modified

		private Rigidbody targetRigidbody;

		void Start ()
		{
			//this.triggers = new List<BoxCollider>();

			//this.triggers.AddRange(GetBoxColliders(this.transform));

			// TODO: should be modified
			this.moderator = GameObject.Find("HumanNaviModerator").GetComponent<HumanNaviModerator>(); 
		}

		private void Update()
		{
			if(this.targetPlaced){ return; }
			if (!this.targetEnterd){ return; }

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

		//public IEnumerator IsTargetContact(GameObject target)
		//{
		//	this.targetObject = target;

		//	Rigidbody targetRigidbody = this.targetObject.GetComponent<Rigidbody>();

		//	float timeLimit = Time.time + WaitingTime;

		//	while (!targetRigidbody.IsSleeping() && Time.time < timeLimit)
		//	{
		//		yield return null;
		//	}
		
		//	if(Time.time >= timeLimit)
		//	{
		//		SIGVerseLogger.Info("Target deployment failed: Time out.");

		//		yield return false;
		//	}
		//	else
		//	{
		//		this.shouldCheck = true;

		//		targetRigidbody.WakeUp();

		//		SIGVerseLogger.Info("Wakeup the target rigidbody");

		//		while (!this.hasTargetCollided && Time.time < timeLimit)
		//		{
		//			yield return null;
		//		}

		//		if(Time.time >= timeLimit)
		//		{
		//			SIGVerseLogger.Info("Target deployment failed: Time out.");
		//		}

		//		this.shouldCheck = false;

		//		yield return hasTargetCollided;
		//	}
		//}

		public void ResetFlags()
		{
			//this.targetObject      = null;
			//this.hasTargetCollided = false;
			//this.shouldCheck       = false;

			this.targetRigidbody = null;
			this.targetEnterd = false;
			this.targetStabled = false;
			this.targetPlaced = false;
		}

		//private static BoxCollider[] GetBoxColliders(Transform rootTransform)
		//{
		//	Transform judgeTriggersTransform = rootTransform.transform.Find(JudgeTriggersName);

		//	if (judgeTriggersTransform==null)
		//	{
		//		throw new Exception("No Judge Triggers object.");
		//	}

		//	BoxCollider[] boxColliders = judgeTriggersTransform.GetComponents<BoxCollider>();
			
		//	if(boxColliders.Length==0)
		//	{
		//		throw new Exception("No Box colliders.");
		//	}
			
		//	return boxColliders;
		//}


		//private void OnTriggerStay(Collider other)
		//{
		//	if (this.shouldCheck)
		//	{
		//		if (other.attachedRigidbody == null) { return; }

		//		if (other.attachedRigidbody.gameObject == this.targetObject)
		//		{
		//			Debug.Log("OnTriggerStay  time=" + Time.time + ", name=" + other.attachedRigidbody.gameObject.name);

		//			hasTargetCollided = true;
		//		}
		//	}
		//}

		private void OnTriggerEnter(Collider other)
		{
			if (!this.moderator.IsTargetAlreadyGrasped()){ return; }
			if (this.name != this.moderator.GetDestinationName()){ return; }
			if (other.gameObject.name != this.moderator.GetTargetObjectName()) { return; }
			if (other.attachedRigidbody == null) { return; }

			this.targetEnterd = true;
			this.targetRigidbody = other.attachedRigidbody;

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


