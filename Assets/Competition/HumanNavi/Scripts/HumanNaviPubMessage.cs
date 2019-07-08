using UnityEngine;
using UnityEngine.EventSystems;
using SIGVerse.RosBridge;
using SIGVerse.Common;

namespace SIGVerse.Competition.HumanNavigation
{
	public interface IRosHumanNaviMessageSendHandler : IEventSystemHandler
	{
		void OnSendRosHumanNaviMessage(string message, string detail);
	}

	public class HumanNaviPubMessage : RosPubMessage<RosBridge.human_navigation.HumanNaviMsg>, IRosHumanNaviMessageSendHandler
	{
		public void OnSendRosHumanNaviMessage(string message, string detail)
		{
			SIGVerseLogger.Info("Sending message : " + message + ", " + detail);

			RosBridge.human_navigation.HumanNaviMsg humanNaviMsg = new RosBridge.human_navigation.HumanNaviMsg();
			humanNaviMsg.message = message;
			humanNaviMsg.detail = detail;

			this.publisher.Publish(humanNaviMsg);
		}
	}
}

