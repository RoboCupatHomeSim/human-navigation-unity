using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SIGVerse.Common
{
	public class PrefabPathPrinter
	{
		[MenuItem("SIGVerse/Print Prefab Path (Please select objects beforehand in the Hierarchy)")]
		private static void PrintPrefabPath()
		{
			List<string> paths = new List<string>();

			foreach (GameObject selectedObj in Selection.gameObjects)
			{
				string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot( selectedObj );

				if (path==string.Empty) 
				{ 
					Debug.LogWarning("Not Prefab! Name:"+selectedObj.name); 
				}
				else
				{
					paths.Add(selectedObj.name.PadRight(30)+": "+path);
				}
			}

			string pathStr = "[Prefab Path List]";

			foreach (string path in paths)
			{
				pathStr += "\n" + path;
			}

			Debug.Log(pathStr + "\n\n");
		}
	}
}
