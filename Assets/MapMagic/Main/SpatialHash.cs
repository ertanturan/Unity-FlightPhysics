using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MapMagic 
{
	[System.Serializable]
	public class SpatialObject //TODO: to struct
	{
		public Vector2 pos;
		public float height;
		public float rotation;
		public float size;
		public int type;
		public int id; //unique num to apply random

		public SpatialObject Copy () { return new SpatialObject() {pos=this.pos, height=this.height, rotation=this.rotation, size=this.size, type=this.type, id=this.id}; }
	}

	[System.Serializable]
	public class SpatialHash : ICloneable
	{
		[System.Serializable]
		public struct Cell
		{
			public List<SpatialObject> objs;
			public Vector2 min;
			public Vector2 max;
		}

		public Cell[] cells;

		public Vector2 offset;
		public float size;
		public int resolution;
		//TODO replace with matrix coords

		public int Count = 0;
		/*{get{
			int count = 0;
			for (int c=0; c<cells.Length; c++) count += cells[c].objs.Count;
			return count;
		}}*/


		public SpatialHash (Vector2 offset, float size, int resolution)
		{
			this.resolution = resolution;
			this.size = size;
			this.offset = offset;
			this.Count = 0;

			cells = new Cell[resolution*resolution];
			float cellSize = size/resolution;

			for (int x=0; x<resolution; x++)
				for (int y=0; y<resolution; y++)
			{
				Cell cell = new Cell();
				cell.min = new Vector2(x*cellSize, y*cellSize) + offset;
				cell.max = new Vector2((x+1)*cellSize, (y+1)*cellSize) + offset;
				cell.objs = new List<SpatialObject>();
				cells[y*resolution + x] = cell;
			}
		}

		public SpatialHash Copy ()
		{
			SpatialHash result = new SpatialHash(offset, size, resolution);
			for (int i=0; i<cells.Length; i++) 
			{
				result.cells[i].min = cells[i].min;
				result.cells[i].max = cells[i].max;
				result.cells[i].objs = new List<SpatialObject>(cells[i].objs);

				List<SpatialObject> objs = result.cells[i].objs;
				for (int o=objs.Count-1; o>=0; o--) objs[o] = objs[o].Copy();
			}
			result.Count = Count;
			return result;
		}
		public object Clone () { return Copy(); }

		public Cell GetCellByPoint (Vector2 point)
		{
			point -= offset;

			float cellSize = size/resolution;
			int x = (int)(point.x / cellSize);
			int y = (int)(point.y / cellSize);

			//Monitor.DrawBounds("Selected Cell", cells[y*resolution + x].min.V3(), cells[y*resolution + x].max.V3());
			return cells[y*resolution + x];
		}

		public void ChangeResolution (int newResolution)
		{
			SpatialHash newHash = new SpatialHash(offset,size,newResolution);
			
			foreach (SpatialObject obj in AllObjs())
				newHash.Add(obj);

			resolution = newResolution;
			cells = newHash.cells;
		}

		#region Enumerables

			public IEnumerator GetEnumerator()
			{
				for (int c=0; c<cells.Length; c++)
				{
					List<SpatialObject> list = cells[c].objs;
					for (int i=list.Count-1; i>=0; i--) //inverse order, member could be removed
						yield return list[i];
				}
			}

			public IEnumerable<SpatialObject> AllObjs()
			{
				for (int c=0; c<cells.Length; c++)
				{
					Cell cell = cells[c];
					int objsCount = cell.objs.Count;
					for (int i=0; i<objsCount; i++)
						yield return cell.objs[i];
				}
			}

			public IEnumerable<SpatialObject> ObjsInCell(int cellNum)
			{
				Cell cell = cells[cellNum];
				int objsCount = cell.objs.Count;
				for (int i=0; i<objsCount; i++)
					yield return cell.objs[i];
			}

			public IEnumerable<SpatialObject> ObjsInCell(Vector2 point)
			{
				Cell cell = GetCellByPoint(point);
				int objsCount = cell.objs.Count;
				for (int i=0; i<objsCount; i++)
					yield return cell.objs[i];
			}

			public IEnumerable<int> CellNumsInRect(Vector2 min, Vector2 max, bool inCenter=true)
			{
				min -= offset; max -= offset;
			
				float cellSize = size/resolution;
				int minX = (int)(min.x / cellSize);
				int minY = (int)(min.y / cellSize);
				int maxX = (int)(max.x / cellSize);
				int maxY = (int)(max.y / cellSize); 

				minX = Mathf.Max(0, minX); minY = Mathf.Max(0, minY);
				maxX = Mathf.Min(resolution-1, maxX); maxY = Mathf.Min(resolution-1, maxY);  

				//processing all the rect
				if (inCenter)
					for (int x=minX; x<=maxX; x++)
						for (int y=minY; y<=maxY; y++)
							yield return y*resolution + x;

				//borders only
				else 
				{
					for (int x=minX; x<=maxX; x++) { yield return minY*resolution + x; yield return maxY*resolution + x; }
					for (int y=minY; y<=maxY; y++) { yield return y*resolution + minX; yield return y*resolution + maxX; }
				}
			}


			public IEnumerable<Cell> CellsInRect(Vector2 min, Vector2 max)
			{
				foreach (int num in CellNumsInRect(min,max))
					yield return cells[num];
			}

			public IEnumerable<SpatialObject> ObjsInRect(Vector2 min, Vector2 max)
			{
				foreach (int num in CellNumsInRect(min,max))
				{
					List<SpatialObject> objs = cells[num].objs;
					int objsCount = objs.Count;
					for (int i=0; i<objsCount; i++)
						yield return objs[i];
				}
			}

			public IEnumerable<SpatialObject> ObjsInRange(Vector2 pos, float range) 
			{
				Vector2 min = new Vector2(pos.x-range, pos.y-range);
				Vector2 max = new Vector2(pos.x+range, pos.y+range);

				foreach (int num in CellNumsInRect(min,max))
				{
					List<SpatialObject> objs = cells[num].objs;
					int objsCount = objs.Count;
					for (int i=0; i<objsCount; i++)
					//for (int i=objs.Count-1; i>=0; i--)
						if ((pos-objs[i].pos).sqrMagnitude < range*range) yield return objs[i];
				}
			}

			public void RemoveObjsInRange(Vector2 pos, float range) 
			{
				Vector2 min = new Vector2(pos.x-range, pos.y-range);
				Vector2 max = new Vector2(pos.x+range, pos.y+range);

				foreach (int num in CellNumsInRect(min,max))
				{
					List<SpatialObject> objs = cells[num].objs;
					for (int i=objs.Count-1; i>=0; i--)
						if ((pos-objs[i].pos).sqrMagnitude < range*range) objs.RemoveAt(i);
				}
			}

			public bool IsAnyObjInRange (Vector2 pos, float range) 
			{
				Vector2 min = new Vector2(pos.x-range, pos.y-range);
				Vector2 max = new Vector2(pos.x+range, pos.y+range);

				foreach (int num in CellNumsInRect(min,max))
				{
					List<SpatialObject> objs = cells[num].objs;
					int objsCount = objs.Count;
					for (int i=0; i<objsCount; i++)
						if ((pos-objs[i].pos).sqrMagnitude < range*range) return true;
				}

				return false;
			}

			/*public IEnumerable<Cell> CellsFrame (int minX, int minZ, int maxX, int maxZ)
			{
				for (int x=minX+1; x<=maxX; x++) yield return cells[
				{
					List<Coord> objs = cells[num].objs;
					for (int i=0; i<objs.Count; i++)
						yield return objs[i];
				}
			}*/

		#endregion

		public void Add (SpatialObject obj, Vector2 pos, float extend)
		{
			Vector2 min = pos - new Vector2(extend,extend);
			Vector2 max = pos + new Vector2(extend,extend);
			foreach (int cellNum in CellNumsInRect(min,max))	cells[cellNum].objs.Add(obj);
			Count++;
		}
		public void Add (SpatialObject obj, Vector2 pos) { GetCellByPoint(pos).objs.Add(obj); Count++; } //no extend
		public void Add (Vector2 p, float h, float r, float s, int id=-1) 
		{ 
			if (id == -1) id = Count; //setting id if it is not defined
			GetCellByPoint(p).objs.Add(new SpatialObject() {pos=p, height=h, rotation=r, size=s, id=id}); 
			Count++;
		} //no extend, non-generic
		public void Add (SpatialObject obj) { GetCellByPoint(obj.pos).objs.Add(obj); Count++; } //no extend, non-generic
		public void Add (SpatialHash addHash) 
		{
			if (addHash.cells.Length != cells.Length) { UnityEngine.Debug.LogError("Add SpatialHash: cell number is different"); return; }
			for (int c=0; c<cells.Length; c++) 
			{
				cells[c].objs.AddRange(addHash.cells[c].objs);
				Count += cells[c].objs.Count;
			}
		}

		public void Clear () { for (int i=0; i<cells.Length; i++) cells[i].objs.Clear(); Count=0; }

		//public void Remove (Coord obj) { for (int i=0; i<cells.Length; i++) cells[i].objs.Remove(obj); }
		public void Remove (SpatialObject obj, Vector2 pos) { GetCellByPoint(pos).objs.Remove(obj); Count--; } //no extend
		public void Remove (SpatialObject obj) { GetCellByPoint(obj.pos).objs.Remove(obj); Count--; } //no extend, non-generic


		//outdated, use Closest with bo rect instead
		public SpatialObject Closest (Vector2 pos, float range, bool skipSelf=true)
		{
			float minDist = 20000000;
			SpatialObject closest = null;

			Vector2 min = new Vector2(pos.x-range, pos.y-range);
			Vector2 max = new Vector2(pos.x+range, pos.y+range);

			foreach (SpatialObject coord in ObjsInRect(min,max))
			{
				float dist = (coord.pos-pos).sqrMagnitude;
				if (dist < minDist)
				{
					if (skipSelf && dist < 0.00001f) continue; //if itself
					minDist = Mathf.Min(dist, minDist);
					closest = coord;
				}
			}
			return closest;
		}

		public SpatialObject Closest (Vector2 pos, bool skipSelf=true)
		{
			float minDist = 20000000;
			SpatialObject closest = null;
		
			float cellSize = size/resolution;
			float searchDist = 0.0001f;
			float maxSearchDist = size * 1.415f;

			bool closestFound = false; //iterating one circle after closest was found

			//Gizmos.color = Color.green;

			while (searchDist <= maxSearchDist)
			{
				Vector2 min = new Vector2(pos.x-searchDist, pos.y-searchDist);
				Vector2 max = new Vector2(pos.x+searchDist, pos.y+searchDist);

				foreach (int cellNum in CellNumsInRect(min, max, inCenter:false))
				{
					Cell cell = cells[cellNum];
					int cellObjsCount = cell.objs.Count;
					for (int i=0; i<cellObjsCount; i++)
					{
						SpatialObject obj = cell.objs[i];
						float dist = (obj.pos-pos).sqrMagnitude;
						if (dist < minDist)
						{
							if (skipSelf && dist < 0.00001f) continue; //if itself
							minDist = dist;
							closest = obj;
						}
					}

					//Gizmos.DrawWireCube((cell.min+Vector2.one*cellSize/2).V3(), (Vector2.one*cellSize).V3());
				}

				//Gizmos.color = new Color( Gizmos.color.r+0.2f, Gizmos.color.g-0.2f, 0, 1);

				searchDist += cellSize;
				if (closestFound) break;
				if (closest != null) closestFound = true; //iterating one circle after closest was found
				
			}

			return closest;

			/*searchDist = cellSize;
			while (searchDist <= maxSearchDist)
			{
				SpatialObject closest1 = Closest(pos, searchDist, skipSelf);
				if (closest1 != null) return closest1;
				searchDist *= 2;
			}
			return null;*/
		}

		public float MinDist (Vector2 p, bool skipSelf=true) 
		{ 
			SpatialObject closest = Closest(p,skipSelf);
			if (closest == null) return 20000000;
			else return (p-closest.pos).magnitude; 
		}


		public SpatialObject Closest_outdated (Vector2 p)
		{
			float minDist = 20000000;
			SpatialObject closest = null;
			foreach (SpatialObject coord in this)
			{
				float dist = (coord.pos-p).sqrMagnitude;
				if (dist < 0.00001f) continue; //if itself
				if (dist < minDist)
				{
					minDist = Mathf.Min(dist, minDist);
					closest = coord;
				}
			}
			return closest;
		}

		public void Debug ()
		{
			/*for (int i=0; i<cells.Length; i++) 
			{
				Monitor.DrawBounds("Cells", cells[i].min.V3(), cells[i].max.V3());
				for (int j=0; j<cells[i].objs.Count; j++)
					Monitor.DrawDot("Objects", cells[i].objs[j].pos.V3());
			}*/
		}
	}
}
