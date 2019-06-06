using UnityEngine;
using UnityEngine.EventSystems;
using SIGVerse.RosBridge;
using SIGVerse.RosBridge.sensor_msgs;
using SIGVerse.Common;
using System.Collections.Generic;
using System;

namespace SIGVerse.Competition.HumanNavigation
{
	public enum GuidanceMessageDisplayType
	{
		All,
		RobotOnly,
		AvatarOnly,
		None,
	}

	public interface IReceiveHumanNaviGuidanceMsgHandler : IEventSystemHandler
	{
		void OnReceiveROSHumanNaviGuidanceMessage(RosBridge.human_navigation.HumanNaviGuidanceMsg guidaneMsg);
	}

	public class HumanNaviSubGuidanceMessage : MonoBehaviour
	{
		//public SAPIVoiceSynthesis tts;
		public SAPIVoiceSynthesisExternal tts;

		public string rosBridgeIP;
		public int rosBridgePort = 9090;

		public string receivingTopicName = "/human_navigation/message/guidance_message";

		//--------------------------------------------------
		private RosBridgeWebSocketConnection webSocketConnection = null;


		void Start()
		{
			if (!ConfigManager.Instance.configInfo.rosbridgeIP.Equals(string.Empty))
			{
				this.rosBridgeIP = ConfigManager.Instance.configInfo.rosbridgeIP;
				this.rosBridgePort = ConfigManager.Instance.configInfo.rosbridgePort;
			}

			this.webSocketConnection = new SIGVerse.RosBridge.RosBridgeWebSocketConnection(rosBridgeIP, rosBridgePort);

			this.webSocketConnection.Subscribe<RosBridge.human_navigation.HumanNaviGuidanceMsg>(receivingTopicName, this.SubscribeHumanNaviGuidanceMessageCallback);

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

		public void SubscribeHumanNaviGuidanceMessageCallback(RosBridge.human_navigation.HumanNaviGuidanceMsg guidaneMsg)
		{
			SIGVerseLogger.Info("Received guide message: " + guidaneMsg.message + ", display type: " + guidaneMsg.display_type + ", source lang: " + guidaneMsg.source_language + ", target lang: " + guidaneMsg.target_language);

			this.tts.OnReceiveROSHumanNaviGuidanceMessage(guidaneMsg);
		}
	}
}
