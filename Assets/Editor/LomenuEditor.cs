#pragma warning disable 0414

using System.Collections;
using UnityEditor;
using UnityEngine;

public class LomenuEditor : EditorWindow {

	private static LomenuEditor instance = null;

	[MenuItem("Tools/Lomenu UI/Layouts/Create Battlefield")]

	static void CreateBattlefield()
	{
		instance = Instantiate(Resources.Load<GameObject>("Battlefield Layout")).GetComponent<LomenuEditor>();
	}

	[MenuItem("Tools/Lomenu UI/Layouts/Create Bloody")]

	static void CreateBloody()
	{
		instance = Instantiate(Resources.Load<GameObject>("Bloody Layout")).GetComponent<LomenuEditor>();
	}

	[MenuItem("Tools/Lomenu UI/Layouts/Create Curaphic")]

	static void CreateCuraphic()
	{
		instance = Instantiate(Resources.Load<GameObject>("Curaphic Layout")).GetComponent<LomenuEditor>();
	}

	[MenuItem("Tools/Lomenu UI/Layouts/Create Field Layout (3D)")]

	static void CreateField3D()
	{
		instance = Instantiate(Resources.Load<GameObject>("Field Layout (3D)")).GetComponent<LomenuEditor>();
	}

	[MenuItem("Tools/Lomenu UI/Layouts/Create Field Layout")]

	static void CreateField()
	{
		instance = Instantiate(Resources.Load<GameObject>("Field Layout")).GetComponent<LomenuEditor>();
	}

	[MenuItem("Tools/Lomenu UI/Layouts/Create Hexart Layout")]

	static void CreateHexart()
	{
		instance = Instantiate(Resources.Load<GameObject>("Hexart Layout")).GetComponent<LomenuEditor>();
	}

    [MenuItem("Tools/Lomenu UI/Layouts/Create PUBG Layout")]

    static void CreatePUBG()
    {
        instance = Instantiate(Resources.Load<GameObject>("PUBG Layout")).GetComponent<LomenuEditor>();
    }

    public static void OnCustomWindow()
	{
		EditorWindow.GetWindow(typeof(LomenuEditor));
	}
}
