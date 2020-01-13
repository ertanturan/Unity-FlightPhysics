using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MapMagic
{
	[System.Serializable]
	public class ObjectPool : ISerializationCallbackReceiver
	{
		[System.Serializable]
		public struct Transition
		{
			public Vector3 pos;
			public Vector3 scale;
			public Quaternion rotation;

			public Transition (float x, float y, float z) { this.pos = new Vector3(x,y,z); this.scale = new Vector3(1,1,1); this.rotation = Quaternion.identity; }
			public Transition (Vector3 pos) { this.pos = pos; this.scale = new Vector3(1,1,1); this.rotation = Quaternion.identity; }
			public Transition (Vector3 pos, Vector3 scale, Quaternion rot) { this.pos = pos; this.scale = scale; this.rotation = rot; }
		}

		[System.Serializable]
		public struct Instance
		{
			public float x;
			public float z;
			public Transform transform;
		}

		public Dictionary<Transform, List<Instance>> instances = new Dictionary<Transform, List<Instance>>();

		public bool allowReposition = true;
		public bool regardPrefabRotation = false;
		public bool regardPrefabScale = false;
		public bool instantiateClones = false;

		//statistics
		public int created = 0;
		public int moved = 0;
		public int deleted = 0;
		public string stats {get{ return "created:" + created + " moved:" + moved + " deleted:" + deleted; }}
		public void ResetStats () { created=0; moved=0; deleted=0; }

		public int Count 
		{get {
			int result = 0;
			foreach (KeyValuePair<Transform,List<Instance>> kvp in instances)
				result += kvp.Value.Count;
			return result;
		}}

		public Instance Instantiate (Transform prefab, Transition draft, Transform parent=null, Vector3 prefabScale=new Vector3(), Quaternion prefabRotation = new Quaternion())
		{
			if (prefab == null) return new Instance() { transform=null, x=draft.pos.x, z=draft.pos.z };
			
			Transform tfm = null;
			#if UNITY_EDITOR

			#if UNITY_2018_3_OR_NEWER
			if (!instantiateClones && !UnityEditor.EditorApplication.isPlaying && UnityEditor.PrefabUtility.GetPrefabAssetType(prefab)==UnityEditor.PrefabAssetType.Regular) tfm = (Transform)UnityEditor.PrefabUtility.InstantiatePrefab(prefab); //if not playing and prefab is prefab
			#else
			if (!instantiateClones && !UnityEditor.EditorApplication.isPlaying && UnityEditor.PrefabUtility.GetPrefabType(prefab)==UnityEditor.PrefabType.Prefab) tfm = (Transform)UnityEditor.PrefabUtility.InstantiatePrefab(prefab); //if not playing and prefab is prefab
			#endif

			else tfm = (Transform)GameObject.Instantiate(prefab);
			#else
			tfm = (Transform)GameObject.Instantiate(prefab);
			#endif

			tfm.localPosition = draft.pos; //transformation.MultiplyPoint3x4(draft.pos); //TODO test multiply point performance

			if (regardPrefabRotation) tfm.localRotation = draft.rotation * prefabRotation;
			else tfm.localRotation = draft.rotation;

			if (regardPrefabScale) tfm.transform.localScale = new Vector3(draft.scale.x*prefabScale.x, draft.scale.y*prefabScale.y, draft.scale.z*prefabScale.z);
			else tfm.transform.localScale = draft.scale;
			
			//if (draft.scale.x<1-float.Epsilon || draft.scale.x>1+float.Epsilon ||
			//	draft.scale.y<1-float.Epsilon || draft.scale.y>1+float.Epsilon ||
			//	draft.scale.z<1-float.Epsilon || draft.scale.z>1+float.Epsilon) 

			if (parent != null) tfm.parent = parent;

			created++;

			return new Instance() { transform=tfm, x=draft.pos.x, z=draft.pos.z };
		}

		public void ClampCount (Transform prefab, int newCount)
		{
			if (!instances.ContainsKey(prefab)) return;
			List<Instance> list = instances[prefab];
			
			if (list.Count > newCount) 
			{
				for (int i=list.Count-1; i>=newCount; i--)
					if (list[i].transform!=null && list[i].transform.gameObject!=null) 
					{
						if (list[i].transform!=null) GameObject.DestroyImmediate(list[i].transform.gameObject);
						deleted++;
					}
				list.RemoveRange(newCount, list.Count-newCount);
			}
		}

		public void Clear (Transform prefab)
		{
			if (!instances.ContainsKey(prefab)) return;
			List<Instance> list = instances[prefab];

			int listCount = list.Count;
			for (int i=0; i<listCount; i++)
				if (list[i].transform!=null && list[i].transform.gameObject!=null) 
				{
					GameObject.DestroyImmediate(list[i].transform.gameObject);
					deleted++;
				}
			list.Clear();
		}

		public void ClearRect (Transform prefab, Rect rect)
		{
			if (!instances.ContainsKey(prefab)) return;
			List<Instance> list = instances[prefab];

			float minX = rect.x; float minZ = rect.y;
			float maxX = rect.x+rect.width; float maxZ = rect.y+rect.height;

			//removing transforms
			for (int i=list.Count-1; i>=0; i--)
			{
				float x = list[i].x;
				if (x>=minX && x<maxX)
				{
					float z = list[i].z;
					if (z>=minZ && z<maxZ)
					{
						if (list[i].transform!=null) GameObject.DestroyImmediate(list[i].transform.gameObject);
						list.RemoveAt(i);
						deleted++;
					}
				}
			}
		}

		public void ClearAllRect (Rect rect)
		{
			foreach (KeyValuePair<Transform,List<Instance>> kvp in instances)
				ClearRect(kvp.Key,rect);
		}

		public void ClearAllRectBut (Rect rect, HashSet<Transform> usedPrefabs)
		{
			foreach (KeyValuePair<Transform,List<Instance>> kvp in instances)
			{
				Transform prefab = kvp.Key;
				if (usedPrefabs.Contains(prefab)) continue;
				ClearRect(kvp.Key,rect);
			}
		}

		public void ClearAllRectBut (Rect rect, Dictionary<Transform, List<Transition>> usedPrefabs) //just to avoid converting transitions dict to hashset
		{
			foreach (KeyValuePair<Transform,List<Instance>> kvp in instances)
			{
				Transform prefab = kvp.Key;
				if (usedPrefabs.ContainsKey(prefab)) continue;
				ClearRect(kvp.Key,rect);
			}
		}

		public void ClearAll ()
		{
			foreach (KeyValuePair<Transform,List<Instance>> kvp in instances)
				Clear(kvp.Key);

			instances.Clear();
		}

		public void RemoveEmptyPools ()
		{
			List<Transform> prefabsToRemove = new List<Transform>();

			foreach (KeyValuePair<Transform,List<Instance>> kvp in instances) //cannot remove directly changes the dictionary
				if (kvp.Value.Count == 0) prefabsToRemove.Add(kvp.Key);

			int prefabsToRemoveCount = prefabsToRemove.Count;
			for (int i=0; i<prefabsToRemoveCount; i++)
				instances.Remove(prefabsToRemove[i]);
		}





		public IEnumerator RepositionCoroutine (Transform prefab, Rect rect, List<Transition> transitions, Transform parent=null, Transform root=null, int objsPerFrame=100)
		//objects will be parented to parent, but will be aligned in root cordinates
		{
			if (prefab == null) yield break;

			//prepare list
			List<Instance> list;
			if (instances.ContainsKey(prefab)) list = instances[prefab];
			else { list = new List<Instance>(); instances.Add(prefab,list); }

			Quaternion prefabRotation = prefab.rotation;
			Vector3 prefabScale = prefab.localScale;


			//extracting objects within rect
			Stack<Transform> pool = new Stack<Transform>();

			float minX = rect.x; float minZ = rect.y;
			float maxX = rect.x+rect.width; float maxZ = rect.y+rect.height;

			for (int i=list.Count-1; i>=0; i--)
			{
				float x = list[i].x;
				if (x>=minX && x<maxX)
				{
					float z = list[i].z;
					if (z>=minZ && z<maxZ)
					{
						if (list[i].transform!=null) pool.Push(list[i].transform);
						list.RemoveAt(i);
					}
				}	
			}

			int initialPoolCount = pool.Count;


			//moving
			int count = Mathf.Min(transitions.Count, pool.Count);
			for (int i=0; i<count; i++)
			{
				Transform tfm = pool.Pop();

				tfm.transform.position = transitions[i].pos;

				if (regardPrefabRotation) tfm.localRotation = transitions[i].rotation * prefabRotation;
				else tfm.localRotation = transitions[i].rotation;

				if (regardPrefabScale) tfm.transform.localScale = new Vector3(transitions[i].scale.x*prefabScale.x, transitions[i].scale.y*prefabScale.y, transitions[i].scale.z*prefabScale.z);
				else tfm.transform.localScale = transitions[i].scale;

				if (tfm.transform.parent != parent) tfm.transform.parent = parent;

				moved++;

				list.Add( new Instance() { transform=tfm, x=transitions[i].pos.x, z=transitions[i].pos.z } );

				//root offset with preserving object internal coordinates
				if (root != null)
				{
					tfm.transform.localPosition += root.transform.position;
				}
			}


			//removing objects left (if any)
			while (pool.Count > 0)
			{
				GameObject.DestroyImmediate( pool.Pop().gameObject );
				deleted++;
			}


			//adding new objects (if needed)
			yield return null;
			int counter = 0;
			for (int i=initialPoolCount; i<transitions.Count; i++)
			{
				Instance instance = Instantiate(prefab, transitions[i], parent, prefabRotation:prefabRotation, prefabScale:prefabScale);
				list.Add(instance);

				if (root != null)
				{
					instance.transform.localPosition += root.transform.position;
				}

				counter++;
				if (counter >= objsPerFrame) yield return null;
			}
		}

		public void RepositionNow (Transform prefab, Rect rect, List<Transition> transitions, bool regardRotation=false, bool regardScale=false, Transform parent=null)
		{
			IEnumerator e = RepositionCoroutine(prefab, rect, transitions, parent:parent, objsPerFrame:1000000000);
			while (e.MoveNext()) { }
		}


		#region Serialization

			[SerializeField] public Transform[] serializedPrefabs; //public for test purpose
			
			//[SerializeField] public Instance[][] serializedInstances; //can't serialize jagged arrays
			[System.Serializable] public struct InstanceHolder { public Instance[] list; }
			[SerializeField] public InstanceHolder[] serializedInstances;

			public void OnBeforeSerialize ()
			{
				int instancesCount = instances.Count;

				if (serializedPrefabs==null || serializedPrefabs.Length!=instancesCount) serializedPrefabs = new Transform[instancesCount];
				if (serializedInstances==null || serializedInstances.Length!=instancesCount) serializedInstances = new InstanceHolder[instancesCount];

				int i = 0;
				foreach (KeyValuePair<Transform,List<Instance>> kvp in instances)
				{
					Transform prefab = kvp.Key;
					List<Instance> list = kvp.Value;

					int listCount = list.Count;
					
					InstanceHolder instanceHolder = serializedInstances[i];
					if (instanceHolder.list == null || instanceHolder.list.Length!=listCount) 
					{ 
						instanceHolder = new InstanceHolder() { list = new Instance[listCount] };
						serializedInstances[i] = instanceHolder; 
					}

					serializedPrefabs[i] = prefab;

					for (int j=0; j<listCount; j++)
						instanceHolder.list[j] = list[j];
					
					i++;
				}
			}

			public void OnAfterDeserialize ()
			{
				if (serializedInstances==null || serializedPrefabs==null) return;
				
				instances.Clear();

				for (int i=0; i<serializedInstances.Length; i++)
				{
					Transform prefab = serializedPrefabs[i];
					if (prefab == null) continue;
					if (serializedInstances[i].list == null) continue;

					List<Instance> list = new List<Instance>();
					list.AddRange(serializedInstances[i].list);

					instances.Add(prefab, list);
				}
			}

		#endregion
	}
}
