using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Text.RegularExpressions;
using SIGVerse.Competition;

namespace SIGVerse.Competition.HumanNavigation
{
	public class GuidanceMessageStatus
	{
		public string Message { get; set; }
		public string DisplayType { get; set; }

		public GuidanceMessageStatus(string message, string displayType)
		{
			this.Message = message;
			this.DisplayType = displayType;
		}

		public GuidanceMessageStatus(GuidanceMessageStatus guidanceMessageStatus)
		{
			this.Message = guidanceMessageStatus.Message;
			this.DisplayType = guidanceMessageStatus.DisplayType;
		}
	}

	public class PlaybackGuidanceMessageEvent : PlaybackEventBase
	{
		public GuidanceMessageStatus GuidanceMessageStatus { get; set; }
		public GameObject Destination{ get; set; }

		public void Execute()
		{
			ExecuteEvents.Execute<IPlaybackGuidanceMessageHandler>
			(
				target: this.Destination, 
				eventData: null, 
				functor: (reciever, eventData) => reciever.OnPlaybackGuidanceMessage(GuidanceMessageStatus)
			);
		}
	}


	public class PlaybackGuidanceMessageEventList : PlaybackEventListBase<PlaybackGuidanceMessageEvent>
	{
		public PlaybackGuidanceMessageEventList()
		{
			this.EventList = new List<PlaybackGuidanceMessageEvent>();
		}

		public PlaybackGuidanceMessageEventList(PlaybackGuidanceMessageEventList guidanceMessageEventList)
		{
			this.ElapsedTime = guidanceMessageEventList.ElapsedTime;
			this.EventList   = new List<PlaybackGuidanceMessageEvent>();

			foreach(PlaybackGuidanceMessageEvent guidanceMessageEventOrg in guidanceMessageEventList.EventList)
			{
				GuidanceMessageStatus guidanceMessageStatus = new GuidanceMessageStatus(guidanceMessageEventOrg.GuidanceMessageStatus);
				GameObject            destination           = guidanceMessageEventOrg.Destination;

				PlaybackGuidanceMessageEvent guidanceMessageEvent = new PlaybackGuidanceMessageEvent();
				guidanceMessageEvent.GuidanceMessageStatus = guidanceMessageStatus;
				guidanceMessageEvent.Destination           = destination;

				this.EventList.Add(guidanceMessageEvent);
			}
		}
	}

	// ------------------------------------------------------------------

	public class PlaybackGuidanceMessageEventController : PlaybackEventControllerBase<PlaybackGuidanceMessageEventList, PlaybackGuidanceMessageEvent>
	{
		private GameObject destination;

		public PlaybackGuidanceMessageEventController(GameObject destination)
		{
			this.destination = destination;
		}

		public override void StartInitializingEvents()
		{
			this.eventLists = new List<PlaybackGuidanceMessageEventList>();
		}

		public override bool ReadEvents(string[] headerArray, string dataStr)
		{
			// Notice of a Panel
			if (headerArray[1] == HumanNaviPlaybackCommon.DataTypeHumanNaviGuidanceMessageEvent)
			{
				PlaybackGuidanceMessageEvent guidanceMessageEvent = new PlaybackGuidanceMessageEvent();

				string[] dataArray = dataStr.Split('\t');

				string message     = Regex.Unescape(dataArray[0]);
				string displayType = Regex.Unescape(dataArray[1]);

				GuidanceMessageStatus guidanceMessageStatus = new GuidanceMessageStatus(message, displayType);

				guidanceMessageEvent.GuidanceMessageStatus = guidanceMessageStatus;
				guidanceMessageEvent.Destination           = this.destination;

				PlaybackGuidanceMessageEventList guidanceMessageEventList = new PlaybackGuidanceMessageEventList();
				guidanceMessageEventList.ElapsedTime = float.Parse(headerArray[0]);
				guidanceMessageEventList.EventList.Add(guidanceMessageEvent);

				this.eventLists.Add(guidanceMessageEventList);

				return true;
			}

			return false;
		}

		public static string GetDataLine(string elapsedTime, GuidanceMessageStatus guidanceMessageStatus)
		{
			string dataLine = elapsedTime + "," + HumanNaviPlaybackCommon.DataTypeHumanNaviGuidanceMessageEvent;

			dataLine += "\t" +
				Regex.Escape(guidanceMessageStatus.Message) + "\t" +
				Regex.Escape(guidanceMessageStatus.DisplayType);

			return dataLine;
		}
	}
}

