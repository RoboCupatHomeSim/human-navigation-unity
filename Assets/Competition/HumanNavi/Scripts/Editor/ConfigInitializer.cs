using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using System.Linq;

namespace SIGVerse.Competition.HumanNavigation
{
	[InitializeOnLoad]
	public class ConfigInitializer
	{
		static ConfigInitializer()
		{
			FileInfo configFileInfo = new FileInfo(Application.dataPath + HumanNaviConfig.FolderPath + "sample/" + HumanNaviConfig.ConfigFileName);

			if(!configFileInfo.Exists) { return; }

			DirectoryInfo sampleDirectoryInfo = new DirectoryInfo(Application.dataPath + HumanNaviConfig.FolderPath + "sample/");

			foreach (FileInfo fileInfo in sampleDirectoryInfo.GetFiles().Where(fileinfo => fileinfo.Name != ".gitignore"))
			{
				string destFilePath = Application.dataPath + HumanNaviConfig.FolderPath + fileInfo.Name;

				if (!File.Exists(destFilePath))
				{
					fileInfo.CopyTo(destFilePath);
				}
			}
		}
	}
}

