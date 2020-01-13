using UnityEngine;
using System.Collections;

using MapMagic;

namespace MapMagicDemo
{

	public class MoveSun : MonoBehaviour 
	{
		public float speed = 100;
		public float max = 1000;
	
		void Update ()
		{
			Vector3 pos = transform.position;
			pos.x += Time.deltaTime*speed;
			//pos.x = pos.x % max;
			transform.position = pos;
		}
	}
}