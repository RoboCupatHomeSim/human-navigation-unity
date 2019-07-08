using UnityEngine;
using UnityEngine.EventSystems;
using SIGVerse.RosBridge;
using SIGVerse.RosBridge.sensor_msgs;
using SIGVerse.Common;
using System.Collections.Generic;
using System;


namespace SIGVerse.Competition.HumanNavigation
{
	public interface IRosAvatarStatusSendHandler : IEventSystemHandler
	{
		void OnSendRosAvatarStatusMessage(RosBridge.human_navigation.HumanNaviAvatarStatus message);
	}

	public class HumanNaviPubAvatarStatus : RosPubMessage<RosBridge.human_navigation.HumanNaviAvatarStatus>, IRosAvatarStatusSendHandler
	{
		public void OnSendRosAvatarStatusMessage(RosBridge.human_navigation.HumanNaviAvatarStatus message)
		{
			SIGVerseLogger.Info("Send pose of avatar: ");
			//SIGVerseLogger.Info("Head       : " + message.head.position + message.head.orientation);
			//SIGVerseLogger.Info("Body       : " + message.body.position + message.body.orientation);
			//SIGVerseLogger.Info("Left Hand  : " + message.left_hand.position + message.left_hand.orientation);
			//SIGVerseLogger.Info("Right Hand : " + message.right_hand.position + message.right_hand.orientation);
			//SIGVerseLogger.Info("Object in Left Hand : " + message.object_in_left_hand);
			//SIGVerseLogger.Info("Object in Right Hand : " + message.object_in_right_hand);
			//SIGVerseLogger.Info("Is Target in Left Hand : " + message.is_target_object_in_left_hand);
			//SIGVerseLogger.Info("Is Target in Right Hand : " + message.is_target_object_in_right_hand);

			RosBridge.human_navigation.HumanNaviAvatarStatus rosMessage = new RosBridge.human_navigation.HumanNaviAvatarStatus
			(
				message.head,
				message.body,
				message.left_hand,
				message.right_hand,
				message.object_in_left_hand,
				message.object_in_right_hand,
				message.is_target_object_in_left_hand,
				message.is_target_object_in_right_hand
			);

			this.publisher.Publish(rosMessage);
		}
	}
}

