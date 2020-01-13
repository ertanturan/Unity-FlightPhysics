using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using MapMagic;

#if UN_MapMagic
using uNature.Core.Extensions.MapMagicIntegration;
using uNature.Core.FoliageClasses;
#endif


namespace MapMagic
{
	public delegate void Action<T1, T2, T3, T4, T5> (T1 p1, T2 p2, T3 p3, T4 p4, T5 p5);
	public abstract class OutputGenerator : Generator
	{
		public abstract Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>> GetProces ();
		public abstract System.Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator> GetApply ();
		public abstract System.Action<CoordRect, Terrain> GetPurge (); //purge is found via reflection so GetPurge should be static
	}

	[System.Serializable]
	[GeneratorMenu(menu = "Output", name = "Height", disengageable = true, priority = -2, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Height")]
	public class HeightOutput : OutputGenerator
	{
		public Input input = new Input(InoutType.Map);
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; }
		public override IEnumerable<Output> Outputs() { if (output == null) output = new Output(InoutType.Map); yield return output; }

		public float layer { get; set; }

		//get static actions using instance
		public override Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>> GetProces () { return Process; }
		public override Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator> GetApply () { return Apply; }
		public override Action<CoordRect, Terrain> GetPurge () { return Purge; }
		
		public static int scale = 1;

		public static void Process(CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop = null)
		{
			if (stop!=null && stop(0)) return;

			//reading height outputs
			if (results.heights == null || results.heights.rect.size.x != rect.size.x) results.heights = new Matrix(rect);
			results.heights.rect.offset = rect.offset;
			results.heights.Clear();

			//processing main height
			foreach (HeightOutput gen in gens.GeneratorsOfType<HeightOutput>(onlyEnabled: true, checkBiomes: true))
			{
				//if (stop!=null && stop(0)) return; //do not break while results.heights is empty!

				//loading inputs
				Matrix heights = (Matrix)gen.input.GetObject(results);
				if (heights == null) continue;

				//loading biome matrix
				Matrix biomeMask = null;
				if (gen.biome != null)
				{
					object biomeMaskObj = gen.biome.mask.GetObject(results);
					if (biomeMaskObj == null) continue; //adding nothing if biome has no mask
					biomeMask = (Matrix)biomeMaskObj;
					if (biomeMask == null) continue;
					if (biomeMask.IsEmpty()) continue; //optimizing empty biomes
				}

				//adding to final result
				if (gen.biome == null) results.heights.Add(heights);
				else if (biomeMask != null) results.heights.Add(heights, biomeMask);
			}

			//creating 2d array
			if (stop!=null && stop(0)) return;
			int heightSize = terrainSize.resolution * scale + 1;
			float[,] heights2D = new float[heightSize, heightSize];
			for (int x = 0; x < heightSize - 1; x++)
				for (int z = 0; z < heightSize - 1; z++)
				{
					if (scale == 1) heights2D[z, x] += results.heights[x + results.heights.rect.offset.x, z + results.heights.rect.offset.z];
					else
					{
						float fx = 1f * x / scale; float fz = 1f * z / scale;
						heights2D[z, x] = results.heights.GetInterpolated(fx + results.heights.rect.offset.x, fz + results.heights.rect.offset.z);
					}
				}

			//blur only original base verts
			if (scale == 2)
			{
				float blurVal = 0.2f;
				
				for (int z=0; z<heightSize-1; z+=2)
					for (int x=2; x<heightSize-1; x+=2)
						heights2D[x,z] = (heights2D[x-1,z] + heights2D[x+1,z])/2 * blurVal  +  heights2D[x,z] * (1-blurVal);

				for (int x=0; x<heightSize-1; x+=2)
					for (int z=2; z<heightSize-1; z+=2)
						heights2D[x,z] = (heights2D[x,z-1] + heights2D[x,z+1])/2 * blurVal  +  heights2D[x,z] * (1-blurVal);
			}

			//blur high scale values
			if (scale == 4)
			{
				int blurIterations = 2;

				for (int i=0; i<blurIterations; i++)
				{
					float prev = 0;
					float curr = 0;
					float next = 0;

					for (int x=0; x<heightSize; x++)
					{
						prev = heights2D[x,0]; curr = prev;
						for (int z=1; z<heightSize-2; z++)
						{
							next = heights2D[x,z+1];
							curr = (next+prev)/2;// * blurVal + curr*(1-blurVal);

							heights2D[x,z] = curr;
							prev = curr;
							curr = next;
						}

					}

					for (int z=0; z<heightSize; z++)
					{
						prev = heights2D[0,z]; curr = prev;
						for (int x=1; x<heightSize-2; x++)
						{
							next = heights2D[x+1,z];
							curr = (next+prev)/2;// * blurVal + curr*(1-blurVal);

							heights2D[x,z] = curr;
							prev = curr;
							curr = next;
						}
					}
				}
			}

			//processing sides
			for (int x = 0; x < heightSize; x++)
			{
				float prevVal = heights2D[heightSize - 3, x]; //size-2
				float currVal = heights2D[heightSize - 2, x]; //size-1, point on border
				float nextVal = currVal - (prevVal - currVal);
				heights2D[heightSize - 1, x] = nextVal;
			}
			for (int z = 0; z < heightSize; z++)
			{
				float prevVal = heights2D[z, heightSize - 3]; //size-2
				float currVal = heights2D[z, heightSize - 2]; //size-1, point on border
				float nextVal = currVal - (prevVal - currVal);
				heights2D[z, heightSize - 1] = nextVal;
			}
			heights2D[heightSize - 1, heightSize - 1] = heights2D[heightSize - 1, heightSize - 2];


			//pushing to apply
			if (stop!=null && stop(0)) return;

            #if UN_MapMagic
            if (FoliageCore_MainManager.instance != null)
            {
                float resolutionDifferences = (float)MapMagic.instance.terrainSize / terrainSize.resolution;

                uNatureHeightTuple heightTuple = new uNatureHeightTuple(heights2D, new Vector3(rect.Min.x * resolutionDifferences, 0, rect.Min.z * resolutionDifferences)); // transform coords
                results.apply.CheckAdd(typeof(HeightOutput), heightTuple, replace: true);
            }
            else
            {
                //Debug.LogError("uNature_MapMagic extension is enabled but no foliage manager exists on the scene.");
                //return;
				results.apply.CheckAdd(typeof(HeightOutput), heights2D, replace: true);
            }

            #else
			results.apply.CheckAdd(typeof(HeightOutput), heights2D, replace: true);
            #endif
        }

		public static void Purge(CoordRect rect, Terrain terrain)
		{
			//skipping if already purged
			if (terrain.terrainData.heightmapResolution<=33) return;

			float[,] heights2D = new float[33, 33];
			terrain.terrainData.heightmapResolution = heights2D.GetLength(0);
			terrain.terrainData.SetHeights(0, 0, heights2D);
			terrain.terrainData.size = new Vector3(MapMagic.instance.terrainSize, MapMagic.instance.terrainHeight, MapMagic.instance.terrainSize);
			//SetNeighbors(); //TODO set neightbors

			//if (MapMagic.instance.debug) 
			Debug.Log("Heights Purged");

		}

		public static IEnumerator Apply(CoordRect rect, Terrain terrain, object dataBox, Func<float,bool> stop= null)
		{


            //init heights
            #if UN_MapMagic

			#if WDEBUG
			Profiler.BeginSample("UNature");
			#endif

            uNatureHeightTuple heightTuple;
            float[,] heights2D;

            if (FoliageCore_MainManager.instance != null)
            {
                heightTuple = (uNatureHeightTuple)dataBox; // get data
                heights2D = heightTuple.normalizedHeights;
                UNMapMagic_Manager.ApplyHeightOutput(heightTuple, terrain);
            }
            else
            {
                //Debug.LogError("uNature_MapMagic extension is enabled but no foliage manager exists on the scene.");
                //yield break;
				heights2D = (float[,])dataBox;
            }

			#if WDEBUG
			Profiler.EndSample();
			#endif

			#else
			float[,] heights2D = (float[,])dataBox;
			#endif

			//quick lod apply
			/*if (chunk.lod)
			{
				//if (chunk.lodTerrain == null) { chunk.lodTerrain = (MapMagic.instance.transform.AddChild("Terrain " + chunk.coord.x + "," + chunk.coord.z + " LOD")).gameObject.AddComponent<Terrain>(); chunk.lodTerrain.terrainData = new TerrainData(); }
				if (chunk.lodTerrain.terrainData==null) chunk.lodTerrain.terrainData = new TerrainData();

				chunk.lodTerrain.Resize(heights2D.GetLength(0), new Vector3(MapMagic.instance.terrainSize, MapMagic.instance.terrainHeight, MapMagic.instance.terrainSize));
				chunk.lodTerrain.terrainData.SetHeightsDelayLOD(0,0,heights2D);
				
				yield break;
			}*/

			//determining data
			if (terrain==null || terrain.terrainData==null) yield break; //chunk removed during apply
			TerrainData data = terrain.terrainData;

			//resizing terrain (standard terrain resize is extremely slow. Even when creating a new terrain)
			Vector3 terrainSize = terrain.terrainData.size; //new Vector3(MapMagic.instance.terrainSize, MapMagic.instance.terrainHeight, MapMagic.instance.terrainSize);
			int terrainResolution = heights2D.GetLength(0); //heights2D[0].GetLength(0);
			if ((data.size - terrainSize).sqrMagnitude > 0.01f || data.heightmapResolution != terrainResolution)
			{
			//	if (terrainResolution <= 64) //brute force
				{
					data.heightmapResolution = terrainResolution;
					data.size = new Vector3(terrainSize.x, terrainSize.y, terrainSize.z);
				}

			/*	else //setting res 64, re-scaling to 1/64, and then changing res
				{
					data.heightmapResolution = 65;
					terrain.Flush(); //otherwise unity crushes without an error
					int resFactor = (terrainResolution - 1) / 64;
					data.size = new Vector3(terrainSize.x / resFactor, terrainSize.y, terrainSize.z / resFactor);
					data.heightmapResolution = terrainResolution;
				}*/
			}
			yield return null;

			//welding
			if (MapMagic.instance != null && MapMagic.instance.heightWeldMargins!=0)
			{
				Coord coord = Coord.PickCell(rect.offset, MapMagic.instance.resolution);
				Chunk chunk = MapMagic.instance.chunks[coord.x, coord.z];
				
				Chunk neigPrevX = MapMagic.instance.chunks[coord.x-1, coord.z];
				if (neigPrevX!=null && neigPrevX.terrain.terrainData.heightmapResolution==terrainResolution)
				{
					if (neigPrevX.worker.ready || neigPrevX.locked) WeldTerrains.WeldToPrevX(ref heights2D, neigPrevX.terrain, MapMagic.instance.heightWeldMargins);
					Chunk.SetNeigsX(neigPrevX, chunk);
				}

				Chunk neigNextX = MapMagic.instance.chunks[coord.x+1, coord.z];
				if (neigNextX!=null && neigNextX.terrain.terrainData.heightmapResolution==terrainResolution)
				{
					if (neigNextX.worker.ready || neigNextX.locked) WeldTerrains.WeldToNextX(ref heights2D, neigNextX.terrain, MapMagic.instance.heightWeldMargins);
					Chunk.SetNeigsX(chunk, neigNextX);
				}

				Chunk neigPrevZ = MapMagic.instance.chunks[coord.x, coord.z-1];
				if (neigPrevZ!=null  && neigPrevZ.terrain.terrainData.heightmapResolution==terrainResolution)
				{
					if (neigPrevZ.worker.ready || neigPrevZ.locked) WeldTerrains.WeldToPrevZ(ref heights2D, neigPrevZ.terrain, MapMagic.instance.heightWeldMargins);
					Chunk.SetNeigsZ(neigPrevZ, chunk);
				}

				Chunk neigNextZ = MapMagic.instance.chunks[coord.x, coord.z+1];
				if (neigNextZ!=null  && neigNextZ.terrain.terrainData.heightmapResolution==terrainResolution)
				{
					if (neigNextZ.worker.ready || neigNextZ.locked) WeldTerrains.WeldToNextZ(ref heights2D, neigNextZ.terrain, MapMagic.instance.heightWeldMargins);
					Chunk.SetNeigsZ(chunk, neigNextZ);
				}
			}
			yield return null;

			data.SetHeightsDelayLOD(0, 0, heights2D);
			yield return null;

			terrain.ApplyDelayedHeightmapModification();
			terrain.Flush();

			yield return null;
		}

		public override void OnGUI(GeneratorsAsset gens)
		{
			layout.Par(20); input.DrawIcon(layout, "Height");
			layout.Par(5);

			if (output == null) output = new Output(InoutType.Map);

			layout.Field(ref scale, "Scale", min:1, max:4f);
			scale = Mathf.NextPowerOfTwo(scale);
		}
	}

	[System.Serializable]
	[GeneratorMenu(menu = "Output", name = "Splats", disengageable = true, priority = -1, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Textures")]
	public class SplatOutput : OutputGenerator
	{
		//layer
		public class Layer
		{
			public Input input = new Input(InoutType.Map);
			public Output output = new Output(InoutType.Map);
			public string name = "Layer";
			public float opacity = 1;
			public SplatPrototype splat = new SplatPrototype();

			public void OnGUI (Layout layout, bool selected, int num) 
			{
				layout.margin = 20; layout.rightMargin = 20;
				layout.Par(20);

				if (num != 0) input.DrawIcon(layout);
				if (selected) layout.Field(ref name, rect: layout.Inset());
				else layout.Label(name, rect: layout.Inset());
				output.DrawIcon(layout);

				if (selected)
				{
					layout.Par(2);
					layout.Par(60); //not 65
					splat.texture = layout.Field(splat.texture, rect: layout.Inset(60));
					splat.normalMap = layout.Field(splat.normalMap, rect: layout.Inset(60));
					layout.Par(2);

					layout.margin = 5; layout.rightMargin = 5; layout.fieldSize = 0.6f;
					//layout.SmartField(ref downscale, "Downscale", min:1, max:8); downscale = Mathf.ClosestPowerOfTwo(downscale);
					opacity = layout.Field(opacity, "Opacity", min: 0);
					splat.tileSize = layout.Field(splat.tileSize, "Size");
					splat.tileOffset = layout.Field(splat.tileOffset, "Offset");
					splat.specular = layout.Field(splat.specular, "Specular");
					splat.smoothness = layout.Field(splat.smoothness, "Smooth", max: 1);
					splat.metallic = layout.Field(splat.metallic, "Metallic", max: 1);
				}
			}
		}

		//layer
		public Layer[] baseLayers = new Layer[] { new Layer() { name = "Background" } };
		public int selected;
		
		public void UnlinkBaseLayer (int p, int n)
		{
			if (baseLayers.Length == 0) return; //no base layer
			if (baseLayers[0].input.link != null) 
				baseLayers[0].input.Link(null, null);
			baseLayers[0].input.guiRect = new Rect(0,0,0,0);
		}
		public void UnlinkBaseLayer (int n) { UnlinkBaseLayer(0,0); }
		
		public void UnlinkLayer (int num)
		{
			baseLayers[num].input.Link(null,null); //unlink input
			baseLayers[num].output.UnlinkInActiveGens(); //try to unlink output
		}


		public static Texture2D _defaultTex;
		public static Texture2D defaultTex { get { if (_defaultTex == null) _defaultTex = Extensions.ColorTexture(2, 2, new Color(0.75f, 0.75f, 0.75f, 0f)); return _defaultTex; } }

		//public class SplatsTuple { public float[,,] array; public SplatPrototype[] prototypes; }

		//generator
		public override IEnumerable<Input> Inputs()
		{
			if (baseLayers == null) baseLayers = new Layer[0];
			for (int i = 0; i < baseLayers.Length; i++)
				if (baseLayers[i].input != null)
					yield return baseLayers[i].input;
		}
		public override IEnumerable<Output> Outputs()
		{
			if (baseLayers == null) baseLayers = new Layer[0];
			for (int i = 0; i < baseLayers.Length; i++)
				if (baseLayers[i].output != null)
					yield return baseLayers[i].output;
		}

		//get static actions using instance
		public override Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>> GetProces () { return Process; }
		public override Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator> GetApply () { return Apply; }
		public override Action<CoordRect, Terrain> GetPurge () { return Purge; }


		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			if ((stop!=null && stop(0)) || !enabled) return;

			//loading inputs
			Matrix[] matrices = new Matrix[baseLayers.Length];
			for (int i = 0; i < baseLayers.Length; i++)
			{
				if (baseLayers[i].input != null)
				{
					matrices[i] = (Matrix)baseLayers[i].input.GetObject(results);
					if (matrices[i] != null) matrices[i] = matrices[i].Copy(null);
				}
				if (matrices[i] == null) matrices[i] = new Matrix(rect);
			}

			//background matrix
			//matrices[0] = terrain.defaultMatrix; //already created
			matrices[0].Fill(1);

			//populating opacity array
			float[] opacities = new float[matrices.Length];
			for (int i = 0; i < baseLayers.Length; i++)
				opacities[i] = baseLayers[i].opacity;
			opacities[0] = 1;

			//blending layers
			Matrix.BlendLayers(matrices, opacities);

			//saving changed matrix results
			for (int i = 0; i < baseLayers.Length; i++)
			{
				if (stop!=null && stop(0)) return; //do not write object is generating is stopped
				baseLayers[i].output.SetObject(results, matrices[i]);
			}
		}

		public static void Process (CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop = null)
		{
			if (stop!=null && stop(0)) return;

			//gathering prototypes and matrices lists
			List<SplatPrototype> prototypesList = new List<SplatPrototype>();
			List<float> opacities = new List<float>();
			List<Matrix> matrices = new List<Matrix>();
			List<Matrix> biomeMasks = new List<Matrix>();

			foreach (SplatOutput gen in gens.GeneratorsOfType<SplatOutput>(onlyEnabled: true, checkBiomes: true))
			{
				//loading biome matrix
				Matrix biomeMask = null;
				if (gen.biome != null)
				{
					object biomeMaskObj = gen.biome.mask.GetObject(results);
					if (biomeMaskObj == null) continue; //adding nothing if biome has no mask
					biomeMask = (Matrix)biomeMaskObj;
					if (biomeMask == null) continue;
					if (biomeMask.IsEmpty()) continue; //optimizing empty biomes
				}

				for (int i = 0; i < gen.baseLayers.Length; i++)
				{
					//reading output directly
					Output output = gen.baseLayers[i].output;
					if (stop!=null && stop(0)) return; //checking stop before reading output
					if (!results.results.ContainsKey(output)) continue;
					Matrix matrix = (Matrix)results.results[output];
					matrix.Clamp01();

					//adding to lists
					matrices.Add(matrix);
					biomeMasks.Add(gen.biome == null ? null : biomeMask);
					prototypesList.Add(gen.baseLayers[i].splat);
					opacities.Add(gen.baseLayers[i].opacity);
				}
			}

			//optimizing matrices list if they are not used
//			for (int i = matrices.Count - 1; i >= 0; i--)
//				if (opacities[i] < 0.001f || matrices[i].IsEmpty() || (biomeMasks[i] != null && biomeMasks[i].IsEmpty()))
//				{ prototypesList.RemoveAt(i); opacities.RemoveAt(i); matrices.RemoveAt(i); biomeMasks.RemoveAt(i); }

			//creating array
			float[,,] splats3D = new float[terrainSize.resolution, terrainSize.resolution, prototypesList.Count];
			if (matrices.Count == 0) { results.apply.CheckAdd(typeof(SplatOutput), new TupleSet<float[,,], SplatPrototype[]>(splats3D, new SplatPrototype[0]), replace: true); return; }

			//filling array
			if (stop!=null && stop(0)) return;

			int numLayers = matrices.Count;
			int maxX = splats3D.GetLength(0); int maxZ = splats3D.GetLength(1); //MapMagic.instance.resolution should not be used because of possible lods
																				//CoordRect rect =  matrices[0].rect;

			float[] values = new float[numLayers]; //row, to avoid reading/writing 3d array (it is too slow)

			for (int x = 0; x < maxX; x++)
				for (int z = 0; z < maxZ; z++)
				{
					int pos = rect.GetPos(x + rect.offset.x, z + rect.offset.z);
					float sum = 0;

					//getting values
					for (int i = 0; i < numLayers; i++)
					{
						float val = matrices[i].array[pos];
						if (biomeMasks[i] != null) val *= biomeMasks[i].array[pos]; //if mask is not assigned biome was ignored, so only main outs with mask==null left here
						if (val < 0) val = 0; if (val > 1) val = 1;
						sum += val; //normalizing: calculating sum
						values[i] = val;
					}

					//setting color
					for (int i = 0; i < numLayers; i++) splats3D[z, x, i] = values[i] / sum;
				}

			//pushing to apply
			if (stop!=null && stop(0)) return;
			TupleSet<float[,,], SplatPrototype[]> splatsTuple = new TupleSet<float[,,], SplatPrototype[]>(splats3D, prototypesList.ToArray());
			results.apply.CheckAdd(typeof(SplatOutput), splatsTuple, replace: true);
		}

		public static IEnumerator Apply(CoordRect rect, Terrain terrain, object dataBox, Func<float,bool> stop= null)
		{
			TupleSet<float[,,], SplatPrototype[]> splatsTuple = (TupleSet<float[,,], SplatPrototype[]>)dataBox;
			float[,,] splats3D = splatsTuple.item1;
			SplatPrototype[] prototypes = splatsTuple.item2;

			if (splats3D.GetLength(2) == 0) { Purge(rect,terrain); yield break; }

			if (terrain == null) yield break;
			TerrainData data = terrain.terrainData;

			//setting resolution
			int size = splats3D.GetLength(0);
			if (data.alphamapResolution != size) data.alphamapResolution = size;

			//checking prototypes texture
			for (int i = 0; i < prototypes.Length; i++)
				if (prototypes[i].texture == null) prototypes[i].texture = defaultTex;
			yield return null;

			//welding
			if (MapMagic.instance != null && MapMagic.instance.splatsWeldMargins!=0)
			{
				Coord coord = Coord.PickCell(rect.offset, MapMagic.instance.resolution);
				//Chunk chunk = MapMagic.instance.chunks[coord.x, coord.z];
				
				Chunk neigPrevX = MapMagic.instance.chunks[coord.x-1, coord.z];
				if (neigPrevX!=null && (neigPrevX.worker.ready || neigPrevX.locked)) WeldTerrains.WeldSplatToPrevX(ref splats3D, neigPrevX.terrain, MapMagic.instance.splatsWeldMargins);

				Chunk neigNextX = MapMagic.instance.chunks[coord.x+1, coord.z];
				if (neigNextX!=null && (neigNextX.worker.ready || neigNextX.locked)) WeldTerrains.WeldSplatToNextX(ref splats3D, neigNextX.terrain, MapMagic.instance.splatsWeldMargins);

				Chunk neigPrevZ = MapMagic.instance.chunks[coord.x, coord.z-1];
				if (neigPrevZ!=null && (neigPrevZ.worker.ready || neigPrevZ.locked)) WeldTerrains.WeldSplatToPrevZ(ref splats3D, neigPrevZ.terrain, MapMagic.instance.splatsWeldMargins);

				Chunk neigNextZ = MapMagic.instance.chunks[coord.x, coord.z+1];
				if (neigNextZ!=null && (neigNextZ.worker.ready || neigNextZ.locked)) WeldTerrains.WeldSplatToNextZ(ref splats3D, neigNextZ.terrain, MapMagic.instance.splatsWeldMargins);
			}
			yield return null;

			//setting
			#if UNITY_2018_3_OR_NEWER
			TerrainLayer[] layers = new TerrainLayer[prototypes.Length];
			for (int i=0; i<prototypes.Length; i++)
			{
				layers[i] = new TerrainLayer() {
					specular = prototypes[i].specular,
					smoothness = prototypes[i].smoothness,
					metallic = prototypes[i].metallic,
					tileSize = prototypes[i].tileSize,
					tileOffset = prototypes[i].tileOffset,
					diffuseTexture = prototypes[i].texture,
					normalMapTexture = prototypes[i].normalMap,
					normalScale = 1 };
			}
			data.terrainLayers = layers;
			#else
			data.splatPrototypes = prototypes;
			#endif

			data.SetAlphamaps(0, 0, splats3D);

			yield return null;
		}

		public static void Purge(CoordRect rect, Terrain terrain)
		{
			//skipping if already purged
			if (terrain.terrainData.alphamapResolution<=16) return; //using 8 will return resolution to 16

			SplatPrototype[] prototypes = new SplatPrototype[1];
			if (prototypes[0] == null) prototypes[0] = new SplatPrototype();
			if (prototypes[0].texture == null) prototypes[0].texture = defaultTex;

			#if UNITY_2018_3_OR_NEWER
			TerrainLayer[] layers = new TerrainLayer[prototypes.Length];
			for (int i=0; i<prototypes.Length; i++)
			{
				layers[i] = new TerrainLayer() {
					diffuseTexture = prototypes[i].texture,};
			}
			terrain.terrainData.terrainLayers = layers;
			#else
			terrain.terrainData.splatPrototypes = prototypes;
			#endif

			float[,,] emptySplats = new float[16, 16, 1];
			for (int x = 0; x < 16; x++)
				for (int z = 0; z < 16; z++)
					emptySplats[z, x, 0] = 1;

			terrain.terrainData.alphamapResolution = 16;
			terrain.terrainData.SetAlphamaps(0, 0, emptySplats);
		}

		public override void OnGUI(GeneratorsAsset gens)
		{
			//Layer buttons
			layout.Par();
			layout.Label("Layers:", layout.Inset(0.4f));

			layout.DrawArrayAdd(ref baseLayers, ref selected, rect:layout.Inset(0.15f), reverse:true, createElement:() => new Layer(), onAdded:UnlinkBaseLayer );
			layout.DrawArrayRemove(ref baseLayers, ref selected, rect:layout.Inset(0.15f), reverse:true, onBeforeRemove:UnlinkLayer, onRemoved:UnlinkBaseLayer);
			layout.DrawArrayDown(ref baseLayers, ref selected, rect:layout.Inset(0.15f), dispUp:true, onSwitch:UnlinkBaseLayer);
			layout.DrawArrayUp(ref baseLayers, ref selected, rect:layout.Inset(0.15f), dispDown:true, onSwitch:UnlinkBaseLayer);

			//layers
			layout.Par(3);
			for (int num=baseLayers.Length-1; num>=0; num--)
				layout.DrawLayer(baseLayers[num].OnGUI, ref selected, num);
		}
	}

	[System.Serializable]
	[GeneratorMenu(menu = "Output", name = "Textures", disengageable = true, priority = -1, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Textures")]
	public class TexturesOutput : OutputGenerator
	{
		//layer
		public class Layer
		{
			public Input input = new Input(InoutType.Map);
			public Output output = new Output(InoutType.Map);
			public string name = "Layer";
			public float opacity = 1;
			#if UNITY_2018_3_OR_NEWER
			public TerrainLayer prototype;
			#endif

			public void OnGUI (Layout layout, bool selected, int num) 
			{
				layout.margin = 20; layout.rightMargin = 20;
				layout.Par(20);

				//if (selected) layout.Field(ref name, rect: layout.Inset());
				//else layout.Label(name, rect: layout.Inset());

				#if UNITY_2018_3_OR_NEWER
				if (num != 0) input.DrawIcon(layout);

				Texture2D icon = prototype!=null ? prototype.diffuseTexture : null;
				layout.Icon(icon, rect:layout.Inset(20), frame:true, alphaBlend:false);

				layout.Field<TerrainLayer>(ref prototype, rect:layout.Inset(layout.field.width-20-35));
				#else
				layout.Label("Unity is pre-2018.3");
				layout.Label("Use Splats Output instead");
				#endif

				output.DrawIcon(layout);

				/*if (selected)
				{
					layout.Par(2);
					layout.Par(60); //not 65
					prototype.diffuseTexture = layout.Field(prototype.diffuseTexture, rect: layout.Inset(60));
					prototype.normalMapTexture = layout.Field(prototype.normalMapTexture, rect: layout.Inset(60));
					prototype.maskMapTexture = layout.Field(prototype.maskMapTexture, rect: layout.Inset(60));
					layout.Par(2);

					layout.margin = 5; layout.rightMargin = 5; layout.fieldSize = 0.6f;
					//layout.SmartField(ref downscale, "Downscale", min:1, max:8); downscale = Mathf.ClosestPowerOfTwo(downscale);
					opacity = layout.Field(opacity, "Opacity", min: 0);
					prototype.tileSize = layout.Field(prototype.tileSize, "Size");
					prototype.tileOffset = layout.Field(prototype.tileOffset, "Offset");
					prototype.specular = layout.Field(prototype.specular, "Specular");
					prototype.smoothness = layout.Field(prototype.smoothness, "Smooth", max: 1);
					prototype.metallic = layout.Field(prototype.metallic, "Metallic", max: 1);
				}*/
			}
		}

		//layer
		public Layer[] baseLayers = new Layer[] { new Layer() { name = "Background" } };
		public int selected;
		
		public void UnlinkBaseLayer (int p, int n)
		{
			if (baseLayers.Length == 0) return; //no base layer
			if (baseLayers[0].input.link != null) 
				baseLayers[0].input.Link(null, null);
			baseLayers[0].input.guiRect = new Rect(0,0,0,0);
		}
		public void UnlinkBaseLayer (int n) { UnlinkBaseLayer(0,0); }
		
		public void UnlinkLayer (int num)
		{
			baseLayers[num].input.Link(null,null); //unlink input
			baseLayers[num].output.UnlinkInActiveGens(); //try to unlink output
		}


		public static Texture2D _defaultTex;
		public static Texture2D defaultTex { get { if (_defaultTex == null) _defaultTex = Extensions.ColorTexture(2, 2, new Color(0.75f, 0.75f, 0.75f, 0f)); return _defaultTex; } }

		//public class SplatsTuple { public float[,,] array; public SplatPrototype[] prototypes; }

		//generator
		public override IEnumerable<Input> Inputs()
		{
			if (baseLayers == null) baseLayers = new Layer[0];
			for (int i = 0; i < baseLayers.Length; i++)
				if (baseLayers[i].input != null)
					yield return baseLayers[i].input;
		}
		public override IEnumerable<Output> Outputs()
		{
			if (baseLayers == null) baseLayers = new Layer[0];
			for (int i = 0; i < baseLayers.Length; i++)
				if (baseLayers[i].output != null)
					yield return baseLayers[i].output;
		}

		//get static actions using instance
		public override Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>> GetProces () { return Process; }
		public override Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator> GetApply () { return Apply; }
		public override Action<CoordRect, Terrain> GetPurge () { return Purge; }


		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			if ((stop!=null && stop(0)) || !enabled) return;

			//loading inputs
			Matrix[] matrices = new Matrix[baseLayers.Length];
			for (int i = 0; i < baseLayers.Length; i++)
			{
				if (baseLayers[i].input != null)
				{
					matrices[i] = (Matrix)baseLayers[i].input.GetObject(results);
					if (matrices[i] != null) matrices[i] = matrices[i].Copy(null);
				}
				if (matrices[i] == null) matrices[i] = new Matrix(rect);
			}

			//background matrix
			//matrices[0] = terrain.defaultMatrix; //already created
			matrices[0].Fill(1);

			//populating opacity array
			float[] opacities = new float[matrices.Length];
			for (int i = 0; i < baseLayers.Length; i++)
				opacities[i] = baseLayers[i].opacity;
			opacities[0] = 1;

			//blending layers
			Matrix.BlendLayers(matrices, opacities);

			//saving changed matrix results
			for (int i = 0; i < baseLayers.Length; i++)
			{
				if (stop!=null && stop(0)) return; //do not write object is generating is stopped
				baseLayers[i].output.SetObject(results, matrices[i]);
			}
		}

		public static void Process (CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop = null)
		{
			#if UNITY_2018_3_OR_NEWER
			if (stop!=null && stop(0)) return;

			//gathering prototypes and matrices lists
			List<TerrainLayer> prototypesList = new List<TerrainLayer>();
			List<float> opacities = new List<float>();
			List<Matrix> matrices = new List<Matrix>();
			List<Matrix> biomeMasks = new List<Matrix>();

			foreach (TexturesOutput gen in gens.GeneratorsOfType<TexturesOutput>(onlyEnabled: true, checkBiomes: true))
			{
				//loading biome matrix
				Matrix biomeMask = null;
				if (gen.biome != null)
				{
					object biomeMaskObj = gen.biome.mask.GetObject(results);
					if (biomeMaskObj == null) continue; //adding nothing if biome has no mask
					biomeMask = (Matrix)biomeMaskObj;
					if (biomeMask == null) continue;
					if (biomeMask.IsEmpty()) continue; //optimizing empty biomes
				}

				for (int i = 0; i < gen.baseLayers.Length; i++)
				{
					//reading output directly
					Output output = gen.baseLayers[i].output;
					if (stop!=null && stop(0)) return; //checking stop before reading output
					if (!results.results.ContainsKey(output)) continue;
					Matrix matrix = (Matrix)results.results[output];
					matrix.Clamp01();

					//adding to lists
					matrices.Add(matrix);
					biomeMasks.Add(gen.biome == null ? null : biomeMask);
					prototypesList.Add(gen.baseLayers[i].prototype);
					opacities.Add(gen.baseLayers[i].opacity);
				}
			}

			//optimizing matrices list if they are not used
//			for (int i = matrices.Count - 1; i >= 0; i--)
//				if (opacities[i] < 0.001f || matrices[i].IsEmpty() || (biomeMasks[i] != null && biomeMasks[i].IsEmpty()))
//				{ prototypesList.RemoveAt(i); opacities.RemoveAt(i); matrices.RemoveAt(i); biomeMasks.RemoveAt(i); }

			//creating array
			float[,,] splats3D = new float[terrainSize.resolution, terrainSize.resolution, prototypesList.Count];
			if (matrices.Count == 0) { results.apply.CheckAdd(typeof(TexturesOutput), new TupleSet<float[,,], SplatPrototype[]>(splats3D, new SplatPrototype[0]), replace: true); return; }

			//filling array
			if (stop!=null && stop(0)) return;

			int numLayers = matrices.Count;
			int maxX = splats3D.GetLength(0); int maxZ = splats3D.GetLength(1); //MapMagic.instance.resolution should not be used because of possible lods
																				//CoordRect rect =  matrices[0].rect;

			float[] values = new float[numLayers]; //row, to avoid reading/writing 3d array (it is too slow)

			for (int x = 0; x < maxX; x++)
				for (int z = 0; z < maxZ; z++)
				{
					int pos = rect.GetPos(x + rect.offset.x, z + rect.offset.z);
					float sum = 0;

					//getting values
					for (int i = 0; i < numLayers; i++)
					{
						float val = matrices[i].array[pos];
						if (biomeMasks[i] != null) val *= biomeMasks[i].array[pos]; //if mask is not assigned biome was ignored, so only main outs with mask==null left here
						if (val < 0) val = 0; if (val > 1) val = 1;
						sum += val; //normalizing: calculating sum
						values[i] = val;
					}

					//setting color
					for (int i = 0; i < numLayers; i++) splats3D[z, x, i] = values[i] / sum;
				}

			//pushing to apply
			if (stop!=null && stop(0)) return;
			TupleSet<float[,,], TerrainLayer[]> splatsTuple = new TupleSet<float[,,], TerrainLayer[]>(splats3D, prototypesList.ToArray());
			results.apply.CheckAdd(typeof(TexturesOutput), splatsTuple, replace: true);
			#endif
		}

		public static IEnumerator Apply(CoordRect rect, Terrain terrain, object dataBox, Func<float,bool> stop= null)
		{
			#if UNITY_2018_3_OR_NEWER
			TupleSet<float[,,], TerrainLayer[]> splatsTuple = (TupleSet<float[,,], TerrainLayer[]>)dataBox;
			float[,,] splats3D = splatsTuple.item1;
			TerrainLayer[] prototypes = splatsTuple.item2;

			if (splats3D.GetLength(2) == 0) { Purge(rect,terrain); yield break; }

			if (terrain == null) yield break;
			TerrainData data = terrain.terrainData;

			//setting resolution
			int size = splats3D.GetLength(0);
			if (data.alphamapResolution != size) data.alphamapResolution = size;
			yield return null;

			//welding
			if (MapMagic.instance != null && MapMagic.instance.splatsWeldMargins!=0)
			{
				Coord coord = Coord.PickCell(rect.offset, MapMagic.instance.resolution);
				//Chunk chunk = MapMagic.instance.chunks[coord.x, coord.z];
				
				Chunk neigPrevX = MapMagic.instance.chunks[coord.x-1, coord.z];
				if (neigPrevX!=null && (neigPrevX.worker.ready || neigPrevX.locked)) WeldTerrains.WeldSplatToPrevX(ref splats3D, neigPrevX.terrain, MapMagic.instance.splatsWeldMargins);

				Chunk neigNextX = MapMagic.instance.chunks[coord.x+1, coord.z];
				if (neigNextX!=null && (neigNextX.worker.ready || neigNextX.locked)) WeldTerrains.WeldSplatToNextX(ref splats3D, neigNextX.terrain, MapMagic.instance.splatsWeldMargins);

				Chunk neigPrevZ = MapMagic.instance.chunks[coord.x, coord.z-1];
				if (neigPrevZ!=null && (neigPrevZ.worker.ready || neigPrevZ.locked)) WeldTerrains.WeldSplatToPrevZ(ref splats3D, neigPrevZ.terrain, MapMagic.instance.splatsWeldMargins);

				Chunk neigNextZ = MapMagic.instance.chunks[coord.x, coord.z+1];
				if (neigNextZ!=null && (neigNextZ.worker.ready || neigNextZ.locked)) WeldTerrains.WeldSplatToNextZ(ref splats3D, neigNextZ.terrain, MapMagic.instance.splatsWeldMargins);
			}
			yield return null;

			//setting
			data.terrainLayers = prototypes;
			data.SetAlphamaps(0, 0, splats3D);
			#endif

			yield return null;
		}

		public static void Purge(CoordRect rect, Terrain terrain)
		{
			#if UNITY_2018_3_OR_NEWER
			//skipping if already purged
			if (terrain.terrainData.alphamapResolution<=16) return; //using 8 will return resolution to 16

			TerrainLayer[] prototypes = new TerrainLayer[1];
			prototypes[0] = new TerrainLayer();
			prototypes[0].diffuseTexture = defaultTex;
			terrain.terrainData.terrainLayers = prototypes;

			float[,,] emptySplats = new float[16, 16, 1];
			for (int x = 0; x < 16; x++)
				for (int z = 0; z < 16; z++)
					emptySplats[z, x, 0] = 1;

			terrain.terrainData.alphamapResolution = 16;
			terrain.terrainData.SetAlphamaps(0, 0, emptySplats);
			#endif
		}

		public override void OnGUI(GeneratorsAsset gens)
		{
			//Layer buttons
			layout.Par();
			layout.Label("Layers:", layout.Inset(0.4f));

			layout.DrawArrayAdd(ref baseLayers, ref selected, rect:layout.Inset(0.15f), reverse:true, createElement:() => new Layer(), onAdded:UnlinkBaseLayer );
			layout.DrawArrayRemove(ref baseLayers, ref selected, rect:layout.Inset(0.15f), reverse:true, onBeforeRemove:UnlinkLayer, onRemoved:UnlinkBaseLayer);
			layout.DrawArrayDown(ref baseLayers, ref selected, rect:layout.Inset(0.15f), dispUp:true, onSwitch:UnlinkBaseLayer);
			layout.DrawArrayUp(ref baseLayers, ref selected, rect:layout.Inset(0.15f), dispDown:true, onSwitch:UnlinkBaseLayer);

			//layers
			layout.Par(3);
			for (int num=baseLayers.Length-1; num>=0; num--)
				layout.DrawLayer(baseLayers[num].OnGUI, ref selected, num);
		}
	}


	[System.Serializable]
	[GeneratorMenu(menu = "Output", name = "Objects", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Objects")]
	public class ObjectOutput : OutputGenerator
	{
		//layer
		public class Layer
		{
			public Input input = new Input(InoutType.Objects);

			public Transform prefab;

			public bool relativeHeight = true;
			
			//public bool regardRotation = false;
			public bool rotate = true;
			public bool takeTerrainNormal = false;

			//public bool regardScale = false;
			public bool scale = true;
			public bool scaleY;

			//public bool usePool = true;
			public bool parentToRoot = false;

			//public bool processChildren = false;
			//public bool floorChildren;
			//public Vector2 rotateChildren;
			//public Vector2 scaleChildren;
			//public float removeChildren = 0;

			public void OnCollapsedGUI(Layout layout)
			{
				layout.margin = 20; layout.rightMargin = 5; layout.fieldSize = 1f;
				layout.Par(20);
				input.DrawIcon(layout);
				layout.Field(ref prefab, rect: layout.Inset());
			}

			public void OnGUI (Layout layout, bool selected, int num) 
			{
				layout.margin = 20; layout.rightMargin = 5;
				layout.Par(20);

				input.DrawIcon(layout);
				layout.Field(ref prefab, rect: layout.Inset());

				if (selected)
				{
					layout.Toggle(ref relativeHeight, "Relative Height");
					layout.Toggle(ref rotate, "Rotate");
					layout.Toggle(ref takeTerrainNormal, "Incline by Terrain");
					layout.Par(); layout.Toggle(ref scale, "Scale", rect: layout.Inset(60));
					layout.disabled = !scale;
					layout.Toggle(ref scaleY, rect: layout.Inset(18)); layout.Label("Y only", rect: layout.Inset(45)); //if (layout.lastChange) scaleU = false;
					layout.disabled = false;
				}
			}

			//public void OnAdd(int n) { }
			//public void OnRemove(int n) { input.Link(null, null); }
			//public void OnSwitch(int o, int n) { }
		}

		//layer
		public Layer[] baseLayers = new Layer[0];
		public int selected;
		
		public void UnlinkBaseLayer (int p, int n)
		{
			if (baseLayers[0].input.link != null) 
				baseLayers[0].input.Link(null, null);
			baseLayers[0].input.guiRect = new Rect(0,0,0,0);
		}
		public void UnlinkBaseLayer (int n) { UnlinkBaseLayer(0,0); }
		
		public void UnlinkLayer (int num)
		{
			baseLayers[num].input.Link(null,null); //unlink input
			//baseLayers[num].output.UnlinkInActiveGens(); //try to unlink output
		}


		public enum BiomeBlendType { Sharp, AdditiveRandom, NormalizedRandom, Scale }
		public static BiomeBlendType biomeBlendType = BiomeBlendType.AdditiveRandom;

		//public class ObjectsTuple { public List<TransformPool.InstanceDraft[]> instances; public List<Layer> layers; }

		//generator
		public override IEnumerable<Input> Inputs()
		{
			if (baseLayers == null) baseLayers = new Layer[0];
			for (int i = 0; i < baseLayers.Length; i++)
				if (baseLayers[i].input != null)
					yield return baseLayers[i].input;
		}

		//get static actions using instance
		public override Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>> GetProces () { return Process; }
		public override Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator> GetApply () { return Apply; }
		public override Action<CoordRect, Terrain> GetPurge () { return Purge; }



		public static void Process(CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop = null)
		{
			if (stop!=null && stop(0)) return;

			Noise noise = new Noise(12345, permutationCount:128); //to pick objects based on biome

			//preparing output
			Dictionary<Transform, List<ObjectPool.Transition>> transitions = new Dictionary<Transform, List<ObjectPool.Transition>>();

			//find all of the biome masks - they will be used to determine object probability
			List<TupleSet<ObjectOutput,Matrix>> allGensMasks = new List<TupleSet<ObjectOutput, Matrix>>();
			foreach (ObjectOutput gen in gens.GeneratorsOfType<ObjectOutput>(onlyEnabled: true, checkBiomes: true))
			{
				Matrix biomeMask = null;
				if (gen.biome != null)
				{
					object biomeMaskObj = gen.biome.mask.GetObject(results);
					if (biomeMaskObj == null) continue; //adding nothing if biome has no mask
					biomeMask = (Matrix)biomeMaskObj;
					if (biomeMask == null) continue;
					if (biomeMask.IsEmpty()) continue; //optimizing empty biomes
				}

				allGensMasks.Add( new TupleSet<ObjectOutput, Matrix>(gen,biomeMask) );
			}
			int allGensMasksCount = allGensMasks.Count;

			//biome rect to find array pos faster
			CoordRect biomeRect = new CoordRect();
			for (int g=0; g<allGensMasksCount; g++)
				if (allGensMasks[g].item2 != null) { biomeRect = allGensMasks[g].item2.rect; break; }

			//prepare biome mask values stack to re-use it to find per-coord biome
			float[] biomeVals = new float[allGensMasksCount]; //+1 for not using any object at all

			//iterating all gens
			for (int g=0; g<allGensMasksCount; g++)
			{
				ObjectOutput gen = allGensMasks[g].item1;

				//iterating in layers
				for (int b = 0; b < gen.baseLayers.Length; b++)
				{
					if (stop!=null && stop(0)) return; //checking stop before reading output
					Layer layer = gen.baseLayers[b];
					if ((object)layer.prefab == null) continue;

					//loading objects from input
					SpatialHash hash = (SpatialHash)gen.baseLayers[b].input.GetObject(results);
					if (hash == null) continue;

					//finding/creating proper transitions list
					List<ObjectPool.Transition> transitionsList;
					if (!transitions.ContainsKey(layer.prefab)) { transitionsList = new List<ObjectPool.Transition>(); transitions.Add(layer.prefab, transitionsList); }
					else transitionsList = transitions[layer.prefab];

					//filling instances (no need to check/add key in multidict)
					foreach (SpatialObject obj in hash.AllObjs())
					{
						//blend biomes - calling continue if improper biome
						if (biomeBlendType == BiomeBlendType.Sharp)
						{
							float biomeVal = 1;
							if (allGensMasks[g].item2 != null) biomeVal = allGensMasks[g].item2[obj.pos];
							if (biomeVal < 0.5f) continue;
						}
						else if (biomeBlendType == BiomeBlendType.AdditiveRandom)
						{
							float biomeVal = 1;
							if (allGensMasks[g].item2 != null) biomeVal = allGensMasks[g].item2[obj.pos];

							float rnd = noise.Random((int)obj.pos.x, (int)obj.pos.y);

							if (biomeVal > 0.5f) rnd = 1-rnd;
							
							if (biomeVal < rnd) continue;
						}
						else if (biomeBlendType == BiomeBlendType.NormalizedRandom)
						{
							//filling biome masks values
							int pos = biomeRect.GetPos(obj.pos);

							for (int i=0; i<allGensMasksCount; i++)
							{
								if (allGensMasks[i].item2 != null) biomeVals[i] = allGensMasks[i].item2.array[pos];
								else biomeVals[i] = 1;
							}

							//calculate normalized sum
							float sum = 0;
							for (int i=0; i<biomeVals.Length; i++) sum += biomeVals[i];
							if (sum > 1) //note that if sum is <1 usedBiomeNum can exceed total number of biomes - it means that none object is used here
								for (int i=0; i<biomeVals.Length; i++) biomeVals[i] = biomeVals[i] / sum;
						
							//finding used biome num
							float rnd = noise.Random((int)obj.pos.x, (int)obj.pos.y);
							int usedBiomeNum = biomeVals.Length; //none biome by default
							sum = 0;
							for (int i=0; i<biomeVals.Length; i++)
							{
								sum += biomeVals[i];
								if (sum > rnd) { usedBiomeNum=i; break; }
							}

							//disable object using biome mask
							if (usedBiomeNum != g) continue;
						}
						//scale mode is applied a bit later


						//flooring
						float terrainHeight = 0;
						if (layer.relativeHeight && results.heights != null) //if checbox enabled and heights exist (at least one height generator is in the graph)
							terrainHeight = results.heights.GetInterpolated(obj.pos.x, obj.pos.y);
						if (terrainHeight > 1) terrainHeight = 1;


						//world-space object position
						Vector3 position = new Vector3(
							(obj.pos.x) / hash.size * terrainSize.dimensions,  // relative (0-1) position * terrain dimension
							(obj.height + terrainHeight) * terrainSize.height, 
							(obj.pos.y) / hash.size * terrainSize.dimensions);

						//rotation + taking terrain normal
						Quaternion rotation;
						float objRotation = layer.rotate ? obj.rotation % 360 : 0;
						if (layer.takeTerrainNormal)
						{
							Vector3 terrainNormal = GetTerrainNormal(obj.pos.x, obj.pos.y, results.heights, terrainSize.height, terrainSize.pixelSize);
							Vector3 sideVector = new Vector3( Mathf.Sin((obj.rotation+90)*Mathf.Deg2Rad), 0, Mathf.Cos((obj.rotation+90)*Mathf.Deg2Rad) );
							Vector3 frontVector = Vector3.Cross(sideVector, terrainNormal);
							rotation = Quaternion.LookRotation(frontVector, terrainNormal);
						}
						else rotation = objRotation.EulerToQuat();

						//scale + biome scale mode
						Vector3 scale = layer.scale ? new Vector3(layer.scaleY ? 1 : obj.size, obj.size, layer.scaleY ? 1 : obj.size) : Vector3.one;

						if (biomeBlendType == BiomeBlendType.Scale)
						{
							float biomeVal = 1;
							if (allGensMasks[g].item2 != null) biomeVal = allGensMasks[g].item2[obj.pos];
							if (biomeVal < 0.001f) continue;
							scale *= biomeVal;
						}
						
						transitionsList.Add(new ObjectPool.Transition() {pos=position, rotation=rotation, scale=scale });
					}
				}
			}

			//queue apply
			if (stop!=null && stop(0)) return;
			results.apply.CheckAdd(typeof(ObjectOutput), transitions, replace: true);
		}

		public static Vector3 GetTerrainNormal (float fx, float fz, Matrix heightmap, float heightFactor, float pixelSize)
		{
			//copy of rect's GetPos to process negative terrains properly
			int x = (int)(fx + 0.5f); 
			if (fx < 0) x--;
			if (x >= heightmap.rect.offset.x+heightmap.rect.size.x) x--;

			int z = (int)(fz + 0.5f); 
			if (fz < 0) z--;
			if (z >= heightmap.rect.offset.z+heightmap.rect.size.z) z--;
			
			int pos = (z-heightmap.rect.offset.z)*heightmap.rect.size.x + x - heightmap.rect.offset.x; 

			float curHeight = heightmap.array[pos];
						
			float prevXHeight = curHeight;
			if (x>=heightmap.rect.offset.x+1) prevXHeight = heightmap.array[pos-1];

			float nextXHeight = curHeight;
			if (x<heightmap.rect.offset.x+heightmap.rect.size.x-1) nextXHeight = heightmap.array[pos+1];
									
			float prevZHeight = curHeight;
			if (z>=heightmap.rect.offset.z+1) prevZHeight = heightmap.array[pos-heightmap.rect.size.x];

			float nextZHeight = curHeight;
			if (z<heightmap.rect.offset.z+heightmap.rect.size.z-1) nextZHeight = heightmap.array[pos+heightmap.rect.size.z];

			return new Vector3((prevXHeight-nextXHeight)*heightFactor, pixelSize*2, (prevZHeight-nextZHeight)*heightFactor).normalized;
		}

		public static IEnumerator Apply(CoordRect rect, Terrain terrain, object dataBox, Func<float,bool> stop= null)
		{
			Dictionary<Transform, List<ObjectPool.Transition>> transitions = (Dictionary<Transform, List<ObjectPool.Transition>>)dataBox;

			float pixelSize = 1f * MapMagic.instance.terrainSize / MapMagic.instance.resolution;
			Rect terrainRect = new Rect(rect.offset.x*pixelSize, rect.offset.z*pixelSize, rect.size.x*pixelSize, rect.size.z*pixelSize);

			//adding
			foreach (KeyValuePair<Transform,List<ObjectPool.Transition>> kvp in transitions)
			{
				Transform prefab = kvp.Key;
				List<ObjectPool.Transition> transitionsList = kvp.Value;
				
				if (terrain == null) yield break;
				IEnumerator e = MapMagic.instance.objectsPool.RepositionCoroutine(
					prefab, 
					terrainRect, 
					transitionsList, 
					parent:terrain.transform, 
					root:MapMagic.instance.transform.position.sqrMagnitude>Mathf.Epsilon? MapMagic.instance.transform : null, //don't use root if it is placed in the center of the scene
					objsPerFrame:500); 
						//not truly "per frame"\

				while (e.MoveNext()) 
					{ yield return null; }
			}

			//clear all of non-included pools from rect
			MapMagic.instance.objectsPool.ClearAllRectBut(terrainRect, transitions);
			
			//remove empty pools
			MapMagic.instance.objectsPool.RemoveEmptyPools();

			//hiding wireframe
			if (MapMagic.instance.hideWireframe) MapMagic.instance.transform.ToggleDisplayWireframe(!MapMagic.instance.guiHideWireframe);

			yield return null;
		}

		public static void Purge(CoordRect rect, Terrain terrain)
		{
			//early exit
			if (terrain.transform.childCount == 0) return;

			Coord coord = Coord.PickCell(rect.offset, MapMagic.instance.resolution);
			Rect terrainRect = new Rect(coord.x*MapMagic.instance.terrainSize, coord.z*MapMagic.instance.terrainSize, MapMagic.instance.terrainSize, MapMagic.instance.terrainSize);

			MapMagic.instance.objectsPool.ClearAllRect(terrainRect);

			MapMagic.instance.objectsPool.RemoveEmptyPools();
		}

		public override void OnGUI(GeneratorsAsset gens)
		{
			if (MapMagic.instance != null)
			{
				layout.Toggle(ref MapMagic.instance.objectsPool.regardPrefabRotation, "Regard Prefab Rotation");
				layout.Toggle(ref MapMagic.instance.objectsPool.regardPrefabScale, "Regard Prefab Scale");
				layout.Toggle(ref MapMagic.instance.objectsPool.instantiateClones, "Instantiate Clones");
				layout.Field(ref biomeBlendType, "Biome Blend", fieldSize:0.47f); 
				
				layout.Par(10);
			}

			//layer buttons
			layout.Par();
			layout.Label("Layers:", layout.Inset(0.4f));

			layout.DrawArrayAdd(ref baseLayers, ref selected, rect:layout.Inset(0.15f), createElement:() => new Layer() );
			layout.DrawArrayRemove(ref baseLayers, ref selected, rect:layout.Inset(0.15f), onBeforeRemove:UnlinkLayer);
			layout.DrawArrayUp(ref baseLayers, ref selected, rect:layout.Inset(0.15f));
			layout.DrawArrayDown(ref baseLayers, ref selected, rect:layout.Inset(0.15f));

			//layers
			layout.Par(3);
			for (int num=0; num<baseLayers.Length; num++)
				layout.DrawLayer(baseLayers[num].OnGUI, ref selected, num);
		}

	}

	[System.Serializable]
	[GeneratorMenu(menu = "Output", name = "Trees", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Trees")]
	public class TreesOutput : OutputGenerator
	{
		public enum BiomeBlendType { Sharp, AdditiveRandom, NormalizedRandom, Scale }
		public static BiomeBlendType biomeBlendType = BiomeBlendType.AdditiveRandom;

		//layer
		public class Layer
		{
			public Input input = new Input(InoutType.Objects);
			public Output output = new Output(InoutType.Objects);

			public GameObject prefab;
			public bool relativeHeight = true;
			public bool rotate;
			public bool widthScale;
			public bool heightScale;
			public Color color = Color.white;
			public float bendFactor;

			public void OnCollapsedGUI(Layout layout)
			{
				layout.margin = 20; layout.rightMargin = 5; layout.fieldSize = 1f;
				layout.Par(20);
				input.DrawIcon(layout);
				layout.Field(ref prefab, rect: layout.Inset());
			}

			public void OnGUI (Layout layout, bool selected, int num) 
			{
				layout.margin = 20; layout.rightMargin = 5;
				layout.Par(20);

				input.DrawIcon(layout);
				layout.Field(ref prefab, rect: layout.Inset());

				if (selected)
				{
					layout.Par(); layout.Toggle(ref relativeHeight, rect: layout.Inset(20)); layout.Label("Relative Height", rect: layout.Inset(100));
					layout.Par(); layout.Toggle(ref rotate, rect: layout.Inset(20)); layout.Label("Rotate", rect: layout.Inset(45));
					layout.Par(); layout.Toggle(ref widthScale, rect: layout.Inset(20)); layout.Label("Width Scale", rect: layout.Inset(100));
					layout.Par(); layout.Toggle(ref heightScale, rect: layout.Inset(20)); layout.Label("Height Scale", rect: layout.Inset(100));
					layout.fieldSize = 0.37f;
					layout.Field(ref color, "Color");
					layout.Field(ref bendFactor, "Bend Factor");
				}
			}

			//public void OnAdd(int n) { }
			//public void OnRemove(int n) { input.Link(null, null); }
			//public void OnSwitch(int o, int n) { }
		}

		//layers
		public Layer[] baseLayers = new Layer[0];
		public int selected;
		
		public void UnlinkBaseLayer (int p, int n)
		{
			if (baseLayers[0].input.link != null) 
				baseLayers[0].input.Link(null, null);
			baseLayers[0].input.guiRect = new Rect(0,0,0,0);
		}
		public void UnlinkBaseLayer (int n) { UnlinkBaseLayer(0,0); }
		
		public void UnlinkLayer (int num)
		{
			baseLayers[num].input.Link(null,null); //unlink input
			baseLayers[num].output.UnlinkInActiveGens(); //try to unlink output
		}


		//generator
		public override IEnumerable<Input> Inputs()
		{
			if (baseLayers == null) baseLayers = new Layer[0];
			for (int i = 0; i < baseLayers.Length; i++)
				if (baseLayers[i].input != null)
					yield return baseLayers[i].input;
		}
		public override IEnumerable<Output> Outputs()
		{
			if (baseLayers == null) baseLayers = new Layer[0];
			for (int i = 0; i < baseLayers.Length; i++)
				if (baseLayers[i].output != null)
					yield return baseLayers[i].output;
		}

		//get static actions using instance
		public override Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>> GetProces () { return Process; }
		public override Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator> GetApply () { return Apply; }
		public override Action<CoordRect, Terrain> GetPurge () { return Purge; }

		public static void Process(CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop = null)
		{
			if (stop!=null && stop(0)) return;

			Noise noise = new Noise(12345, permutationCount:128); //to pick objects based on biome

			List<TreeInstance> instancesList = new List<TreeInstance>();
			List<TreePrototype> prototypesList = new List<TreePrototype>();

			//find all of the biome masks - they will be used to determine object probability
			List<TupleSet<TreesOutput,Matrix>> allGensMasks = new List<TupleSet<TreesOutput, Matrix>>();
			foreach (TreesOutput gen in gens.GeneratorsOfType<TreesOutput>(onlyEnabled: true, checkBiomes: true))
			{
				Matrix biomeMask = null;
				if (gen.biome != null)
				{
					object biomeMaskObj = gen.biome.mask.GetObject(results);
					if (biomeMaskObj == null) continue; //adding nothing if biome has no mask
					biomeMask = (Matrix)biomeMaskObj;
					if (biomeMask == null) continue;
					if (biomeMask.IsEmpty()) continue; //optimizing empty biomes
				}

				allGensMasks.Add( new TupleSet<TreesOutput, Matrix>(gen,biomeMask) );
			}
			int allGensMasksCount = allGensMasks.Count;

			//biome rect to find array pos faster
			CoordRect biomeRect = new CoordRect();
			for (int g=0; g<allGensMasksCount; g++)
				if (allGensMasks[g].item2 != null) { biomeRect = allGensMasks[g].item2.rect; break; }

			//prepare biome mask values stack to re-use it to find per-coord biome
			float[] biomeVals = new float[allGensMasksCount]; //+1 for not using any object at all

			//iterating all gens
			for (int g=0; g<allGensMasksCount; g++)
			{
				TreesOutput gen = allGensMasks[g].item1;

				//iterating in layers
				for (int b = 0; b < gen.baseLayers.Length; b++)
				{
					if (stop!=null && stop(0)) return; //checking stop before reading output
					Layer layer = gen.baseLayers[b];
//					if (layer.prefab == null) continue;

					//loading objects from input
					SpatialHash hash = (SpatialHash)gen.baseLayers[b].input.GetObject(results);
					if (hash == null) continue;

					//adding prototype
//					if (layer.prefab == null) continue;
					TreePrototype prototype = new TreePrototype() { prefab = layer.prefab, bendFactor = layer.bendFactor };
					prototypesList.Add(prototype);
					int prototypeNum = prototypesList.Count - 1;

					//filling instances (no need to check/add key in multidict)
					foreach (SpatialObject obj in hash.AllObjs())
					{
						//blend biomes - calling continue if improper biome
						if (biomeBlendType == BiomeBlendType.Sharp)
						{
							float biomeVal = 1;
							if (allGensMasks[g].item2 != null) biomeVal = allGensMasks[g].item2[obj.pos];
							if (biomeVal < 0.5f) continue;
						}
						else if (biomeBlendType == BiomeBlendType.AdditiveRandom)
						{
							float biomeVal = 1;
							if (allGensMasks[g].item2 != null) biomeVal = allGensMasks[g].item2[obj.pos];

							float rnd = noise.Random((int)obj.pos.x, (int)obj.pos.y);

							if (biomeVal > 0.5f) rnd = 1-rnd;
							
							if (biomeVal < rnd) continue;
						}
						else if (biomeBlendType == BiomeBlendType.NormalizedRandom)
						{
							//filling biome masks values
							int pos = biomeRect.GetPos(obj.pos);

							for (int i=0; i<allGensMasksCount; i++)
							{
								if (allGensMasks[i].item2 != null) biomeVals[i] = allGensMasks[i].item2.array[pos];
								else biomeVals[i] = 1;
							}

							//calculate normalized sum
							float sum = 0;
							for (int i=0; i<biomeVals.Length; i++) sum += biomeVals[i];
							if (sum > 1) //note that if sum is <1 usedBiomeNum can exceed total number of biomes - it means that none object is used here
								for (int i=0; i<biomeVals.Length; i++) biomeVals[i] = biomeVals[i] / sum;
						
							//finding used biome num
							float rnd = noise.Random((int)obj.pos.x, (int)obj.pos.y);
							int usedBiomeNum = biomeVals.Length; //none biome by default
							sum = 0;
							for (int i=0; i<biomeVals.Length; i++)
							{
								sum += biomeVals[i];
								if (sum > rnd) { usedBiomeNum=i; break; }
							}

							//disable object using biome mask
							if (usedBiomeNum != g) continue;
						}
						//scale mode is applied a bit later

						//flooring
						float terrainHeight = 0;
						if (layer.relativeHeight && results.heights != null) //if checbox enabled and heights exist (at least one height generator is in the graph)
							terrainHeight = results.heights.GetInterpolated(obj.pos.x, obj.pos.y);
						if (terrainHeight > 1) terrainHeight = 1;

						TreeInstance tree = new TreeInstance();
						tree.position = new Vector3(
							(obj.pos.x - hash.offset.x) / hash.size,
							obj.height + terrainHeight,
							(obj.pos.y - hash.offset.y) / hash.size);
						tree.rotation = layer.rotate ? obj.rotation % 360 : 0;
						tree.widthScale = layer.widthScale ? obj.size : 1;
						tree.heightScale = layer.heightScale ? obj.size : 1;
						tree.prototypeIndex = prototypeNum;
						tree.color = layer.color;
						tree.lightmapColor = layer.color;

						if (biomeBlendType == BiomeBlendType.Scale)
						{
							float biomeVal = 1;
							if (allGensMasks[g].item2 != null) biomeVal = allGensMasks[g].item2[obj.pos];
							if (biomeVal < 0.001f) continue;
							tree.widthScale *= biomeVal;
							tree.heightScale *= biomeVal;
						}

						instancesList.Add(tree);
					}
				}
			}

			//setting output
			if (stop!=null && stop(0)) return;
			if (instancesList.Count == 0 && prototypesList.Count == 0) return; //empty, process is caused by height change
			TupleSet<TreeInstance[], TreePrototype[]> treesTuple = new TupleSet<TreeInstance[], TreePrototype[]>(instancesList.ToArray(), prototypesList.ToArray());
			results.apply.CheckAdd(typeof(TreesOutput), treesTuple, replace: true);
		}

		/*public static void Process(CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop = null)
		{
			if (stop!=null && stop(0)) return;

			List<TreeInstance> instancesList = new List<TreeInstance>();
			List<TreePrototype> prototypesList = new List<TreePrototype>();

			//InstanceRandom rnd = new InstanceRandom(MapMagic.instance.seed + 12345 + chunk.coord.x*1000 + chunk.coord.z); //to disable objects according biome masks

			foreach (TreesOutput gen in gens.GeneratorsOfType<TreesOutput>(onlyEnabled: true, checkBiomes: true))
			{
				//loading biome matrix
				Matrix biomeMask = null;
				if (gen.biome != null)
				{
					object biomeMaskObj = gen.biome.mask.GetObject(results);
					if (biomeMaskObj == null) continue; //adding nothing if biome has no mask
					biomeMask = (Matrix)biomeMaskObj;
					if (biomeMask == null) continue;
					if (biomeMask.IsEmpty()) continue; //optimizing empty biomes
				}

				for (int i = 0; i < gen.baseLayers.Length; i++)
				{
					if (stop!=null && stop(0)) return; //checking stop before reading output
					Layer layer = gen.baseLayers[i];

					//loading objects from input
					SpatialHash hash = (SpatialHash)gen.baseLayers[i].input.GetObject(results);
					if (hash == null) continue;

					//adding prototype
					if (layer.prefab == null) continue;
					TreePrototype prototype = new TreePrototype() { prefab = layer.prefab, bendFactor = layer.bendFactor };
					prototypesList.Add(prototype);
					int prototypeNum = prototypesList.Count - 1;

					foreach (SpatialObject obj in hash.AllObjs())
					{
						//disabling object using biome mask
						if (gen.biome != null)
						{
							if (biomeMask == null || biomeMask[obj.pos] < 0.5f) continue;
							//if (biomeMask[obj.pos] < rnd.CoordinateRandom((int)obj.pos.x, (int)obj.pos.y)) continue; 
						}

						//flooring
						float terrainHeight = 0;
						if (layer.relativeHeight && results.heights != null) //if checbox enabled and heights exist (at least one height generator is in the graph)
							terrainHeight = results.heights.GetInterpolated(obj.pos.x, obj.pos.y);
						if (terrainHeight > 1) terrainHeight = 1;

						TreeInstance tree = new TreeInstance();
						tree.position = new Vector3(
							(obj.pos.x - hash.offset.x) / hash.size,
							obj.height + terrainHeight,
							(obj.pos.y - hash.offset.y) / hash.size);
						tree.rotation = layer.rotate ? obj.rotation % 360 : 0;
						tree.widthScale = layer.widthScale ? obj.size : 1;
						tree.heightScale = layer.heightScale ? obj.size : 1;
						tree.prototypeIndex = prototypeNum;
						tree.color = layer.color;
						tree.lightmapColor = layer.color;

						instancesList.Add(tree);
					}
				}
			}

			//setting output
			if (stop!=null && stop(0)) return;
			if (instancesList.Count == 0 && prototypesList.Count == 0) return; //empty, process is caused by height change
			Tuple<TreeInstance[], TreePrototype[]> treesTuple = new Tuple<TreeInstance[], TreePrototype[]>(instancesList.ToArray(), prototypesList.ToArray());
			results.apply.CheckAdd(typeof(TreesOutput), treesTuple, replace: true);
		}*/

		public static IEnumerator Apply(CoordRect rect, Terrain terrain, object dataBox, Func<float,bool> stop= null)
		{
			if (terrain == null) yield break;
			terrain.terrainData.treeInstances = new TreeInstance[0];
			TupleSet<TreeInstance[], TreePrototype[]> treesTuple = (TupleSet<TreeInstance[], TreePrototype[]>)dataBox;
			if (treesTuple.item2 != null) //if tree prototype is not null
			{
				terrain.terrainData.treePrototypes = treesTuple.item2;
				terrain.terrainData.treeInstances = treesTuple.item1;
			}

			yield return null;
		}


		public static void Purge(CoordRect rect, Terrain terrain)
		{
			//skipping if already purged
			if (terrain.terrainData.treeInstances.Length==0) return;
			
			//if (chunk.locked) return;
			terrain.terrainData.treeInstances = new TreeInstance[0];
			terrain.terrainData.treePrototypes = new TreePrototype[0];

		}


		public override void OnGUI(GeneratorsAsset gens)
		{
			layout.Field(ref biomeBlendType, "Biome Blend", fieldSize:0.47f); 
			
			//layer buttons
			layout.Par(5);
			layout.Par();
			layout.Label("Layers:", layout.Inset(0.4f));

			layout.DrawArrayAdd(ref baseLayers, ref selected, rect:layout.Inset(0.15f), createElement:() => new Layer() );
			layout.DrawArrayRemove(ref baseLayers, ref selected, rect:layout.Inset(0.15f), onBeforeRemove:UnlinkLayer);
			layout.DrawArrayUp(ref baseLayers, ref selected, rect:layout.Inset(0.15f));
			layout.DrawArrayDown(ref baseLayers, ref selected, rect:layout.Inset(0.15f));

			//layers
			layout.Par(3);
			for (int num=0; num<baseLayers.Length; num++)
				layout.DrawLayer(baseLayers[num].OnGUI, ref selected, num);
		}

	}


	[System.Serializable]
	[GeneratorMenu(menu = "Output", name = "Grass", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Grass")]
	public class GrassOutput : OutputGenerator
	{
		//layer
		public class Layer
		{
			public Input input = new Input(InoutType.Map);
			public Output output = new Output(InoutType.Map);

			public DetailPrototype det = new DetailPrototype();
			public string name;
			public float density = 0.5f;
			public enum GrassRenderMode { Grass, GrassBillboard, VertexLit, Object };
			public GrassRenderMode renderMode;

			public void OnGUI (Layout layout, bool selected, int num) 
			{
				layout.margin = 20; layout.rightMargin = 20; layout.fieldSize = 1f;
				layout.Par(20);

				input.DrawIcon(layout);
				if (selected) layout.Field(ref name, rect: layout.Inset());
				else layout.Label(name, rect: layout.Inset());
				if (output == null) output = new Output(InoutType.Map); //backwards compatibility
				output.DrawIcon(layout);

				if (selected)
				{
					layout.margin = 5; layout.rightMargin = 10; layout.fieldSize = 0.6f;
					layout.fieldSize = 0.65f;

					//setting render mode
					if (renderMode == GrassRenderMode.Grass && det.renderMode != DetailRenderMode.Grass) //loading outdated
					{
						if (det.renderMode == DetailRenderMode.GrassBillboard) renderMode = GrassRenderMode.GrassBillboard;
						else renderMode = GrassRenderMode.VertexLit;
					}

					renderMode = layout.Field(renderMode, "Mode");

					if (renderMode == GrassRenderMode.Object || renderMode == GrassRenderMode.VertexLit)
					{
						det.prototype = layout.Field(det.prototype, "Object");
						det.prototypeTexture = null; //otherwise this texture will be included to build even if not displayed
						det.usePrototypeMesh = true;
					}
					else
					{
						layout.Par(60); //not 65
						layout.Inset((layout.field.width - 60) / 2);
						det.prototypeTexture = layout.Field(det.prototypeTexture, rect: layout.Inset(60));
						det.prototype = null; //otherwise this object will be included to build even if not displayed
						det.usePrototypeMesh = false;
						layout.Par(2);
					}
					switch (renderMode)
					{
						case GrassRenderMode.Grass: det.renderMode = DetailRenderMode.Grass; break;
						case GrassRenderMode.GrassBillboard: det.renderMode = DetailRenderMode.GrassBillboard; break;
						case GrassRenderMode.VertexLit: det.renderMode = DetailRenderMode.VertexLit; break;
						case GrassRenderMode.Object: det.renderMode = DetailRenderMode.Grass; break;
					}

					density = layout.Field(density, "Density", max: 50);
					//det.bendFactor = layout.Field(det.bendFactor, "Bend");
					det.dryColor = layout.Field(det.dryColor, "Dry");
					det.healthyColor = layout.Field(det.healthyColor, "Healthy");

					Vector2 temp = new Vector2(det.minWidth, det.maxWidth);
					layout.Field(ref temp, "Width", max: 10);
					det.minWidth = temp.x; det.maxWidth = temp.y;

					temp = new Vector2(det.minHeight, det.maxHeight);
					layout.Field(ref temp, "Height", max: 10);
					det.minHeight = temp.x; det.maxHeight = temp.y;

					det.noiseSpread = layout.Field(det.noiseSpread, "Noise", max: 1);
				}
			}

			//public void OnAdd(int n) { name = "Grass"; }
			//public void OnRemove(int n) { input.Link(null, null); }
			//public void OnSwitch(int o, int n) { }
		}
		public Layer[] baseLayers = new Layer[0];
		public int selected;

		public void UnlinkBaseLayer (int p, int n)
		{
			if (baseLayers[0].input.link != null) 
				baseLayers[0].input.Link(null, null);
			baseLayers[0].input.guiRect = new Rect(0,0,0,0);
		}
		public void UnlinkBaseLayer (int n) { UnlinkBaseLayer(0,0); }
		
		public void UnlinkLayer (int num)
		{
			baseLayers[num].input.Link(null,null); //unlink input
			baseLayers[num].output.UnlinkInActiveGens(); //try to unlink output
		}


		//params
		public Input maskIn = new Input(Generator.InoutType.Map);
		public static int patchResolution = 16;
		public bool obscureLayers = false;

		//public class GrassTuple { public int[][,] details; public DetailPrototype[] prototypes; }

		//generator
		public override IEnumerable<Input> Inputs()
		{
			if (maskIn == null) maskIn = new Input(InoutType.Map); //for backwards compatibility, input should not be null
			yield return maskIn;

			if (baseLayers == null) baseLayers = new Layer[0];
			for (int i = 0; i < baseLayers.Length; i++)
				if (baseLayers[i].input != null)
					yield return baseLayers[i].input;
		}

		public override IEnumerable<Output> Outputs()
		{
			if (baseLayers == null) baseLayers = new Layer[0];
			for (int i = 0; i < baseLayers.Length; i++)
				if (baseLayers[i].output != null)
					yield return baseLayers[i].output;
		}

		//get static actions using instance
		public override Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>> GetProces () { return Process; }
		public override Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator> GetApply () { return Apply; }
		public override Action<CoordRect, Terrain> GetPurge () { return Purge; }


		public override void Generate(CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop= null)
		{
			//loading inputs
			Matrix[] matrices = new Matrix[baseLayers.Length];
			for (int i = 0; i < baseLayers.Length; i++)
			{
				if (baseLayers[i].input != null)
				{
					matrices[i] = (Matrix)baseLayers[i].input.GetObject(results);
					if (matrices[i] != null) matrices[i] = matrices[i].Copy(null);
				}
				if (matrices[i] == null) matrices[i] = new Matrix(rect);
			}

			//blending layers
			if (obscureLayers) Matrix.BlendLayers(matrices);

			//masking layers
			Matrix mask = (Matrix)maskIn.GetObject(results);
			if (mask != null)
				for (int i = 0; i < matrices.Length; i++) matrices[i].Multiply(mask);

			//saving outputs
			for (int i = 0; i < baseLayers.Length; i++)
			{
				if (stop!=null && stop(0)) return; //do not write object is generating is stopped
				if (baseLayers[i].output == null) baseLayers[i].output = new Output(InoutType.Map); //back compatibility
				baseLayers[i].output.SetObject(results, matrices[i]);
			}
		}

		public static void Process(CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop = null)
		{
			if (stop!=null && stop(0)) return;

			//debug timer
			/*			if (MapMagic.instance!=null && MapMagic.instance.guiDebug && worker!=null)
						{
							if (worker.timer==null) worker.timer = new System.Diagnostics.Stopwatch(); 
							else worker.timer.Reset();
							worker.timer.Start();
						}*/

			//values to calculate density
			float pixelSize = terrainSize.pixelSize;
			float pixelSquare = pixelSize * pixelSize;

			//a random needed to convert float to int
			InstanceRandom rnd = new InstanceRandom(terrainSize.Seed(rect));

			//calculating the totoal number of prototypes
			int prototypesNum = 0;
			foreach (GrassOutput grassOut in gens.GeneratorsOfType<GrassOutput>())
				prototypesNum += grassOut.baseLayers.Length;

			//preparing results
			List<int[,]> detailsList = new List<int[,]>();
			List<DetailPrototype> prototypesList = new List<DetailPrototype>();

			//filling result
			foreach (GrassOutput gen in gens.GeneratorsOfType<GrassOutput>(onlyEnabled: true, checkBiomes: true))
			{
				//loading biome matrix
				Matrix biomeMask = null;
				if (gen.biome != null)
				{
					object biomeMaskObj = gen.biome.mask.GetObject(results);
					if (biomeMaskObj == null) continue; //adding nothing if biome has no mask
					biomeMask = (Matrix)biomeMaskObj;
					if (biomeMask == null) continue;
					if (biomeMask.IsEmpty()) continue; //optimizing empty biomes
				}

				for (int i = 0; i < gen.baseLayers.Length; i++)
				{
					if (stop!=null && stop(0)) return;

					//loading objects from input
					//Matrix matrix = (Matrix)gen.baseLayers[i].input.GetObject(chunk);
					//if (matrix == null) continue;

					//reading output directly
					Output output = gen.baseLayers[i].output;
					if (stop!=null && stop(0)) return; //checking stop before reading output
					if (!results.results.ContainsKey(output)) continue;
					Matrix matrix = (Matrix)results.results[output];

					//filling array
					int[,] detail = new int[matrix.rect.size.x, matrix.rect.size.z];
					for (int x = 0; x < matrix.rect.size.x; x++)
						for (int z = 0; z < matrix.rect.size.z; z++)
						{
							float val = matrix[x + matrix.rect.offset.x, z + matrix.rect.offset.z];
							float biomeVal = 1;
							if (gen.biome != null)
							{
								if (biomeMask == null) biomeVal = 0;
								else biomeVal = biomeMask[x + matrix.rect.offset.x, z + matrix.rect.offset.z];
							}
							detail[z, x] = rnd.RandomToInt(val * gen.baseLayers[i].density * pixelSquare * biomeVal);
						}

					//adding to arrays
					detailsList.Add(detail);
					prototypesList.Add(gen.baseLayers[i].det);
				}
			}

			//pushing to apply
			if (stop!=null && stop(0)) return;

            TupleSet<int[][,], DetailPrototype[]> grassTuple = new TupleSet<int[][,], DetailPrototype[]>(detailsList.ToArray(), prototypesList.ToArray());

            #if UN_MapMagic
            if (FoliageCore_MainManager.instance != null)
            {
                float resolutionDifferences = (float)MapMagic.instance.terrainSize / terrainSize.resolution;

                var uNatureTuple = new uNatureGrassTuple(grassTuple, new Vector3(rect.Min.x * resolutionDifferences, 0, rect.Min.z * resolutionDifferences)); // transform coords
                results.apply.CheckAdd(typeof(GrassOutput), uNatureTuple, replace: true);
            }
            else
            {
                //Debug.LogError("uNature_MapMagic extension is enabled but no foliage manager exists on the scene.");
                //return;
				results.apply.CheckAdd(typeof(GrassOutput), grassTuple, replace: true);
            }
            #else
            results.apply.CheckAdd(typeof(GrassOutput), grassTuple, replace: true);
            #endif
        }


        public static IEnumerator Apply(CoordRect rect, Terrain terrain, object dataBox, Func<float,bool> stop= null)
		{
            TupleSet<int[][,], DetailPrototype[]> grassTuple;

            #if UN_MapMagic
            uNatureGrassTuple uNatureTuple = null;

            if (FoliageCore_MainManager.instance != null)
            {
                uNatureTuple = (uNatureGrassTuple)dataBox;
                grassTuple = uNatureTuple.tupleInformation;
            }
            else
            {
                //Debug.LogError("uNature_MapMagic extension is enabled but no foliage manager exists on the scene.");
                //yield break;
				grassTuple = (TupleSet<int[][,], DetailPrototype[]>)dataBox;
            }
            #else
            grassTuple = (TupleSet<int[][,], DetailPrototype[]>)dataBox;
            #endif

            int[][,] details = grassTuple.item1;
			DetailPrototype[] prototypes = grassTuple.item2;

			//resolution
			if (details.Length != 0)
			{
				int resolution = details[0].GetLength(1);
				if (terrain == null) yield break;
				terrain.terrainData.SetDetailResolution(resolution, patchResolution);
			}

            #if UN_MapMagic
            if (FoliageCore_MainManager.instance != null)
            {
                UNMapMagic_Manager.RegisterGrassPrototypesChange(prototypes);
            }
            #endif

            //prototypes
            terrain.terrainData.detailPrototypes = prototypes;

            #if UN_MapMagic
            if (FoliageCore_MainManager.instance != null)
            {
                UNMapMagic_Manager.ApplyGrassOutput(uNatureTuple);
            }
            else
            {
                //Debug.LogError("uNature_MapMagic extension is enabled but no foliage manager exists on the scene.");
                //yield break;
				for (int i = 0; i < details.Length; i++)
				{
					terrain.terrainData.SetDetailLayer(0, 0, i, details[i]);
				}
            }
			#else
			for (int i = 0; i < details.Length; i++)
			{
				terrain.terrainData.SetDetailLayer(0, 0, i, details[i]);
			}
			#endif

			yield return null;
		}

		public static void Purge(CoordRect rect, Terrain terrain)
		{
			//skipping if already purged
			if (terrain.terrainData.detailPrototypes.Length==0) return;

			DetailPrototype[] prototypes = new DetailPrototype[0];
			terrain.terrainData.detailPrototypes = prototypes;
			terrain.terrainData.SetDetailResolution(16, 8);

			//if (MapMagic.instance.guiDebug) Debug.Log("Grass Cleared");

		}

		public override void OnGUI(GeneratorsAsset gens)
		{
			layout.Par(20); maskIn.DrawIcon(layout, "Mask");

			layout.Field(ref patchResolution, "Patch Res", min:4, max:64, fieldSize:0.35f);
			patchResolution = Mathf.ClosestPowerOfTwo(patchResolution);
			layout.Field(ref obscureLayers, "Obscure Layers", fieldSize: 0.35f);
			
			//layer buttons
			layout.Par(3);
			layout.Par();
			layout.Label("Layers:", layout.Inset(0.4f));

			layout.DrawArrayAdd(ref baseLayers, ref selected, rect:layout.Inset(0.15f), reverse:true, createElement:() => new Layer() );
			layout.DrawArrayRemove(ref baseLayers, ref selected, rect:layout.Inset(0.15f), reverse:true, onBeforeRemove:UnlinkLayer);
			layout.DrawArrayDown(ref baseLayers, ref selected, rect:layout.Inset(0.15f), dispUp:true);
			layout.DrawArrayUp(ref baseLayers, ref selected, rect:layout.Inset(0.15f), dispDown:true);

			//layers
			layout.Par(3);
			for (int num=baseLayers.Length-1; num>=0; num--)
				layout.DrawLayer(baseLayers[num].OnGUI, ref selected, num);

			layout.fieldSize = 0.4f; layout.margin = 10; layout.rightMargin = 10;
			layout.Par(5);
		}

	}

}
