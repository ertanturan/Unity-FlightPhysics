using UnityEngine;
using System.Collections;

using MapMagic;

namespace MapMagicDemo
{
	public class LodDisable : MonoBehaviour 
	{
		public Transform parent;
		public bool processing = false;
		public float maxDist = 500;
		public int objPerFrame = 1000;
		public bool test = false;
	
		void Update () 
		{
			if (!processing) StartCoroutine(ProcessLods());
			if (test) { test=false; Test(); }
		}

		IEnumerator ProcessLods () 
		{
			processing = true;
			int counter = 0;
			foreach (Transform tfm in parent)
			{
				if (tfm == null) continue;
				if (tfm.name == "ObjectPool Unused") continue;
				Vector3 camPos = Camera.main.transform.position;
				foreach (Transform child in tfm)
				{
					float distSq = (camPos - child.position).sqrMagnitude;
					LODGroup lodGroup = child.GetComponent<LODGroup>();
					if (lodGroup == null) continue;

					if (distSq > maxDist*maxDist && lodGroup.enabled) lodGroup.enabled = false;
					if (distSq < maxDist*maxDist && !lodGroup.enabled) lodGroup.enabled = true;

					counter++;
					if (counter>=objPerFrame) { counter=0; yield return null; }
				}
			}
			processing = false;
		}

		void Test ()
		{
			int numLodgroups = 0;
			int numActive = 0;

			foreach (Transform tfm in parent)
				foreach (Transform child in tfm)
				{
					if (!child.gameObject.activeSelf) continue;
					LODGroup lodGroup = child.GetComponent<LODGroup>();
					if (lodGroup == null) continue;

					numLodgroups++;
					if (lodGroup.enabled) numActive++;
				}
		
			Debug.Log("Num Lodgroups:" + numLodgroups + " Active:" + numActive + " Disabled:" + (numLodgroups-numActive));
		}
	}
}