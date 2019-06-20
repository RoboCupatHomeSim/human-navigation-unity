using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SIGVerse.Common;

namespace SIGVerse.Competition.HumanNavigation
{
	public class SetNotificationDestination : MonoBehaviour
	{
		public List<string> destinationNames;

		void Start()
		{
			///// TODO: should be modified /////
			CollisionDetector collisionDetector = this.GetComponent<CollisionDetector>();
			collisionDetector.collisionNotificationDestinations.Clear();

			foreach(string destinationName in this.destinationNames)
			{
				collisionDetector.collisionNotificationDestinations.Add(GameObject.Find(destinationName));
			}
		}
	}
}
