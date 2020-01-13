using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#if VEGETATION_STUDIO_PRO
using AwesomeTechnologies.VegetationSystem;
using AwesomeTechnologies.Vegetation.Masks;
#endif

namespace MapMagic.VegetationStudio
{
	[System.Serializable]
	[GeneratorMenu(menu = "Output", name = "VS Pro Maps", disengageable = true, priority = -1, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Textures")]
	public class VSProMapsOutput : OutputGenerator, ISerializationCallbackReceiver
	{
		public static string[] channelNames = new string[] { "Red", "Green", "Blue", "Alpha" }; //should be readonly

		#if VEGETATION_STUDIO_PRO
		[NonSerialized] //can't serialize the type from the other assembly 
		public VegetationPackagePro package;

		public ScriptableObject serializedPackage;
		public void OnBeforeSerialize () { serializedPackage = package; } 
		public void OnAfterDeserialize () { package = (VegetationPackagePro)serializedPackage;  }
		#else
		public void OnBeforeSerialize () { } 
		public void OnAfterDeserialize () { }
		#endif

		public bool obscureLayers = false;

		//layer
		public class Layer
		{
			public Input input = new Input(InoutType.Map);
			public Output output = new Output(InoutType.Map);
			public string name = "Layer";

			public float density = 0.5f; //number of meshes per map pixel. Should not be confused with opacity, it's not used for layer blending.
			
			public int maskGroup = 0;
			public int textureChannel = 0;

			public void OnGUI (Layout layout, bool selected, int num, object parent) 
			{
				#if VEGETATION_STUDIO_PRO
				VSProMapsOutput vsOut = (VSProMapsOutput)parent;

				layout.margin = 20; layout.rightMargin = 20;
				layout.Par(20);

				input.DrawIcon(layout);

				if (selected) layout.Field(ref name, rect: layout.Inset());
				else layout.Label(name, rect: layout.Inset());

				output.DrawIcon(layout);

				if (selected)
				{
					
					//populating masks list to choose from
					int masksCount = vsOut.package.TextureMaskGroupList.Count;
					string[] maskNames = new string[masksCount];
					for (int i=0; i<masksCount; i++)
					{
						maskNames[i] = (i + 1).ToString() + ". " + 
							vsOut.package.TextureMaskGroupList[i].TextureMaskName + " - " +
							vsOut.package.TextureMaskGroupList[i].TextureMaskType;
					}

					layout.Par(2);
					maskGroup = layout.Popup(maskGroup, maskNames, "Group");
					textureChannel = layout.Popup(textureChannel, channelNames, "Channel");
				}
				#endif
			}
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

			//saving outputs
			for (int i = 0; i < baseLayers.Length; i++)
			{
				if (stop!=null && stop(0)) return; //do not write object is generating is stopped
				if (baseLayers[i].output == null) baseLayers[i].output = new Output(InoutType.Map); //back compatibility
				baseLayers[i].output.SetObject(results, matrices[i]);
			}
		}


		public static void Process (CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop = null)
		{
			#if VEGETATION_STUDIO_PRO
			if (stop!=null && stop(0)) return;

			//gathering prototypes and matrices lists
			List<Layer> prototypesList = new List<Layer>();
			List<Matrix> matrices = new List<Matrix>();
			List<Matrix> biomeMasks = new List<Matrix>();
			VegetationPackagePro package = null;

			foreach (VSProMapsOutput gen in gens.GeneratorsOfType<VSProMapsOutput>(onlyEnabled: true, checkBiomes: true))
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
					prototypesList.Add(gen.baseLayers[i]);
					package = gen.package;
				}
			}

			//optimizing matrices list if they are not used
//			for (int i = matrices.Count - 1; i >= 0; i--)
//				if (opacities[i] < 0.001f || matrices[i].IsEmpty() || (biomeMasks[i] != null && biomeMasks[i].IsEmpty()))
//				{ prototypesList.RemoveAt(i); opacities.RemoveAt(i); matrices.RemoveAt(i); biomeMasks.RemoveAt(i); }

			int numLayers = matrices.Count;
			if (numLayers == 0) { results.apply.CheckAdd(typeof(VSProMapsOutput), new TupleSet<Color[][], int[], VegetationPackagePro>(new Color[0][], new int[0], package), replace: true); return; }

			int maxX = MapMagic.instance.resolution; int maxZ = MapMagic.instance.resolution; 

			Dictionary<int,Color[]> grNumToColors = new Dictionary<int, Color[]>();

			for (int i = 0; i<numLayers; i++)
			{
				int grNum = prototypesList[i].maskGroup;

				if (!grNumToColors.ContainsKey(grNum))
					grNumToColors.Add(grNum, new Color[maxX*maxZ]);
			}

			//filling colors
			if (stop!=null && stop(0)) return;

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

					//normalizing
					for (int i = 0; i < numLayers; i++) values[i] = values[i] / sum;

					//setting color
					for (int i = 0; i < numLayers; i++)
					{
						Layer layer = prototypesList[i];

						Color[] texColors = grNumToColors[layer.maskGroup];
						
						int texturePos = z*maxX + x;
						float val = values[i];
						switch (layer.textureChannel)
						{
							case 0: texColors[texturePos].r = val; break;
							case 1: texColors[texturePos].g = val; break;
							case 2: texColors[texturePos].b = val; break;
							case 3: texColors[texturePos].a = val; break;
						}
					}
				}

			//creating arrays
			Color[][] colors = new Color[grNumToColors.Count][];
			int[] maskGroupNums = new int[grNumToColors.Count];

			int counter = 0;
			foreach (var kvp in grNumToColors)
			{
				maskGroupNums[counter] = kvp.Key;
				colors[counter] = kvp.Value;
				counter ++;
			}

			//pushing to apply
			if (stop!=null && stop(0)) return;
			TupleSet<Color[][], int[], VegetationPackagePro> mapsTuple = new TupleSet<Color[][], int[], VegetationPackagePro>(colors, maskGroupNums, package);
			results.apply.CheckAdd(typeof(VSProMapsOutput), mapsTuple, replace: true);
			#endif
		}


		public static IEnumerator Apply(CoordRect rect, Terrain terrain, object dataBox, Func<float,bool> stop= null)
		{
			#if VEGETATION_STUDIO_PRO
			TupleSet<Color[][], int[], VegetationPackagePro> splatsTuple = (TupleSet<Color[][], int[], VegetationPackagePro>)dataBox;
			Color[][] colors = splatsTuple.item1;
			int[] maskGroupNums = splatsTuple.item2;
			VegetationPackagePro package = splatsTuple.item3;

			if (colors.Length == 0) { Purge(rect,terrain); yield break; }

			VegetationStudioTile vsTile = terrain.GetComponent<VegetationStudioTile>();
			if (vsTile == null) vsTile = terrain.gameObject.AddComponent<VegetationStudioTile>();

			Texture2D[] textures = WriteTextures(vsTile.lastUsedTextures, colors);
			
			//Rect terrainRect = new Rect(terrain.transform.position.x, terrain.transform.position.z, terrain.terrainData.size.x, terrain.terrainData.size.z);
			//SetTextures(terrainRect, textures, maskGroupNums, package);

			vsTile.lastUsedTextures = textures;
			vsTile.lastUsedMaskGroupNums = maskGroupNums;
			vsTile.lastUsedPackage = package;
			vsTile.masksApplied = false;

			yield return null;
			#else
			yield return null;
			#endif
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

		public static Texture2D[] WriteTextures (Texture2D[] oldTextures, Color[][] colors)
		{
			int numTextures = colors.Length;
			if (numTextures==0) return new Texture2D[0];
			int resolution = (int)Mathf.Sqrt(colors[0].Length);

			Texture2D[] textures = new Texture2D[numTextures];

			//making textures of colors in coroutine
			for (int i=0; i<numTextures; i++)
			{
				//trying to reuse last used texture
				Texture2D tex;
				if (oldTextures != null  &&
					i < oldTextures.Length  &&
					oldTextures[i] != null && 
					oldTextures[i].width == resolution && 
					oldTextures[i].height == resolution)
						tex = oldTextures[i];
					
				else
				{
					tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true, true);
					#if UNITY_2017_OR_NEWER
					tex.wrapMode = TextureWrapMode.Mirror; //to avoid border seams
					#endif
				}
					
				tex.SetPixels(0,0,tex.width,tex.height,colors[i]);
				tex.Apply();

				textures[i] = tex;
			}

			return textures;
		}

		public override void OnGUI(GeneratorsAsset gens)
		{
			#if VEGETATION_STUDIO_PRO
			layout.Field(ref package, "Package", fieldSize:0.6f);
			#endif
			layout.Field(ref obscureLayers, "Obscure Layers", fieldSize: 0.35f);

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
				layout.DrawLayer(baseLayers[num].OnGUI, ref selected, num, this); 
		}
	}
}

/*
namespace MapMagic.VegetationStudio
{
	[System.Serializable]
	[GeneratorMenu(
		menu = "Map", 
		name = "VS Pro Maps", 
		section =2,
		drawButtons = false,
		colorType = typeof(MatrixWorld), 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Grass")]
	public class VSProMapsOutput : LayersGenerator, IOutputGenerator
	{
		[Val("Package", type = typeof(VegetationPackagePro))] public VegetationPackagePro package;
		[Val("Normalize Layers")]  public bool normalizeLayers = false;

		[System.Serializable]
		[SpecialEditor("VegetationStudioUI", "DrawMapLayer")]
		public class MapLayer : Layer, IOutlet<MatrixWorld>, ISpecial
		{
			public readonly Inlet<MatrixWorld> inlet = new Inlet<MatrixWorld>();
			public override IEnumerable<Inlet> Inlets() { yield return inlet; }

			public string name = "Layer";
			public float density = 0.5f; //number of meshes per map pixel. Should not be confused with opacity, it's not used for layer blending.
			
			public int maskGroup = 0;
			public int textureChannel = 0;
		}

		public int guiExpanded;

		public override Layer CreateLayer () { return new MapLayer() { parent = this }; }

		public override void GenerateProducts (TileData data) 
		{
			MatrixWorld[] matrices = new MatrixWorld[layers.Length];
			for (int i=0; i<layers.Length; i++)
			{
				if (data.stop) return;
				matrices[i] = data.GetProduct(((MapLayer)layers[i]).inlet);
				if (matrices[i] != null) matrices[i] = (MatrixWorld)matrices[i].Clone();
			}

			//normalizing
			if (data.stop) return;
			if (normalizeLayers)
			{
				MatrixWorld[] nMatrices = new MatrixWorld[matrices.Length+1]; //array with background matrix
				Array.Copy(matrices, 0, nMatrices, 1, matrices.Length);
				nMatrices.FillNulls(() => data.area.full.CreateMatrix());
				
				nMatrices[0].Fill(1);
				Matrix.BlendLayers(nMatrices, null);
			}

			//saving products
			if (data.stop) return;
			for (int i=0; i<layers.Length; i++)
				data.SetProduct(layers[i], matrices[i]);

			//appending finalize actions
			if (data.stop) return;
			if (!data.finalizeActions.Contains(FinalizeAction))
				data.finalizeActions.Add(FinalizeAction);

			//saving outputs
			if (data.stop) return;
			TileData.Output[] outputs = new TileData.Output[layers.Length];
			for (int i=0; i<layers.Length; i++)
				data.SetOutput(layer: (MapLayer)layers[i], gen: this, biome: data.currentBiome, product: data.GetProduct(layers[i]));
		}


		public Action<TileData> FinalizeAction  { get{ return Finalize; } }
		public static void Finalize (TileData data)
		{
			if (data.draft) return; //no vs maps for draft terrains
			if (data.stop) return;
			TileData.Output[] outputs = data.GetOutputs(typeof(VSProMapsOutput));

			int fullSize = data.area.full.rect.size.x;
			int splatsSize = data.area.active.rect.size.x;
			int margins = data.area.Margins;

			//purging if no outputs
			if (outputs==null || outputs.Length == 0)
			{
				if (data.stop) return;
				data.SetApply( ApplyData.Empty );
				return;
			}

			//preparing texture colors
			VegetationPackagePro package = ((VSProMapsOutput)outputs[0].gen).package;
			Color[][] colors = new Color[package.TextureMaskGroupList.Count][];
			for (int i=0; i<colors.Length; i++)
				colors[i] = new Color[splatsSize*splatsSize];
			int[] maskGroupNums = new int[colors.Length];

			//filling colors
			for (int i=0; i < outputs.Length; i++)
			{
				Matrix matrix = (Matrix)outputs[i].product;
				MatrixWorld biomeMask = data.GetSpecial<MatrixWorld>(outputs[i].biome);
				MapLayer layer = (MapLayer)outputs[i].special;

				if (matrix == null) continue;

				for (int x=0; x<splatsSize; x++)
					for (int z=0; z<splatsSize; z++)
					{
						if (data.stop) return;

						int matrixPos = (z+margins)*fullSize + (x+margins);

						float val = matrix.arr[matrixPos];

						if (biomeMask != null) //no empty biomes in list (so no mask == root biome)
							val *= biomeMask.arr[matrixPos]; //if mask is not assigned biome was ignored, so only main outs with mask==null left here

						if (val < 0) val = 0; if (val > 1) val = 1;

						Color[] texColors = colors[layer.maskGroup];
						
						int texturePos = z*splatsSize + x;
						switch (layer.textureChannel)
						{
							case 0: texColors[texturePos].r = val; break;
							case 1: texColors[texturePos].g = val; break;
							case 2: texColors[texturePos].b = val; break;
							case 3: texColors[texturePos].a = val; break;
						}
					}
				
				maskGroupNums[i] = layer.maskGroup;
			}

			//pushing to apply
			if (data.stop) return;
			data.SetApply( new ApplyData() {package = package, colors = colors, maskGroupNums = maskGroupNums} );
		}


		public override void Clear (TileData data)
		{
			for (int i=0; i<layers.Length; i++)
				data.special.Remove((MapLayer)layers[i]);

			base.Clear(data);
		}


		public class ApplyData : ITerrainData
		{
			public VegetationPackagePro package;
			public Color[][] colors;
			public int[] maskGroupNums;

			public void Read (Terrain terrain)  { throw new System.NotImplementedException(); }

			public void Apply (Terrain terrain)
			{
				VegetationStudioTile vsTile = terrain.GetComponent<VegetationStudioTile>();
				if (vsTile == null) vsTile = terrain.gameObject.AddComponent<VegetationStudioTile>();

				Texture2D[] textures = WriteTextures(vsTile.lastUsedTextures, colors);
			
				//Rect terrainRect = new Rect(terrain.transform.position.x, terrain.transform.position.z, terrain.terrainData.size.x, terrain.terrainData.size.z);
				//SetTextures(terrainRect, textures, maskGroupNums, package);

				vsTile.lastUsedTextures = textures;
				vsTile.lastUsedMaskGroupNums = maskGroupNums;
				vsTile.lastUsedPackage = package;
				vsTile.masksApplied = false;
			}

			public static ApplyData Empty
			{get{
				return new ApplyData() { 
					colors = new Color[0][],
					maskGroupNums = new int[0]  };
			}}
		}


		public static Texture2D[] WriteTextures (Texture2D[] oldTextures, Color[][] colors)
		{
			int numTextures = colors.Length;
			if (numTextures==0) return new Texture2D[0];
			int resolution = (int)Mathf.Sqrt(colors[0].Length);

			Texture2D[] textures = new Texture2D[numTextures];

			//making textures of colors in coroutine
			for (int i=0; i<numTextures; i++)
			{
				//trying to reuse last used texture
				Texture2D tex;
				if (oldTextures != null  &&
					i < oldTextures.Length  &&
					oldTextures[i] != null && 
					oldTextures[i].width == resolution && 
					oldTextures[i].height == resolution)
						tex = oldTextures[i];
					
				else
				{
					tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true, true);
					tex.wrapMode = TextureWrapMode.Mirror; //to avoid border seams
				}
					
				tex.SetPixels(0,0,tex.width,tex.height,colors[i]);
				tex.Apply();

				textures[i] = tex;
			}

			return textures;
		}
	}

}
*/