using UnityEngine;
using System.Collections;

using MapMagic;
//using Plugins;

namespace MapMagicDemo
{
	public class GeneratingLabel : MonoBehaviour 
	{
		public MapMagic.MapMagic magic;
	
		void OnGUI()
		{
			if (magic == null) magic = FindObjectOfType<MapMagic.MapMagic>();
		
			if (ThreadWorker.IsWorking("MapMagic"))
			{
				GUI.Box(new Rect(Screen.width/2 - 100, Screen.height-100, 200, 27), "");
				GUI.Box(new Rect(Screen.width/2 - 100, Screen.height-100, 200, 27), "Generating new terrain...");
			}
		}
	}
}
