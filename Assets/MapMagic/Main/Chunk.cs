using UnityEngine;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

using MapMagic;

namespace MapMagic 
{
		[System.Serializable]
		public class Chunk : IChunk
		{
			public class Results
			{
				public HashSet<Generator> ready = new HashSet<Generator>();
				public Dictionary<Generator.Output, object> results = new Dictionary<Generator.Output, object>(); //saved outputs per-generator during generate
				public Dictionary<System.Type, object> apply = new Dictionary<System.Type, object>(); //the final sum, created during apply, generally obj-prototypes tuples
				public Matrix heights = null; //last heights applied to floor objects
				public HashSet<System.Type> nonEmpty = new HashSet<System.Type>(); 
//				public object preview; //matrix or spatial hash for preview

				public void Clear ()
				{
					results.Clear();
					ready.Clear();
					apply.Clear();
					heights = null; //if (heights != null) heights.Clear(); //clear is faster, but tends to miss an error
					nonEmpty.Clear();
				}
			}
			[System.NonSerialized] public Results results = new Results();

			public Coord coord {get; set;} //in chunk-space
			public CoordRect rect {get{return new CoordRect(coord.x*MapMagic.instance.resolution, coord.z*MapMagic.instance.resolution, MapMagic.instance.resolution, MapMagic.instance.resolution); } }	 //in block-space, to read data
			public int hash {get; set;}
			public bool pinned {get; set;}
			public bool locked;

			public ThreadWorker worker = new ThreadWorker("MMChunk", "MapMagic");
			
			public Terrain terrain;
			public TerrainCollider terrainCollider;

			public Terrain.MaterialType previewBackupMatType = Terrain.MaterialType.BuiltInStandard;
			public Material previewBackupMaterial = null; //to store original material while preview is on

			//terrain neighbouring
			private Terrain neigPrevX = null;
			private Terrain neigNextX = null;
			private Terrain neigPrevZ = null;
			private Terrain neigNextZ = null;
			public static void SetNeigsX (Chunk prev, Chunk next)
			{
				next.neigPrevX = prev.terrain; prev.neigNextX = next.terrain;
				prev.terrain.SetNeighbors(prev.neigPrevX, prev.neigNextZ, prev.neigNextX, prev.neigPrevZ);
				next.terrain.SetNeighbors(next.neigPrevX, next.neigNextZ, next.neigNextX, next.neigPrevZ);
			}
			public static void SetNeigsZ (Chunk prev, Chunk next)
			{
				next.neigPrevZ = prev.terrain; prev.neigNextZ = next.terrain;
				prev.terrain.SetNeighbors(prev.neigPrevX, prev.neigNextZ, prev.neigNextX, prev.neigPrevZ);
				next.terrain.SetNeighbors(next.neigPrevX, next.neigNextZ, next.neigNextX, next.neigPrevZ);
			}


			public struct Size
			{
				public int resolution;
				public float dimensions;
				public float height;

				public float pixelSize { get{ return dimensions / resolution; } }
				public int Seed (CoordRect rect) { return (int)(1000f*rect.offset.x/resolution) + (int)(1f*rect.offset.z/resolution); }

				public Size (int resolution, float dimensions, float height) { this.resolution=resolution; this.dimensions=dimensions; this.height=height; }
			}

			public void InitWorker ()
			{
				if (!worker.initialized)
				{
					worker.Prepare += PrepareFn;
					worker.Calculate += ThreadFn;
					worker.Coroutine = ApplyRoutine;
				}
			}


			#region OnCreate, OnMove, OnRemove

				public void OnCreate (object parentBox)
				{
					MapMagic mapMagic = (MapMagic)parentBox;

					//creating terrain
					GameObject go = new GameObject();
					go.SetActive(false);
					go.name = "Terrain " + coord.x + "," + coord.z;
					go.transform.parent = mapMagic.transform;
					go.transform.localPosition = coord.ToVector3(mapMagic.terrainSize);
					go.transform.localScale = Vector3.one; //no need to make it every move
					//go.SetActive(false); //enabling/disabling is now in update control

					//creating terrain
					terrain = go.AddComponent<Terrain>();
					terrainCollider = go.AddComponent<TerrainCollider>();

					TerrainData terrainData = new TerrainData();
					terrain.terrainData = terrainData;
					terrainCollider.terrainData = terrainData;
					terrainData.size = new Vector3(mapMagic.terrainSize, mapMagic.terrainHeight, mapMagic.terrainSize);
					
					//settings
					SetSettings();

					InitWorker();
					worker.name = "MMChunk " + coord.x + "," + coord.z;
					worker.ready = false;

					MapMagic.CallRepaintWindow(); //if (MapMagic.instance.isEditor) if (RepaintWindow != null) RepaintWindow();
				}

				public void OnMove (Coord oldCoord, Coord newCoord) 
				{
					worker.Stop();
					results.Clear();
					/*if (terrain != null &&  //it could be destroyed by undo
						terrain.materialTemplate!=null) 
					{
						#if UNITY_EDITOR
						bool isAsset = UnityEditor.AssetDatabase.Contains(terrain.materialTemplate);
						if (terrain.materialTemplate != null  &&  !isAsset) GameObject.DestroyImmediate(terrain.materialTemplate); //removes custom shader textures as well. Unity does not remove them!
						Resources.UnloadUnusedAssets();
						#endif
					}*/
					
					terrain.transform.localPosition = coord.ToVector3(MapMagic.instance.terrainSize);
					
					//clearing pools
					Rect terrainRect = new Rect(oldCoord.x*MapMagic.instance.terrainSize, oldCoord.z*MapMagic.instance.terrainSize, MapMagic.instance.terrainSize, MapMagic.instance.terrainSize);
						//using oldCoords because instances are bound to old coordinates
					MapMagic.instance.objectsPool.ClearAllRect(terrainRect);

				//	foreach (KeyValuePair<Transform,ObjectPool> kvp in MapMagic.pools) kvp.Value.ClearAllRect(terrainRect);
				}

				public void OnRemove () 
				{ 
					worker.Stop();
					results.Clear();
					if (terrain != null) //it could be destroyed by undo
					{
						#if UNITY_2019_2_OR_NEWER
						if (terrain.materialTemplate != null) 
							GameObject.DestroyImmediate(terrain.materialTemplate); //removes custom shader textures as well
						#else
						if (terrain.materialTemplate != null  &&  terrain.materialTemplate != MapMagic.instance.customTerrainMaterial) 
							GameObject.DestroyImmediate(terrain.materialTemplate); //removes custom shader textures as well
						#endif
						GameObject.DestroyImmediate(terrain.gameObject);
						Resources.UnloadUnusedAssets();
					}
					
					//clearing pools
					Rect terrainRect = new Rect(coord.x*MapMagic.instance.terrainSize, coord.z*MapMagic.instance.terrainSize, MapMagic.instance.terrainSize, MapMagic.instance.terrainSize);
					MapMagic.instance.objectsPool.ClearAllRect(terrainRect);

				//	foreach (KeyValuePair<Transform,ObjectPool> kvp in MapMagic.pools)
				//		kvp.Value.ClearAllRect(terrainRect);
				}
			#endregion

			#region Neighbors
				[System.NonSerialized] private Terrain oldNeig_x = null;
				[System.NonSerialized] private Terrain oldNeig_X = null;
				[System.NonSerialized] private Terrain oldNeig_z = null;
				[System.NonSerialized] private Terrain oldNeig_Z = null;
			
				public void SetNeighbors (bool force=false)
				{
					#if WDEBUG
					Profiler.BeginSample("Set Neighbors");
					#endif

					ChunkGrid<Chunk> chunks = MapMagic.instance.chunks;

					if (terrain == null || terrain.terrainData == null || terrain.terrainData.heightmapResolution < 64) 
					{
						#if WDEBUG
						Profiler.EndSample();
						#endif

						return;
					}
					
					Chunk newNeig_x_chunk = chunks[coord.x-1, coord.z];
					Terrain newNeig_x = (newNeig_x_chunk!= null && newNeig_x_chunk.worker.ready)? newNeig_x_chunk.terrain : null;

					Chunk newNeig_Z_chunk = chunks[coord.x, coord.z+1];
					Terrain newNeig_Z = (newNeig_Z_chunk!= null && newNeig_Z_chunk.worker.ready)? newNeig_Z_chunk.terrain : null;

					Chunk newNeig_X_chunk = chunks[coord.x+1, coord.z];
					Terrain newNeig_X = (newNeig_X_chunk!= null && newNeig_X_chunk.worker.ready)? newNeig_X_chunk.terrain : null;

					Chunk newNeig_z_chunk = chunks[coord.x, coord.z-1];
					Terrain newNeig_z = (newNeig_z_chunk!= null && newNeig_z_chunk.worker.ready)? newNeig_z_chunk.terrain : null;


					if (oldNeig_x != newNeig_x || oldNeig_Z != newNeig_Z || oldNeig_X != newNeig_X || oldNeig_z != newNeig_z || force)
					{
						terrain.SetNeighbors( newNeig_x, newNeig_Z, newNeig_X, newNeig_z );

						oldNeig_x = newNeig_x;  oldNeig_Z = newNeig_Z;  oldNeig_X = newNeig_X;  oldNeig_z = newNeig_z;
					}

					#if WDEBUG
					Profiler.EndSample();
					#endif
				}
			#endregion

			public void PrepareFn ()
			{
				MapMagic.instance.gens.Prepare(this);
			}

			public void ThreadFn ()  
			{
				Size size = new Size(MapMagic.instance.resolution, MapMagic.instance.terrainSize, MapMagic.instance.terrainHeight);

				try { MapMagic.instance.gens.Calculate(rect, results, size, MapMagic.instance.seed, worker.StopCallback); }
				catch (System.Exception e) 
				{ 
					Debug.LogError("Error generating chunk: " + coord + ": " + e);  
					MapMagic.CallOnGenerateFailed(terrain);
				}
			}

			// applying
			public IEnumerator ApplyRoutine ()
			{
				//bool containsHeight = results.apply.ContainsKey(typeof(HeightOutput));
				//bool containsSplat = results.apply.ContainsKey(typeof(SplatOutput));

				IEnumerator e = MapMagic.instance.gens.Apply(rect,results,terrain,worker.StopCallback,purge:!locked);
				while (e.MoveNext()) 
				{				
					if (terrain==null) yield break; //guard in case max terrains count < actual terrains: terrain destroyed or still processing
					yield return null;
				}


			}


			public void SetSettings ()
			{
				MapMagic magic = MapMagic.instance;

				terrain.heightmapPixelError = magic.pixelError;
				if (magic.showBaseMap) terrain.basemapDistance = magic.baseMapDist;
				else terrain.basemapDistance = 999999;
				terrain.castShadows = magic.castShadows;
				#if UNITY_2017_4_OR_NEWER
				terrain.reflectionProbeUsage = magic.reflectionProbeUsage;
				#endif
				
				if (terrainCollider==null) terrainCollider = terrain.GetComponent<TerrainCollider>();
				if (terrainCollider!=null) terrainCollider.enabled = MapMagic.instance.applyColliders;

				#if UNITY_2018_3_OR_NEWER
				terrain.drawInstanced = magic.drawInstanced;
				terrain.allowAutoConnect = magic.autoConnect;
				#endif

				//material
				if (!Preview.enabled)
				{
					#if UNITY_2019_2_OR_NEWER
					if (MapMagic.instance.customTerrainMaterial != null)
					{
						//checking if customTerrainMaterial assigned as a terrain mat
						if (terrain.materialTemplate == MapMagic.instance.customTerrainMaterial)
						{
							Debug.LogError("Terrain material template == MM.customTerrainMaterial (" + terrain.materialTemplate + "," + terrain.materialTemplate.shader + ")");
							terrain.materialTemplate = null;
						}

						//remove previous material if the shader doesn't match
						if (terrain.materialTemplate != null  &&  terrain.materialTemplate.shader != MapMagic.instance.customTerrainMaterial.shader) 
						{
							GameObject.DestroyImmediate(terrain.materialTemplate); //removes custom shader textures as well. Unity does not remove them!
							Resources.UnloadUnusedAssets();
							terrain.materialTemplate = null; //need to reset material template to prevent unity crash
						}
						
						//duplicating material to terrain
						if (terrain.materialTemplate == null) 
						{
							terrain.materialTemplate = new Material(MapMagic.instance.customTerrainMaterial);
							terrain.materialTemplate.name += " (Copy)";
						}
					}
					#else
					terrain.materialType = MapMagic.instance.terrainMaterialType;

					if (MapMagic.instance.terrainMaterialType == Terrain.MaterialType.Custom && MapMagic.instance.assignCustomTerrainMaterial)
						terrain.materialTemplate = MapMagic.instance.customTerrainMaterial;
					#endif
				}

				terrain.drawTreesAndFoliage = magic.detailDraw;
				terrain.detailObjectDistance = magic.detailDistance;
				terrain.detailObjectDensity = magic.detailDensity;
				terrain.treeDistance = magic.treeDistance;
				terrain.treeBillboardDistance = magic.treeBillboardStart;
				terrain.treeCrossFadeLength = magic.treeFadeLength;
				terrain.treeMaximumFullLODCount = magic.treeFullLod;
				#if UNITY_EDITOR
				terrain.bakeLightProbesForTrees = magic.bakeLightProbesForTrees;
				#endif

				terrain.terrainData.wavingGrassSpeed = magic.windSpeed;
				terrain.terrainData.wavingGrassAmount = magic.windSize;
				terrain.terrainData.wavingGrassStrength = magic.windBending;
				terrain.terrainData.wavingGrassTint = magic.grassTint;

				//copy layer, tag, scripts from mm to terrains
				if (MapMagic.instance.copyLayersTags)
				{
					GameObject go = terrain.gameObject;
					go.layer = MapMagic.instance.gameObject.layer;
					go.isStatic = MapMagic.instance.gameObject.isStatic;
					try { go.tag = MapMagic.instance.gameObject.tag; } catch { Debug.LogError("MapMagic: could not copy object tag"); }
				}
				if (MapMagic.instance.copyComponents)
				{
					GameObject go = terrain.gameObject;
					MonoBehaviour[] components = MapMagic.instance.GetComponents<MonoBehaviour>();
					for (int i=0; i<components.Length; i++)
					{
						if (components[i] is MapMagic || components[i] == null) continue; //if MapMagic itself or script not assigned
						if (terrain.gameObject.GetComponent(components[i].GetType()) == null) Extensions.CopyComponent(components[i], go);
					}
				}
			}

		}//class
}//namespace