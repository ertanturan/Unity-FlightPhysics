using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Profiling;
using UnityEngine;

#if VEGETATION_STUDIO
using AwesomeTechnologies.Vegetation.PersistentStorage;
using AwesomeTechnologies.VegetationStudio;
using AwesomeTechnologies;
using AwesomeTechnologies.Common;
using AwesomeTechnologies.Billboards;
#endif


namespace MapMagic
{
	[System.Serializable]
	[GeneratorMenu(menu = "Output", name = "Vegetation Studio", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/VegetationStudio")]
	public class VegetationStudioOutput : OutputGenerator
	{
		#if VEGETATION_STUDIO
		public VegetationPackage package;
		public Transform prefab;
		public bool guiAdvanced;
		[NonSerialized] private static VegetationPackage packageShared; //to make sure all vs outputs use the same package
		[NonSerialized] private static Dictionary<CoordRect,PersistentVegetationStorage> vetStorComponents = new Dictionary<CoordRect, PersistentVegetationStorage>();  //using component instead of packege since it can auto-locate cell
		private static float cellSize;
		//[NonSerialized] private static Dictionary<CoordRect,BillboardSystem> billboardComponents = new Dictionary<CoordRect, BillboardSystem>();

		public static readonly byte VS_MM_id = 15; // 15 for MapMagic, 18 for Voxeland. 10 for Gaia.

		public static ObjectOutput.BiomeBlendType biomeBlendType = ObjectOutput.BiomeBlendType.AdditiveRandom;
		#endif

		//layer
		public class Layer
		{
			public Input objInput = new Input(InoutType.Objects);
			public Input mapInput = new Input(InoutType.Map);

			public enum Type { Object, Map };
			public Type type;

			public float density = 1;

			public bool relativeHeight = true;
			public bool rotate = true;
			public bool takeTerrainNormal = false;
			public bool scale = true; //for obj
			public bool scaleY = true; //for obj
			public Vector2 scaleMinMax = new Vector2(1,1); //for map
			public bool applyMeshRotation = false; //some VS stuff
		}

		public Layer[] layers = new Layer[0];
		public int selected;

		public void UnlinkLayer (int num)
		{
			layers[num].objInput.Link(null,null);
			layers[num].mapInput.Link(null,null);
		}

		public override IEnumerable<Input> Inputs()
		{
			if (layers == null) layers = new Layer[0];
			for (int i = 0; i < layers.Length; i++)
			{
				if (layers[i].objInput != null) yield return layers[i].objInput;
				if (layers[i].mapInput != null) yield return layers[i].mapInput;
			}
		}

		//get static actions using instance
		public override Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>> GetProces () { return Process; }
		public override Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator> GetApply () { return Apply; }
		public override Action<CoordRect, Terrain> GetPurge () { return Purge; }

		//Prepare
		public void CheckAddComponent(Chunk chunk)
		{
			#if VEGETATION_STUDIO
			Profiler.BeginSample("VS Prepare");
			
			//adding the main vegetation object
			VegetationStudioManager.AddVegetationStudioManagerToScene();

			//checking if all packs for all of the generators are the same
			if (packageShared == null) packageShared = package;
			else package = packageShared;

			//finding/creating vegetation system component
			VegetationSystem vetSys = chunk.terrain.gameObject.GetComponentInChildren<VegetationSystem>();
			if (vetSys == null  &&  prefab != null)
				vetSys = CopyVegetationSystem(prefab.gameObject, chunk.terrain);

			if (vetSys == null) 
				vetSys = VegetationStudioManager.AddVegetationSystemToTerrain(chunk.terrain, package, createPersistentVegetationStoragePackage:true);
			
			if (vetSys.VegetationPackageList.Count == 0) vetSys.VegetationPackageList.Add(package);
			if (vetSys.VegetationPackageList.Count == 1 && vetSys.VegetationPackageList[0] == null) vetSys.VegetationPackageList[0] = package;
			if (!vetSys.VegetationPackageList.Contains(package)) vetSys.VegetationPackageList.Add(package);

			if (!vetSys.InitDone) 
			{
				vetSys.SetupVegetationSystem();
				vetSys.RefreshVegetationPackage();
			}
			cellSize = vetSys.CellSize;
			

			//saving per-chunk storage (to access it from thread)
			PersistentVegetationStorage storage = vetSys.gameObject.GetComponent<PersistentVegetationStorage>();
			if (!vetStorComponents.ContainsKey(chunk.rect)) vetStorComponents.Add(chunk.rect,null);
			vetStorComponents[chunk.rect] = storage;

			Profiler.EndSample();
			#endif
		}

		#if VEGETATION_STUDIO
		public VegetationSystem CopyVegetationSystem (GameObject srcGameObj, Terrain terrain)
		{
			GameObject vegetationSystemObject = GameObject.Instantiate(srcGameObj);
			vegetationSystemObject.transform.SetParent(terrain.transform);

			VegetationSystem vegetationSystem = vegetationSystemObject.GetComponent<VegetationSystem>();
			if (vegetationSystem == null) vegetationSystem = vegetationSystemObject.AddComponent<VegetationSystem>();

            if (package != null)
				vegetationSystem.AddVegetationPackage(package, true);

			if (vegetationSystemObject.GetComponent<TerrainSystem>() == null)
				vegetationSystemObject.AddComponent<TerrainSystem>();

            BillboardSystem billboardSystem = vegetationSystemObject.GetComponent<BillboardSystem>();
			if (billboardSystem == null) billboardSystem = vegetationSystemObject.AddComponent<BillboardSystem>();
            billboardSystem.VegetationSystem = vegetationSystem;

			PersistentVegetationStorage srcStorage = vegetationSystemObject.GetComponent<PersistentVegetationStorage>();
			if (srcStorage != null) GameObject.DestroyImmediate(srcStorage);

            PersistentVegetationStorage persistentVegetationStorage = vegetationSystemObject.AddComponent<PersistentVegetationStorage>();
            persistentVegetationStorage.VegetationSystem = vegetationSystem;
            vegetationSystem.AutoselectTerrain = false;
            vegetationSystem.currentTerrain = terrain;
            //vegetationSystem.AutomaticWakeup = true;

            PersistentVegetationStoragePackage persistentVegetationStoragePackage = ScriptableObject.CreateInstance<PersistentVegetationStoragePackage>();
            persistentVegetationStorage.PersistentVegetationStoragePackage = persistentVegetationStoragePackage;
            persistentVegetationStorage.AutoInitPersistentVegetationStoragePackage = true;

            vegetationSystem.SetSleepMode(false);

            return vegetationSystem;
		}
		#endif


		public static void Process(CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop = null)
		{
			#if VEGETATION_STUDIO
			if (stop!=null && stop(0)) return;

			//preparing and clearing storage
			if (vetStorComponents == null || vetStorComponents.Count == 0) return; //vs not used (process runs anyway)
			PersistentVegetationStorage storage = vetStorComponents[rect];
			
			int cellXCount = Mathf.CeilToInt(terrainSize.dimensions / cellSize);
			int cellZCount = Mathf.CeilToInt(terrainSize.dimensions / cellSize);

			Noise noise = new Noise(12345, permutationCount:128); //to pick objects based on biome

			
			//clearing all of the items
			foreach (VegetationStudioOutput gen in gens.GeneratorsOfType<VegetationStudioOutput>(onlyEnabled:true, checkBiomes:true))
			{
				for (int b = 0; b < gen.layers.Length; b++)
				{
					string id = gen.package.VegetationInfoList[b].VegetationItemID;
					storage.RemoveVegetationItemInstances(id, VS_MM_id);
				}
				break; //iterating in one generator only - they use the same layers
			}

			if (stop!=null && stop(0)) return;

			//object outputs
			foreach (VegetationStudioOutput gen in gens.GeneratorsOfType<VegetationStudioOutput>(onlyEnabled:true, checkBiomes:true))
			{
				//gen biome mask
				Matrix biomeMask = null;
				if (gen.biome != null)
				{
					object biomeMaskObj = gen.biome.mask.GetObject(results);
					if (biomeMaskObj == null) continue; //adding nothing if biome has no mask
					biomeMask = (Matrix)biomeMaskObj;
					if (biomeMask == null) continue;
					if (biomeMask.IsEmpty()) continue; //optimizing empty biomes
				}

				//iterating in layers
				for (int b = 0; b < gen.layers.Length; b++)
				{
					if (stop!=null && stop(0)) return; //checking stop before reading output
					Layer layer = gen.layers[b];

					string id = gen.package.VegetationInfoList[b].VegetationItemID;


					//objects layer
					if (layer.type == Layer.Type.Object)
					{
						//loading objects from input
						SpatialHash hash = (SpatialHash)gen.layers[b].objInput.GetObject(results);
						if (hash == null) continue;

						//filling instances (no need to check/add key in multidict)
						foreach (SpatialObject obj in hash.AllObjs())
						{
							//skipping on biome not used
							float biomeFactor = 0;
							if (gen.biome == null) biomeFactor = 1;
							else if (biomeMask != null) biomeFactor = biomeMask.GetInterpolated(obj.pos.x, obj.pos.y);
							if (biomeFactor < 0.00001f) continue;

							float rnd;
							switch (biomeBlendType)
							{
								case ObjectOutput.BiomeBlendType.Sharp: rnd = 0.5f; break;
								case ObjectOutput.BiomeBlendType.AdditiveRandom: case ObjectOutput.BiomeBlendType.NormalizedRandom: 
									rnd = noise.Random((int)obj.pos.x, (int)obj.pos.y); 
									if (biomeFactor > 0.5f) rnd = 1-rnd; //test
									break;
								case ObjectOutput.BiomeBlendType.Scale: rnd = 0.0f; break;
								default: rnd = 0.5f; break;
							}
							
							if (biomeFactor < rnd) continue;
							
							//flooring
							float terrainHeight = 0;
							if (layer.relativeHeight && results.heights != null) //if checbox enabled and heights exist (at least one height generator is in the graph)
								terrainHeight = results.heights.GetInterpolated(obj.pos.x, obj.pos.y);
							if (terrainHeight > 1) terrainHeight = 1;


							//terrain-space object position
							Vector3 position = new Vector3(
								(obj.pos.x - hash.offset.x) / hash.size * terrainSize.dimensions,
								(obj.height + terrainHeight) * terrainSize.height, 
								(obj.pos.y - hash.offset.y) / hash.size * terrainSize.dimensions);

							//cell number
							int cx = (int)(position.x / cellSize);
							int cz = (int)(position.z / cellSize);
							PersistentVegetationCell cell = storage.PersistentVegetationStoragePackage.PersistentVegetationCellList[cz + cx*cellXCount];

							//rotation + taking terrain normal
							Quaternion rotation;
							float objRotation = layer.rotate ? obj.rotation % 360 : 0;
							if (layer.takeTerrainNormal)
							{
								Vector3 terrainNormal = ObjectOutput.GetTerrainNormal(obj.pos.x, obj.pos.y, results.heights, terrainSize.height, terrainSize.pixelSize);
								Vector3 sideVector = new Vector3( Mathf.Sin((obj.rotation+90)*Mathf.Deg2Rad), 0, Mathf.Cos((obj.rotation+90)*Mathf.Deg2Rad) );
								Vector3 frontVector = Vector3.Cross(sideVector, terrainNormal);
								rotation = Quaternion.LookRotation(frontVector, terrainNormal);
							}
							else rotation = objRotation.EulerToQuat();

							//scale + biome scale mode
							Vector3 scale = layer.scale ? new Vector3(layer.scaleY ? 1 : obj.size, obj.size, layer.scaleY ? 1 : obj.size) : Vector3.one;

							if (biomeBlendType == ObjectOutput.BiomeBlendType.Scale  &&  gen.biome != null)
							{
								float biomeVal = 1;
								if (biomeMask != null) biomeVal = biomeMask[obj.pos];
								if (biomeVal < 0.001f) continue;  //skip zero-scaled objects
								scale *= biomeVal;
							}
						
							//storage.AddVegetationItemInstance(id, position, scale, rotation, layer.applyMeshRotation, VS_MM_id, true);
							cell.AddVegetationItemInstance(id, position, scale, rotation, VS_MM_id);
						}

						if (stop!=null && stop(0)) return;
					}


					//map outputs
					if (layer.type == Layer.Type.Map)
					{
						//reading output directly
						//Output output = gen.layers[b].output;
						//if (stop!=null && stop(0)) return; //checking stop before reading output
						//if (!results.results.ContainsKey(output)) continue;
						//Matrix matrix = (Matrix)results.results[output];
					
						//loading from input
						if (stop!=null && stop(0)) return;
						Matrix matrix = (Matrix)gen.layers[b].mapInput.GetObject(results);
						if (matrix == null) continue;
						Matrix heights = results.heights; //get heights before the chunk is removed

						//setting bush by bush using the sample dist
						float sampleDist = 1f / layer.density;

						//filling
						//float terrainPosX = 1f*rect.offset.x/terrainSize.resolution*terrainSize.dimensions;
						//float terrainPosZ = 1f*rect.offset.z/terrainSize.resolution*terrainSize.dimensions;



						for (int cx = 0; cx <= cellXCount - 1; cx++)
							for (int cz = 0; cz <= cellZCount - 1; cz++)
						{
							//Vector3 cellCorner = new Vector3(terrainPosX + (cellSize * cx), 0, terrainPosZ + (cellSize * cz));           
							PersistentVegetationCell cell = storage.PersistentVegetationStoragePackage.PersistentVegetationCellList[cz + cx*cellXCount];

							for (float x = 0; x < cellSize; x+=sampleDist)
								for (float z = 0; z < cellSize; z+=sampleDist)
								{
									//world position
									float wx = cellSize*cx + x;
									float wz = cellSize*cz + z;

									//randomizing position
									wx += noise.Random((int)(wx*10), (int)(wz*10), 2) * sampleDist - sampleDist/2;
									wz += noise.Random((int)(wx*10), (int)(wz*10), 3) * sampleDist - sampleDist/2;

									//map position
									float mx = wx / terrainSize.dimensions * rect.size.x  +  rect.offset.x;  // relative (0-1) position * terrain res
									float mz = wz / terrainSize.dimensions * rect.size.z  +  rect.offset.z;

									float val = matrix.GetInterpolated(mx,mz);

									float biomeFactor = 0;
									if (gen.biome == null) biomeFactor = 1;
									else if (biomeMask != null) biomeFactor = biomeMask.GetInterpolated(mx,mz);

									//placing object
									float rnd = (noise.Random((int)(wx*10), (int)(wz*10)));
									if (rnd < val*biomeFactor)
									{
										float terrainHeight = heights.GetInterpolated(mx,mz) * terrainSize.height;

										//rotation + taking terrain normal
										Quaternion rotation;
										float rotRnd = noise.Random((int)(wx*10), (int)(wz*10), 1);
										float objRotation = layer.rotate ? rotRnd*360 : 0;
										if (layer.takeTerrainNormal)
										{
											Vector3 terrainNormal = ObjectOutput.GetTerrainNormal(mx, mz, heights, terrainSize.height, terrainSize.pixelSize);
											Vector3 sideVector = new Vector3( Mathf.Sin((objRotation+90)*Mathf.Deg2Rad), 0, Mathf.Cos((objRotation+90)*Mathf.Deg2Rad) );
											Vector3 frontVector = Vector3.Cross(sideVector, terrainNormal);
											rotation = Quaternion.LookRotation(frontVector, terrainNormal);
										}
										else rotation = objRotation.EulerToQuat();

										//scale
										float rndScale = noise.Random((int)(wx*10), (int)(wz*10), 1);
										rndScale = layer.scaleMinMax.x + (layer.scaleMinMax.y-layer.scaleMinMax.x)*rndScale;
										Vector3 scale = new Vector3(rndScale, rndScale, rndScale);
									
										//storage.AddVegetationItemInstance(id, new Vector3(wx,terrainHeight,wz), scale, rotation, layer.applyMeshRotation, VS_MM_id, true);
										cell.AddVegetationItemInstance(id, new Vector3(wx,terrainHeight,wz), scale, rotation, VS_MM_id);
									}
								}

							if (stop!=null && stop(0)) return;
						}
					}


				}
			}
		
			//refreshing billboards
			//calling it from thread ruins all the billboards
			//BillboardSystem billboardSys = billboardComponents[rect];
			//if (billboardSys != null)
			//	 billboardSys.RefreshBillboards();
			#endif

			//pushing anything to apply
			if (stop!=null && stop(0)) return;
			results.apply.CheckAdd(typeof(VegetationStudioOutput), null, replace: true);
		}



		public static IEnumerator Apply(CoordRect rect, Terrain terrain, object dataBox, Func<float,bool> stop= null)
		{
			Profiler.BeginSample("VS Apply");

			#if VEGETATION_STUDIO

			//clear cache
			VegetationSystem vetSys = terrain.gameObject.GetComponentInChildren<VegetationSystem>();
			for (int i=0; i<vetSys.VegetationCellList.Count; i++)
			{
				vetSys.VegetationCellList[i].ClearCache();
			}

			vetSys.UnityTerrainData = new UnityTerrainData(terrain, false, false);

			//refresh billboards
			BillboardSystem billboardSystem = vetSys.GetComponent<BillboardSystem>(); 
			billboardSystem.RefreshBillboards();

			Profiler.EndSample();

			#endif

			yield return null;
		}

		public static void Purge(CoordRect rect, Terrain terrain)
		{

		}



		public override void OnGUI (GeneratorsAsset gens)
		{
			#if VEGETATION_STUDIO
			layout.Par(30); layout.Icon("VegetationStudioSplashSmall", layout.Inset(), Layout.IconAligment.resize, Layout.IconAligment.resize);
			layout.Par(5);
			layout.fieldSize = 0.6f;
			package = layout.Field(package, "Package"); packageShared = package;
			//prefab = layout.Field(prefab, "Prefab");
			
			//layers
			if (package != null)
			{
				if (layers.Length != package.VegetationInfoList.Count) layers = new Layer[package.VegetationInfoList.Count];

				layout.Par(5);
				for (int num=0; num<layers.Length; num++)
					layout.DrawLayer(OnLayerGUI, ref selected, num);
			}
			else layers = new Layer[0];

			//layout.margin = 5;
			layout.Field(ref guiAdvanced, "Advanced");
			if (guiAdvanced)
				prefab = layout.Field(prefab, "Prefab", fieldSize:0.5f);

			//warnings
			layout.margin = 5;
			if (package != null)
			{
				if (package.UseTerrainTextures)
				{
					layout.Par(42);
					layout.Label("Package Update Terrain Textures On Init is turned on.", rect:layout.Inset(0.8f), helpbox:true);
					if (layout.Button("Fix",rect:layout.Inset(0.2f))) package.UseTerrainTextures = false;
				}

				bool runtimeSpawnEnabled = false;
				foreach (var v in package.VegetationInfoList)
					if (v.EnableRuntimeSpawn) { runtimeSpawnEnabled = true; break; }
				if (runtimeSpawnEnabled)
				{
					layout.Par(42);
					layout.Label("Runtime spawn is enabled on some Vegetetaion Items.", rect:layout.Inset(0.8f), helpbox:true);
					if (layout.Button("Fix",rect:layout.Inset(0.2f)))
					{
						foreach (var v in package.VegetationInfoList)
							v.EnableRuntimeSpawn = false;
					}
				}
			}

			#endif
		}

		public void OnLayerGUI (Layout layout, bool selected, int num) 
		{
			#if VEGETATION_STUDIO
			layout.margin = 20; layout.rightMargin = 5; layout.fieldSize = 1f;
			layout.Par(36);

			if (layers[num] == null) layers[num] = new Layer();
			Layer layer = layers[num];
		
			
			if (layer.type == Layer.Type.Map) layer.mapInput.DrawIcon(layout);
			else layer.objInput.DrawIcon(layout);

			layout.Icon(GetPreview(package, num), layout.Inset(36));

			layout.cursor.y -= 36;
			layout.margin += 36;

			if (package != null)
				layout.Label(package.VegetationInfoList[num].Name);
			else
				layout.Label(num + ": no package assigned");

			layout.Field(ref layer.type);
			if (layout.lastChange) UnlinkLayer(num);

			layout.margin -= 36;

			if (selected)
			{
				layout.margin -= 10;
				
				layout.Par(5);
				
				if (layer.type == Layer.Type.Map) layout.Field(ref layer.density, "Density", fieldSize:0.5f);
				
				if (layer.type == Layer.Type.Object) layout.Toggle(ref layer.relativeHeight, "Relative Height");
				layout.Toggle(ref layer.takeTerrainNormal, "Take Terrain Normal");
				layout.Toggle(ref layer.rotate, "Rotate");
				layout.Toggle(ref layer.applyMeshRotation, "Apply Mesh Rotation");

				if (layer.type == Layer.Type.Map) layout.Field(ref layer.scaleMinMax, "Scale", fieldSize:0.5f);

				if (layer.type == Layer.Type.Object) layout.Toggle(ref layer.scale, "Scale");
				if (layer.type == Layer.Type.Object) layout.Toggle(ref layer.scaleY, "Scale Y Only", disabled:!layer.scale);

				layout.margin += 10;
			}

			#endif
		}

		#if VEGETATION_STUDIO
		public Texture2D GetPreview (VegetationPackage package, int i)
		{
			#if UNITY_EDITOR
			if (package.VegetationInfoList[i].PrefabType == VegetationPrefabType.Mesh)
				//return AssetPreviewCache.GetAssetPreview(package.VegetationInfoList[i].VegetationPrefab);
				return UnityEditor.AssetPreview.GetAssetPreview( package.VegetationInfoList[i].VegetationPrefab );

			else
				//return AssetPreviewCache.GetAssetPreview(package.VegetationInfoList[i].VegetationTexture);
				return UnityEditor.AssetPreview.GetAssetPreview( package.VegetationInfoList[i].VegetationTexture );
			#else
			return null;
			#endif
		}
		#endif
	}
}
