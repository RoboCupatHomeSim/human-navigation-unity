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
	public interface IROSAvatarPoseSendHandler : IEventSystemHandler
	{
		void OnSendROSAvatarPoseMessage(ROSBridge.human_navigation.HumanNaviAvatarPose message);
	}

	public class HumanNaviPubAvatarPose : MonoBehaviour, IROSAvatarPoseSendHandler
	{
		public string rosBridgeIP;
		public int rosBridgePort = 9090;
		public string sendingTopicName = "/human_navigation/message/avatar_pose";

		//--------------------------------------------------
		private ROSBridgeWebSocketConnection webSocketConnection = null;

		private ROSBridgePublisher<ROSBridge.human_navigation.HumanNaviAvatarPose> messagePublisher;


		void Start()
		{
			if (!ConfigManager.Instance.configInfo.rosbridgeIP.Equals(string.Empty))
			{
				this.rosBridgeIP   = ConfigManager.Instance.configInfo.rosbridgeIP;
				this.rosBridgePort = ConfigManager.Instance.configInfo.rosbridgePort;
			}

			this.webSocketConnection = new SIGVerse.ROSBridge.ROSBridgeWebSocketConnection(rosBridgeIP, rosBridgePort);

			this.messagePublisher = this.webSocketConnection.Advertise<ROSBridge.human_navigation.HumanNaviAvatarPose>(sendingTopicName);

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

		//void Update()
		//{
		//	this.webSocketConnection.Render();
		//}

		//public void SendROSMessage(ROSBridge.human_navigation.HumanNaviAvatarPose message)
		//{
		//	SIGVerseLogger.Info("Sending pose of avatar: ");
		//	SIGVerseLogger.Info("Head       : " + message.head.position + message.head.orientation);
		//	SIGVerseLogger.Info("Left Hand  : " + message.left_hand.position + message.left_hand.orientation);
		//	SIGVerseLogger.Info("Right Hand : " + message.right_hand.position + message.right_hand.orientation);

		//	ROSBridge.human_navigation.HumanNaviAvatarPose rosMessage = new ROSBridge.human_navigation.HumanNaviAvatarPose(
		//		message.head,
		//		message.left_hand,
		//		message.right_hand
		//		);

		//	this.messagePublisher.Publish(rosMessage);
		//}

		public void OnSendROSAvatarPoseMessage(ROSBridge.human_navigation.HumanNaviAvatarPose message)
		{
			SIGVerseLogger.Info("Sending pose of avatar: ");
			SIGVerseLogger.Info("Head       : " + message.head.position + message.head.orientation);
			SIGVerseLogger.Info("Left Hand  : " + message.left_hand.position + message.left_hand.orientation);
			SIGVerseLogger.Info("Right Hand : " + message.right_hand.position + message.right_hand.orientation);

			ROSBridge.human_navigation.HumanNaviAvatarPose rosMessage = new ROSBridge.human_navigation.HumanNaviAvatarPose(
				message.head,
				message.left_hand,
				message.right_hand
				);

			this.messagePublisher.Publish(rosMessage);
		}
	}
}

