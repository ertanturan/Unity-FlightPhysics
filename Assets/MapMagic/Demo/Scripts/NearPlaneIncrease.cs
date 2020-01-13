using UnityEngine;
using System.Collections;

using MapMagic;

namespace MapMagicDemo
{
	public class NearPlaneIncrease : MonoBehaviour 
	{
		int oldDistFactor;
	
		void Update () 
		{
			float camDist = Mathf.Max(Camera.main.transform.position.x, Camera.main.transform.position.z);
			int distFactor = (int)(camDist/1000); //changin near plane every 1000 units
			if (distFactor != oldDistFactor)
			{
				Camera.main.nearClipPlane = Mathf.Max(0.66f, distFactor/25f);
				oldDistFactor = distFactor;
			}
		}
	}
}