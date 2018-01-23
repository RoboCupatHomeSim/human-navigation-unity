using UnityEngine;
using UnityEngine.EventSystems;
using SIGVerse.ROSBridge;
using SIGVerse.ROSBridge.sensor_msgs;
using SIGVerse.Common;
using System.Collections.Generic;
using System;

//using SIGVerse.ROSBridge.HumanNavigation;


namespace SIGVerse.Competition.HumanNavigation
{
	public class HumanNaviPubTaskInfo : MonoBehaviour
	{
		public string rosBridgeIP;
		public int rosBridgePort = 9090;
		public string sendingTopicName = "/human_navigation/message/to_robot";

		//--------------------------------------------------
		private ROSBridgeWebSocketConnection webSocketConnection = null;

		private ROSBridgePublisher<ROSBridge.human_navigation.HumanNaviTaskInfo> messagePublisher;


		void Start()
		{
			if (!ConfigManager.Instance.configInfo.rosbridgeIP.Equals(string.Empty))
			{
				this.rosBridgeIP   = ConfigManager.Instance.configInfo.rosbridgeIP;
				this.rosBridgePort = ConfigManager.Instance.configInfo.rosbridgePort;
			}

			this.webSocketConnection = new SIGVerse.ROSBridge.ROSBridgeWebSocketConnection(rosBridgeIP, rosBridgePort);

			this.messagePublisher = this.webSocketConnection.Advertise<ROSBridge.human_navigation.HumanNaviTaskInfo>(sendingTopicName);

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

		public void SendROSMessage(ROSBridge.human_navigation.HumanNaviTaskInfo message)
		{
			SIGVerseLogger.Info("Sending Task Info message : ");
			SIGVerseLogger.Info("Environment ID : " + message.environment_id);
			SIGVerseLogger.Info("Target object : " + message.target_object.name + " " + message.target_object.position);

			ROSBridge.human_navigation.HumanNaviTaskInfo rosMessage = new ROSBridge.human_navigation.HumanNaviTaskInfo(
				message.environment_id,
				message.objects_info,
				message.target_object,
				message.destination
				);

			this.messagePublisher.Publish(rosMessage);
		}
	}
}

