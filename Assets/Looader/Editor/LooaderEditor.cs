using System.Collections;
using UnityEditor;
using UnityEngine;

public class LooaderEditor : EditorWindow {

	private static LooaderEditor instance = null;

	[MenuItem("Tools/Looader/Create Looader Manager")]
	static void CreateLooaderSystem()
	{
		instance = Instantiate(Resources.Load<GameObject>("Looader Manager")).GetComponent<LooaderEditor>();
	}

	[MenuItem("Tools/Looader/Create Trigger Object")]
	static void CreateTriggerObject()
	{
		instance = Instantiate(Resources.Load<GameObject>("Trigger Object")).GetComponent<LooaderEditor>();
	}

	public static void OnCustomWindow()
	{
		EditorWindow.GetWindow(typeof(LooaderEditor));
	}
}
