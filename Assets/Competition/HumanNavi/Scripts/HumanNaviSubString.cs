using UnityEngine;
using UnityEngine.EventSystems;
using SIGVerse.ROSBridge;
using SIGVerse.ROSBridge.sensor_msgs;
using SIGVerse.Common;
using System.Collections.Generic;
using System;

namespace SIGVerse.Competition.HumanNavigation
{
	public interface IReceiveStringMsgHandler : IEventSystemHandler
	{
		void OnReceiveROSStringMessage(ROSBridge.std_msgs.String stringMsg);
	}

	public class HumanNaviSubString : MonoBehaviour
	{
		public SAPIVoiceSynthesis tts;

		public string rosBridgeIP;
		public int rosBridgePort = 9090;

		public string receivingTopicName = "/human_navigation/message/guidance_message";

		//--------------------------------------------------
		private ROSBridgeWebSocketConnection webSocketConnection = null;


		void Start()
		{
			if (!ConfigManager.Instance.configInfo.rosbridgeIP.Equals(string.Empty))
			{
				this.rosBridgeIP = ConfigManager.Instance.configInfo.rosbridgeIP;
				this.rosBridgePort = ConfigManager.Instance.configInfo.rosbridgePort;
			}

			this.webSocketConnection = new SIGVerse.ROSBridge.ROSBridgeWebSocketConnection(rosBridgeIP, rosBridgePort);

			this.webSocketConnection.Subscribe<ROSBridge.std_msgs.String>(receivingTopicName, this.SubscribeStringMessageCallback);

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

		public void SubscribeStringMessageCallback(ROSBridge.std_msgs.String message)
		{
			SIGVerseLogger.Info("Received guide message : " + message.data);

			this.tts.OnReceiveROSStringMessage(message);
		}
	}
}
