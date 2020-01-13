using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MapMagic;

namespace MapMagic
{
	[System.Serializable]
	[GeneratorMenu (menu="Output", name ="Voxeland", disengageable = true, priority = 10, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Voxeland")]
	public class VoxelandOutput : OutputGenerator
	{
		public class Layer
		{
			public string name = "Layer";
			public Input input = new Input(InoutType.Map);
			public Input heightInput = new Input(InoutType.Map);
			public Output output = new Output(InoutType.Map);
			public bool enabled = true;
			public int blockType; //0-based
			#if VOXELAND
			public Voxeland5.Generator.LayerOverlayType applyType = Voxeland5.Generator.LayerOverlayType.add;
			#endif
			public int paintThickness = 5;

			public void TempLogBlock () { Debug.Log(blockType); } //just to avoid warning
		}
		public Layer[] layers = new Layer[] { };
		private int selected = -1;
		//public void OnAddLayer (int num, object obj) { layers[num] = new Layer(); } 

		public Output areaOutput = new Output(InoutType.Voxel);
		public Output heightOut = new Output(InoutType.Map);

		public override IEnumerable<Input> Inputs() 
		{ 
			for (int i=0; i<layers.Length; i++) 
			{ 
				yield return layers[i].input; 
				yield return layers[i].heightInput; 
			} 
		}
		public override IEnumerable<Output> Outputs() 
		{ 
			if (areaOutput==null) areaOutput = new Output(InoutType.Voxel);  yield return areaOutput; 
			for (int i=0; i<layers.Length; i++) yield return layers[i].output; 
		}

		//get static actions using instance
		public override Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>> GetProces () { return Process; }
		public override Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator> GetApply () { return Apply; }
		public override Action<CoordRect, Terrain> GetPurge () { return null; }

		public void TempLogSelected () { Debug.Log(selected); } //just to avoid warning


		public float layer { get; set; }

		#if VOXELAND 
		public static Voxeland5.Voxeland voxeland; //TODO static is not serialized
		//TODO: somehow assign voxeland on data asssign or window open
		#endif

		//[System.Diagnostics.Conditional("VOXELAND")] //wtf it does not work!
		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null) 
		{ 
			#if VOXELAND
			if (stop!=null && stop(0)) return;

			//finding instance
			//Voxeland5.Voxeland voxeland = null;
			//foreach (Voxeland5.Voxeland v in Voxeland5.Voxeland.instances)
			//	if (v.data.generator.mapMagicGens.ContainsGenerator(this)) voxeland = v;
			if (voxeland==null) return;

			//preparing random
			//Noise noise = new Noise(123, permutationCount:512); //random to floor floats 

			//preparing area
			Voxeland5.Data.Area area = new Voxeland5.Data.Area();
			area.Init(rect.offset.x/terrainSize.resolution, rect.offset.z/terrainSize.resolution, terrainSize.resolution, null);

			int heightFactor = voxeland.data.generator.heightFactor;

			//iterating layers
			for (int l=0; l<layers.Length; l++)
			{
				Layer layer = layers[l];
				if (!layer.enabled) continue;

				int blockType = layer.blockType;
				if (blockType >= voxeland.landTypes.array.Length) blockType = Voxeland5.Data.emptyByte;

				//loading inputs
				Matrix src = (Matrix)layer.input.GetObject(results);
				Matrix heightSrc = (Matrix)layer.heightInput.GetObject(results);
				//SpatialHash objectsSrc = (SpatialHash)layer.objectInput.GetObject(results);
				if (src == null) continue;

				//apply
				switch (layer.applyType)
				{
					//case Voxeland5.Generator.LayerOverlayType.add: area.AddLayer(src, blockType, heightFactor:heightFactor, noise:noise); break;
					//case Voxeland5.Generator.LayerOverlayType.clampAppend: area.ClampAppendLayer(src, blockType, heightFactor:heightFactor, noise:noise); break;
					//case Voxeland5.Generator.LayerOverlayType.absolute: area.SetLayer(src, heightSrc, blockType, heightFactor:heightFactor, noise:noise); break;
					//case Voxeland5.Generator.LayerOverlayType.paint: area.PaintLayer(src, blockType, paintThickness:layer.paintThickness, noise:noise); break;
					case Voxeland5.Generator.LayerOverlayType.add: area.AddLayer(src.rect.offset.x, src.rect.offset.z, src.rect.size.x, src.array, blockType, heightFactor:heightFactor); break;
					case Voxeland5.Generator.LayerOverlayType.clampAppend: area.ClampAppendLayer(src.rect.offset.x, src.rect.offset.z, src.rect.size.x, src.array, blockType, heightFactor:heightFactor); break;
					case Voxeland5.Generator.LayerOverlayType.absolute: area.SetLayer(src.rect.offset.x, src.rect.offset.z, src.rect.size.x, src.array, heightSrc!=null? heightSrc.array : null, blockType, heightFactor:heightFactor); break;
					case Voxeland5.Generator.LayerOverlayType.paint: area.PaintLayer(src.rect.offset.x, src.rect.offset.z, src.rect.size.x, src.array, blockType, paintThickness:layer.paintThickness); break;
				}
			}

			//getting outputs
			MultiDict<int,int> typeToLayer = new MultiDict<int,int>();
			for (int l=0; l<layers.Length; l++) typeToLayer.Add(layers[l].blockType, l);

			Matrix[] layerMatrices = new Matrix[layers.Length];
			for (int l=0; l<layerMatrices.Length; l++) layerMatrices[l] = new Matrix(rect);

			for (int x=0; x<rect.size.x; x++)
			{
				Voxeland5.Data.Area.Line line = area.lines[x];
				for (int z=0; z<rect.size.z; z++)
				{
					int topType = line.columns[z].topType;
					if (topType == Voxeland5.Data.emptyByte) continue; //empty column
					List<int> typeLayerNums = typeToLayer[topType];

					#if WDEBUG
					if (typeLayerNums == null) 
						Debug.LogError("This should not happen " + topType);
					#endif

					int matrixPos = z*rect.size.x + x;
					for (int m=0; m<typeLayerNums.Count; m++)
						layerMatrices[ typeLayerNums[m] ].array[matrixPos] = 1;
				}
			}

			for (int l=0; l<layers.Length; l++) layers[l].output.SetObject(results, layerMatrices[l]);
			

			//saving results
			areaOutput.SetObject(results, area);
			#endif
		}


		public static void Process (CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop= null)
		{
			#if VOXELAND
			if (stop!=null && stop(0)) return;
			if (voxeland==null) return;

			//TODO get height factor
			int heightFactor = 200;

			//finding area by rect offset
			Coord areaCoord = Coord.PickCell(rect.offset.x, rect.offset.z, voxeland.data.areaSize);
			Voxeland5.Data.Area area = voxeland.data.areas[areaCoord.x, areaCoord.z];
			
			//clearing area
			area.ClearLand();

			//finding a list of areas and their opacities
			List<Voxeland5.Data.Area> areas = new List<Voxeland5.Data.Area>();
			List<Matrix> opacities = new List<Matrix>();
			foreach (VoxelandOutput gen in gens.GeneratorsOfType<VoxelandOutput>(onlyEnabled:true, checkBiomes:true))
			{
				//reading output directly
				Output output = gen.areaOutput;
				if (stop!=null && stop(0)) return; //checking stop before reading output
				if (!results.results.ContainsKey(output)) continue;
				Voxeland5.Data.Area genArea = (Voxeland5.Data.Area)results.results[output];

				//loading biome matrix
				Matrix biomeMask = null;
				if (gen.biome != null) 
				{
					object biomeMaskObj = gen.biome.mask.GetObject(results);
					if (biomeMaskObj==null) continue; //adding nothing if biome has no mask
					biomeMask = (Matrix)biomeMaskObj;
					if (biomeMask == null) continue; 
					if (biomeMask.IsEmpty()) continue; //optimizing empty biomes
				}

				areas.Add(genArea);
				opacities.Add(biomeMask);
			}

			//merge areas using biome mask
			if (areas.Count>=2)
				//area.MixAreas(areas.ToArray(), opacities.ToArray());
				{
				float[][] opacityArrays = new float[opacities.Count][];
				for (int i=0; i<opacityArrays.Length; i++)
					if (opacities[i] != null) opacityArrays[i] = opacities[i].array;
				area.MixAreas(areas.ToArray(), rect.offset.x, rect.offset.z, rect.size.x, opacityArrays);
				}
			else
				Voxeland5.Data.Area.CopyLand(areas[0],area);

			//reading heights
			if (results.heights==null || results.heights.rect.size.x!=rect.size.x) results.heights = new Matrix(rect);
			if (results.heights.rect != rect) results.heights.Resize(rect);
			results.heights.Clear();

			for (int x=0; x<results.heights.rect.size.x; x++)
				for (int z=0; z<results.heights.rect.size.z; z++)
					results.heights[x+results.heights.rect.offset.x, z+results.heights.rect.offset.z] = 1f * area.lines[x].columns[z].topLevel / heightFactor;

			//pushing to apply
			if (stop!=null && stop(0)) return;
			results.apply.CheckAdd(typeof(VoxelandOutput), null, replace: true);

			#endif
		}

		public static void Purge (CoordRect rect, Terrain terrain)
		{
			//Purge is not used, output is purged derectly in GeneratorsAsset when turned off
			/*#if VOXELAND
			//finding instance
			foreach (Voxeland5.Voxeland v in Voxeland5.Voxeland.instances)
			{
				if (v.data==null || v.data.generator==null || v.data.generator.mapMagicGens==null) continue;

				int numGens = 0;
				foreach (VoxelandOutput gen in v.data.generator.mapMagicGens.GeneratorsOfType<VoxelandOutput>()) numGens++;

				if (numGens==0)
				{
					//finding area by rect offset
					Coord areaCoord = Coord.PickCell(rect.offset.x, rect.offset.z, v.data.areaSize);
					Voxeland5.Data.Area area = v.data.areas[areaCoord.x, areaCoord.z];

					area.ClearLand();
				}
			}
			#endif*/
		}

		public static IEnumerator Apply (CoordRect rect, Terrain terrain, object dataBox, Func<float,bool> stop= null)
		{
			yield return null;
		}

		
		#if VOXELAND
		[System.NonSerialized] string[] blockNames = new string[0];
		#endif
		public override void OnGUI (GeneratorsAsset gens)
		{
			#if VOXELAND
			//voxeland = MapMagic.instance.GetComponent("Voxeland");

			//refreshing voxeland blocks information
			if (voxeland == null) voxeland = GameObject.FindObjectOfType<Voxeland5.Voxeland>();
			
			//gathering block names
			if (voxeland != null) 
			{
				int namesCount = voxeland.landTypes.array.Length+2;
				if (blockNames == null || blockNames.Length != namesCount) blockNames = new string[namesCount];
				for (int i=0; i<voxeland.landTypes.array.Length; i++)
					blockNames[i] = voxeland.landTypes.array[i].name;
				blockNames[namesCount-1] = "Empty";
			}

			//drawing layers
			layout.Par(1); layout.Label("Temp",rect:layout.Inset()); //needed to reset label bold style
			layout.margin = 10; layout.rightMargin = 10;
			for (int i=layers.Length-1; i>=0; i--)
				layout.DrawLayer(OnLayerGUI, ref selected, i);

			layout.Par(3); layout.Par();
			layout.DrawArrayAdd(ref layers, ref selected, layout.Inset(0.15f), reverse:true, createElement:() => new Layer() );
			layout.DrawArrayRemove(ref layers, ref selected, layout.Inset(0.15f), reverse:true);
			layout.DrawArrayDown(ref layers, ref selected, layout.Inset(0.15f), dispUp:true);
			layout.DrawArrayUp(ref layers, ref selected, layout.Inset(0.15f), dispDown:true);
			
			layout.Par(5);
			
			#endif
		}

		public void OnLayerGUI (Layout layout, bool selected, int num)
		{
			#if VOXELAND
			
			Layer layer = layers[num];
			
			layout.margin = 10; layout.rightMargin = 10;
			layout.Par(2);

			//name
			layout.Par(19); layout.Inset(5);
			layout.Toggle(ref layer.enabled,rect:layout.Inset(15));
			if (selected) layout.Field(ref layer.name,rect:layout.Inset(layout.field.width-40), style:layout.labelStyle);
			else layout.Label(layer.name,rect:layout.Inset(layout.field.width));
			
			//inputs
			layer.output.DrawIcon(layout);
			if (layer.applyType == Voxeland5.Generator.LayerOverlayType.absolute)
			{
				layout.Par(); layer.input.DrawIcon(layout, "Thickness", mandatory:false);
				layout.Par(); layer.heightInput.DrawIcon(layout, "Height", mandatory:false);
			}
			else layer.input.DrawIcon(layout, "", mandatory:false);
			
			//disconnecting improper inputs
			if (layer.applyType != Voxeland5.Generator.LayerOverlayType.absolute) layer.heightInput.Unlink();
			//else layer.input.Unlink();

			if (selected)
			{
				//overlay mode
				layout.Par(5);
				layout.fieldSize = 0.7f;
				layout.Field(ref layer.applyType, "Mode");
				if (layout.lastChange) layout.Repaint();
				layout.Par(5);
			
				//type selector
				layout.margin = 12;
				Rect cursor = layout.cursor;
				layout.Par(); layout.Label("Type:", rect:layout.Inset(layout.field.width-74));
				layout.Par(); layer.blockType = (byte)layout.Popup(layer.blockType, blockNames, rect:layout.Inset(layout.field.width - 74));
				if (layout.lastChange && layer.blockType==blockNames.Length-1) layer.blockType = Voxeland5.Data.emptyByte;
				layout.Par(); layer.blockType = (byte)layout.Field((int)layer.blockType, rect:layout.Inset(layout.field.width-74), dragChange:false);
				layout.margin = 10;

				//drawing icon
				layout.cursor = cursor;
				layout.Par(5);
				layout.Par(50, margin:0);
				layout.Inset(layout.field.width-60, margin:0);
				if (voxeland != null && layer.blockType<voxeland.landTypes.array.Length)
					layout.Icon(voxeland.landTypes.array[layer.blockType].mainTex, rect:layout.Inset(50), alphaBlend:false);

				//other properties
				layout.Par(5);
				layout.fieldSize = 0.45f;
				if (layer.applyType == Voxeland5.Generator.LayerOverlayType.paint) layout.Field(ref layer.paintThickness, "Paint Depth");
			}

			#endif
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Output", name ="Voxeland Objects", disengageable = true, priority = 10, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Voxeland%20Objects")]
	public class VoxelandObjectsOutput : OutputGenerator
	{
		public class Layer
		{
			public Input input = new Input(InoutType.Objects);
			public bool enabled = true;
			public bool relativeHeight = true;
		}
		public Layer[] layers = new Layer[] { };
		private int selected = -1;

		public override IEnumerable<Input> Inputs() 
		{ 
			for (int i=0; i<layers.Length; i++) 
			{ 
				if (layers[i]==null) layers[i] = new Layer();
				yield return layers[i].input; 
			} 
		}

		public float layer { get; set; }

		#if VOXELAND 
		public static Voxeland5.Voxeland voxeland; //TODO static is not serialized
		//TODO: somehow assign voxeland on data asssign or window open
		#endif

		public void TempLogSelected () { Debug.Log(selected); } //just to avoid warning

		//get static actions using instance
		public override Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>> GetProces () { return Process; }
		public override System.Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator> GetApply () { return Apply; }
		public override System.Action<CoordRect, Terrain> GetPurge () { return null; }


		public static void Process (CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop= null)
		{
			#if VOXELAND
			if (stop!=null && stop(0)) return;
			if (voxeland==null) return;

			//TODO get height factor
			int heightFactor = 200;

			//finding area by rect offset
			Coord areaCoord = Coord.PickCell(rect.offset.x, rect.offset.z, voxeland.data.areaSize);
			Voxeland5.Data.Area area = voxeland.data.areas[areaCoord.x, areaCoord.z];
			
			//clearing objects
			area.ClearObjects();

			//preparing random
			Noise noise = new Noise(12345); //to disable biome objects 

			//processing
			foreach (VoxelandObjectsOutput gen in gens.GeneratorsOfType<VoxelandObjectsOutput>(onlyEnabled:true, checkBiomes:true))
			{
				//reading output directly
				//Output output = gen.areaOutput;
				if (stop!=null && stop(0)) return; //checking stop before reading output
				//if (!results.results.ContainsKey(output)) continue;
				//Voxeland5.Data.Area genArea = (Voxeland5.Data.Area)results.results[output];

				//loading biome matrix
				Matrix biomeMask = null;
				if (gen.biome != null) 
				{
					object biomeMaskObj = gen.biome.mask.GetObject(results);
					if (biomeMaskObj==null) continue; //adding nothing if biome has no mask
					biomeMask = (Matrix)biomeMaskObj;
					if (biomeMask == null) continue; 
					if (biomeMask.IsEmpty()) continue; //optimizing empty biomes
				}

				//iterating layers
				for (int l=0; l<gen.layers.Length; l++)
				{
					Layer layer = gen.layers[l];

					//loading inputs
					SpatialHash src = (SpatialHash)layer.input.GetObject(results);
					if (src==null) continue;

					foreach (SpatialObject obj in src.AllObjs())
					{
						int objX = (int)(obj.pos.x+0.5f); 
						int objZ = (int)(obj.pos.y+0.5f);

						//biome masking
						float biomeVal = 1;
						if (gen.biome != null)
						{
							if (biomeMask == null) biomeVal = 0;
							else biomeVal = biomeMask[objX, objZ];
						}
						if (biomeVal < noise.Random(objX,objZ)) continue;

						//flooring
						float terrainHeight = layer.relativeHeight? results.heights[objX, objZ] : 0;
						int objHeight = (int)((obj.height+terrainHeight)*heightFactor + 0.5f);

						//area.AddObject(new CoordDir(objX, objHeight, objZ), (short)l);
						area.AddObject(objX, objHeight, objZ, 0, (short)l);
					} 
				}
			}

			//pushing to apply
			if (stop!=null && stop(0)) return;
			results.apply.CheckAdd(typeof(VoxelandOutput), null, replace: true);

			#endif
		}

		public static void Purge (CoordRect rect, Terrain terrain)
		{

		}

		public static IEnumerator Apply (CoordRect rect, Terrain terrain, object dataBox, Func<float,bool> stop= null)
		{
			yield return null;
		}

		
		public override void OnGUI (GeneratorsAsset gens)
		{
			#if VOXELAND
			//voxeland = MapMagic.instance.GetComponent("Voxeland");

			//refreshing voxeland blocks information
			if (voxeland == null) voxeland = GameObject.FindObjectOfType<Voxeland5.Voxeland>();
			
			//creating layers
			if (voxeland != null)
			{
				int layersCount = voxeland.objectsTypes.array.Length;
				if (layersCount != layers.Length)
				{
					for (int i=layersCount; i<layers.Length; i++) layers[i].input.Unlink();
					ArrayTools.Resize(ref layers, layersCount, null);
				}
				for (int i=0; i<layers.Length; i++) 
					if (layers[i]==null) layers[i] = new Layer();
			}

			if (layers.Length == 0)
			{
				layout.Par(32);
				layout.Label("Voxeland terrain has no Object Block types", rect:layout.Inset(), helpbox:true);
			}

			//drawing layers
			layout.Par(1); layout.Label("Temp",rect:layout.Inset()); //needed to reset label bold style
			layout.margin = 10; layout.rightMargin = 10;
			for (int i=layers.Length-1; i>=0; i--)
				layout.DrawLayer(OnLayerGUI, ref selected, i);

			#endif
		}

		public void OnLayerGUI (Layout layout, bool selected, int num)
		{
			#if VOXELAND
			
			Layer layer = layers[num];
			
			layout.margin = 10; layout.rightMargin = 10;
			layout.Par(2);

			//name
			layout.Par(19); layout.Inset(5);
			layout.Toggle(ref layer.enabled,rect:layout.Inset(15));
			layout.Label(voxeland.objectsTypes.array[num].name, rect:layout.Inset(layout.field.width));
			
			//input
			layer.input.DrawIcon(layout, "", mandatory:false);
			
			if (selected)
			{
				layout.margin = 30;
				layout.Field(ref layer.relativeHeight, "Relative Height", fieldSize:0.2f);
				layout.margin = 10;
			}
			#endif
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Output", name ="Voxeland Grass", disengageable = true, priority = 10, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Voxeland%20Grass")]
	public class VoxelandGrassOutput : OutputGenerator
	{
		public class Layer
		{
			public Input input = new Input(InoutType.Map);
			public bool enabled = true;
			public float density = 1f;
		}
		public Layer[] layers = new Layer[] { };
		private int selected = -1;

		public override IEnumerable<Input> Inputs() 
		{ 
			for (int i=0; i<layers.Length; i++) 
			{ 
				if (layers[i]==null) layers[i] = new Layer();
				yield return layers[i].input; 
			} 
		}

		public float layer { get; set; }

		#if VOXELAND 
		public static Voxeland5.Voxeland voxeland; //TODO static is not serialized
		//TODO: somehow assign voxeland on data asssign or window open
		#endif

		public void TempLogSelected () { Debug.Log(selected); } //just to avoid warning

		//get static actions using instance
		public override Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>> GetProces () { return Process; }
		public override System.Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator> GetApply () { return Apply; }
		public override System.Action<CoordRect, Terrain> GetPurge () { return Purge; }

		public static void Process (CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop= null)
		{
			#if VOXELAND
			if (stop!=null && stop(0)) return;
			if (voxeland==null) return;

			//finding area by rect offset
			Coord areaCoord = Coord.PickCell(rect.offset.x, rect.offset.z, voxeland.data.areaSize);
			Voxeland5.Data.Area area = voxeland.data.areas[areaCoord.x, areaCoord.z];
			
			//clearing grass
			area.ClearGrass();

			//preparing random
			//Noise noise = new Noise(12345); //to switch grass depending on it's opacity

			//processing
			foreach (VoxelandGrassOutput gen in gens.GeneratorsOfType<VoxelandGrassOutput>(onlyEnabled:true, checkBiomes:true))
			{
				//reading output directly
				if (stop!=null && stop(0)) return; //checking stop before reading output

				//loading biome matrix
				Matrix biomeMask = null;
				if (gen.biome != null) 
				{
					object biomeMaskObj = gen.biome.mask.GetObject(results);
					if (biomeMaskObj==null) continue; //adding nothing if biome has no mask
					biomeMask = (Matrix)biomeMaskObj;
					if (biomeMask == null) continue; 
					if (biomeMask.IsEmpty()) continue; //optimizing empty biomes
				}

				//iterating layers
				for (int l=0; l<gen.layers.Length; l++)
				{
					Layer layer = gen.layers[l];

					//loading inputs
					Matrix src = (Matrix)layer.input.GetObject(results);
					if (src==null) continue;
					//multiplying with biome mask - in SetGrassLayer

					//apply
					//area.SetGrassLayer(src, (byte)l, layer.density, noise:noise, layerNum:l, mask:biomeMask);
					area.SetGrassLayer(src.rect.offset.x, src.rect.offset.z, src.rect.size.x, src.array, (byte)l, layer.density, l, biomeMask==null? null : biomeMask.array);
				}
			}

			//pushing to apply
			if (stop!=null && stop(0)) return;
			results.apply.CheckAdd(typeof(VoxelandOutput), null, replace:true);

			#endif
		}

		public static void Purge (CoordRect rect, Terrain terrain)
		{
		
		}

		public static IEnumerator Apply (CoordRect rect, Terrain terrain, object dataBox, Func<float,bool> stop= null)
		{

			yield return null;
		}

		
		public override void OnGUI (GeneratorsAsset gens)
		{
			#if VOXELAND
			//voxeland = MapMagic.instance.GetComponent("Voxeland");

			//refreshing voxeland blocks information
			if (voxeland == null) voxeland = GameObject.FindObjectOfType<Voxeland5.Voxeland>();
			
			//creating layers
			if (voxeland != null)
			{
				int layersCount = voxeland.grassTypes.array.Length;
				if (layersCount != layers.Length)
				{
					for (int i=layersCount; i<layers.Length; i++) layers[i].input.Unlink();
					ArrayTools.Resize(ref layers, layersCount, null);
				}
				for (int i=0; i<layers.Length; i++) 
					if (layers[i]==null) layers[i] = new Layer();
			}

			if (layers.Length == 0)
			{
				layout.Par(32);
				layout.Label("Voxeland terrain has no Grass Block types", rect:layout.Inset(), helpbox:true);
			}

			//drawing layers
			layout.Par(1); layout.Label("Temp",rect:layout.Inset()); //needed to reset label bold style
			layout.margin = 10; layout.rightMargin = 10;
			for (int i=layers.Length-1; i>=0; i--)
				layout.DrawLayer(OnLayerGUI, ref selected, i);

			
			#endif
		}

		public void OnLayerGUI (Layout layout, bool selected, int num)
		{
			#if VOXELAND
			
			Layer layer = layers[num];
			
			layout.margin = 10; layout.rightMargin = 10;
			layout.Par(2);

			//name
			layout.Par(19); layout.Inset(5);
			layout.Toggle(ref layer.enabled,rect:layout.Inset(15));
			layout.Label(voxeland.grassTypes.array[num].name, rect:layout.Inset(layout.field.width));
			
			//input
			layer.input.DrawIcon(layout, "", mandatory:false);
			
			if (selected)
			{
				layout.margin = 30;
				layout.Field(ref layer.density, "Density");
				layout.margin = 10;
			}
			#endif
		}
	}


}