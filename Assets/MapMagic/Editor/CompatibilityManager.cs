
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MapMagic
{
	public class CompatibilityManagerWindow : EditorWindow
	{
		[MenuItem ("Window/MapMagic/Compatibility")]
		public static void ShowWindow ()
		{
			//opening if force open
			CompatibilityManagerWindow instance = (CompatibilityManagerWindow)EditorWindow.GetWindow (typeof (CompatibilityManagerWindow), utility:true);
			
			//finding instance
			if (instance == null)
			{
				CompatibilityManagerWindow[] windows = Resources.FindObjectsOfTypeAll<CompatibilityManagerWindow>();
				if (windows.Length==0) return;
				instance = windows[0];
			}
			
			instance.name = "Compatibility Manager";
			instance.minSize = new Vector2(290, 245);
			instance.maxSize = instance.minSize;
			instance.Show();
			instance.Repaint();
		}


		public void OnGUI () 
		{
			BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
			string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

			bool auto = false;

			EditorGUILayout.BeginVertical();
				EditorGUILayout.LabelField("Enable compatibility for (requires re-compile):");
				DrawPlugin("Voxeland 5", "Voxeland5.Voxeland, Assembly-CSharp", "VOXELAND", symbols, auto);
				DrawPlugin("MapMagic", "MapMagic.MapMagic, Assembly-CSharp", "MAPMAGIC", symbols, auto);
				DrawPlugin("MegaSplat", "JBooth.MegaSplat.MegaSplatDefines, Assembly-CSharp-Editor", "__MEGASPLAT__", symbols, auto);
				DrawPlugin("Relief Terrain Pack", "ReliefTerrain, Assembly-CSharp", "RTP", symbols, auto);
				DrawPlugin("uNature", "uNature.Core.Terrains.UNTerrain, Assembly-CSharp", "UN_MapMagic", symbols, auto);
				DrawPlugin("CTS", "CTS.CTSConstants, Assembly-CSharp", "CTS_PRESENT", symbols, auto);
				DrawPlugin("Vegetation Studio Pro", "AwesomeTechnologies.VegetationSystem.VegetationSystemPro, AwesomeTechnologies.VegetationStudioPro.Runtime", "VEGETATION_STUDIO_PRO", symbols, auto);

				EditorGUILayout.HelpBox("It is recommended to disable compatibility checkboxes before removing appropriate assets. " +
					"\n\nRemoving compatible asset from outside Unity can cause compile errors. To fix this go to \n     Edit -> \n     Project Settings -> \n     Player -> \n     Scripting Define Symbols\n and erase the string.",
					MessageType.Info);
			EditorGUILayout.EndVertical();
		}


		public void DrawPlugin (string name, string script, string keyword, string symbols, bool auto=false)
		{
			bool enabled = symbols.Contains(keyword+";") || symbols.EndsWith(keyword);
			bool available = System.Type.GetType(script) != null;

			EditorGUILayout.BeginHorizontal();

				EditorGUI.BeginDisabledGroup(auto);
				bool newEnabled = EditorGUILayout.Toggle(enabled, GUILayout.Width(20));
				EditorGUI.EndDisabledGroup();
				if (newEnabled != enabled)
				{
					if (newEnabled && !available)
					{ 
						if (!EditorUtility.DisplayDialog (name + " is not available",
							name + " seems to be not installed. Enabling it's compatibility will most probably result in a compile error. Are you sure you want to enable compatibility?",
							"Enable", "Cancel")) newEnabled = false;
					}
					if (newEnabled) EnableKeyword(keyword);
					else DisableKeyword(keyword);
				}

				EditorGUILayout.LabelField(name, GUILayout.Width(140));

				if (!available)
				{
					EditorGUI.BeginDisabledGroup(auto);
					EditorGUILayout.LabelField("Not installed", GUILayout.Width(100));
					EditorGUI.EndDisabledGroup();
				}
			
			EditorGUILayout.EndHorizontal();
		}


		static void EnableKeyword (string keyword)
		{
			BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
			string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
				
			if (!symbols.Contains(keyword+";") && !symbols.EndsWith(keyword)) 
			{
				symbols += (symbols.Length!=0? ";" : "") + keyword;
				Debug.Log("MapMagic/Voxeland " + keyword + " Compatibility Mode Enabled");

				PlayerSettings.SetScriptingDefineSymbolsForGroup(group, symbols);
			}
		}


		static void DisableKeyword (string keyword)
		{
			BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
			string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
				
			if (symbols.Contains(keyword+";") || symbols.EndsWith(keyword)) 
			{
				symbols = symbols.Replace(keyword,""); 
				symbols = symbols.Replace(";;", ";"); 
				Debug.Log("MapMagic: " + keyword + " Compatibility Mode Disabled");
			}

			PlayerSettings.SetScriptingDefineSymbolsForGroup(group, symbols);
		}


		//using asset postprocessor instead of direct class checking
		//because there is no class unless scripts are compiled. And scripts are not compiled because there is no class.
		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			foreach (string str in deletedAssets)
			{
				if (str.EndsWith("Voxeland.cs")) DisableKeyword("VOXELAND");
				if (str.EndsWith("MapMagic.cs")) DisableKeyword("MAPMAGIC");
				if (str.EndsWith("ReliefTerrain/ReliefTerrain.cs")) DisableKeyword("RTP");
				if (str.EndsWith("Editor/MegaSplatUtilities.cs")) DisableKeyword("__MEGASPLAT__");
				if (str.EndsWith("Scripts/Core/Terrain/UNTerrain.cs")) DisableKeyword("UN_MapMagic");
			}
		}
	}
}

