using UnityEngine;
using UnityEngine.EventSystems;
using SIGVerse.ROSBridge;
using SIGVerse.ROSBridge.sensor_msgs;
using SIGVerse.Common;
using System.Collections.Generic;
using System;

namespace SIGVerse.Competition.HumanNavigation
{
	public interface IReceiveHumanNaviMsgHandler : IEventSystemHandler
	{
		void OnReceiveRosMessage(ROSBridge.human_navigation.HumanNaviMsg humanNaviMsg);
	}

	public class HumanNaviSubMessage : MonoBehaviour
	{
		public HumanNaviModerator moderator;
		public string rosBridgeIP;
		public int rosBridgePort = 9090;

		public string receivingTopicName = "/human_navigation/message/to_moderator";

		//--------------------------------------------------
		private ROSBridgeWebSocketConnection webSocketConnection = null;

		private ROSBridgeSubscriber<ROSBridge.human_navigation.HumanNaviMsg> subscriber = null;

		void Start()
		{
			if (!ConfigManager.Instance.configInfo.rosbridgeIP.Equals(string.Empty))
			{
				this.rosBridgeIP = ConfigManager.Instance.configInfo.rosbridgeIP;
				this.rosBridgePort = ConfigManager.Instance.configInfo.rosbridgePort;
			}

			this.webSocketConnection = new SIGVerse.ROSBridge.ROSBridgeWebSocketConnection(rosBridgeIP, rosBridgePort);

			this.subscriber = this.webSocketConnection.Subscribe<ROSBridge.human_navigation.HumanNaviMsg>(receivingTopicName, this.SubscribeMessageCallback);

			// Connect to ROSbridge server
			this.webSocketConnection.Connect();
		}

		void OnDestroy()
		{
			if (this.webSocketConnection != null)
			{
				this.webSocketConnection.Unsubscribe(this.subscriber);

				this.webSocketConnection.Disconnect();
			}
		}

		void Update()
		{
			this.webSocketConnection.Render();
		}

		public void SubscribeMessageCallback(ROSBridge.human_navigation.HumanNaviMsg humanNaviMsg)
		{
			SIGVerseLogger.Info("Received message :"+ humanNaviMsg.message);

			this.moderator.OnReceiveROSMessage(humanNaviMsg);
		}
	}
}
