using UnityEngine;
using UnityEngine.EventSystems;
using SIGVerse.RosBridge;
using SIGVerse.RosBridge.sensor_msgs;
using SIGVerse.Common;
using System.Collections.Generic;
using System;


namespace SIGVerse.Competition.HumanNavigation
{
	public interface IRosAvatarPoseSendHandler : IEventSystemHandler
	{
		void OnSendRosAvatarPoseMessage(RosBridge.human_navigation.HumanNaviAvatarPose message);
	}

	public class HumanNaviPubAvatarPose : RosPubMessage<RosBridge.human_navigation.HumanNaviAvatarPose>, IRosAvatarPoseSendHandler
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

		public void OnSendRosAvatarPoseMessage(RosBridge.human_navigation.HumanNaviAvatarPose message)
		{
			SIGVerseLogger.Info("Sending pose of avatar: ");
			SIGVerseLogger.Info("Head       : " + message.head.position + message.head.orientation);
			SIGVerseLogger.Info("Left Hand  : " + message.left_hand.position + message.left_hand.orientation);
			SIGVerseLogger.Info("Right Hand : " + message.right_hand.position + message.right_hand.orientation);

			RosBridge.human_navigation.HumanNaviAvatarPose rosMessage = new RosBridge.human_navigation.HumanNaviAvatarPose(
				message.head,
				message.left_hand,
				message.right_hand
				);

			this.publisher.Publish(rosMessage);
		}
	}
}

