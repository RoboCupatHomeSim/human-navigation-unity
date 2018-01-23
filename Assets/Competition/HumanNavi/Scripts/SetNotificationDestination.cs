using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SIGVerse.ToyotaHSR;

namespace SIGVerse.Competition.HumanNavigation
{
	public class SetNotificationDestination : MonoBehaviour
	{
		public List<string> destinationNames;

		void Start()
		{
			///// TODO: should be modified /////
			HSRCollisionDetector hsrCollisionDetector = this.GetComponent<HSRCollisionDetector>();
			hsrCollisionDetector.collisionNotificationDestinations.Clear();

			foreach(string destinationName in this.destinationNames)
			{
				hsrCollisionDetector.collisionNotificationDestinations.Add(GameObject.Find(destinationName));
			}
		}
	}
}
