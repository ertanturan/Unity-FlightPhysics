using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;


namespace MapMagic
{

	public interface IChunk
	{
		Coord coord { get; set; }		//cell size = 1
		//CoordRect rect { get; set; }	//cell size = resolution
		//Rect pos { get; set; }			//in world units
		int hash { get; set; }
		bool pinned { get; set; }

		void OnCreate(object parent=null);
		void OnMove(Coord oldCoord, Coord newCoord); //actually newCoord==coord, but anyways
		void OnRemove();
	}

	[System.Serializable] 
	public class ChunkGrid<T>  where T: class,IChunk,new()
	{
//		public float cellSize = 100;
//		public int cellRes = 10;
		//public MonoBehaviour parent = null; //in case of chunks are components, otherwise leave it null

		[System.NonSerialized] //can't serialize it anyways
		public Dictionary<int,T> grid = new Dictionary<int,T>(); 

		//public int[] pinnedHashes = new int[0];
		public CoordRect[] deployedRects;// = new CoordRect[0];


		public void Clear ()
		{
			foreach (IChunk chunk in All()) chunk.OnRemove();
			grid.Clear();
		}


		public T defaultObj 
		{get{
			if (typeof(T).BaseType == typeof(MonoBehaviour))
			{
				GameObject go = new GameObject();
				//if (parent != null) go.transform.parent = parent.transform;
				go.SetActive(false);
				return (T)(object)go.AddComponent(typeof(T));
			}
			else return new T();
		}}


		public int GetCellHash (int bx, int bz)
		{
			int aax = bx>=0? bx:-bx; int aaz = bz>=0? bz :-bz;
			int hash = (bx>=0? 0x40000000:0)  |  (bz>=0? 0x20000000:0)  |  ((aax & 0x3FFF) << 14)  |  (aaz & 0x3FFF);

			return hash;
		}

		public IEnumerable<T> WithinRect (CoordRect rect, bool skipMissing=true)
		{
			Coord min = rect.Min;
			Coord max = rect.Max;

			for (int bx=min.x; bx<max.x; bx++)
				for (int bz=min.z; bz<max.z; bz++)
			{
				int aax = bx>=0? bx:-bx; int aaz = bz>=0? bz :-bz;
				int hash = (bx>=0? 0x40000000:0)  |  (bz>=0? 0x20000000:0)  |  ((aax & 0x3FFF) << 14)  |  (aaz & 0x3FFF);
					
				if (!grid.ContainsKey(hash))
				{
					if (skipMissing) continue;
					else yield return null;
				}
				else yield return grid[hash];
			}
		}

		public IEnumerable<T> All (bool pinnedOnly=false)
		{
			foreach(KeyValuePair<int,T> kvp in grid)
			{
				T obj = kvp.Value;
				if (!pinnedOnly || obj.pinned) yield return obj;
			}
		}

		public T Any (bool pinnedOnly=false) //for test purpose only
		{
			foreach(KeyValuePair<int,T> kvp in grid)
			{
				T obj = kvp.Value;
				if (!pinnedOnly || obj.pinned) return obj;
			}
			return null;
		}

		public IEnumerable<T> ObjectsFromCoord (Coord coord) 
		{ 
			int counter = 0;
			foreach (Coord c in coord.DistanceArea(20000)) //to infinity
			{
				int aax = c.x>=0? c.x:-c.x; int aaz = c.z>=0? c.z :-c.z;
				int hash = (c.x>=0? 0x40000000:0)  |  (c.z>=0? 0x20000000:0)  |  ((aax & 0x3FFF) << 14)  |  (aaz & 0x3FFF);
				if (grid.ContainsKey(hash)) 
				{ 
					yield return grid[hash];
					counter++;	
				}
				if (counter >= grid.Count) break;
			}
		}

		public T GetClosestObj (Coord coord) 
		{ 
			if (grid.Count==0) return null; 
			foreach (T obj in ObjectsFromCoord(coord)) return obj; 
			return null; 
		} 


		#region Get/Set

			public T this[int bx, int bz]
			{
				get
				{
					int aax = bx>=0? bx:-bx; int aaz = bz>=0? bz :-bz;
					int hash = (bx>=0? 0x40000000:0)  |  (bz>=0? 0x20000000:0)  |  ((aax & 0x3FFF) << 14)  |  (aaz & 0x3FFF);

					if (!grid.ContainsKey(hash)) return null;
					else return grid[hash];
				}

				set
				{
					int aax = bx>=0? bx:-bx; int aaz = bz>=0? bz :-bz;
					int hash = (bx>=0? 0x40000000:0)  |  (bz>=0? 0x20000000:0)  |  ((aax & 0x3FFF) << 14)  |  (aaz & 0x3FFF);

					value.coord = new Coord(bx,bz);
					//value.rect = new CoordRect(bx*cellRes, bz*cellRes, cellRes, cellRes);
					//value.pos = new Rect(bx*cellSize, bz*cellSize, cellSize, cellSize);
					value.hash = hash;

					if (grid.ContainsKey(hash)) grid[hash] = value;
					else grid.Add(hash,value);
				}
			}

			public T this[Coord coord]
			{
				get
				{
					int aax = coord.x>=0? coord.x:-coord.x; int aaz = coord.z>=0? coord.z :-coord.z;
					int hash = (coord.x>=0? 0x40000000:0)  |  (coord.z>=0? 0x20000000:0)  |  ((aax & 0x3FFF) << 14)  |  (aaz & 0x3FFF);

					if (!grid.ContainsKey(hash)) return null;
					else return grid[hash];
				}

				set
				{
					int aax = coord.x>=0? coord.x:-coord.x; int aaz =coord.z>=0? coord.z :-coord.z;
					int hash = (coord.x>=0? 0x40000000:0)  |  (coord.z>=0? 0x20000000:0)  |  ((aax & 0x3FFF) << 14)  |  (aaz & 0x3FFF);

					value.coord = new Coord(coord.x,coord.z);
					//value.rect = new CoordRect(coord.x*cellRes, coord.z*cellRes, cellRes, cellRes);
					//value.pos = new Rect(coord.x*cellSize, coord.z*cellSize, cellSize, cellSize);
					value.hash = hash;

					if (grid.ContainsKey(hash)) grid[hash] = value;
					else grid.Add(hash,value);
				}
			}

		#endregion

		#region Create/Remove/Pin

			public void Create (Coord coord, object parent, bool pin=true)
			{
				//calculating hash
				int aax = coord.x>=0? coord.x:-coord.x; int aaz = coord.z>=0? coord.z :-coord.z;
				int hash = (coord.x>=0? 0x40000000:0)  |  (coord.z>=0? 0x20000000:0)  |  ((aax & 0x3FFF) << 14)  |  (aaz & 0x3FFF);
		
				//finding object in grid
				T obj = null;
				if (grid.ContainsKey(hash)) obj = grid[hash];

				//creating object if it was not found
				if (obj==null || obj.Equals(null))
				{
					obj = defaultObj;
					grid.Add(hash, obj);
					obj.coord = coord;
					obj.pinned = pin;
					obj.OnCreate(parent);
				}

				//setting pinned state if obj already exists
				else
					if (pin && !obj.pinned) obj.pinned = true;
			}

			public void Remove (Coord coord)
			{
				int aax = coord.x>=0? coord.x:-coord.x; int aaz = coord.z>=0? coord.z :-coord.z;
				int hash = (coord.x>=0? 0x40000000:0)  |  (coord.z>=0? 0x20000000:0)  |  ((aax & 0x3FFF) << 14)  |  (aaz & 0x3FFF);

				if (grid.ContainsKey(hash)) 
				{
					T obj = grid[hash];
					if (obj!=null) obj.OnRemove();
					grid.Remove(hash);
				}
			}

			public void RemoveNonPinned ()
			{
				Dictionary<int,T> dstGrid = new Dictionary<int,T>();
				
				foreach (KeyValuePair<int,T> kvp in grid) 
				{ 
					int hash = kvp.Key;
					T obj = kvp.Value;

					if (obj.pinned) dstGrid.Add(hash, obj);
					else obj.OnRemove();
				}

				deployedRects = new CoordRect[0];

				grid = dstGrid;
			}

			public int PinnedCount
			{get{
				int count = 0;
				foreach (KeyValuePair<int,T> kvp in grid) 
					if (kvp.Value.pinned) count++;
				return count;
			}}

			public Coord[] PinnedCoords
			{get{
				List<Coord> pinnedCoords = new List<Coord>();
				foreach (T obj in All())
					if (obj.pinned) pinnedCoords.Add(obj.coord);
				return pinnedCoords.ToArray();
			}}

		#endregion

		#region Deploy

			public bool CheckDeploy (CoordRect[] rects)
			{		
				if (deployedRects==null || deployedRects.Length!=rects.Length) return true;
				else for (int r=0; r<rects.Length; r++)
				{
					if (rects[r] != deployedRects[r]) return true;
				}
				return false;
			}

			public void Deploy (CoordRect createRect, object parent=null, bool allowMove=true) { Deploy(new CoordRect[] {createRect}, new CoordRect[] {createRect}, parent, allowMove); }
			public void Deploy (CoordRect createRect, CoordRect removeRect, object parent=null, bool allowMove=true) { Deploy(new CoordRect[] {createRect}, new CoordRect[] {removeRect}, parent, allowMove); }
			public void Deploy (CoordRect[] createRects, CoordRect[] removeRects, object parent=null, bool allowMove=true)
			{
				//it would be easier to create new grid and fill it then, but 
				Dictionary<int,T> dstGrid = new Dictionary<int,T>();
				Dictionary<int,T> srcGrid = new Dictionary<int,T>(); //no change should be made in original grid because of multithreading
				foreach(KeyValuePair<int,T> kvp in grid) 
					srcGrid.Add(kvp.Key, kvp.Value);


				//adding pinned objs
				List<T> pinnedObjs = new List<T>();
				foreach(KeyValuePair<int,T> kvp in srcGrid)
				{
					T obj = kvp.Value;
					if (obj.pinned) pinnedObjs.Add(obj);
				}
				int pinnedObjsCount = pinnedObjs.Count;
				for (int i=0; i<pinnedObjsCount; i++)
				{
					T obj = pinnedObjs[i];
					
					//hash
					int aax = obj.coord.x>=0? obj.coord.x:-obj.coord.x; int aaz = obj.coord.z>=0? obj.coord.z :-obj.coord.z;
					int hash = (obj.coord.x>=0? 0x40000000:0)  |  (obj.coord.z>=0? 0x20000000:0)  |  ((aax & 0x3FFF) << 14)  |  (aaz & 0x3FFF);
					
					//copy from src to dst
					dstGrid.Add(hash,obj);
					srcGrid.Remove(hash);
				}

				//adding objects within remove rect
				for (int r=0; r<removeRects.Length; r++)
				{
					CoordRect rect = removeRects[r];
					Coord min = rect.Min; Coord max = rect.Max;
					for (int bx=min.x; bx<max.x; bx++)
						for (int bz=min.z; bz<max.z; bz++)
					{
						//hash
						int aax = bx>=0? bx:-bx; int aaz = bz>=0? bz :-bz;
						int hash = (bx>=0? 0x40000000:0)  |  (bz>=0? 0x20000000:0)  |  ((aax & 0x3FFF) << 14)  |  (aaz & 0x3FFF);

						//adding to new grid
						if (srcGrid.ContainsKey(hash))
						{
							T obj = srcGrid[hash];
							if (obj!=null && !obj.Equals(null)) dstGrid.Add(hash,obj);
							srcGrid.Remove(hash);
						}
					}
				}

				//filling create rects empty areas with unused (or new) objects
				for (int r=0; r<createRects.Length; r++)
				{
					//CoordRect rect = createRects[r];
					//Coord center = rect.Center;
					//foreach (Coord c in center.DistanceArea(rect))
					
					CoordRect rect = createRects[r];
					Coord min = rect.Min; Coord max = rect.Max;
					for (int bx=min.x; bx<max.x; bx++)
						for (int bz=min.z; bz<max.z; bz++)
					{
						//hash
						int aax = bx>=0? bx:-bx; int aaz = bz>=0? bz :-bz;
						int hash = (bx>=0? 0x40000000:0)  |  (bz>=0? 0x20000000:0)  |  ((aax & 0x3FFF) << 14)  |  (aaz & 0x3FFF);
						
						if (dstGrid.ContainsKey(hash)) continue;

						//moving
						if (srcGrid.Count != 0 && allowMove)
						{
							KeyValuePair<int,T> first = srcGrid.First();
							T obj = first.Value;
							srcGrid.Remove(first.Key);

							Coord oldCoord = obj.coord;
							obj.coord = new Coord(bx,bz);
							//obj.rect = new CoordRect(bx*cellRes, bz*cellRes, cellRes, cellRes);
							//obj.pos = new Rect(bx*cellSize, bz*cellSize, cellSize, cellSize);
							obj.hash = hash;
							obj.OnMove(oldCoord, obj.coord);

							dstGrid.Add(hash,obj);
						}

						//creating
						else 
						{
							T obj = defaultObj;

							obj.coord = new Coord(bx,bz);
							//obj.rect = new CoordRect(bx*cellRes, bz*cellRes, cellRes, cellRes);
							//obj.pos = new Rect(bx*cellSize, bz*cellSize, cellSize, cellSize);
							obj.hash = hash;
							obj.OnCreate(parent);

							dstGrid.Add(hash,obj);
						}

						
					}
				}

			//calling pre-remove fn on all other objs left
			foreach (KeyValuePair<int,T> kvp in srcGrid) { kvp.Value.OnRemove(); }

			//assigning new grid and deployed rects
			lock (grid) 
			{ 
				grid = dstGrid; 

				deployedRects = new CoordRect[createRects.Length];
				for (int i=0; i<deployedRects.Length; i++) deployedRects[i] = createRects[i];
			} 
		}
		#endregion

		#region Debug

			public void DrawDeployedRects (float cellSize)
			{
				if (deployedRects == null) return;
				for (int i=0; i<deployedRects.Length; i++)
					Gizmos.DrawWireCube( deployedRects[i].offset.vector3*cellSize + deployedRects[i].size.vector3*cellSize/2, deployedRects[i].size.vector3*cellSize );


			}

			public void DrawObjectRects (float cellSize)
			{
				foreach (IChunk chunk in All())
					Gizmos.DrawWireCube( new Vector3(chunk.coord.x*cellSize,0,chunk.coord.z*cellSize)+new Vector3(cellSize/2f,0,cellSize/2f), new Vector3(cellSize,0,cellSize) );
			}

		#endregion

		#region Serialization 
		//generics do not serialize anyways

			public void Serialize (out T[] objs, out Coord[] coords, out bool[] pinned, bool pinnedOnly=false)
			{
				//calculating arrays length
				int count = 0;
				if (pinnedOnly)
					{ foreach (KeyValuePair<int,T> kvp in grid) if (kvp.Value.pinned) count++; }
				else count = grid.Count;

				//creating arrays
				objs = new T[count];
				coords = new Coord[count];
				pinned = new bool[count];

				//filling arrays
				int counter = 0;
				foreach (KeyValuePair<int,T> kvp in grid)
				{
					T obj = kvp.Value;
					bool objPinned = obj.pinned; if (pinnedOnly && !objPinned) continue;
					Coord objCoord = obj.coord;

					objs[counter] = obj;
					coords[counter].x = objCoord.x; coords[counter].z = objCoord.z;
					pinned[counter] = objPinned;

					counter++;
				}
			}

			public void Deserialize (T[] objs, Coord[] coords, bool[] pinned)
			{
				Dictionary<int,T> newGrid = new Dictionary<int,T>();
				
				for (int i=0; i<objs.Length; i++)
				{
					T obj = objs[i];
					obj.coord = coords[i];
					//obj.rect = new CoordRect(obj.coord.x*cellRes, obj.coord.z*cellRes, cellRes, cellRes);
					//obj.pos = new Rect(obj.coord.x*cellSize, obj.coord.z*cellSize, cellSize, cellSize);
					obj.pinned = pinned[i];
					
					int aax = obj.coord.x>=0? obj.coord.x:-obj.coord.x; int aaz = obj.coord.z>=0? obj.coord.z :-obj.coord.z;
					obj.hash = (obj.coord.x>=0? 0x40000000:0)  |  (obj.coord.z>=0? 0x20000000:0)  |  ((aax & 0x3FFF) << 14)  |  (aaz & 0x3FFF);

					newGrid.Add(obj.hash, obj); 
				}

				grid = newGrid;
			}

			//public void 
			 
			/*
			[SerializeField] public T[] serializedObjects = new T[0];
			[SerializeField] public int[] serializedHashes = new int[0];

			public virtual void OnBeforeSerialize () 
			{
				if (serializedObjects.Length != grid.Count) { serializedObjects=new T[grid.Count]; serializedHashes=new int[grid.Count]; }
			
				int counter = 0;
				foreach (KeyValuePair<int,T> kvp in grid)
				{
					serializedObjects[counter] = kvp.Value;
					serializedHashes[counter] = kvp.Key;
					counter++;
				}
			}
			public virtual void OnAfterDeserialize () 
			{  
				
				Dictionary<int,T> newGrid = new Dictionary<int,T>();
				for (int i=0; i<serializedObjects.Length; i++) newGrid.Add(serializedHashes[i], serializedObjects[i]);
				lock (grid) { grid = newGrid; } 
			}
			*/  
		#endregion
	}

}
