using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using UnityEditor;

using MapMagic;

namespace MapMagic 
{
	public static class Preview 
	{
		//could be an editor class, but scripts need an access to enabled and previewGenerator

		public static void Show (IMapMagic mapMagic, Generator generator, Generator.Output output)
		{
			#if UNITY_EDITOR

			Preview.mapMagic = mapMagic;
			Preview.previewGenerator = generator;
			Preview.previewOutput = output;

			#endif
		}

		public static void Clear ()
		{
			#if UNITY_EDITOR

			mapMagic = null;
			previewGenerator = null;
			previewOutput = null;

			drawGizmos = false;
			//PreviewWindow.CloseWindow();

			//returning materials - MM case
			if (MapMagic.instance != null)
			{
				foreach (Chunk chunk in MapMagic.instance.chunks.All()) 
				{
					if (chunk.terrain == null) continue; //if terrain was deleted 

					//chunk.terrain.materialType = MapMagic.instance.terrainMaterialType;
					//chunk.terrain.materialTemplate = MapMagic.instance.customTerrainMaterial;
					chunk.SetSettings(); 

					//assigning saved material
					if (originalMaterials.ContainsKey(chunk.terrain.transform))
					{
						if (chunk.terrain.materialTemplate != null) GameObject.DestroyImmediate(chunk.terrain.materialTemplate); //removes custom shader textures as well. Unity does not remove them!
						Resources.UnloadUnusedAssets();

						chunk.terrain.materialTemplate = originalMaterials[chunk.terrain.transform];
					}
					
					//checking if material is null and switching to standard if so
/*					if (chunk.terrain.materialType == Terrain.MaterialType.Custom && MapMagic.instance.assignCustomTerrainMaterial && chunk.terrain.materialTemplate==null)
					{
						chunk.terrain.materialType = Terrain.MaterialType.BuiltInStandard; 
					}
					
					//rebuilding: bug #118
					if (chunk.terrain.materialType==Terrain.MaterialType.Custom && !MapMagic.instance.assignCustomTerrainMaterial && chunk.terrain.materialTemplate==null) 
						{ MapMagic.instance.ClearResults(); MapMagic.instance.Generate(force:true); } //rebuilding if terrain has no material saved 
*/

				}
			}

			//returning materials - Voxeland case
			#if VOXELAND
			if (Voxeland5.Voxeland.instances != null && Voxeland5.Voxeland.instances.Count != 0)
			{
				Voxeland5.Voxeland voxeland = Voxeland5.Voxeland.instances.Any();
				foreach (Transform tfm in voxeland.Transforms())
				{
					MeshRenderer renderer = tfm.GetComponent<MeshRenderer>();
					if (renderer != null) 
					{
						renderer.sharedMaterial = voxeland.material;

						if (originalMaterials.ContainsKey(tfm))
							renderer.sharedMaterial = originalMaterials[tfm];
					}
				}
			}
			#endif

			matrices.Clear();

			/*
			if (MapMagic.instance != null)
				foreach (Chunk tw in MapMagic.instance.chunks.All()) tw.SetSettings();
			#if VOXELAND
			foreach (Voxeland5.Voxeland voxeland in Voxeland5.Voxeland.instances)
				voxeland.RefreshMaterials();
			#endif  
			*/
			
			#endif
		}

		public static bool enabled {get { return previewGenerator!=null && previewOutput!=null && mapMagic!=null; }}

		public static IMapMagic mapMagic;
		public static Generator previewGenerator {get;set;}
		public static Generator.Output previewOutput {get;set;}

		public static Dictionary<Matrix,Texture2D> matrices = new Dictionary<Matrix, Texture2D>(); //preview window needs an access

		#if UNITY_EDITOR
		private static Dictionary<Transform, Material> originalMaterials = new Dictionary<Transform, Material>(); //to exit preview clearly when rtp or megasplat is used
		#endif

		public static bool drawGizmos = false; //on terrain

		//TODO: refresh if transforms changed

		#if UNITY_EDITOR

		//clear on script compile or scene save
		[UnityEditor.Callbacks.DidReloadScripts]
		private static void OnScriptsReloaded() { Clear(); }

		[UnityEditor.InitializeOnLoad]
		public class OnLoad { static OnLoad() { Clear(); }}

		public class PreviewAssetModificationProcessor : UnityEditor.AssetModificationProcessor
		{ public static string[] OnWillSaveAssets(string[] paths) { Clear(); return paths; } }


		//drawing gizmos
		[UnityEditor.DrawGizmo(UnityEditor.GizmoType.Active)]
		static void DrawGizmos(Transform objectTransform, UnityEditor.GizmoType gizmoType)
		{
			if (mapMagic == null) return;
			if (mapMagic.ToString()=="null"  ||  ReferenceEquals(mapMagic.gameObject,null)) //check game object in case it was deleted
			//mapMagic.ToString()!="null" - the only efficient interface delete check. Nor Equals neither ReferenceEquals are reliable. I <3 Unity!
				{ Clear(); return; }

			//disabled 
			if (previewOutput==null || previewGenerator==null || !drawGizmos) return;

			//matrix
			if (previewOutput.type == Generator.InoutType.Map) DrawMatrices();

			//objects
			else if (previewOutput.type == Generator.InoutType.Objects) DrawObjects();
		}


		static void DrawMatrices ()
		{
			//refreshing matrices if needed
			if (RefreshMatricesNeeded()) RefreshMatrices();

			//getting pixel size
			float pixelSize = GetPixelSize();

			//preparing shader
			Shader previewShader = MapMagic.instance.previewShader; //Shader.Find("MapMagic/TerrainPreview");

			//apply matrix textures to all objects
			foreach (Transform tfm in mapMagic.Transforms())
			{
				MeshRenderer renderer = tfm.GetComponent<MeshRenderer>();
				Terrain terrain = tfm.GetComponent<Terrain>();
				if (renderer == null && terrain == null) continue;

				//finding object rect
				Rect worldRect = new Rect(0,0,0,0);
				if (renderer != null) worldRect = new Rect(renderer.bounds.min.x, renderer.bounds.min.z, renderer.bounds.size.x, renderer.bounds.size.z);
				else if (terrain != null) worldRect = new Rect(terrain.transform.localPosition.x, terrain.transform.localPosition.z, terrain.terrainData.size.x, terrain.terrainData.size.z);
				
				CoordRect matrixRect = CoordRect.PickIntersectingCellsByPos(worldRect, pixelSize);

				//finding a matrix/texture that object should use
				Coord center = matrixRect.Center;
				foreach (KeyValuePair<Matrix,Texture2D> kvp in matrices)
				{
					Matrix matrix = kvp.Key;

					// Using the matrix that contains center - it would be the biggest intersection
					if (matrix.rect.Contains(center.x, center.z)) 
					{
						Texture2D texture = kvp.Value;

						//assigning material
						Material material = null;
						if (renderer != null)
						{
							if (renderer.sharedMaterial.shader != previewShader) 
							{
								if (!originalMaterials.ContainsKey(tfm)) originalMaterials.Add(tfm,renderer.sharedMaterial);
								renderer.sharedMaterial = new Material(previewShader);
							}
							material = renderer.sharedMaterial;
						}
						else if (terrain != null)
						{
							#if UNITY_2019_2_OR_NEWER
							if (terrain.materialTemplate==null || terrain.materialTemplate.shader != previewShader) 
							{
								if (!originalMaterials.ContainsKey(tfm)) originalMaterials.Add(tfm, terrain.materialTemplate);
								else originalMaterials[tfm] = terrain.materialTemplate;

								terrain.materialTemplate = new Material(previewShader); 
							}
							#else
							if (terrain.materialTemplate==null || terrain.materialTemplate.shader != previewShader) 
							{
								if (terrain.materialType == Terrain.MaterialType.Custom && !originalMaterials.ContainsKey(tfm)) originalMaterials.Add(tfm, terrain.materialTemplate );
								terrain.materialTemplate = new Material(previewShader); 
							}
							terrain.materialType = Terrain.MaterialType.Custom;
							#endif
							material = terrain.materialTemplate;
						}

						//assigning texture and params
						material.name = tfm.name + " Preview";

						//calculate parent MM object offset
						//Vector3 offset = tfm.parent.position / (worldRect.size.x/matrix.rect.size.x); // /pixelsize

						material.SetTexture("_Preview", texture);
						material.SetVector("_Rect", new Vector4(matrix.rect.offset.x, matrix.rect.offset.z, matrix.rect.size.x, matrix.rect.size.z ) );
						material.SetFloat("_Scale", pixelSize);
						
						break; //interrupting when texture assigned (to center)
					}
				}
			}
		}

		public static bool RefreshMatricesNeeded ()		///if number of cached matrices/objects changed OR mm has matrix/object that is not contained in cache
		{
			int matricesCount = 0;

			foreach (Chunk.Results result in mapMagic.Results())
			{
				if (result==null || !result.results.ContainsKey(previewOutput)) continue;

				object previewBox = Preview.previewOutput.GetObject<object>(result);
				if (previewBox == null) continue;

				if (previewBox is Matrix)
				{
					if (!matrices.ContainsKey((Matrix)previewBox)) return true;
					matricesCount ++;
				}
			}

			if (matricesCount != matrices.Count) return true;
			else return false;
		}


		public static void RefreshMatrices (float rangeMin=0, float rangeMax=1)
		{
			//gathering matrices cache
			matrices.Clear();
			foreach (Chunk.Results result in mapMagic.Results())
			{
				if (result==null || !result.results.ContainsKey(previewOutput)) continue;

				object previewBox = Preview.previewOutput.GetObject<object>(result);

				if (previewBox is Matrix) 
				{
					Matrix matrix = (Matrix)previewBox;
					Texture2D texture = matrix.SimpleToTexture(rangeMin:rangeMin, rangeMax:rangeMax);
					texture.wrapMode = TextureWrapMode.Clamp;
					matrices.Add(matrix, texture);
				}
			}
		}


		static public void DrawObjects ()
		{
			foreach (Chunk.Results result in mapMagic.Results())
			{
				if (result==null || !result.results.ContainsKey(previewOutput)) continue;

				object objsBox =  Preview.previewOutput.GetObject<object>(result);
				
				SpatialHash objs = null; 
				if (objsBox is SpatialHash) objs = (SpatialHash)objsBox;
				if (objs == null) continue;

				float pixelSize = GetPixelSize();
				float terrainHeight = GetTerrainHeight();

				int objsCount = objs.Count;
				foreach (SpatialObject obj in objs)
				{
					float height = 0;
					if (result.heights != null) height = result.heights.GetInterpolated(obj.pos.x, obj.pos.y);
					Vector3 pos = new Vector3(obj.pos.x*pixelSize, (obj.height+height)*terrainHeight, obj.pos.y*pixelSize);
					pos += MapMagic.instance.transform.position;

					UnityEditor.Handles.color = new Color(0.3f,1,0.2f,1);
					UnityEditor.Handles.DrawLine(pos+new Vector3(obj.size/2f,0,0), pos-new Vector3(obj.size/2f,0,0));
					UnityEditor.Handles.DrawLine(pos+new Vector3(0,0,obj.size/2f), pos-new Vector3(0,0,obj.size/2f));

					if (objsCount < 300)
					{
						Vector3 oldPoint = pos;
						foreach (Vector3 point in pos.CircleAround(obj.size/2f, objsCount<100? 12 : 4, true))
						{
							UnityEditor.Handles.DrawLine(oldPoint,point);
							oldPoint = point;
						}
					}

					UnityEditor.Handles.color = new Color(0.3f,1,0.2f,0.3333f); 
					UnityEditor.Handles.DrawLine(new Vector3(pos.x, 0, pos.z), new Vector3(pos.x, terrainHeight, pos.z));
				} // in objects
			}//foreach chunk
		}

		private static float GetPixelSize ()
		{
			float pixelSize = 1; //TODO: rather clamsy here. Change when TerrainSize would be ready

			//#if MAPMAGIC
			if (mapMagic is MapMagic) 
			{
				pixelSize = 1f * MapMagic.instance.terrainSize / MapMagic.instance.resolution;
			}
			//#endif
			#if VOXELAND
			if (mapMagic is Voxeland5.Voxeland) 
			{
				Voxeland5.Voxeland voxeland = mapMagic as Voxeland5.Voxeland;
				if (Voxeland5.Voxeland.instances.Contains(voxeland))
				{
					pixelSize = voxeland.transform.localScale.x;
				}
			}
			#endif

			return pixelSize;
		}

		private static float GetTerrainHeight ()
		{
			float terrainHeight = 100;

			//#if MAPMAGIC
			if (mapMagic is MapMagic) 
			{
				terrainHeight = MapMagic.instance.terrainHeight;
			}
			//#endif
			#if VOXELAND
			if (mapMagic is Voxeland5.Voxeland) 
			{
				Voxeland5.Voxeland voxeland = mapMagic as Voxeland5.Voxeland;
				if (Voxeland5.Voxeland.instances.Contains(voxeland))
				{
					terrainHeight = voxeland.data.generator.heightFactor;
				}
			}
			#endif

			return terrainHeight;
		}

		#endif
	}
}
