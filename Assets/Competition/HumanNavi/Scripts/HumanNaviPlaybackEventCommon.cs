using System;
using System.Collections.Generic;
using UnityEngine;

namespace SIGVerse.Competition.HumanNavigation
{
	public class HumanNaviPlaybackEventCommon : WorldPlaybackCommon
	{
		public const string FilePathFormat = "/../SIGVerseConfig/HumanNavi/Event{0:D2}.dat";

		// Events
		public const string DataTypeHumanNaviObjectGrasped = "ObjectGrasped";
		public const string DataTypeHumanNaviObjectPlaced = "ObjectPlaced";
		public const string DataTypeHumanNaviGuidanceRequested = "GuidanceRequested";
		public const string DataTypeHumanNaviROSMessageSent = "ROSMessageSent";
		public const string DataTypeHumanNaviROSMessageReceived = "ROSMessageReceived";
		public const string DataTypeHumanNaviEvent = "HumanNaviEvent";
	}
}

