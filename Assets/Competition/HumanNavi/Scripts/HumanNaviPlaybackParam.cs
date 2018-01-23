using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SIGVerse.Common;


namespace SIGVerse.Competition.HumanNavigation
{
	public static class HumanNaviPlaybackParam
	{
		public const string WorldRecorderName = "WorldRecorder";

		public const string FilePath = "/../SIGVerseConfig/HumanNavi/Log_";
		//public const string FilePathForCommand = "/../SIGVerseConfig/HumanNavi/Log_";

		public const int PlaybackTypeNone   = 0;
		public const int PlaybackTypeRecord = 1;
		public const int PlaybackTypePlay   = 2;

		// Status
		public const string DataType1Transform = "11";

		// Events
		public const string DataType1HumanNaviMsgFromAvatar = "21";
		public const string DataType1HumanNaviMsgFromRobot = "22";
		public const string DataType1JointTrajectoryFromRobot = "23";
		public const string DataType1TwistFromRobot           = "24";
		public const string DataType1JointStateFromAvatar     = "25";

		public const string DataType2TransformDef = "0";
		public const string DataType2TransformVal = "1";
	}
}
