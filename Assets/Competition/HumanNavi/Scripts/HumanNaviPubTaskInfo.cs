using UnityEngine;
using UnityEngine.EventSystems;
using SIGVerse.RosBridge;
using SIGVerse.RosBridge.sensor_msgs;
using SIGVerse.Common;
using System.Collections.Generic;
using System;

namespace SIGVerse.Competition.HumanNavigation
{
	public interface IRosTaskInfoSendHandler : IEventSystemHandler
	{
		void OnSendRosTaskInfoMessage(RosBridge.human_navigation.HumanNaviTaskInfo message);
	}

	public class HumanNaviPubTaskInfo : RosPubMessage<RosBridge.human_navigation.HumanNaviTaskInfo>, IRosTaskInfoSendHandler
	{
		public void OnSendRosTaskInfoMessage(RosBridge.human_navigation.HumanNaviTaskInfo message)
		{
			SIGVerseLogger.Info("Send Task Info message : ");
			SIGVerseLogger.Info("Environment ID : " + message.environment_id);
			SIGVerseLogger.Info("Target object : " + message.target_object.name + " " + message.target_object.position + " " + message.target_object.orientation);
			SIGVerseLogger.Info("Destination : " + message.destination.position + " " + message.destination.orientation + " " + message.destination.size);

			//foreach (RosBridge.human_navigation.HumanNaviObjectInfo objInfo in message.non_target_objects)
			//{
			//	SIGVerseLogger.Info("Non-target object : " + objInfo.name + " " + objInfo.position + " " + objInfo.orientation);
			//}
			//foreach (RosBridge.human_navigation.HumanNaviObjectInfo objInfo in message.furniture)
			//{
			//	SIGVerseLogger.Info("Furniture : " + objInfo.name + " " + objInfo.position + " " + objInfo.orientation);
			//}

			RosBridge.human_navigation.HumanNaviTaskInfo rosMessage = new RosBridge.human_navigation.HumanNaviTaskInfo
			(
				message.environment_id,
				message.target_object,
				message.destination,
				message.non_target_objects,
				message.furniture
			);

			this.publisher.Publish(rosMessage);
		}
	}
}

