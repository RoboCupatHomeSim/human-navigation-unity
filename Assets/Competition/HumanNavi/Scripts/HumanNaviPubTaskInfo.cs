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
		public override void Clear()
		{
		}

		public override void Close()
		{
			if (this.webSocketConnection != null)
			{
				this.webSocketConnection.Unadvertise(this.publisher);
			}

			base.Close();
		}

		public void OnSendRosTaskInfoMessage(RosBridge.human_navigation.HumanNaviTaskInfo message)
		{
			SIGVerseLogger.Info("Sending Task Info message : ");
			SIGVerseLogger.Info("Environment ID : " + message.environment_id);
			SIGVerseLogger.Info("Target object : " + message.target_object.name + " " + message.target_object.position);

			RosBridge.human_navigation.HumanNaviTaskInfo rosMessage = new RosBridge.human_navigation.HumanNaviTaskInfo(
				message.environment_id,
				message.objects_info,
				message.target_object,
				message.destination
				);

			this.publisher.Publish(rosMessage);
		}
	}
}

