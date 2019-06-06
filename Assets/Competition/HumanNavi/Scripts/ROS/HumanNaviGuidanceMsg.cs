// Generated by gencs from human_navigation/HumanNaviGuidanceMsg.msg
// DO NOT EDIT THIS FILE BY HAND!

using System;
using System.Collections;
using System.Collections.Generic;
using SIGVerse.RosBridge;
using UnityEngine;


namespace SIGVerse.RosBridge
{
	namespace human_navigation
	{
		[System.Serializable]
		public class HumanNaviGuidanceMsg : RosMessage
		{
			public string message;
			public string display_type;
			public string source_language;
			public string target_language;


			public HumanNaviGuidanceMsg()
			{
				this.message = "";
				this.display_type = "";
				this.source_language = "";
				this.target_language = "";
			}

			public HumanNaviGuidanceMsg(string message, string display_type, string source_language, string target_language)
			{
				this.message = message;
				this.display_type = display_type;
				this.source_language = source_language;
				this.target_language = target_language;
			}

			new public static string GetMessageType()
			{
				return "human_navigation/HumanNaviGuidanceMsg";
			}

			new public static string GetMD5Hash()
			{
				return "c47d160b61b90f152746eda86238ceca";
			}
		} // class HumanNaviGuidanceMsg
	} // namespace human_navigation
} // namespace SIGVerse.RosBridge
