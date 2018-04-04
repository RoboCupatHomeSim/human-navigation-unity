using UnityEngine;
using UnityEngine.EventSystems;
using SIGVerse.RosBridge;
using SIGVerse.RosBridge.sensor_msgs;
using SIGVerse.Common;
using System.Collections.Generic;
using System;

//using SIGVerse.RosBridge.HumanNavigation;


namespace SIGVerse.Competition.HumanNavigation
{
	public interface IRosTaskInfoSendHandler : IEventSystemHandler
	{
		void OnSendRosTaskInfoMessage(RosBridge.human_navigation.HumanNaviTaskInfo message);
	}

	public class HumanNaviPubTaskInfo : MonoBehaviour, IRosTaskInfoSendHandler
	{
		public string rosBridgeIP;
		public int rosBridgePort = 9090;
		public string sendingTopicName = "/human_navigation/message/to_robot";

		//--------------------------------------------------
		private RosBridgeWebSocketConnection webSocketConnection = null;

		private RosBridgePublisher<RosBridge.human_navigation.HumanNaviTaskInfo> messagePublisher;


		void Start()
		{
			if (!ConfigManager.Instance.configInfo.rosbridgeIP.Equals(string.Empty))
			{
				this.rosBridgeIP   = ConfigManager.Instance.configInfo.rosbridgeIP;
				this.rosBridgePort = ConfigManager.Instance.configInfo.rosbridgePort;
			}

			this.webSocketConnection = new SIGVerse.RosBridge.RosBridgeWebSocketConnection(rosBridgeIP, rosBridgePort);

			this.messagePublisher = this.webSocketConnection.Advertise<RosBridge.human_navigation.HumanNaviTaskInfo>(sendingTopicName);

			// Connect to ROSbridge server
			this.webSocketConnection.Connect();
		}

		void OnApplicationQuit()
		{
			if (this.webSocketConnection != null)
			{
				this.webSocketConnection.Disconnect();
			}
		}

		void Update()
		{
			this.webSocketConnection.Render();
		}

		//public void SendROSMessage(RosBridge.human_navigation.HumanNaviTaskInfo message)
		//{
		//	SIGVerseLogger.Info("Sending Task Info message : ");
		//	SIGVerseLogger.Info("Environment ID : " + message.environment_id);
		//	SIGVerseLogger.Info("Target object : " + message.target_object.name + " " + message.target_object.position);

		//	RosBridge.human_navigation.HumanNaviTaskInfo rosMessage = new RosBridge.human_navigation.HumanNaviTaskInfo(
		//		message.environment_id,
		//		message.objects_info,
		//		message.target_object,
		//		message.destination
		//		);

		//	this.messagePublisher.Publish(rosMessage);
		//}

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

			this.messagePublisher.Publish(rosMessage);
		}
	}
}

