using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#if __MEGASPLAT__
using JBooth.MegaSplat;
#endif

using MapMagic;

namespace MapMagic
{
	[System.Serializable]
    //[GeneratorMenu (menu="Custom", name ="Randomly Clear", disengageable = false)]
    public class RandomClearGenerator : Generator
    {
        public Input input = new Input(InoutType.Objects);
        public Output output = new Output(InoutType.Objects);
        public override IEnumerable<Input> Inputs() { yield return input; }
        public override IEnumerable<Output> Outputs() { yield return output; }

		public int seed = 12345;
        public float chance = 1;

        public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
        {
            SpatialHash src = (SpatialHash)input.GetObject(results);
            SpatialHash dst = new SpatialHash(src.offset, src.size, src.resolution);

			//initializing random
			InstanceRandom rnd = new InstanceRandom(seed + this.seed + terrainSize.Seed(rect));

			if (rnd.CoordinateRandom(rect.offset.x, rect.offset.z) < chance)
			foreach (SpatialObject obj in src.AllObjs())
			{
				dst.Add(obj);
			}

            output.SetObject(results, dst);
        }

        public override void OnGUI (GeneratorsAsset gens)
        {
            layout.Par(20); input.DrawIcon(layout, "Input"); output.DrawIcon(layout, "Output");
            layout.Par(5); 
			layout.Field(ref seed, "Seed");
			layout.Field(ref chance, "Chance");
        }
    }


	[System.Serializable]
    //[GeneratorMenu (menu="Custom", name ="Single", disengageable = false)]
    public class SingleGenerator : Generator
    {
        public Output output = new Output(InoutType.Objects);
        public override IEnumerable<Output> Outputs() { yield return output; }

		public Vector2 pos = new Vector2(1500, 2000);

        public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
        {
            SpatialHash dst = new SpatialHash(new Vector2(rect.offset.x,rect.offset.z), rect.size.x, 16);
			Vector2 pixelPos = pos / terrainSize.pixelSize;

			if (pixelPos.x > rect.offset.x && pixelPos.x <= rect.offset.x + rect.size.x &&
				pixelPos.y > rect.offset.z && pixelPos.y <= rect.offset.z + rect.size.z)
				 dst.Add(pixelPos, 0,0,1); //position, height, rotation, scale

            output.SetObject(results, dst);
        }
        public override void OnGUI (GeneratorsAsset gens)
        {
			layout.Par();
            output.DrawIcon(layout, "Output");
			layout.Par(5); 
			layout.Field(ref pos, "Position", fieldSize:0.7f);
        }
    }



	[GeneratorMenu (menu="Legacy", name ="CTS Output (1.83 Legacy)", disengageable = true, priority = 10, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/CTS",
		updateType = typeof(CustomShaderOutput))]
	public class CTSOutput  : SplatOutput
	{
		#if CTS_PRESENT
		public static CTS.CTSProfile ctsProfile;
		#endif

		//layer
		public new class Layer
		{
			public Input input = new Input(InoutType.Map);
			public Output output = new Output(InoutType.Map);
			public int index = 0;
			public float opacity = 1;
		}

		public new Layer[] baseLayers = new Layer[0];
		public new int selected = 0;

		//get static actions using instance
		public override Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>> GetProces() { return Process; }
		public override Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator> GetApply() { return Apply; }
		public override Action<CoordRect, Terrain> GetPurge() { return Purge; }


		//generator
		public override IEnumerable<Input> Inputs()
		{ 
			if (baseLayers == null)
				baseLayers = new Layer[0];

			for (int i = 1; i < baseLayers.Length; i++) //layer 0 is background
			{
				if (baseLayers[i] == null)
					baseLayers[i] = new Layer();
				if (baseLayers[i].input == null)
					baseLayers[i].input = new Input(InoutType.Map);
				
				yield return baseLayers[i].input; 
			}
		}

		public override IEnumerable<Output> Outputs()
		{ 
			if (baseLayers == null)
				baseLayers = new Layer[0];
			for (int i = 0; i < baseLayers.Length; i++)
			{
				if (baseLayers[i] == null) baseLayers[i] = new Layer(); 
				if (baseLayers[i].output == null)
					baseLayers[i].output = new Output(InoutType.Map);

				yield return baseLayers[i].output; 
			}
		}

		public override void Generate(CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop= null)
		{
			#if CTS_PRESENT
			if ((stop!=null && stop(0)) || !enabled) return;

			//loading inputs
			Matrix[] matrices = new Matrix[baseLayers.Length];
			for (int i = 0; i < baseLayers.Length; i++)
			{
				if (baseLayers[i].input != null)
				{
				   matrices[i] = (Matrix)baseLayers[i].input.GetObject(results);
				   if (matrices[i] != null)
					  matrices[i] = matrices[i].Copy(null);
				}
				if (matrices[i] == null)
				   matrices[i] = new Matrix(rect);
			}
			if (matrices.Length == 0)
				return;

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
				if (stop!=null && stop(0))
				   return; //do not write object is generating is stopped
				baseLayers[i].output.SetObject(results, matrices[i]);
			}
			#endif
		}

		public static new void Process (CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop = null)
		{
			#if CTS_PRESENT
			if (stop!=null && stop(0)) return;

			//gathering prototypes and matrices lists
			List<int> indexesList = new List<int>();
			List<float> opacities = new List<float>();
			List<Matrix> matrices = new List<Matrix>();
			List<Matrix> biomeMasks = new List<Matrix>();

			foreach (CTSOutput gen in gens.GeneratorsOfType<CTSOutput>(onlyEnabled: true, checkBiomes: true))
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
					indexesList.Add(gen.baseLayers[i].index);
					opacities.Add(gen.baseLayers[i].opacity);
				}
			}

			//optimizing matrices list if they are not used
			//CTS always use 16 channels, so this optimization is useless (and does not work when layer order changed)
			//for (int i = matrices.Count - 1; i >= 0; i--)
			//	if (opacities[i] < 0.001f || matrices[i].IsEmpty() || (biomeMasks[i] != null && biomeMasks[i].IsEmpty()))
			//	{ indexesList.RemoveAt(i); opacities.RemoveAt(i); matrices.RemoveAt(i); biomeMasks.RemoveAt(i); }

			//creating array
			float[,,] splats3D = new float[terrainSize.resolution, terrainSize.resolution, 16]; //TODO: use max index
			if (matrices.Count == 0) { results.apply.CheckAdd(typeof(CTSOutput), splats3D, replace: true); return; }

			//filling array
			if (stop!=null && stop(0)) return;

			int numLayers = matrices.Count;
			int numPrototypes = splats3D.GetLength(2);
			int maxX = splats3D.GetLength(0); int maxZ = splats3D.GetLength(1); //MapMagic.instance.resolution should not be used because of possible lods
																				//CoordRect rect =  matrices[0].rect;

			float[] values = new float[numPrototypes]; //row, to avoid reading/writing 3d array (it is too slow)

			for (int x = 0; x < maxX; x++)
				for (int z = 0; z < maxZ; z++)
				{
					int pos = rect.GetPos(x + rect.offset.x, z + rect.offset.z);
					float sum = 0;

					//clearing values
					for (int i = 0; i < numPrototypes; i++)
						values[i] = 0;

					//getting values
					for (int i = 0; i < numLayers; i++)
					{
						float val = matrices[i].array[pos];
						if (biomeMasks[i] != null) val *= biomeMasks[i].array[pos]; //if mask is not assigned biome was ignored, so only main outs with mask==null left here
						if (val < 0) val = 0; if (val > 1) val = 1;
						sum += val; //normalizing: calculating sum
						values[indexesList[i]] += val;
					}

					//setting color
					for (int i = 0; i < numLayers; i++) splats3D[z, x, i] = values[i] / sum;
				}

			//pushing to apply
			if (stop!=null && stop(0)) return;
			results.apply.CheckAdd(typeof(CTSOutput), splats3D, replace: true);
			#endif
		}

		public static new IEnumerator Apply(CoordRect rect, Terrain terrain, object dataBox, Func<float,bool> stop= null)
		{
			#if CTS_PRESENT

			float[,,] splats3D = (float[,,])dataBox;

			if (splats3D.GetLength(2) == 0) { Purge(rect,terrain); yield break; }

			TerrainData data = terrain.terrainData;

			//setting resolution
			int size = splats3D.GetLength(0);
			if (data.alphamapResolution != size) data.alphamapResolution = size;

			//welding
			if (MapMagic.instance != null && MapMagic.instance.splatsWeldMargins!=0)
			{
				Coord coord = Coord.PickCell(rect.offset, MapMagic.instance.resolution);
				//Chunk chunk = MapMagic.instance.chunks[coord.x, coord.z];
				
				Chunk neigPrevX = MapMagic.instance.chunks[coord.x-1, coord.z];
				if (neigPrevX!=null && neigPrevX.worker.ready) WeldTerrains.WeldSplatToPrevX(ref splats3D, neigPrevX.terrain, MapMagic.instance.splatsWeldMargins);

				Chunk neigNextX = MapMagic.instance.chunks[coord.x+1, coord.z];
				if (neigNextX!=null && neigNextX.worker.ready) WeldTerrains.WeldSplatToNextX(ref splats3D, neigNextX.terrain, MapMagic.instance.splatsWeldMargins);

				Chunk neigPrevZ = MapMagic.instance.chunks[coord.x, coord.z-1];
				if (neigPrevZ!=null && neigPrevZ.worker.ready) WeldTerrains.WeldSplatToPrevZ(ref splats3D, neigPrevZ.terrain, MapMagic.instance.splatsWeldMargins);

				Chunk neigNextZ = MapMagic.instance.chunks[coord.x, coord.z+1];
				if (neigNextZ!=null && neigNextZ.worker.ready) WeldTerrains.WeldSplatToNextZ(ref splats3D, neigNextZ.terrain, MapMagic.instance.splatsWeldMargins);
			}
			yield return null;

			//number of splat prototypes should match splats3D layers
			if (data.alphamapLayers != splats3D.GetLength(2))
			{
				SplatPrototype[] prototypes = new SplatPrototype[splats3D.GetLength(2)];
				for (int i=0; i<prototypes.Length; i++) prototypes[i] = new SplatPrototype() {texture = SplatOutput.defaultTex};
				data.splatPrototypes = prototypes;
//Debug.Log("Setting prototypes " + prototypes.Length + " in data: " + data.splatPrototypes.Length + " alphamaplayers: " + data.alphamapLayers);
			}
		
			//setting
			data.SetAlphamaps(0, 0, splats3D);

			//alphamap textures hide flag
			//data.alphamapTextures[0].hideFlags = HideFlags.None;
			

			//assigning CTS
			CTS.CompleteTerrainShader cts = terrain.gameObject.GetComponent<CTS.CompleteTerrainShader>();
			if (cts == null) cts = terrain.gameObject.AddComponent<CTS.CompleteTerrainShader>();
			cts.Profile = ctsProfile; 

//Debug.Log("Splats Length: " + splats3D.GetLength(2) + ", alphamaplayers: " + data.alphamapTextures.Length);	

			//cts.UpdateShader();
			//using CustomShaderOutput.CTS_ProfileToMaterial instead

/*
Debug.Log("Checking Update Shader");

Texture2D tex = mat.GetTexture("_Texture_Splat_1") as Texture2D;
Plugins.Matrix matrix = new Plugins.Matrix(tex);
if (matrix.IsEmpty()) Debug.Log("After Splat1 BLACK"); 
else Debug.Log("Its NOT black");

tex = Extensions.ColorTexture(tex.width, tex.height, Color.red);
mat.SetTexture("_Texture_Splat_1", tex);
*/


Chunk zero = MapMagic.instance.chunks[0,0];
//Texture2DArray array = zero.terrain.materialTemplate.Get("_Texture_Array_Albedo");
terrain.materialTemplate = zero.terrain.materialTemplate;

CustomShaderOutput.CTS_UpdateShader(ctsProfile, terrain.materialTemplate);


			yield return null;

			#else
			yield return null;
			#endif

		}

		public static new void Purge(CoordRect rect, Terrain terrain)
		{
			//purged on switching back to the standard shader
			//TODO: it's wrong, got to be filled with background layer
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			layout.Label("Replaced by ");
			layout.Label("CustomShader Output");

			#if CTS_PRESENT

			//wrong material and settings warnings
			/*if (MapMagic.instance.terrainMaterialType != Terrain.MaterialType.Custom)
			{
				layout.Par(30);
				layout.Label("Material Type is not switched to Custom.", rect:layout.Inset(0.8f), helpbox:true);
				if (layout.Button("Fix",rect:layout.Inset(0.2f))) 
				{
					MapMagic.instance.terrainMaterialType = Terrain.MaterialType.Custom;
					foreach (Chunk tw in MapMagic.instance.chunks.All()) tw.SetSettings();
				}
			}
			if (MapMagic.instance.assignCustomTerrainMaterial)
			{
				layout.Par(30);
				layout.Label("Assign Custom Material is turned on.", rect:layout.Inset(0.8f), helpbox:true);
				if (layout.Button("Fix",rect:layout.Inset(0.2f))) 
				{
					MapMagic.instance.assignCustomTerrainMaterial = false;
				}
			}*/


			//profile
			layout.Par(5);
			layout.Field(ref ctsProfile, "Profile", fieldSize:0.7f);
			if (ctsProfile == null) { ResetLayers(0); return; }
			
			//refreshing layers from cts
			List<CTS.CTSTerrainTextureDetails> textureDetails = ctsProfile.TerrainTextures;
			if (baseLayers.Length != textureDetails.Count) ResetLayers(textureDetails.Count);

			//drawing layers
			layout.Par(5);
			layout.margin = 20; layout.rightMargin = 20; layout.fieldSize = 1f;
			for (int i=baseLayers.Length-1; i>=0; i--)
			{
				//if (baseLayers[i] == null)
				//baseLayers[i] = new Layer();
			
				layout.DrawLayer(OnLayerGUI, ref selected, i);
				//if (layout.DrawWithBackground(OnLayerGUI, active:i==selected, num:i, frameDisabled:false)) selected = i;
			}

			layout.Par(3); layout.Par();
			//layout.DrawArrayAdd(ref baseLayers, ref selected, layout.Inset(0.25f));
			//layout.DrawArrayRemove(ref baseLayers, ref selected, layout.Inset(0.25f));
			layout.DrawArrayUp(ref baseLayers, ref selected, layout.Inset(0.25f), dispDown:true);
			layout.DrawArrayDown(ref baseLayers, ref selected, layout.Inset(0.25f), dispUp:true);

			#endif
		}

		public void OnLayerGUI (Layout layout, bool selected, int num)
		{

			#if CTS_PRESENT
				Layer layer = baseLayers[num];

				layout.Par(40); 

				if (num != 0) layer.input.DrawIcon(layout);
				else 
					if (layer.input.link != null) { layer.input.Link(null,null); } 
				
				//layout.Par(40); //not 65
				//layout.Field(ref rtp.globalSettingsHolder.splats[layer.index], rect:layout.Inset(40));
				layout.Inset(3);
				layout.Icon(ctsProfile.TerrainTextures[layer.index].Albedo, rect:layout.Inset(40), frame:true, alphaBlend:false);
				layout.Label(ctsProfile.TerrainTextures[layer.index].m_name, rect:layout.Inset(layout.field.width-80));
				if (num==0)
				{ 
					layout.cursor.y += layout.lineHeight;
					layout.cursor.height -= layout.lineHeight;
					layout.cursor.x -= layout.field.width-80;

					layout.Label("Background", rect:layout.Inset(layout.field.width-80), fontSize:9, fontStyle:FontStyle.Italic);
				}

				//layout.Label(rtp.globalSettingsHolder.splats[layer.index].name + (num==0? "\n(Background)" : ""), rect:layout.Inset(layout.field.width-60));

				baseLayers[num].output.DrawIcon(layout);
			#endif

		}

		public void ResetLayers (int newcount)
		{
			for (int i=0; i<baseLayers.Length; i++) 
			{
				baseLayers[i].input.Link(null,null); 

				Input connectedInput = baseLayers[i].output.GetConnectedInput(MapMagic.instance.gens.list);
				if (connectedInput != null) connectedInput.Link(null, null);
			}

			baseLayers = new Layer[newcount];
				
			for (int i=0; i<baseLayers.Length; i++) 
			{
				baseLayers[i] = new Layer();
				baseLayers[i].index = i;
			}
		}
	}


	[System.Serializable]
	[GeneratorMenu(menu = "Legacy", name = "MegaSplat Output (1.83 Legacy)", disengageable = true, priority = 10, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/MegaSplat",
		updateType = typeof(CustomShaderOutput))]
	public class MegaSplatOutput : OutputGenerator
	{
		#if __MEGASPLAT__
		public MegaSplatTextureList textureList; //TODO: think about texture list shared for all megasplat outputs (including biomes). Non-static
		public float clusterNoiseScale = 0.05f; //what's that?
		public bool smoothFallof = false;
		public static bool enableReadWrite = false;
		#endif

		public class Layer
		{
			public Input input = new Input(InoutType.Map);
			public Output output = new Output(InoutType.Map);
			public int index = 0;
			public float opacity = 1;
		}

		public Layer[] baseLayers = new Layer[0];
		public int selected = 0;

		public Input wetnessIn = new Input(InoutType.Map);
		public Input puddlesIn = new Input(InoutType.Map);
		public Input displaceDampenIn = new Input(InoutType.Map);

		public static bool formatARGB = false;


		public override IEnumerable<Input> Inputs()
		{ 
			if (baseLayers == null)
				baseLayers = new Layer[0];

			for (int i = 1; i < baseLayers.Length; i++) //layer 0 is background
			{
				if (baseLayers[i] == null)
					baseLayers[i] = new Layer();
				if (baseLayers[i].input == null)
					baseLayers[i].input = new Input(InoutType.Map);
				
				yield return baseLayers[i].input; 
			}
		
			yield return wetnessIn;
			yield return puddlesIn;
			yield return displaceDampenIn;
		}

		public override IEnumerable<Output> Outputs()
		{ 
			if (baseLayers == null)
				baseLayers = new Layer[0];
			for (int i = 0; i < baseLayers.Length; i++)
			{
				if (baseLayers[i] == null) baseLayers[i] = new Layer(); 
				if (baseLayers[i].output == null)
					baseLayers[i].output = new Output(InoutType.Map);

				yield return baseLayers[i].output; 
			}
		}



		//get static actions using instance
		public override Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>> GetProces()
		{
			return Process;
		}

		public override Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator> GetApply()
		{
			return Apply;
		}

		public override Action<CoordRect, Terrain> GetPurge()
		{
			return Purge;
		}



		public override void Generate(CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
	 		#if __MEGASPLAT__
			if ((stop!=null && stop(0)) || !enabled || textureList == null)
				return;

			//loading inputs
			Matrix[] matrices = new Matrix[baseLayers.Length];
			for (int i = 0; i < baseLayers.Length; i++)
			{
				if (baseLayers[i] == null)
					baseLayers[i] = new Layer();

				if (baseLayers[i].input != null)
				{
				   matrices[i] = (Matrix)baseLayers[i].input.GetObject(results);
				   if (matrices[i] != null)
				   {
					  matrices[i] = matrices[i].Copy(null);
					  matrices[i].Clamp01();
					}
				}
				if (matrices[i] == null)
				   matrices[i] = new Matrix(rect);
			}
			if (matrices.Length == 0)
				return;

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
				if (stop!=null && stop(0))
				   return; //do not write object is generating is stopped
				baseLayers[i].output.SetObject(results, matrices[i]);
			}
			#endif
		}

		public class MegaSplatData
		{
			//public Material template;
			public Color[] control;
			public Color[] param;
		}

		public static void Process(CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop = null)
		{
			#if __MEGASPLAT__
			if (stop!=null && stop(0)) return;

			//using the first texture list for all
			MegaSplatTextureList textureList = null;
			bool smoothFallof = false;
         float clusterScale = 0.05f;
			foreach (MegaSplatOutput gen in gens.GeneratorsOfType<MegaSplatOutput>(onlyEnabled: true, checkBiomes: true))
			{
				if (gen.textureList != null) textureList = gen.textureList;
				smoothFallof = gen.smoothFallof;
            clusterScale = gen.clusterNoiseScale;
			}

			//creating color arrays
			MegaSplatData result = new MegaSplatData();

			result.control = new Color[MapMagic.instance.resolution * MapMagic.instance.resolution];
			result.param = new Color[MapMagic.instance.resolution * MapMagic.instance.resolution];
			
			//creating all and special layers/biomes lists
			List<Layer> allLayers = new List<Layer>(); //all layers count = gen num * layers num in each gen (excluding empty biomes, matrices, etc)
			List<Matrix> allMatrices = new List<Matrix>();
			List<Matrix> allBiomeMasks = new List<Matrix>();

			List<Matrix> specialWetnessMatrices = new List<Matrix>(); //special count = number of generators (excluding empty biomes only)
			List<Matrix> specialPuddlesMatrices = new List<Matrix>();
			List<Matrix> specialDampeningMatrices = new List<Matrix>();
			List<Matrix> specialBiomeMasks = new List<Matrix>();

			//filling all layers/biomes
			foreach (MegaSplatOutput gen in gens.GeneratorsOfType<MegaSplatOutput>(onlyEnabled: true, checkBiomes: true))
			{
				gen.textureList = textureList;

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
					if (matrix.IsEmpty()) continue;

					if (i >= textureList.clusters.Length)
					{
						Debug.LogError("Cluster out of range");
						continue;
					}

					//adding to lists
					allLayers.Add(gen.baseLayers[i]);
					allMatrices.Add(matrix);
					allBiomeMasks.Add(gen.biome == null ? null : biomeMask);
				}

				//adding special
				object wetnessObj = gen.wetnessIn.GetObject(results);
				specialWetnessMatrices.Add( wetnessObj!=null? (Matrix)wetnessObj : null );

				object puddlesObj = gen.puddlesIn.GetObject(results);
				specialPuddlesMatrices.Add( puddlesObj!=null? (Matrix)puddlesObj : null );

				object dampeingObj = gen.displaceDampenIn.GetObject(results);
				specialDampeningMatrices.Add( dampeingObj!=null? (Matrix)dampeingObj : null );

				specialBiomeMasks.Add(gen.biome == null ? null : biomeMask);
			}

			//if no texture list found in any of generators - returning
			if (textureList == null || allLayers.Count==0) return;

			//processing
			int allLayersCount = allLayers.Count;
			int specialCount = specialWetnessMatrices.Count;
			for (int x = 0; x<rect.size.x; x++)
				for (int z = 0; z<rect.size.z; z++)
				{
					int pos = rect.GetPos(x + rect.offset.x, z + rect.offset.z);

					// doesn't use height, normal, but I'm not sure how to get that here..
					Vector3 worldPos = new Vector3(
						1f * (x+rect.offset.x) / MapMagic.instance.resolution * rect.size.x,
						0,
						1f * (z+rect.offset.z) / MapMagic.instance.resolution * rect.size.z);
					float heightRatio = results.heights!=null? results.heights.array[pos] : 0.5f; //0 is the bottom point, 1 is the maximum top
					Vector3 normal = new Vector3(0,1,0);

					// find highest two layers
					int botIdx = 0;
					int topIdx = 0;
					float botWeight = 0;
					float topWeight = 0;

					for (int i = 0; i<allLayersCount; i++)
					{
						float val = allMatrices[i].array[pos];
						if (allBiomeMasks[i] != null) val *= allBiomeMasks[i].array[pos];

						// really want world position, Normal, and height ratio for brushes, but for now, just use x/z..

						if (val > botWeight)
						{
							topWeight = botWeight;
							topIdx = botIdx;

							botWeight = val;
							botIdx = i;
						}
						else if (val > topWeight)
						{
							topIdx = i;
							topWeight = val;
						}
					}

					//converting layer index to texture index
               topIdx = textureList.clusters[ allLayers[topIdx].index ].GetIndex(worldPos *  clusterScale, normal, heightRatio);
               botIdx = textureList.clusters[ allLayers[botIdx].index ].GetIndex(worldPos * clusterScale, normal, heightRatio);

					//swapping indexes to make topIdx always on top
					if (botIdx > topIdx) 
					{
						int tempIdx = topIdx;
						topIdx = botIdx;
						botIdx = tempIdx;

						float tempWeight = topWeight;
						topWeight = botWeight;
						botWeight = tempWeight;
					}

					//finding blend
					float totalWeight = topWeight + botWeight;	if (totalWeight<0.01f) totalWeight = 0.01f; //Mathf.Max and Clamp are slow
					float blend = botWeight / totalWeight;		if (blend>1) blend = 1;

					//adjusting blend curve
					if (smoothFallof) blend = (Mathf.Sqrt(blend) * (1-blend)) + blend*blend*blend;  //Magic secret formula! Inverse to 3*x^2 - 2*x^3

					//setting color
					result.control[pos] = new Color(botIdx / 255.0f, topIdx / 255.0f, 1.0f - blend, 1.0f);

					//params
					for (int i = 0; i<specialCount; i++)
					{
						float biomeVal = specialBiomeMasks[i]!=null? specialBiomeMasks[i].array[pos] : 1;

						if (specialWetnessMatrices[i]!=null) result.param[pos].b = specialWetnessMatrices[i].array[pos] * biomeVal;
						if (specialPuddlesMatrices[i]!=null) 
						{
							result.param[pos].a = specialPuddlesMatrices[i].array[pos] * biomeVal;
							result.param[pos].r = 0.5f;
							result.param[pos].g = 0.5f;
						}
						if (specialDampeningMatrices[i]!=null) result.control[pos].a = specialDampeningMatrices[i].array[pos] * biomeVal;
					}
						
				}
			
			//pushing to apply
			if (stop!=null && stop(0))
				return;
			results.apply.CheckAdd(typeof(MegaSplatOutput), result, replace: true);
			#endif
		}

		public static IEnumerator Apply(CoordRect rect, Terrain terrain, object dataBox, Func<float,bool> stop= null)
		{
			#if __MEGASPLAT__
			//loading objects
			MegaSplatData tuple = (MegaSplatData)dataBox;
			if (tuple == null)
				yield break;

			//terrain.materialType = Terrain.MaterialType.Custom; //it's already done with MapMagic
			//terrain.materialTemplate = new Material(tuple.template);

			// TODO: We should pool these textures instead of creating and destroying them!

			int res = MapMagic.instance.resolution;


			//control texture
			var control = new Texture2D(res, res, MegaSplatOutput.formatARGB? TextureFormat.ARGB32 : TextureFormat.RGBA32, false, true);
			control.wrapMode = TextureWrapMode.Clamp;
			control.filterMode = FilterMode.Point;
 
			control.SetPixels(0, 0, control.width, control.height, tuple.control);
			control.Apply(updateMipmaps:true, makeNoLongerReadable:!enableReadWrite);
			yield return null;
			

			//param texture
			var paramTex = new Texture2D(res, res, MegaSplatOutput.formatARGB? TextureFormat.ARGB32 : TextureFormat.RGBA32, false, true);
			paramTex.wrapMode = TextureWrapMode.Clamp;
			paramTex.filterMode = FilterMode.Point;

			paramTex.SetPixels(0, 0, paramTex.width, paramTex.height, tuple.param);
			control.Apply(updateMipmaps:true, makeNoLongerReadable:!enableReadWrite);
			yield return null;


			//welding
			if (MapMagic.instance != null && MapMagic.instance.splatsWeldMargins!=0)
			{
				Coord coord = Coord.PickCell(rect.offset, MapMagic.instance.resolution);
				//Chunk chunk = MapMagic.instance.chunks[coord.x, coord.z];
				
				Chunk neigPrevX = MapMagic.instance.chunks[coord.x-1, coord.z];
				if (neigPrevX!=null && neigPrevX.worker.ready && neigPrevX.terrain.materialTemplate.HasProperty("_SplatControl")) 
				{
					WeldTerrains.WeldTextureToPrevX(control, (Texture2D)neigPrevX.terrain.materialTemplate.GetTexture("_SplatControl"));
					control.Apply();
				}

				Chunk neigNextX = MapMagic.instance.chunks[coord.x+1, coord.z];
				if (neigNextX!=null && neigNextX.worker.ready && neigNextX.terrain.materialTemplate.HasProperty("_SplatControl")) 
				{
					WeldTerrains.WeldTextureToNextX(control, (Texture2D)neigNextX.terrain.materialTemplate.GetTexture("_SplatControl"));
					control.Apply();
				}
				
				Chunk neigPrevZ = MapMagic.instance.chunks[coord.x, coord.z-1];
				if (neigPrevZ!=null && neigPrevZ.worker.ready && neigPrevZ.terrain.materialTemplate.HasProperty("_SplatControl")) 
				{
					WeldTerrains.WeldTextureToPrevZ(control, (Texture2D)neigPrevZ.terrain.materialTemplate.GetTexture("_SplatControl"));
					control.Apply();
				}

				Chunk neigNextZ = MapMagic.instance.chunks[coord.x, coord.z+1];
				if (neigNextZ!=null && neigNextZ.worker.ready && neigNextZ.terrain.materialTemplate.HasProperty("_SplatControl")) 
				{
					WeldTerrains.WeldTextureToNextZ(control, (Texture2D)neigNextZ.terrain.materialTemplate.GetTexture("_SplatControl"));
					control.Apply();
				}
			}
			yield return null;




			//TODO: weld textures with 1-pixel margin

			//assign textures using material property (not saving for fixed terrains)
			//#if UNITY_5_5_OR_NEWER
			//MaterialPropertyBlock matProp = new MaterialPropertyBlock();
			//matProp.SetTexture("_SplatControl", control);
			//matProp.SetTexture("_SplatParams", paramTex);
			//matProp.SetFloat("_ControlSize", res);
			//terrain.SetSplatMaterialPropertyBlock(matProp);
			//#endif

			//duplicating material
			if (MapMagic.instance.customTerrainMaterial != null)
			{
				terrain.materialTemplate = new Material(MapMagic.instance.customTerrainMaterial);

				//assigning control textures
				if (terrain.materialTemplate.HasProperty("_SplatControl"))
					terrain.materialTemplate.SetTexture("_SplatControl", control);
				if (terrain.materialTemplate.HasProperty("_SplatParams"))
					terrain.materialTemplate.SetTexture("_SplatParams", paramTex);
			}

			#else
			yield return null;
			#endif
		}

		public static void Purge(CoordRect rect, Terrain terrain)
		{
			//switch back to standard material to purge
			//TODO: it's wrong, got to be filled with background layer

			// destroy created textures, etc..
			/*var mat = terrain.materialTemplate;
			if (mat != null)
			{
				Texture controlTex = mat.GetTexture("_SplatControl");
				Texture paramTex = mat.GetTexture("_SplatParams");
				if (controlTex != null)
				{
				   GameObject.Destroy(controlTex);
				}
				if (paramTex != null)
				{
				   GameObject.Destroy(paramTex);
				}
				GameObject.Destroy(mat);
			}*/
		}


		#if __MEGASPLAT__
		private string[] clusterNames = new string[0];
		#endif

		public override void OnGUI (GeneratorsAsset gens)
		{
			layout.Label("Replaced by ");
			layout.Label("CustomShader Output");

			#if __MEGASPLAT__
			layout.fieldSize = 0.5f; 

			//finding texture list from other generators
			if (textureList == null) 
				foreach (MegaSplatOutput gen in gens.GeneratorsOfType<MegaSplatOutput>(onlyEnabled: true, checkBiomes: true))
					if (gen.textureList != null) textureList = gen.textureList;

			//wrong material and settings warnings
			if (MapMagic.instance.showBaseMap) 
			{
				layout.Par(30);
            layout.Label("Show Base Map is turned on in Settings.", rect:layout.Inset(0.8f), helpbox:true);
				if (layout.Button("Fix",rect:layout.Inset(0.2f))) MapMagic.instance.showBaseMap = false;
			}

			#if !UNITY_2019_2_OR_NEWER
			if (MapMagic.instance.terrainMaterialType != Terrain.MaterialType.Custom)
			{
				layout.Par(30);
            layout.Label("Material Type is not switched to Custom.", rect:layout.Inset(0.8f), helpbox:true);
				if (layout.Button("Fix",rect:layout.Inset(0.2f))) 
				{
					MapMagic.instance.terrainMaterialType = Terrain.MaterialType.Custom;
					foreach (Chunk tw in MapMagic.instance.chunks.All()) tw.SetSettings();
				}
			}
			#endif

			if (MapMagic.instance.customTerrainMaterial == null || !MapMagic.instance.customTerrainMaterial.shader.name.Contains("MegaSplat"))
			{
				layout.Par(42);
            layout.Label("No MegaSplat material is assigned as Custom Material in Terrain Settings.", rect:layout.Inset(), helpbox:true);
			}

			if (MapMagic.instance.customTerrainMaterial != null)
			{
				if (!MapMagic.instance.customTerrainMaterial.IsKeywordEnabled("_TERRAIN") || !MapMagic.instance.customTerrainMaterial.HasProperty("_SplatControl"))
				{
					layout.Par(42);
               layout.Label("Material must use a MegaSplat shader set to Terrain.", rect:layout.Inset(), helpbox:true);
				}

				if (MapMagic.instance.customTerrainMaterial.GetTexture("_Diffuse") == null)
				{
					layout.Par(42);
					layout.Label("Material does not have texture arrays assigned, please assign them.", rect:layout.Inset(), helpbox:true);
				}
			}

			/*if (!MapMagic.instance.customTerrainMaterialMode)
			{
				layout.Par(30);
				layout.Label("Material Template Mode is off.", rect:layout.Inset(0.8f), helpbox:true);
				if (layout.Button("Fix",rect:layout.Inset(0.2f))) MapMagic.instance.customTerrainMaterialMode = true;
			}*/

			#if !UNITY_2019_2_OR_NEWER
			if (MapMagic.instance.assignCustomTerrainMaterial)
			{
				layout.Par(30);
				layout.Label("Assign Custom Material is turned on.", rect:layout.Inset(0.8f), helpbox:true);
				if (layout.Button("Fix",rect:layout.Inset(0.2f))) 
				{
					MapMagic.instance.assignCustomTerrainMaterial = false;
				}
			}
			#endif

			if (textureList == null || textureList.clusters == null || textureList.clusters.Length <= 0)
			{
				layout.Par(30);
            layout.Label("Please assign textures and list with clusters below:", rect:layout.Inset(), helpbox:true);

				layout.Field<MegaSplatTextureList>(ref textureList, "TextureList");
				foreach(Input input in Inputs()) input.link = null;
				return;
			}

			//drawing texture list field
			layout.Field<MegaSplatTextureList>(ref textureList, "TextureList");
			
			//setting all of the generators list to this one
			if (layout.change)
				foreach (MegaSplatOutput gen in gens.GeneratorsOfType<MegaSplatOutput>(onlyEnabled: true, checkBiomes: true))
					gen.textureList = textureList;

			//noise field
			layout.Par(5);
			layout.Field<float>(ref clusterNoiseScale, "Noise Scale");
			layout.Toggle(ref smoothFallof, "Smooth Fallof");
			layout.Toggle(ref enableReadWrite, "Enable R/W maps");
			layout.Toggle(ref MegaSplatOutput.formatARGB, "ARGB (since MS 1.14)");

			//gathering cluster names
			if (clusterNames.Length != textureList.clusters.Length)
				clusterNames = new string[textureList.clusters.Length];
			for (int i=0; i<clusterNames.Length; i++)
				clusterNames[i] = textureList.clusters[i].name;

			//drawing layers
			layout.Par(5);
			layout.Label("Layers:"); //needed to reset label bold style
			layout.margin = 20;
			layout.rightMargin = 20; 

			for (int i=baseLayers.Length-1; i>=0; i--)
			{
				if (baseLayers[i] == null)
					baseLayers[i] = new Layer();
			
				layout.DrawLayer(OnLayerGUI, ref selected, i);
				//if (layout.DrawWithBackground(OnLayerGUI, active:i==selected, num:i, frameDisabled:false)) selected = i;
			}

			layout.Par(3); layout.Par();
			layout.DrawArrayAdd(ref baseLayers, ref selected, layout.Inset(0.25f), createElement:() => new Layer());
			layout.DrawArrayRemove(ref baseLayers, ref selected, layout.Inset(0.25f));
			layout.DrawArrayUp(ref baseLayers, ref selected, layout.Inset(0.25f), dispDown:true);
			layout.DrawArrayDown(ref baseLayers, ref selected, layout.Inset(0.25f), dispUp:true);

			 //drawing effect layers
			layout.Par(5);
		 
			layout.Par(20); 
			wetnessIn.DrawIcon(layout);
			layout.Label("Wetness", layout.Inset());

			layout.Par(20); 
			puddlesIn.DrawIcon(layout);
			layout.Label("Puddles", layout.Inset());

			layout.Par(20); 
			displaceDampenIn.DrawIcon(layout);
			layout.Label("Displace Dampen", layout.Inset());

			#else

			layout.margin = 5;
			layout.rightMargin = 5;

			layout.Par(65);
			layout.Label("MegaSplat is not installed. Please install it from the Asset Store, it's really amazing, you'll like it..\n\t   Jason Booth", rect:layout.Inset(), helpbox:true);

			//What about adding a link to MegaSplat asset store page? Denis

			layout.Par(30);
			layout.Label("Restart Unity if you have just installed it.", rect:layout.Inset(), helpbox:true);

			#endif
		}

		public void OnLayerGUI (Layout layout, bool selected, int num)
		{
			#if __MEGASPLAT__
			if (baseLayers[num].index >= textureList.clusters.Length) baseLayers[num].index = textureList.clusters.Length-1;

			//disconnecting background
			if (num == 0) baseLayers[num].input.link = null;

			layout.Par(); 

			if (num != 0) baseLayers[num].input.DrawIcon(layout);

			if (selected)
				baseLayers[num].index = layout.Popup(baseLayers[num].index, clusterNames,rect:layout.Inset());
			else 
				layout.Label(textureList.clusters[baseLayers[num].index].name + (num==0? " (Background)" : ""), rect:layout.Inset());

			baseLayers[num].output.DrawIcon(layout);
			#endif
		}
	}


	[GeneratorMenu (menu="Legacy", name ="RTP Output (1.83 Legacy)", disengageable = true, priority = 10, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/RTP",
		updateType = typeof(CustomShaderOutput))]
	public class RTPOutput  : OutputGenerator 
	{
		#if RTP
		[System.NonSerialized] public static ReliefTerrain rtp;
		#endif
		[System.NonSerialized] public static MeshRenderer renderer; //for gui purpose only

		//layer
		public class Layer
		{
			public Input input = new Input(InoutType.Map);
			public Output output = new Output(InoutType.Map);
			public int index = 0;
			public string name = "Layer";
			public float opacity = 1;

			public void OnAdd (int n) { }
			public void OnRemove (int n) 
			{ 
				input.Link(null,null); 
				Input connectedInput = output.GetConnectedInput(MapMagic.instance.gens.list);
				if (connectedInput != null) connectedInput.Link(null, null);
			}
			public void OnSwitch (int o, int n) { }
		}
		public Layer[] baseLayers = new Layer[0];
		public int selected = 0;

		public static Texture2D _defaultTex;
		public static Texture2D defaultTex {get{ if (_defaultTex==null) _defaultTex=Extensions.ColorTexture(2,2,new Color(0.75f, 0.75f, 0.75f, 0f)); return _defaultTex; }}

		public class RTPTuple { public int layer; public Color[] colorsA; public Color[] colorsB; public float opacity; }

		//get static actions using instance
		public override Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>> GetProces() { return Process; }
		public override Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator> GetApply() { return Apply; }
		public override Action<CoordRect, Terrain> GetPurge() { return Purge; }


		//generator
		public override IEnumerable<Input> Inputs() 
		{ 
			if (baseLayers==null) baseLayers = new Layer[0];
			for (int i=1; i<baseLayers.Length; i++)  //layer 0 is background
				if (baseLayers[i] != null && baseLayers[i].input != null)
					yield return baseLayers[i].input; 
		}
		public override IEnumerable<Output> Outputs() 
		{ 
			if (baseLayers==null) baseLayers = new Layer[0];
			for (int i=0; i<baseLayers.Length; i++) 
				if (baseLayers[i] != null && baseLayers[i].output != null)
					yield return baseLayers[i].output; 
		}

		public override void Generate(CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop= null)
		{
			#if RTP
			if ((stop!=null && stop(0)) || !enabled) return;

			//loading inputs
			Matrix[] matrices = new Matrix[baseLayers.Length];
			for (int i = 0; i < baseLayers.Length; i++)
			{
				if (baseLayers[i].input != null)
				{
				   matrices[i] = (Matrix)baseLayers[i].input.GetObject(results);
				   if (matrices[i] != null)
					  matrices[i] = matrices[i].Copy(null);
				}
				if (matrices[i] == null)
				   matrices[i] = new Matrix(rect);
			}
			if (matrices.Length == 0)
				return;

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
				if (stop!=null && stop(0))
				   return; //do not write object is generating is stopped
				baseLayers[i].output.SetObject(results, matrices[i]);
			}
			#endif
		}

		public static void Process (CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop = null)
		{
			#if RTP
			if (stop!=null && stop(0)) return;

			//finding number of layers
			int layersCount = 0;
			foreach (RTPOutput gen in MapMagic.instance.gens.GeneratorsOfType<RTPOutput>(onlyEnabled:true, checkBiomes:true))
				{ layersCount = gen.baseLayers.Length; break; }
			
			//creating color arrays
			RTPTuple result = new RTPTuple();
			result.colorsA = new Color[MapMagic.instance.resolution * MapMagic.instance.resolution];
			if (layersCount > 4) result.colorsB = new Color[MapMagic.instance.resolution * MapMagic.instance.resolution];
			
			//filling color arrays
			foreach (RTPOutput gen in MapMagic.instance.gens.GeneratorsOfType<RTPOutput>(onlyEnabled:true, checkBiomes:true))
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

				for (int i=0; i<gen.baseLayers.Length; i++)
				{
					//reading output directly
					Output output = gen.baseLayers[i].output;
					if (stop!=null && stop(0)) return; //checking stop before reading output
					if (!results.results.ContainsKey(output)) continue;
					Matrix matrix = (Matrix)results.results[output];
					if (matrix.IsEmpty()) continue;

					for (int x=0; x<rect.size.x; x++)
						for (int z=0; z<rect.size.z; z++)
					{
						int pos = matrix.rect.GetPos(x+matrix.rect.offset.x, z+matrix.rect.offset.z); //pos should be the same for colors array and matrix array
						
						//get value and adjust with biome mask
						float val = matrix.array[pos];
						float biomeVal = biomeMask!=null? biomeMask.array[pos] : 1;
						val *= biomeVal;

						//save value to colors array
						switch (gen.baseLayers[i].index)
						{
							case 0: result.colorsA[pos].r += val; break;
							case 1: result.colorsA[pos].g += val; break;
							case 2: result.colorsA[pos].b += val; break;
							case 3: result.colorsA[pos].a += val; break;
							case 4: result.colorsB[pos].r += val; break;
							case 5: result.colorsB[pos].g += val; break;
							case 6: result.colorsB[pos].b += val; break;
							case 7: result.colorsB[pos].a += val; break;
						}
					}

					if (stop!=null && stop(0)) return;
				}
			}

			//TODO: normalizing color arrays (if needed)

			//pushing to apply
			if (stop!=null && stop(0)) return;
			results.apply.CheckAdd(typeof(RTPOutput), result, replace: true);
			#endif
		}

		public static IEnumerator Apply(CoordRect rect, Terrain terrain, object dataBox, Func<float,bool> stop= null)
		{
			#if RTP

			//guard if old-style rtp approach is used
			ReliefTerrain chunkRTP = terrain.gameObject.GetComponent<ReliefTerrain>();
			if (chunkRTP !=null && chunkRTP.enabled) 
			{
				Debug.Log("MapMagic: RTP component on terain chunk detected. RTP Output Generator works with one RTP script assigned to main MM object only. Make sure that Copy Components is turned off.");
				chunkRTP.enabled = false;
			}
			yield return null;

			//loading objects
			RTPTuple tuple = (RTPTuple)dataBox;
			if (tuple == null) yield break;

			//creating control textures
			Texture2D controlA = new Texture2D(MapMagic.instance.resolution, MapMagic.instance.resolution);
			controlA.wrapMode = TextureWrapMode.Clamp;
			controlA.SetPixels(0,0,controlA.width,controlA.height,tuple.colorsA);
			controlA.Apply();
			yield return null;

			Texture2D controlB = null;
			if (tuple.colorsB != null) 
			{
				controlB = new Texture2D(MapMagic.instance.resolution, MapMagic.instance.resolution);
				controlB.wrapMode = TextureWrapMode.Clamp;
				controlB.SetPixels(0,0,controlB.width,controlB.height,tuple.colorsB);
				controlB.Apply();
				yield return null;
			}

			//welding
			if (MapMagic.instance != null && MapMagic.instance.splatsWeldMargins!=0)
			{
				Coord coord = Coord.PickCell(rect.offset, MapMagic.instance.resolution);
				//Chunk chunk = MapMagic.instance.chunks[coord.x, coord.z];
				
				Chunk neigPrevX = MapMagic.instance.chunks[coord.x-1, coord.z];
				if (neigPrevX!=null && neigPrevX.worker.ready && neigPrevX.terrain.materialTemplate.HasProperty("_Control1")) 
				{
					WeldTerrains.WeldTextureToPrevX(controlA, (Texture2D)neigPrevX.terrain.materialTemplate.GetTexture("_Control1"));
					if (controlB != null && neigPrevX.terrain.materialTemplate.HasProperty("_Control2"))
						WeldTerrains.WeldTextureToPrevX(controlB, (Texture2D)neigPrevX.terrain.materialTemplate.GetTexture("_Control2"));
				}

				Chunk neigNextX = MapMagic.instance.chunks[coord.x+1, coord.z];
				if (neigNextX!=null && neigNextX.worker.ready && neigNextX.terrain.materialTemplate.HasProperty("_Control1")) 
				{
					WeldTerrains.WeldTextureToNextX(controlA, (Texture2D)neigNextX.terrain.materialTemplate.GetTexture("_Control1"));
					if (controlB != null && neigNextX.terrain.materialTemplate.HasProperty("_Control2"))
						WeldTerrains.WeldTextureToNextX(controlB, (Texture2D)neigNextX.terrain.materialTemplate.GetTexture("_Control2"));
				}
				
				Chunk neigPrevZ = MapMagic.instance.chunks[coord.x, coord.z-1];
				if (neigPrevZ!=null && neigPrevZ.worker.ready && neigPrevZ.terrain.materialTemplate.HasProperty("_Control1")) 
				{
					WeldTerrains.WeldTextureToPrevZ(controlA, (Texture2D)neigPrevZ.terrain.materialTemplate.GetTexture("_Control1"));
					if (controlB != null && neigPrevZ.terrain.materialTemplate.HasProperty("_Control2"))
						WeldTerrains.WeldTextureToPrevZ(controlB, (Texture2D)neigPrevZ.terrain.materialTemplate.GetTexture("_Control2"));
				}

				Chunk neigNextZ = MapMagic.instance.chunks[coord.x, coord.z+1];
				if (neigNextZ!=null && neigNextZ.worker.ready && neigNextZ.terrain.materialTemplate.HasProperty("_Control1")) 
				{
					WeldTerrains.WeldTextureToNextZ(controlA, (Texture2D)neigNextZ.terrain.materialTemplate.GetTexture("_Control1"));
					if (controlB != null && neigNextZ.terrain.materialTemplate.HasProperty("_Control2"))
						WeldTerrains.WeldTextureToNextZ(controlB, (Texture2D)neigNextZ.terrain.materialTemplate.GetTexture("_Control2"));
				}
			}
			yield return null;

			//assigning material propery block (not saving for fixed terrains)
			//#if UNITY_5_5_OR_NEWER
			//assign textures using material property
			//MaterialPropertyBlock matProp = new MaterialPropertyBlock();
			//matProp.SetTexture("_Control1", controlA);
			//if (controlB!=null) matProp.SetTexture("_Control2", controlB);
			//#endif	
			
			//duplicating material and assign it's values
			//if (MapMagic.instance.customTerrainMaterial != null)
			//{
			//	//duplicating material
			//	terrain.materialTemplate = new Material(MapMagic.instance.customTerrainMaterial);
			//
			//	//assigning control textures
			//	if (terrain.materialTemplate.HasProperty("_Control1"))
			//		terrain.materialTemplate.SetTexture("_Control1", controlA);
			//	if (controlB != null && terrain.materialTemplate.HasProperty("_Control2"))
			//		terrain.materialTemplate.SetTexture("_Control2", controlB);
			//}

			if (rtp == null) rtp = MapMagic.instance.gameObject.GetComponent<ReliefTerrain>();
			if (rtp==null || rtp.globalSettingsHolder==null) yield break;
			
			//getting rtp material
			Material mat = null;
			if (terrain.materialTemplate!=null && terrain.materialTemplate.shader.name=="Relief Pack/ReliefTerrain-FirstPas")  //if relief terrain material assigned to terrain
				mat = terrain.materialTemplate;
			//if (mat==null && chunk.previewBackupMaterial!=null && chunk.previewBackupMaterial.shader.name=="Relief Pack/ReliefTerrain-FirstPas") //if it is backed up for preview
			//	mat = chunk.previewBackupMaterial;
			if (mat == null) //if still could not find material - creating new
			{
				Shader shader = Shader.Find("Relief Pack/ReliefTerrain-FirstPass");
				mat = new Material(shader);

				if (Preview.previewOutput == null) terrain.materialTemplate = mat;
				//else chunk.previewBackupMaterial = mat;
			}
			terrain.materialType = Terrain.MaterialType.Custom;

			//setting
			rtp.RefreshTextures(mat);
			rtp.globalSettingsHolder.Refresh(mat, rtp);
			mat.SetTexture("_Control1", controlA);
			if (controlB!=null) { mat.SetTexture("_Control2", controlB); mat.SetTexture("_Control3", controlB); }

			#else
			yield return null;
			#endif
		}

		public static void Purge(CoordRect rect, Terrain terrain)
		{
			//purged on switching back to the standard shader
			//TODO: it's wrong, got to be filled with background layer
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			layout.Label("Replaced by ");
			layout.Label("CustomShader Output");


			#if RTP
			if (rtp==null) rtp = MapMagic.instance.GetComponent<ReliefTerrain>();
			if (renderer==null) renderer = MapMagic.instance.GetComponent<MeshRenderer>();

			//wrong material and settings warnings
			if (MapMagic.instance.copyComponents)
			{
				layout.Par(42);
				layout.Label("Copy Component should be turned off to prevent copying RTP to chunks.", rect:layout.Inset(0.8f), helpbox:true);
				if (layout.Button("Fix",rect:layout.Inset(0.2f))) MapMagic.instance.copyComponents = false;
			}

			if (rtp==null) 
			{
				layout.Par(42);
				layout.Label("Could not find Relief Terrain component on MapMagic object.", rect:layout.Inset(0.8f), helpbox:true);
				if (layout.Button("Fix",rect:layout.Inset(0.2f))) 
				{
					renderer = MapMagic.instance.gameObject.GetComponent<MeshRenderer>();
					if (renderer==null) renderer = MapMagic.instance.gameObject.AddComponent<MeshRenderer>();
					renderer.enabled = false;
					rtp = MapMagic.instance.gameObject.AddComponent<ReliefTerrain>();

					//if (MapMagic.instance.gameObject.GetComponent<InstantUpdater>()==null) MapMagic.instance.gameObject.AddComponent<InstantUpdater>();

					//filling empty splats
					Texture2D emptyTex = Extensions.ColorTexture(4,4,new Color(0.5f, 0.5f, 0.5f, 1f));
					emptyTex.name = "Empty";
					rtp.globalSettingsHolder.splats = new Texture2D[] { emptyTex,emptyTex,emptyTex,emptyTex };
				}
			}

			if (MapMagic.instance.terrainMaterialType != Terrain.MaterialType.Custom)
			{
				layout.Par(30);
				layout.Label("Material Type is not switched to Custom.", rect:layout.Inset(0.8f), helpbox:true);
				if (layout.Button("Fix",rect:layout.Inset(0.2f))) 
				{
					MapMagic.instance.terrainMaterialType = Terrain.MaterialType.Custom;
					foreach (Chunk tw in MapMagic.instance.chunks.All()) tw.SetSettings();
				}
			}

			if (MapMagic.instance.assignCustomTerrainMaterial)
			{
				layout.Par(30);
				layout.Label("Assign Custom Material is turned on.", rect:layout.Inset(0.8f), helpbox:true);
				if (layout.Button("Fix",rect:layout.Inset(0.2f))) 
				{
					MapMagic.instance.assignCustomTerrainMaterial = false;
				}
			}

			if (MapMagic.instance.GetComponent<InstantUpdater>() == null)
			{
				layout.Par(52);
				layout.Label("Use Instant Updater component to apply RTP changes to all the terrains.", rect:layout.Inset(0.8f), helpbox:true);
				if (layout.Button("Fix",rect:layout.Inset(0.2f))) 
				{
					MapMagic.instance.gameObject.AddComponent<InstantUpdater>();
				}
			}

			/*if (!MapMagic.instance.customTerrainMaterialMode)
			{
				layout.Par(30);
				layout.Label("Material Template Mode is off.", rect:layout.Inset(0.8f), helpbox:true);
				if (layout.Button("Fix",rect:layout.Inset(0.2f))) MapMagic.instance.customTerrainMaterialMode = true;
			}*/

			/*if ((renderer != null) &&
				(renderer.sharedMaterial == null || !renderer.sharedMaterial.shader.name.Contains("ReliefTerrain")))
			{
				layout.Par(50);
				layout.Label("No Relief Terrain material is assigned as Custom Material in Terrain Settings.", rect:layout.Inset(0.8f), helpbox:true);
				if (layout.Button("Fix",rect:layout.Inset(0.2f)))
				{
					//if (renderer.sharedMaterial == null)
					//{
						Shader shader = Shader.Find("Relief Pack/ReliefTerrain-FirstPass");
						if (shader != null) renderer.sharedMaterial = new Material(shader);
						else Debug.Log ("MapMagic: Could not find Relief Pack/ReliefTerrain-FirstPass shader. Make sure RTP is installed or switch material type to Standard.");
					//}
					MapMagic.instance.customTerrainMaterial = renderer.sharedMaterial;
					foreach (Chunk tw in MapMagic.instance.chunks.All()) tw.SetSettings();
				}
			}*/

			if (rtp == null) return;

			bool doubleLayer = false;
			for (int i=0; i<baseLayers.Length; i++)
				for (int j=0; j<baseLayers.Length; j++)
			{
				if (i==j) continue;
				if (baseLayers[i].index == baseLayers[j].index) doubleLayer = true;
			}
			if (doubleLayer)
			{
				layout.Par(30);
				layout.Label("Seems that multiple layers use the same splat index.", rect:layout.Inset(0.8f), helpbox:true);
				if (layout.Button("Fix",rect:layout.Inset(0.2f))) ResetLayers(baseLayers.Length);
			}


			//refreshing layers from rtp
			Texture2D[] splats = rtp.globalSettingsHolder.splats;
			if (baseLayers.Length != splats.Length) ResetLayers(splats.Length);

			//drawing layers
			layout.margin = 20; layout.rightMargin = 20; layout.fieldSize = 1f;
			for (int i=baseLayers.Length-1; i>=0; i--)
			{
				//if (baseLayers[i] == null)
				//baseLayers[i] = new Layer();
			
				//if (layout.DrawWithBackground(OnLayerGUI, active:i==selected, num:i, frameDisabled:false)) selected = i;
				layout.DrawLayer(OnLayerGUI, ref selected, i);
			}

			layout.Par(3); layout.Par();
			//layout.DrawArrayAdd(ref baseLayers, ref selected, layout.Inset(0.25f));
			//layout.DrawArrayRemove(ref baseLayers, ref selected, layout.Inset(0.25f));
			layout.DrawArrayUp(ref baseLayers, ref selected, layout.Inset(0.25f), dispDown:true);
			layout.DrawArrayDown(ref baseLayers, ref selected, layout.Inset(0.25f), dispUp:true);

			layout.margin = 3; layout.rightMargin = 3;   
			layout.Par(64); layout.Label("Use Relief Terrain component to set layer properties. \"Refresh All\" in RTP settings might be required.", rect:layout.Inset(), helpbox: true);


			#else
			layout.margin = 5;
			layout.rightMargin = 5;

			layout.Par(45);
			layout.Label("Cannot find Relief Terrain plugin. Restart Unity if you have just installed it.", rect:layout.Inset(), helpbox:true);
			#endif
		}

		public void OnLayerGUI (Layout layout, bool selected, int num)
		{
			#if RTP
				Layer layer = baseLayers[num];

				layout.Par(40); 

				if (num != 0) layer.input.DrawIcon(layout);
				else 
					if (layer.input.link != null) { layer.input.Link(null,null); } 
				
				//layout.Par(40); //not 65
				//layout.Field(ref rtp.globalSettingsHolder.splats[layer.index], rect:layout.Inset(40));
				layout.Icon(rtp.globalSettingsHolder.splats[layer.index], rect:layout.Inset(40), frame:true, alphaBlend:false);
				layout.Label(rtp.globalSettingsHolder.splats[layer.index].name + (num==0? "\n(Background)" : ""), rect:layout.Inset(layout.field.width-60));

				baseLayers[num].output.DrawIcon(layout);
			#endif
		}

		public void ResetLayers (int newcount)
		{
			for (int i=0; i<baseLayers.Length; i++) 
			{
				baseLayers[i].input.Link(null,null); 

				Input connectedInput = baseLayers[i].output.GetConnectedInput(MapMagic.instance.gens.list);
				if (connectedInput != null) connectedInput.Link(null, null);
			}

			baseLayers = new Layer[newcount];
				
			for (int i=0; i<baseLayers.Length; i++) 
			{
				baseLayers[i] = new Layer();
				baseLayers[i].index = i;
			}
		}
	}

}