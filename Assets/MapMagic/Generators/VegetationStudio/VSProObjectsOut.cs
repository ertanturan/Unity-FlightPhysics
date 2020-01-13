using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#if VEGETATION_STUDIO_PRO
using AwesomeTechnologies.VegetationSystem;
using AwesomeTechnologies.Vegetation.Masks;
using AwesomeTechnologies.Vegetation.PersistentStorage;
using AwesomeTechnologies.Billboards;
using AwesomeTechnologies.Common;
#endif

namespace MapMagic.VegetationStudio
{
	[System.Serializable]
	[GeneratorMenu(menu = "Output", name = "VS Pro Objects", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Objects")]
	public class VSProObjectsOutput : OutputGenerator, ISerializationCallbackReceiver 
	{
		//layer
		public class Layer
		{
			public Input input = new Input(InoutType.Objects);

			public string id; //= "d825a526-4ba2-4c8f-9f4d-3f855049718a";

			public bool relativeHeight = true;
			public bool rotate = true;
			public bool takeTerrainNormal = false;
			public bool scale = true;
			public bool scaleY;

			public void OnCollapsedGUI(Layout layout)
			{
				layout.margin = 20; layout.rightMargin = 5; layout.fieldSize = 1f;
				layout.Par(20);
				input.DrawIcon(layout);
			}

			public void OnGUI (Layout layout, bool selected, int num, object parent) 
			{
				#if VEGETATION_STUDIO_PRO
				VSProObjectsOutput vsOut = (VSProObjectsOutput)parent;
				VegetationPackagePro package = vsOut.package;  //(VegetationPackagePro)vsOut.serializedPackage;
				VegetationSystemPro system = GameObject.FindObjectOfType<VegetationSystemPro>();
				Layer layer = vsOut.baseLayers[num];

				layout.margin = 20; layout.rightMargin = 5;
				layout.Par(20);

				input.DrawIcon(layout);

				if (package != null)
				{
					int itemInfoIndex = package.VegetationInfoList.FindIndex(i => i.VegetationItemID == layer.id);
					VegetationItemInfoPro itemInfo = itemInfoIndex>=0 ? package.VegetationInfoList[itemInfoIndex] : null;

					Texture2D icon = null;
					if (itemInfo != null)
					{
						#if UNITY_EDITOR
						if (itemInfo.PrefabType == VegetationPrefabType.Mesh) icon = AssetPreviewCache.GetAssetPreview(itemInfo.VegetationPrefab);
						else icon = AssetPreviewCache.GetAssetPreview(itemInfo.VegetationTexture);
						#endif
					}
					layout.Icon(icon, rect:layout.Inset(20), frame:true, alphaBlend:false);
					layout.Inset(10);

					itemInfoIndex = layout.Popup(itemInfoIndex, objectNames, rect:layout.Inset(layout.field.width-20-45));
					if (itemInfoIndex >= 0)
						layer.id = package.VegetationInfoList[itemInfoIndex].VegetationItemID;
				}

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
				#endif
			}

			//public void OnAdd(int n) { }
			//public void OnRemove(int n) { input.Link(null, null); }
			//public void OnSwitch(int o, int n) { }
		}

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

		public static string[] objectNames;

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

			//find all of the biome masks - they will be used to determine object probability
			List<TupleSet<VSProObjectsOutput,Matrix>> allGensMasks = new List<TupleSet<VSProObjectsOutput, Matrix>>();
			foreach (VSProObjectsOutput gen in gens.GeneratorsOfType<VSProObjectsOutput>(onlyEnabled: true, checkBiomes: true))
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

				allGensMasks.Add( new TupleSet<VSProObjectsOutput, Matrix>(gen,biomeMask) );
			}
			int allGensMasksCount = allGensMasks.Count;

			//biome rect to find array pos faster
			CoordRect biomeRect = new CoordRect();
			for (int g=0; g<allGensMasksCount; g++)
				if (allGensMasks[g].item2 != null) { biomeRect = allGensMasks[g].item2.rect; break; }

			//prepare biome mask values stack to re-use it to find per-coord biome
			float[] biomeVals = new float[allGensMasksCount]; //+1 for not using any object at all

			//preparing output
			Dictionary<string, List<ObjectPool.Transition>> transitions = new Dictionary<string, List<ObjectPool.Transition>>();

			//iterating all gens
			for (int g=0; g<allGensMasksCount; g++)
			{
				VSProObjectsOutput gen = allGensMasks[g].item1;

				//iterating in layers
				for (int b = 0; b < gen.baseLayers.Length; b++)
				{
					if (stop!=null && stop(0)) return; //checking stop before reading output
					Layer layer = gen.baseLayers[b];

					//loading objects from input
					SpatialHash hash = (SpatialHash)gen.baseLayers[b].input.GetObject(results);
					if (hash == null) continue;

					//finding/creating proper transitions list
					List<ObjectPool.Transition> transitionsList;
					if (!transitions.ContainsKey(layer.id)) { transitionsList = new List<ObjectPool.Transition>(); transitions.Add(layer.id, transitionsList); }
					else transitionsList = transitions[layer.id];

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
						position += MapMagic.position;

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
			results.apply.CheckAdd(typeof(VSProObjectsOutput), transitions, replace: true);
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
			#if VEGETATION_STUDIO_PRO
			Dictionary<string, List<ObjectPool.Transition>> transitions = (Dictionary<string, List<ObjectPool.Transition>>)dataBox;

			VegetationStudioTile vsTile = terrain.GetComponent<VegetationStudioTile>();
			if (vsTile == null) vsTile = terrain.gameObject.AddComponent<VegetationStudioTile>();

			vsTile.lastUsedTransitions = new List<ObjectPool.Transition>[transitions.Count];
			transitions.Values.CopyTo(vsTile.lastUsedTransitions,0);

			vsTile.lastUsedObjectIds = new string[transitions.Count];
			transitions.Keys.CopyTo(vsTile.lastUsedObjectIds,0);

			vsTile.objectApplied = false;

			/*
			float pixelSize = 1f * MapMagic.instance.terrainSize / MapMagic.instance.resolution;
			Rect terrainRect = new Rect(rect.offset.x*pixelSize, rect.offset.z*pixelSize, rect.size.x*pixelSize, rect.size.z*pixelSize);

			//adding
			foreach (KeyValuePair<Transform,List<ObjectPool.Transition>> kvp in transitions)
			{
				Transform prefab = kvp.Key;
				List<ObjectPool.Transition> transitionsList = kvp.Value;
				
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
			if (MapMagic.instance.hideWireframe) MapMagic.instance.transform.ToggleDisplayWireframe(!MapMagic.instance.guiHideWireframe);*/
			#endif
			yield return null;
		}

		public static void Purge(CoordRect rect, Terrain terrain)
		{
			//early exit
			/*if (terrain.transform.childCount == 0) return;

			Coord coord = Coord.PickCell(rect.offset, MapMagic.instance.resolution);
			Rect terrainRect = new Rect(coord.x*MapMagic.instance.terrainSize, coord.z*MapMagic.instance.terrainSize, MapMagic.instance.terrainSize, MapMagic.instance.terrainSize);

			MapMagic.instance.objectsPool.ClearAllRect(terrainRect);

			MapMagic.instance.objectsPool.RemoveEmptyPools();*/
		}

		public override void OnGUI(GeneratorsAsset gens)
		{
			#if VEGETATION_STUDIO_PRO
			//VegetationPackagePro package = (VegetationPackagePro)serializedPackage;
			layout.Field(ref package, "Package", fieldSize:0.6f);
			//serializedPackage = package; 

			//filling object names array for popup
			if (package != null)
			{
				objectNames = new string[package.VegetationInfoList.Count];
				for (int i=0; i<objectNames.Length; i++)
					objectNames[i] = package.VegetationInfoList[i].Name;
			}
			else objectNames = null;

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
			#endif
		}

	}
}