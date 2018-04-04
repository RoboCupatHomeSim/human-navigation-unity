using UnityEngine;
using UnityEngine.EventSystems;
using SIGVerse.RosBridge;
using SIGVerse.Common;

namespace SIGVerse.Competition.HumanNavigation
{
	public interface IRosHumanNaviMessageSendHandler : IEventSystemHandler
	{
		void OnSendROSHumanNaviMessage(string message, string detail);
	}

	public class HumanNaviPubMessage : MonoBehaviour, IRosHumanNaviMessageSendHandler
	{
		public string rosBridgeIP;
		public int rosBridgePort = 9090;
		public string sendingTopicName = "/human_navigation/message/to_robot";

		//--------------------------------------------------
		private RosBridgeWebSocketConnection webSocketConnection = null;

		private RosBridgePublisher<RosBridge.human_navigation.HumanNaviMsg> publisher;


		void Start()
		{
			if (!ConfigManager.Instance.configInfo.rosbridgeIP.Equals(string.Empty))
			{
				this.rosBridgeIP = ConfigManager.Instance.configInfo.rosbridgeIP;
				this.rosBridgePort = ConfigManager.Instance.configInfo.rosbridgePort;
			}

			this.webSocketConnection = new SIGVerse.RosBridge.RosBridgeWebSocketConnection(rosBridgeIP, rosBridgePort);

			this.publisher = this.webSocketConnection.Advertise<RosBridge.human_navigation.HumanNaviMsg>(sendingTopicName);

			// Connect to ROSbridge server
			this.webSocketConnection.Connect();
		}

		void OnDestroy()
		{
			if (this.webSocketConnection != null)
			{
				this.webSocketConnection.Unadvertise(this.publisher);

				this.webSocketConnection.Disconnect();
			}
		}

		void Update()
		{
			this.webSocketConnection.Render();
		}

		public void OnSendROSHumanNaviMessage(string message, string detail)
		{
			SIGVerseLogger.Info("Sending message : " + message + ", " + detail);

			RosBridge.human_navigation.HumanNaviMsg humanNaviMsg = new RosBridge.human_navigation.HumanNaviMsg();
			humanNaviMsg.message = message;
			humanNaviMsg.detail = detail;

			this.publisher.Publish(humanNaviMsg);
		}
	}
}

