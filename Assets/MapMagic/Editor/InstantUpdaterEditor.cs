using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

//using MapMagic;

namespace MapMagic 
{
	[CustomEditor(typeof(InstantUpdater))]
	public class InstantUpdaterEditor : Editor
	{
		#if RTP
		private ReliefTerrain rtp;

		public override void OnInspectorGUI ()
		{
			InstantUpdater script = (InstantUpdater)target;

			EditorGUILayout.BeginVertical();

			script.enabledEditor = EditorGUILayout.ToggleLeft("Enabled in Editor", script.enabledEditor);
			script.enabledPlaymode = EditorGUILayout.ToggleLeft("Enabled in Playmode", script.enabledPlaymode);
			
			if (script.enabledEditor) script.Refresh();
			
			if (GUILayout.Button("Update Now")) script.Refresh();
			
			EditorGUILayout.EndVertical();

		}

		#endif
	}
}