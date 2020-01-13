using UnityEngine;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using MapMagic;

namespace MapMagic 
{
	[SelectionBase]
	[ExecuteInEditMode]
	[HelpURL("https://gitlab.com/denispahunov/mapmagic/wikis/home")]
	public class MapMagic : MonoBehaviour, ISerializationCallbackReceiver, IMapMagic
	{
		public static readonly int version = 199; 
		public static readonly string versionName = "1.10.8"; 
		public static Extensions.VersionState versionState = Extensions.VersionState.release;

		//terrains and generators
		public ChunkGrid<Chunk> chunks = new ChunkGrid<Chunk>();  //serialized with a callback
		public GeneratorsAsset gens;
		//public static Dictionary<Transform,ObjectPool> pools = new Dictionary<Transform, ObjectPool>(); //serialized with a callback
		[SerializeField] public ObjectPool objectsPool = new ObjectPool();

		//main parameters
		public int seed = 12345;
		public bool changeSeed = false;
		public int terrainSize = 1000; //should be int to avoid terrain start between pixels
		public int terrainHeight = 200;
		public int resolution = 512;
		public int lodResolution = 128;
		public bool useTerrainPooling = true;
		public Shader previewShader;

		public static MapMagic instance = null;
		public static Vector3 position; //because transform.position could not be accessed from thread

		//private Vector3[] camPoses; //this arrays will be reused and will never be used directly
		//private Coord[] camCoords; 
		public int mouseButton = -1; //mouse button in MapMagicWindow, not scene view. So it is not scene view delegate, but assigned from window script

		public bool generateInfinite = true;
		public int generateRange = 350;
		public int removeRange = 400;
		public int enableRange = 300;

		//events
		public delegate void ChangeEvent (Terrain terrain);
		public static event ChangeEvent OnPrepareStarted;	public static void CallOnPrepareStarted (Terrain terrain) { if (OnPrepareStarted != null) OnPrepareStarted(terrain); }
		public static event ChangeEvent OnGenerateCompleted;	public static void CallOnGenerateCompleted (Terrain terrain) { if (OnGenerateCompleted != null) OnGenerateCompleted(terrain); }
		public static event ChangeEvent OnGenerateFailed; public static void CallOnGenerateFailed (Terrain terrain) { if (OnGenerateFailed != null) OnGenerateFailed(terrain); }
		public static event ChangeEvent OnApplyCompleted;	public static void CallOnApplyCompleted (Terrain terrain) { if (OnApplyCompleted != null) OnApplyCompleted(terrain); }

		//preview
//		public Generator previewGenerator {get;set;}
//		public Generator.Output previewOutput {get;set;}

		//settings
		public bool instantGenerate = true;
		public bool saveIntermediate = true;
		public int heightWeldMargins = 5;
		public int splatsWeldMargins = 2;
		public bool hideWireframe = true;
		public bool hideFarTerrains = true;
		public bool copyLayersTags = true;
		public bool copyComponents = false;
		public bool applyColliders = true;
		public bool drawInstanced = false;
		public bool autoConnect = true;
		
		public bool genAroundMainCam = true;
		public bool genAroundObjsTag = false;
		public string genAroundTag = null;

		public bool shift = false;
		public int shiftThreshold = 4000;
		public int shiftExcludeLayers = 0;

		//terrain settings
		public int pixelError = 1;
		public int baseMapDist = 1000;
		public bool showBaseMap = true;
		public bool castShadows = false;
		#if UNITY_2017_4_OR_NEWER
		public UnityEngine.Rendering.ReflectionProbeUsage reflectionProbeUsage;
		#endif

		//public enum TerrainMaterialType { BuiltInLegacyDiffuse=1, BuiltInLegacySpecular=2, BuiltInStandard=0, RTP=4, Custom=3 };
		//public static Terrain.MaterialType ToUnityTerrMatType (TerrainMaterialType src)
		//{
		//	if (src==TerrainMaterialType.RTP) return Terrain.MaterialType.Custom;
		//	else return (Terrain.MaterialType)src;
		//}

		#if UNITY_2019_2_OR_NEWER
		public Material customTerrainMaterial = null; 
		#else
		public Terrain.MaterialType terrainMaterialType = Terrain.MaterialType.BuiltInStandard;
		public bool assignCustomTerrainMaterial = true;
		public Material customTerrainMaterial = null;
		#endif

		//details and trees
		public bool detailDraw = true;
		public float detailDistance = 80;
		public float detailDensity = 1;
		public float treeDistance = 1000;
		public float treeBillboardStart = 200;
		public float treeFadeLength = 5;
		public int treeFullLod = 150;
		public bool bakeLightProbesForTrees = false;

		public float windSpeed = 0.5f;
		public float windSize = 0.5f;
		public float windBending = 0.5f;
		public Color grassTint = Color.gray;

		//thread worker
		public bool multithreading = true;
		public int maxThreads = 3; 
		public bool autoMaxThreads = true;
		public int maxApplyTime = 15;

		//GUI values
		public int selected=0;
		//public GeneratorsAsset guiGens = null;
		public Vector2 guiScroll = new Vector2(0,0);
		public float guiZoom = 1;
		[System.NonSerialized] public Layout layout;
		[System.NonSerialized] public Layout toolbarLayout;
		public bool guiHideWireframe = false;
		public bool guiGenerators = true;
		public bool guiSettings = false;
		public bool guiTerrainSettings = false;
		public bool guiTreesGrassSettings = false;
		public bool guiMassPin = false;
		public CoordRect guiPinRect;
public bool guiInstantUpdate = false;
public bool guiInstantUpdateEnabled = false;
		public bool guiAbout = false;
		public GameObject sceneRedrawObject;
		public int guiGeneratorWidth = 160;

		public Dictionary<Type,long> guiDebugProcessTimes = new Dictionary<Type, long>();
		public Dictionary<Type,long> guiDebugApplyTimes = new Dictionary<Type, long>();
	
		public delegate void RepaintWindowAction();
		public static event RepaintWindowAction RepaintWindow;
		public static void CallRepaintWindow () { if (MapMagic.instance.isEditor) if (RepaintWindow != null) RepaintWindow(); }

		public bool setDirty; //registering change for undo. Inverting this value if Unity Undo does not see a change, but actually there is one (for example when saving data)

		//reusing update arrays
		[System.NonSerialized] Vector3[] prevCamPoses; //to notify if cam poses changed
		[System.NonSerialized] Vector3[] camPoses;
		[System.NonSerialized] CoordRect[] deployRects;  
		[System.NonSerialized] CoordRect[] removeRects;  //actually not 'remove' but 'remove everything that is out of those rects


		#region isEditor
		public bool isEditor 
		{get{
			#if UNITY_EDITOR
				return 
					!UnityEditor.EditorApplication.isPlaying; //if not playing
					//(UnityEditor.EditorWindow.focusedWindow != null && UnityEditor.EditorWindow.focusedWindow.GetType() == System.Type.GetType("UnityEditor.GameView,UnityEditor")) //if game view is focused
					//UnityEditor.SceneView.lastActiveSceneView == UnityEditor.EditorWindow.focusedWindow; //if scene view is focused
			#else
				return false;
			#endif
		}}
		#endregion


		public void OnEnable ()
		{
			#if UNITY_EDITOR
			//adding delegates
			UnityEditor.EditorApplication.update -= Update;	

			if (isEditor) 
				UnityEditor.EditorApplication.update += Update;	
			#endif

			//finding singleton instance
			instance = FindObjectOfType<MapMagic>();

			//checking terrains consistency
			if (chunks != null)
			foreach (Chunk chunk in chunks.All())
				if (chunk.terrain == null) chunks.Remove(chunk.coord);

			//changing seed on playmode start
			if (changeSeed && Extensions.isPlaying)
			{
				seed = (int)(System.DateTime.Now.Ticks % 1000000);
				ClearResults();
				Generate(force:true);
			}

			if (previewShader == null) previewShader = Shader.Find("MapMagic/TerrainPreview");

			//assigning default shaders for 2019.2 and later
			#if UNITY_2019_2_OR_NEWER
			if (customTerrainMaterial == null)
			{
				Shader terrainShader = Shader.Find("HDRP/TerrainLit");
				if (terrainShader == null) terrainShader = Shader.Find("Lightweight Render Pipeline/Terrain/Lit");
				if (terrainShader == null) terrainShader = Shader.Find("Nature/Terrain/Standard");
				customTerrainMaterial = new Material(terrainShader);
			}
			#endif
		}


		public void OnDisable ()
		{
			//removing delegates
			#if UNITY_EDITOR
			UnityEditor.EditorApplication.update -= Update;	
			#endif
		}


		public void Update () 
		{ 
			//shifting world
			if (!isEditor && shift) WorldShifter.Update(shiftThreshold, shiftExcludeLayers);
			position = transform.position;

			//checking if instance already exists and disabling if it is another mm
			if (instance != null && instance != this) { Debug.LogError("MapMagic object already present in scene. Disabling duplicate"); this.enabled = false; return; }
		
			//do nothing if chink size is zero
			if (terrainSize < 0.1f) return;

			//finding camera positions
			camPoses = Extensions.GetCamPoses(genAroundMainCam:genAroundMainCam, genAroundTag:genAroundObjsTag? genAroundTag : null, camPoses:camPoses);
			if (camPoses.Length == 0) return; //no cameras to deploy Voxeland
			transform.InverseTransformPoint(camPoses); 
				
			//deploy
			if (!isEditor && generateInfinite) 
			{
				//finding deploy rects
				if (deployRects == null || deployRects.Length!=camPoses.Length)
				{
					deployRects = new CoordRect[camPoses.Length]; 
					removeRects = new CoordRect[camPoses.Length];
				}
				
				for (int r=0; r<camPoses.Length; r++) //TODO: add cam pos change check
				{
					deployRects[r] = CoordRect.PickIntersectingCellsByPos(camPoses[r], generateRange, cellSize:terrainSize);
					removeRects[r] = CoordRect.PickIntersectingCellsByPos(camPoses[r], removeRange, cellSize:terrainSize);
				}

				//checking and deploying
				bool chunksChange = chunks.CheckDeploy(deployRects);
				if (chunksChange) chunks.Deploy(deployRects, removeRects, parent:this, allowMove:useTerrainPooling);
			}

			//updating chunks
			foreach (Chunk chunk in chunks.All()) 
			{
				//removing (unpinning) chunk if it's terrain was removed somehow
				if (chunk.terrain==null || chunk.terrain.transform==null || chunk.terrain.terrainData==null) { chunk.pinned = false; return; } //TODO: causes out of sync error

				//distance, priority and visibility
				float distance = camPoses.DistToRectAxisAligned(chunk.coord.x*terrainSize, chunk.coord.z*terrainSize, terrainSize);
				chunk.worker.priority = 1f / distance;

				//starting generate
				if ((distance<MapMagic.instance.generateRange || MapMagic.instance.isEditor) && chunk.worker.blank && !chunk.locked && instantGenerate) chunk.worker.Start(); 

				//enabling/disabling (after starting generate to avoid blink)
				if (!MapMagic.instance.isEditor &&
					(!chunk.worker.ready && !chunk.locked || //if non-ready
					(MapMagic.instance.hideFarTerrains && distance>MapMagic.instance.enableRange) ))  //or if out of range in playmode
						{ 
							if (chunk.terrain.gameObject.activeSelf) 
								chunk.terrain.gameObject.SetActive(false); 
						} 
				else 
				{ 
					if (!chunk.terrain.gameObject.activeSelf) 
						chunk.terrain.gameObject.SetActive(true); 
				}
					

				//setting terrain neighbors (they reset after each serialize)
				chunk.SetNeighbors(); //TODO: try doing in ondeserialize
			}


			//updating threads
			ThreadWorker.multithreading = multithreading;
			ThreadWorker.maxThreads = maxThreads; 
			ThreadWorker.autoMaxThreads = autoMaxThreads;
			ThreadWorker.maxApplyTime = maxApplyTime;
			ThreadWorker.Refresh();

		}

		public void ClearResults (Generator gen)
		{
			foreach (Chunk chunk in chunks.All()) 
			{
				chunk.worker.Stop(); //just in case
				if (saveIntermediate) chunk.results.ready.CheckRemove(gen);
				else chunk.results.Clear(); //if do not save intermediate - clearing all generators
			}
		}

		public void ClearResults (Generator[] gens)
		{
			foreach (Chunk chunk in chunks.All()) 
			{
				chunk.worker.Stop(); //just in case
				if (saveIntermediate) 
					for (int g=0; g<gens.Length; g++) 
						chunk.results.ready.CheckRemove(gens[g]);
				else chunk.results.Clear(); //if do not save intermediate - clearing all generators
			}
		}

		public void ClearResults ()
		{
			foreach (Chunk chunk in chunks.All())
			{
				chunk.worker.Stop(); //just in case
				chunk.results.Clear();
			}
		}

		public void Generate (bool force=false) //does not start generate when instant generate is off
		{
			foreach (Chunk chunk in chunks.All())
			{
				if (chunk.locked) continue;
				if (force || instantGenerate) chunk.worker.Start();

				//resetting chunk material for CSO
				//should be done in apply to prevent pink flickering
				/*if (chunk.terrain != null) //it could be destroyed by undo
				{
					if (chunk.terrain.materialTemplate != null) GameObject.DestroyImmediate(chunk.terrain.materialTemplate); //removes custom shader textures as well. Unity does not remove them!
					Resources.UnloadUnusedAssets();
				}*/
			}
		}

		public Chunk.Results ClosestResults (Vector3 camPos)
		{
			float minDist = Mathf.Infinity;
			Chunk.Results closestResults = null;
			foreach (Chunk chunk in chunks.All())
			{
				float dist = (chunk.rect.Center.vector3 - camPos).sqrMagnitude;
				if (dist<minDist)
				{
					dist = minDist;
					closestResults = chunk.results;
				}
			}
			return closestResults;
		}

		public bool IsGeneratorReady (Generator gen)
		{
			foreach (Chunk chunk in MapMagic.instance.chunks.All())
				if (!chunk.results.ready.Contains(gen)) return false;
			return true;
		}


		public bool IsWorking 
		{get{
			return ThreadWorker.IsWorking("MapMagic");
		}}

		public void ResetChunks ()
		{
			Coord[] pinned = chunks.PinnedCoords;
			chunks.Clear();
			for (int c=0; c<pinned.Length; c++)
				chunks.Create(pinned[c], this, pin:true);
		}

		public IEnumerable<Chunk.Results> Results ()
		{
			foreach (Chunk chunk in chunks.All()) 
				yield return chunk.results;
		}

		public IEnumerable<Transform> Transforms ()
		{
			foreach (Chunk chunk in chunks.All()) 
				if (chunk.pinned)
					yield return chunk.terrain.transform;
		}


		#region Serialization
			[SerializeField] public Chunk[] serializedChunks = new Chunk[0];
			[SerializeField] public Coord[] serializedChunkCoords = new Coord[0];
			[SerializeField] public bool[] serializedChunkPin = new bool[0];
			[SerializeField] public ObjectPool[] serializedPools = new ObjectPool[0];

			public virtual void OnBeforeSerialize () 
			{
				//serializing chunks
				chunks.Serialize(out serializedChunks, out serializedChunkCoords, out serializedChunkPin);

				//serializing pools
//				objectsPool.OnBeforeSerialize();

				/*if (serializedPools.Length != pools.Count) serializedPools = new ObjectPool[pools.Count];
				int counter = 0;
				foreach (KeyValuePair<Transform,ObjectPool> kvp in pools) 
				{
					serializedPools[counter] = kvp.Value;
					counter++;
				}*/
			}

			public virtual void OnAfterDeserialize ()  
			{  
				//chunks = new ChunkGrid<Chunk>(); //created in default constructor
				chunks.Deserialize(serializedChunks, serializedChunkCoords, serializedChunkPin);

				//pools
//TODO: NEW POOL
//				if (pools.Count != 0) pools.Clear();
//				for (int i=0; i<serializedPools.Length; i++) pools.Add(serializedPools[i].prefab, serializedPools[i]);
				objectsPool.OnAfterDeserialize();

				//initializing workers
				foreach (Chunk chunk in chunks.All()) chunk.InitWorker();

				//loading non-asset data if no generators
				if (gens == null) TryLoadOldNonAssetData();
			}
		#endregion

		#region Outdated
		[System.Serializable]
			public class GeneratorsList //one class is easier to serialize than multiple arrays
			{
				public Generator[] list = new Generator[0];
				public Generator[] outputs = new Generator[0];
			}

			public Serializer serializer = null;

			public void TryLoadOldNonAssetData ()
			{
				if (serializer != null && serializer.entities != null && serializer.entities.Count != 0) 
				{	
					Debug.Log("MapMagic: Loading outdated scene format. Please check node consistency and re-save the scene.");
				
					serializer.ClearLinks();
					GeneratorsList generators = new GeneratorsList();
					generators = (GeneratorsList)serializer.Retrieve(0);
					serializer.ClearLinks();

					gens = ScriptableObject.CreateInstance<GeneratorsAsset>();
					gens.list = generators.list;
					//gens.outputs = generators.outputs; 

					serializer = null;
				}
				else { Debug.Log("MapMagic: Could not find the proper graph data. It the data file was changed externally reload the scene, otherwise create the new one in the General Settings tab."); return; }
			}
		#endregion



	}//class

	public interface IMapMagic //to use in Voxeland. Note that there should not be anything mapmagic related.
	{
	//	void ChangeGenerator (Generator gen);
	//	void ChangeGenerators (Generator[] gens);

		void ClearResults ();
		void ClearResults (Generator gen);
		void ClearResults (Generator[] gens);
		void Generate (bool force=false);

		bool IsWorking {get;} //TODO: float Progress?
		bool IsGeneratorReady (Generator gen); //TODO: expose all chunks enumerable instead

		//Preview
		Chunk.Results ClosestResults (Vector3 camPos); //TODO: use results instead
		IEnumerable<Chunk.Results> Results ();
		IEnumerable<Transform> Transforms ();  ///for MapMagic each transform is a terrain. For Voxeland transform is chunk

		//bool debug {get;set;} //TODO: or in GeneratorsAsset?
		GameObject gameObject { get; } //already available by default
	}



	//for Voxeland v2 compatibility
	/*[System.Serializable]
	public class Graph : GeneratorsAsset 
	{ 
		public void Generate(Chunk.Results results, int offsetX, int offsetZ, int sizeX, int sizeZ, float heightFactor, int seed, Func<float,bool> stop = null)
		{
			CoordRect rect = new CoordRect(offsetX, offsetZ, sizeX, sizeZ);
			Chunk.Size size = new Chunk.Size(MapMagic.instance.resolution, sizeX, heightFactor);
			Calculate(rect, results, size, seed, stop);
		}

		#if VOXELAND
		public void Prepare (Chunk.Results results, Voxeland5.CoordRect rect, int resolution, int height, int seed)
		{
			Prepare (null);
		}
		#endif
	} */

}//namespace