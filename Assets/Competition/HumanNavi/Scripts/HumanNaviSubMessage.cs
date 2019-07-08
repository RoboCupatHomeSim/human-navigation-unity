using UnityEngine;
using UnityEngine.EventSystems;
using SIGVerse.RosBridge;
using SIGVerse.RosBridge.sensor_msgs;
using SIGVerse.Common;
using System.Collections.Generic;
using System;

namespace SIGVerse.Competition.HumanNavigation
{
	public interface IReceiveHumanNaviMsgHandler : IEventSystemHandler
	{
		void OnReceiveRosMessage(RosBridge.human_navigation.HumanNaviMsg humanNaviMsg);
	}

	public class HumanNaviSubMessage : RosSubMessage<RosBridge.human_navigation.HumanNaviMsg>
	{
		public List<GameObject> destinations;

		protected override void SubscribeMessageCallback(RosBridge.human_navigation.HumanNaviMsg humanNaviMsg)
		{
			SIGVerseLogger.Info("Received message :" + humanNaviMsg.message);

			foreach (GameObject destination in this.destinations)
			{
				ExecuteEvents.Execute<IReceiveHumanNaviMsgHandler>
				(
					target: destination,
					eventData: null,
					functor: (reciever, eventData) => reciever.OnReceiveRosMessage(humanNaviMsg)
				);
			}
		}
	}
}
