using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SIGVerse.Competition.HumanNavigation
{
	public class IkController : MonoBehaviour
	{
		private const char   SymbolSeparator = ';';
		private const string DefineVRIK = "ENABLE_VRIK";

		public GameObject avatarForSimpleIK;
		public GameObject avatarForFinalIK;

		private bool isFinalIkUsed;

		private void EnableDisableAvatars()
		{
			if (this.isFinalIkUsed)
			{
				this.avatarForFinalIK .SetActive(true);
				this.avatarForSimpleIK.SetActive(false);
			}
			else
			{
				this.avatarForFinalIK .SetActive(false);
				this.avatarForSimpleIK.SetActive(true);
			}
		}

#if UNITY_EDITOR
		[InitializeOnLoad]
		[CustomEditor(typeof(IkController))]
		public class IkManagerEditor : Editor
		{
			void OnEnable()
			{
				if (10 < EditorApplication.timeSinceStartup) { return; } // It means that it will be executed only when you start Unity Editor.

				this.InitializeIkStatus();
			}

			private void Awake()
			{
				this.InitializeIkStatus();
			}

			private void InitializeIkStatus()
			{
				string[] scriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Split(SymbolSeparator);

				// Initialize avatar status
				IkController ikController = (IkController)target;

				ikController.isFinalIkUsed = scriptingDefineSymbols.Contains(DefineVRIK);
				ikController.EnableDisableAvatars();
			}

			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				GUILayout.Space(10);

				EditorGUILayout.BeginHorizontal();
				{
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Use FinalIK", GUILayout.Width(100), GUILayout.Height(40)))
					{
						IkController ikController = (IkController)target;

						if (ikController.isFinalIkUsed)
						{
							Debug.LogWarning("Already using Final IK");
							return;
						}

						ikController.isFinalIkUsed = true;
						ikController.EnableDisableAvatars();
						UpdateScriptingDefineSymbolList(DefineVRIK, true);
					}

					GUILayout.Space(20);

					if (GUILayout.Button("Use SimpleIK", GUILayout.Width(100), GUILayout.Height(40)))
					{
						IkController ikController = (IkController)target;

						if (!ikController.isFinalIkUsed)
						{
							Debug.LogWarning("Already using Simple IK");
							return;
						}

						ikController.isFinalIkUsed = false;
						ikController.EnableDisableAvatars();
						UpdateScriptingDefineSymbolList(DefineVRIK, false);
					}
					GUILayout.FlexibleSpace();
				}
				EditorGUILayout.EndHorizontal();

				GUILayout.Space(10);
			}

			private static void UpdateScriptingDefineSymbolList(string defineStr, bool isDefined)
			{
				string[] scriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Split(SymbolSeparator);

				List<string> scriptingDefineSymbolList = new List<string>(scriptingDefineSymbols);

				// Add/Remove a define
				if (isDefined && !scriptingDefineSymbolList.Contains(defineStr))
				{
					scriptingDefineSymbolList.Add(defineStr);
				}
				if (!isDefined && scriptingDefineSymbolList.Contains(defineStr))
				{
					scriptingDefineSymbolList.RemoveAll(symbol => symbol == defineStr);
				}

				string defineSymbolsStr = String.Join(SymbolSeparator.ToString(), scriptingDefineSymbolList.ToArray());

				// Update ScriptingDefineSymbols of PlayerSettings
				PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defineSymbolsStr);
			}
		}
#endif
	}
}

