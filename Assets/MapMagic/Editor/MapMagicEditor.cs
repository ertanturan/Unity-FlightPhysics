
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using MapMagic;

namespace MapMagic 
{
	[CustomEditor(typeof(MapMagic))]
	public class MapMagicEditor : Editor
	{
		MapMagic script; //aka target
		Layout layout;
		
		public int backgroundHeight = 0; //to draw type background
		public int oldSelected = 0; //to repaint gui with new background if new type was selected
		public enum SelectionMode { none, nailing, locking, exporting }
		public SelectionMode selectionMode = SelectionMode.none;
		Color nailColor = new Color(0.6f,0.8f,1,1); Color lockColor = new Color(1,0.3f,0.2f,1); Color exportColor = new Color(0.3f,1,0.2f,1);
		//public List<Vector3> selectedCoords; use script.selectedcoords
		public enum Pots { _64=64, _128=128, _256=256, _512=512, _1024=1024, _2048=2048, _4096=4096 };


		//when both selected and not selected
		static float lastGizmoFrame = 0;
		[DrawGizmo(GizmoType.NonSelected | GizmoType.Active)]
		static void OnDeselectedSceneGUI(Transform objectTransform, GizmoType gizmoType)
		{
			//updating with 10 fps
			if (lastGizmoFrame == Time.renderedFrameCount) return;
			lastGizmoFrame = Time.renderedFrameCount;
			
			if (MapMagic.instance==null || MapMagic.instance.chunks==null) return; 
			MapMagic mapMagic = MapMagic.instance;

			//previewing
			//foreach (Chunk tw in MapMagic.instance.chunks.All()) tw.Preview(); 

			//update custom shader templates
			/*#if UNITY_2019_2_OR_NEWER
			CustomShaderOutput.UpdateCustomShaderMaterials();
			#else
			if (CustomShaderOutput.instantUpdateMaterial && !mapMagic.assignCustomTerrainMaterial && mapMagic.terrainMaterialType==Terrain.MaterialType.Custom && mapMagic.materialTemplate != null)
				CustomShaderOutput.UpdateCustomShaderMaterials();
			#endif
			*/
		}


		//when selected
		public void OnSceneGUI ()
		{	
			if (script == null) script = (MapMagic)target;
			MapMagic.instance = script;
			if (!script.enabled) return;

			//checking removed terrains
			foreach (Chunk chunk in script.chunks.All())
				if (chunk.terrain == null) script.chunks.Remove(chunk.coord);
			

			#region Drawing Selection

			//drawing frames
			if (Event.current.type == EventType.Repaint)
			foreach(Chunk chunk in MapMagic.instance.chunks.All())
			{
				Handles.color = nailColor*0.8f;
				if (chunk.locked) Handles.color = lockColor*0.8f;
				DrawSelectionFrame(chunk.coord, 5f);
			}

			#endregion
		

			#region Selecting terrains
			if (selectionMode!=SelectionMode.none)
			{
				//disabling selection
				HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

				//finding aiming ray
				float pixelsPerPoint = 1;
				#if UNITY_5_4_OR_NEWER 
				pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
				#endif

				SceneView sceneview = UnityEditor.SceneView.lastActiveSceneView;
				if (sceneview==null || sceneview.camera==null) return;
				
				Vector2 mousePos = Event.current.mousePosition;
				mousePos = new Vector2(mousePos.x/sceneview.camera.pixelWidth, 1f/pixelsPerPoint - mousePos.y/sceneview.camera.pixelHeight) * pixelsPerPoint;
				Ray aimRay = sceneview.camera.ViewportPointToRay(mousePos);

				//aiming terrain or empty place
				Vector3 aimPos = Vector3.zero;
				RaycastHit hit;
				if (Physics.Raycast(aimRay, out hit, Mathf.Infinity)) aimPos = hit.point;
				else
				{
					aimRay.direction = aimRay.direction.normalized;
					float aimDist = aimRay.origin.y / (-aimRay.direction.y);
					aimPos = aimRay.origin + aimRay.direction*aimDist;
				}
				aimPos -= MapMagic.instance.transform.position;

				Coord aimCoord = aimPos.FloorToCoord(MapMagic.instance.terrainSize);

				if (selectionMode == SelectionMode.nailing && !Event.current.alt)
				{
					//drawing selection frame
					Handles.color = nailColor;
					DrawSelectionFrame(aimCoord, width:5f);

					//selecting / unselecting
					if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
					{
						if (MapMagic.instance.chunks[aimCoord]==null) //if obj not exists - nail
						{
							Undo.RegisterFullObjectHierarchyUndo(MapMagic.instance, "MapMagic Pin Terrain");
							MapMagic.instance.chunks.Create(aimCoord, script, pin:true); 
							//MapMagic.instance.terrains.maxCount++;
						}
						else 
						{
							Terrain terrain = MapMagic.instance.chunks[aimCoord].terrain;
							if (terrain != null) Undo.DestroyObjectImmediate(terrain.gameObject);
							Undo.RecordObject(MapMagic.instance, "MapMagic Unpin Terrain");
							MapMagic.instance.setDirty = !MapMagic.instance.setDirty;

							MapMagic.instance.chunks.Remove(aimCoord);
							//MapMagic.instance.chunks[aimCoord].pinned = false;
							//MapMagic.instance.terrains.maxCount--;
						}
						//if (MapMagic.instance.terrains.maxCount < MapMagic.instance.terrains.nailedHashes.Count+4) MapMagic.instance.terrains.maxCount = MapMagic.instance.terrains.nailedHashes.Count+4;
						//EditorUtility.SetDirty(MapMagic.instance); //already done via undo
					}
				}

				if (selectionMode == SelectionMode.locking  && !Event.current.alt)
				{
					Chunk aimedTerrain = MapMagic.instance.chunks[aimCoord];
					if (aimedTerrain != null)
					{
						//drawing selection frame
						Handles.color = lockColor;
						DrawSelectionFrame(aimCoord, width:5f);

						//selecting / unselecting
						if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
						{
							Undo.RecordObject(MapMagic.instance, "MapMagic Lock Terrain");
							MapMagic.instance.setDirty = !MapMagic.instance.setDirty;

							aimedTerrain.locked = !aimedTerrain.locked;
							//EditorUtility.SetDirty(MapMagic.instance); //already done via undo
						}
					}
				}

				if (selectionMode == SelectionMode.exporting && !Event.current.alt)
				{
					Chunk aimedTerrain = MapMagic.instance.chunks[aimCoord];
					if (aimedTerrain != null)
					{
						//drawing selection frame
						Handles.color = exportColor;
						DrawSelectionFrame(aimCoord, width:5f);

						//exporting
						if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
						{
							string path= UnityEditor.EditorUtility.SaveFilePanel(
										"Export Terrain Data",
										"",
										"TerrainData.asset", 
										"asset");
							if (path!=null && path.Length!=0)
							{
								path = path.Replace(Application.dataPath, "Assets");
								if (!path.Contains("Assets")) { Debug.LogError("MapMagic: Path is out of the Assets folder"); return; }
								if (AssetDatabase.LoadAssetAtPath(path, typeof(TerrainData)) as TerrainData == aimedTerrain.terrain.terrainData) { Debug.Log("MapMagic: this terrain was already exported at the given path"); return; }
     
								Terrain terrain = aimedTerrain.terrain;
								float[,,] splats = terrain.terrainData.GetAlphamaps(0,0,terrain.terrainData.alphamapResolution, terrain.terrainData.alphamapResolution);
								//else splats = new float[terrain.terrainData.alphamapResolution,terrain.terrainData.alphamapResolution,1];

								if (terrain.terrainData.alphamapLayers==1 && terrain.terrainData.alphamapTextures[0].width==2)
								{
									#if UNITY_2018_3_OR_NEWER
									terrain.terrainData.terrainLayers = new TerrainLayer[0];
									#else
									terrain.terrainData.splatPrototypes = new SplatPrototype[0];
									#endif

									terrain.terrainData.SetAlphamaps(0,0,new float[0,0,0]);
								}
     
								AssetDatabase.DeleteAsset(path);

								if (AssetDatabase.Contains(terrain.terrainData)) 
								{
									AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(terrain.terrainData),path);
									terrain.terrainData = AssetDatabase.LoadAssetAtPath(path, typeof(TerrainData)) as TerrainData;
									if (terrain.GetComponent<TerrainCollider>()!=null) terrain.GetComponent<TerrainCollider>().terrainData = terrain.terrainData;
								}
								else AssetDatabase.CreateAsset(terrain.terrainData, path);

								terrain.terrainData.SetAlphamaps(0,0,splats);

								AssetDatabase.SaveAssets();
							}
						}
					}
				}
				
				//redrawing scene by moving temp object
				if (script.sceneRedrawObject==null) { script.sceneRedrawObject = new GameObject(); script.sceneRedrawObject.hideFlags = HideFlags.HideInHierarchy; }
				script.sceneRedrawObject.transform.position = aimPos;
			}
			#endregion
		

		}

		public void DrawSelectionFrame (Coord coord, float width=3f)
		{
			float margins = 3;
			int numSteps = 50;
			float sideSize = MapMagic.instance.terrainSize - margins*2f;
			float stepDist = sideSize / numSteps;
			Vector3 start = coord.ToVector3(MapMagic.instance.terrainSize) + new Vector3(margins,0,margins) + MapMagic.instance.transform.position;

			Chunk chunk = MapMagic.instance.chunks[coord];

			if (chunk==null || chunk.terrain==null)
			{
				Handles.DrawAAPolyLine( width, new Vector3[] {
					start, start+new Vector3(sideSize,0,0), start+new Vector3(sideSize,0,sideSize), start+new Vector3(0,0,sideSize), start} );
			}
			else
			{
				//DrawTerrainFrame(terrain.terrain, margins, numSteps:50, width:width);
				DrawLineOnTerrain(chunk.terrain, start, new Vector3(stepDist,0,0), numSteps+1, width);
				DrawLineOnTerrain(chunk.terrain, start+new Vector3(0,0,sideSize), new Vector3(stepDist,0,0), numSteps+1, width);
				DrawLineOnTerrain(chunk.terrain, start, new Vector3(0,0,stepDist), numSteps+1, width);
				DrawLineOnTerrain(chunk.terrain, start+new Vector3(sideSize,0,0), new Vector3(0,0,stepDist), numSteps+1, width);
			}
		}

		public void DrawLineOnTerrain (Terrain terrain, Vector3 start, Vector3 step, int numSteps, float width=3f)
		{
			if (terrain == null || terrain.terrainData == null) return;
			Vector3[] steps = new Vector3[numSteps];
			for (int i=0; i<steps.Length; i++)
			{
				steps[i] = start + step*i;
				steps[i].y = terrain.SampleHeight(steps[i]) + terrain.transform.position.y;
			}
			Handles.DrawAAPolyLine(width, steps);
		}

		public void DrawTerrainFrame (Terrain terrain, float margins=3, int numSteps=50, float width=3f)
		{
			float relativeMargins = margins / terrain.terrainData.size.x;
			float lineLength = terrain.terrainData.size.x - margins*2;
			float step = lineLength / (numSteps-1);

			Vector3[] poses = new Vector3[numSteps];
			float[] heights = new float[numSteps];

			GetTerrainHeightsX(terrain, relativeMargins, ref heights, fromEnd:false);
			for (int i=0; i<poses.Length; i++)
			{
				Vector3 pos = new Vector3(margins + i*step, heights[i], margins);
				pos += terrain.transform.position; //pos = terrain.transform.TransformPoint(pos);
				poses[i] = pos;
			}
			Handles.DrawAAPolyLine(width, poses);
			Vector3 lastPosStart = poses[poses.Length-1];
			Vector3 firstPosStart = poses[0];

			GetTerrainHeightsX(terrain, relativeMargins, ref heights, fromEnd:true);
			for (int i=0; i<poses.Length; i++)
			{
				Vector3 pos = new Vector3(margins + i*step, heights[i], terrain.terrainData.size.x-margins);
				pos += terrain.transform.position; //pos = terrain.transform.TransformPoint(pos);
				poses[i] = pos;
			}
			Handles.DrawAAPolyLine(width, poses);
			Vector3 lastPosEnd = poses[poses.Length-1];
			Vector3 firstPosEnd = poses[0];

			GetTerrainHeightsZ(terrain, relativeMargins, ref heights, fromEnd:false);
			for (int i=0; i<poses.Length; i++)
			{
				Vector3 pos = new Vector3(margins, heights[i], margins + i*step);
				pos += terrain.transform.position; //pos = terrain.transform.TransformPoint(pos);
				poses[i] = pos;
			}
			poses[0] = firstPosStart; poses[poses.Length-1] = firstPosEnd;
			Handles.DrawAAPolyLine(width, poses);

			GetTerrainHeightsZ(terrain, relativeMargins, ref heights, fromEnd:true);
			for (int i=0; i<poses.Length; i++)
			{
				Vector3 pos = new Vector3(terrain.terrainData.size.x-margins, heights[i], margins + i*step);
				pos += terrain.transform.position; //pos = terrain.transform.TransformPoint(pos);
				poses[i] = pos;
			}
			poses[0] = lastPosStart; poses[poses.Length-1] = lastPosEnd;
			Handles.DrawAAPolyLine(width, poses);
		}

		public void GetTerrainHeightsX (Terrain terrain, float relativeMargin, ref float[] heights, bool fromEnd=false)
		{
			int margin = (int)(relativeMargin * terrain.terrainData.heightmapResolution);
			int arrLength = terrain.terrainData.heightmapResolution - margin*2;
			float step = 1f * arrLength / heights.Length;
			
			float[,] array = terrain.terrainData.GetHeights(margin,fromEnd? terrain.terrainData.heightmapResolution-margin-1 : margin, arrLength,1);

			for (int i=0; i<heights.Length; i++)
			{
				int pos = (int)(step*i);
				heights[i] = array[0,pos] * terrain.terrainData.size.y;
			}
		}

		public void GetTerrainHeightsZ (Terrain terrain, float relativeMargin, ref float[] heights, bool fromEnd=false)
		{
			int margin = (int)(relativeMargin * terrain.terrainData.heightmapResolution);
			int arrLength = terrain.terrainData.heightmapResolution - margin*2;
			float step = 1f * arrLength / heights.Length;
			
			float[,] array = terrain.terrainData.GetHeights(fromEnd? terrain.terrainData.heightmapResolution-margin-1 : margin,margin, 1,arrLength);

			for (int i=0; i<heights.Length; i++)
			{
				int pos = (int)(step*i);
				heights[i] = array[pos,0] * terrain.terrainData.size.y;
			}
		}



		public override void  OnInspectorGUI ()
		{
			script = (MapMagic)target;
			if (MapMagic.instance == null) MapMagic.instance = script;
			
			//checking removed terrains
			foreach (Chunk chunk in script.chunks.All())
				if (chunk.terrain == null) script.chunks.Remove(chunk.coord);

			//assigning mapmagic to mapmagic window
			if (MapMagicWindow.instance != null && MapMagicWindow.instance.mapMagic != (IMapMagic)script)
				MapMagicWindow.Show(script.gens, script, forceOpen:false);
			
			if (layout == null) layout = new Layout();
			layout.margin = 0;
			layout.rightMargin = 0;
			layout.field = Layout.GetInspectorRect();
			layout.cursor = new Rect();
			layout.undoObject = script;
			layout.undoName =  "MapMagic settings change";

			layout.Par(20); bool modeNailing = layout.CheckButton(selectionMode == SelectionMode.nailing, "Pin", rect:layout.Inset(0.3333f), icon:"MapMagic_PinIcon");
				if (layout.lastChange && modeNailing) selectionMode = SelectionMode.nailing;
				if (layout.lastChange && !modeNailing) selectionMode = SelectionMode.none;
			bool modeLocking = layout.CheckButton(selectionMode == SelectionMode.locking, "Lock", rect:layout.Inset(0.3333f), icon:"MapMagic_LockIcon");
				if (layout.lastChange && modeLocking) selectionMode = SelectionMode.locking;
				if (layout.lastChange && !modeLocking) selectionMode = SelectionMode.none;
			bool modeExporting = layout.CheckButton(selectionMode == SelectionMode.exporting, "Save", rect:layout.Inset(0.3333f), icon:"MapMagic_ExportIcon");
				if (layout.lastChange && modeExporting) selectionMode = SelectionMode.exporting;
				if (layout.lastChange && !modeExporting) selectionMode = SelectionMode.none;


			layout.Par(4);
			layout.Par(24); if (layout.Button("Show Editor", rect:layout.Inset(), icon:"MapMagic_EditorIcon"))
				MapMagicWindow.Show(script.gens, script, forceOpen:true);

//			layout.ComplexField(ref MapMagic.instance.seed, "Seed");
//			layout.ComplexSlider(script.terrains.terrainSize, "Terrain Size", max:2048, quadratic:true);
//			layout.ComplexSlider(ref script.terrainHeight, "Terrain Height", max:2048, quadratic:true);

//			layout.Par();

			
//			layout.Par(); if (layout.Button("Generate")) { MapMagic.instance.terrains.start = true; script.ProcessThreads(); }
//			layout.Par(); if (layout.Button("Clear")) MapMagic.instance.generators.ClearGenerators();

//			Undo.RecordObject (script, "MapMagic settings change");

			layout.fieldSize = 0.4f;
			layout.Par(8); layout.Foldout(ref script.guiSettings, "General Settings");
			if (script.guiSettings)
			{
				Rect anchor = layout.lastRect;
				layout.margin += 10; layout.rightMargin += 5;
				
				//layout.Field(ref MapMagic.instance.voxelandMode, "Voxeland Mode");
				layout.Field(ref MapMagic.instance.seed, "Seed");
				layout.Toggle(ref MapMagic.instance.changeSeed, "Change Seed on Playmode Start");
				MapMagic.instance.resolution = (int)layout.Field((Pots)MapMagic.instance.resolution, "Resolution");
				if (layout.lastChange) { MapMagic.instance.ClearResults(); MapMagic.instance.Generate(); }
				layout.Field(ref MapMagic.instance.terrainSize, "Terrain Size");
				if (layout.lastChange) MapMagic.instance.ResetChunks();
				layout.Field(ref MapMagic.instance.terrainHeight, "Terrain Height");
				if (layout.lastChange) MapMagic.instance.ResetChunks();
				layout.Toggle(ref MapMagic.instance.useTerrainPooling, "Use Terrain Pooling");

				layout.Par(5);
				layout.Field(ref MapMagic.instance.generateInfinite, "Generate Infinite Terrain");
				if (MapMagic.instance.generateInfinite)
				{
					layout.Field(ref MapMagic.instance.generateRange, "Generate Range");
					layout.Field(ref MapMagic.instance.removeRange, "Remove Range", min:MapMagic.instance.generateRange);
					layout.Field(ref MapMagic.instance.enableRange, "Enable Range");
					//layout.Field(ref MapMagic.instance.terrains.enableRange, "Low Detail Range");
					//layout.Field(ref MapMagic.instance.terrains.detailRange, "Full Detail Range");
				}

				//threads
				layout.Par(5);
				layout.Field(ref script.multithreading, "Multithreading");
				if (script.multithreading)
				{
					layout.Par();
					layout.Field(ref script.maxThreads, "Max Threads", rect:layout.Inset(0.75f), fieldSize:0.2f, disabled:script.autoMaxThreads);
					layout.Toggle(ref script.autoMaxThreads, "Auto",rect:layout.Inset(0.25f));
				}
				else layout.Field(ref script.maxThreads, "Max Coroutines");
				layout.Field(ref script.maxApplyTime, "Max Apply Time");

				layout.Par(5);
				script.instantGenerate = layout.Field(script.instantGenerate, "Instant Generate");
				layout.Field(ref script.saveIntermediate, "Save Intermediate Results");
				#if WDEBUG
				layout.Label("Ready Count: " +script.chunks.Any().results.ready.Count);
				layout.Label("Results Count: " +script.chunks.Any().results.results.Count);
				layout.Label("Apply Count: " +script.chunks.Any().results.apply.Count); 
				#endif
				
				layout.Field(ref script.guiHideWireframe, "Hide Frame");
				if (layout.lastChange) script.transform.ToggleDisplayWireframe(!script.guiHideWireframe);

				layout.Par(5);
				layout.Field(ref script.heightWeldMargins, "Height Weld Margins", max:100);
				layout.Field(ref script.splatsWeldMargins, "Splats Weld Margins", max:100);
				//layout.ComplexField(ref script.hideWireframe, "Hide Wireframe");
				
				layout.Par(5);
				layout.Toggle(ref script.hideFarTerrains, "Hide Out-of-Range Terrains");

				layout.Par(5);
				layout.Field(ref script.previewShader, "Preview Shader");
				
				layout.Par(10);
				layout.Par(0,padding:0); layout.Inset();
				Rect internalAnchor = layout.lastRect;
					layout.Toggle(ref script.copyLayersTags, "Copy Layers and Tags to Terrains");
					layout.Toggle(ref script.copyComponents, "Copy Components to Terrains");
				layout.Foreground(internalAnchor);

				layout.Par(10);
				layout.Par(0,padding:0); layout.Inset();
				internalAnchor = layout.lastRect;
					layout.Label("Generate Terrain Markers:");
					layout.Field(ref script.genAroundMainCam, "Around Main Camera");
					layout.Par(); layout.Field(ref script.genAroundObjsTag, "Around Objects Tagged", rect:layout.Inset());	
				
					int tagFieldWidth = (int)(layout.field.width*layout.fieldSize - 25);
					layout.cursor.x -= tagFieldWidth;
					script.genAroundTag = EditorGUI.TagField(layout.Inset(tagFieldWidth), script.genAroundTag);
				layout.Foreground(internalAnchor);

				layout.Par(10);
				layout.Par(0,padding:0); layout.Inset();
				internalAnchor = layout.lastRect;
					layout.Label("Floating Point Origin Solution:");
					layout.Toggle(ref script.shift, "Shift World");
					layout.Field(ref script.shiftThreshold, "Shift Threshold", disabled:!script.shift);
					layout.LayersField(ref script.shiftExcludeLayers, "Exclude Layers", disabled:!script.shift);
				layout.Foreground(internalAnchor);

				//data
				layout.Par(10);
				layout.Par(0,padding:0); layout.Inset();
				internalAnchor = layout.lastRect;
					layout.fieldSize = 0.7f;
					script.gens = layout.ScriptableAssetField(script.gens, construct:GeneratorsAsset.Default, savePath: null);
					if (layout.lastChange) 
						MapMagicWindow.Show(script.gens, script, forceOpen:false, asBiome:false);
				layout.Foreground(internalAnchor);

				//debug
				BuildTargetGroup buildGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
				string defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildGroup);
				
				bool debug = false;
				if (defineSymbols.Contains("WDEBUG;") || defineSymbols.EndsWith("WDEBUG")) debug = true;
				
				layout.Par(7);
				layout.Toggle(ref debug, "Debug (Requires re-compile)");
				if (layout.lastChange) 
				{
					if (debug)
					{
						defineSymbols += (defineSymbols.Length!=0? ";" : "") + "WDEBUG";
					}
					else
					{
						defineSymbols = defineSymbols.Replace("WDEBUG",""); 
						defineSymbols = defineSymbols.Replace(";;", ";"); 
					}
					PlayerSettings.SetScriptingDefineSymbolsForGroup(buildGroup, defineSymbols);
				}

				layout.margin -= 10; layout.rightMargin -= 5;
				layout.Foreground(anchor);
			}

			layout.fieldSize = 0.5f; layout.sliderSize = 0.6f;
			layout.Par(8); layout.Foldout(ref script.guiTerrainSettings, "Terrain Settings");
			if (script.guiTerrainSettings)
			{
				Rect anchor = layout.lastRect;
				layout.margin += 10; layout.rightMargin += 5;

				layout.Field(ref script.pixelError, "Pixel Error", min:0, max:200, slider:true);
				layout.Field(ref script.baseMapDist, "Base Map Dist.", min:0, max:2000, slider:true, disabled:!script.showBaseMap);
				layout.Field(ref script.showBaseMap, "Show Base Map");
				layout.Field(ref script.castShadows, "Cast Shadows");
				layout.Field(ref script.applyColliders, "Apply Terrain Colliders");
				
				#if UNITY_2017_4_OR_NEWER
				layout.Field(ref script.reflectionProbeUsage, "Reflection Probes");
				#endif

				#if UNITY_2018_3_OR_NEWER
				layout.Field(ref script.drawInstanced, "Draw Instanced");
				layout.Field(ref script.autoConnect, "Auto Connect");
				#endif

				layout.Par(5);
				#if UNITY_2019_2_OR_NEWER
				layout.Field(ref script.customTerrainMaterial, "Material Template"); 
				#else
				layout.Field(ref script.terrainMaterialType, "Material Type");
				layout.Field(ref script.customTerrainMaterial, "Custom Material", disabled:script.terrainMaterialType!=Terrain.MaterialType.Custom); 
				layout.Toggle(ref script.assignCustomTerrainMaterial, "Assign Material (disable for MegaSplat/RTP)", disabled:script.terrainMaterialType!=Terrain.MaterialType.Custom);
				#endif



				//layout.Toggle(ref script.materialTemplateMode, "Material Template Mode (MegaSplat/RTP)");
				if (Preview.enabled) layout.Label("Terrain Material is disabled in preview mode", helpbox: true);

				layout.margin -= 10; layout.rightMargin -= 5;
				layout.Foreground(anchor);
			}

			layout.Par(8); layout.Foldout(ref script.guiTreesGrassSettings, "Trees, Details and Grass Settings");
			if (script.guiTreesGrassSettings)
			{
				Rect anchor = layout.lastRect;
				layout.margin += 10; layout.rightMargin += 5;

				layout.Field(ref script.detailDraw, "Draw");
				layout.Field(ref script.detailDistance, "Detail Distance", min:0, max:250, slider:true);
				layout.Field(ref script.detailDensity, "Detail Density", min:0, max:1, slider:true);
				layout.Field(ref script.treeDistance, "Tree Distance", min:0, max:5000, slider:true);
				layout.Field(ref script.treeBillboardStart, "Billboard Start", min:0, max:2000, slider:true);
				layout.Field(ref script.treeFadeLength, "Fade Length", min:0, max:200, slider:true);
				layout.Field(ref script.treeFullLod, "Max Full LOD Trees", min:0, max:10000, slider:true);
				layout.Field(ref script.bakeLightProbesForTrees, "Bake Light Probes For Trees");

				layout.Par(5);
				layout.Field(ref script.windSpeed, "Wind Amount", min:0, max:1, slider:true);
				layout.Field(ref script.windSize, "Wind Bending", min:0, max:1, slider:true);
				layout.Field(ref script.windBending, "Wind Speed", min:0, max:1, slider:true); //there's no mistake here. Variable names are swapped in unity
				layout.Field(ref script.grassTint, "Grass Tint");

				layout.margin -= 10; layout.rightMargin -= 5;
				layout.Foreground(anchor);
			}

			if (layout.change) 
				foreach (Chunk tw in MapMagic.instance.chunks.All()) tw.SetSettings();


			#region Mass Pin
			layout.Par(8); layout.Foldout(ref script.guiMassPin, "Mass Pin/Lock");
			if (script.guiMassPin)
			{
				Rect anchor = layout.lastRect;
				layout.margin += 10; layout.rightMargin += 5;

				layout.Par(52);
				layout.Label("This feature is designed to be used with streaming plugins. Using it in all other purposes is not recommended because of performance reasons", layout.Inset(), helpbox:true);

				layout.Par();
				layout.Label("Offset (chunks):", layout.Inset(0.5f));
				layout.Field(ref script.guiPinRect.offset.x, "X", layout.Inset(0.25f), fieldSize:0.7f);
				layout.Field(ref script.guiPinRect.offset.z, "Z", layout.Inset(0.25f), fieldSize:0.7f);

				layout.Par();
				layout.Label("Area Size (chunks):", layout.Inset(0.5f));
				layout.Field(ref script.guiPinRect.size.x, "X", layout.Inset(0.25f), fieldSize:0.7f);
				layout.Field(ref script.guiPinRect.size.z, "Z", layout.Inset(0.25f), fieldSize:0.7f);

				layout.Par();
				if (layout.Button("Pin", layout.Inset(0.25f)))
				{
					Undo.RegisterFullObjectHierarchyUndo(MapMagic.instance, "MapMagic Mass Pin Terrain");
					Coord min = script.guiPinRect.Min; Coord max = script.guiPinRect.Max;
					for (int x=min.x; x<max.x; x++)
						for (int z=min.z; z<max.z; z++)
							MapMagic.instance.chunks.Create(new Coord(x,z), script, pin:true); 
				}

				if (layout.Button("Lock All", layout.Inset(0.25f)))
				{
					Undo.RegisterFullObjectHierarchyUndo(MapMagic.instance, "MapMagic Mass Lock Terrain");
					foreach (Chunk chunk in MapMagic.instance.chunks.All(pinnedOnly:true))
						chunk.locked = true;
				}

				if (layout.Button("Unpin All", layout.Inset(0.25f)))
				{
					Undo.RegisterFullObjectHierarchyUndo(MapMagic.instance, "MapMagic Mass Pin Terrain");
					MapMagic.instance.chunks.Clear();
				}

				if (layout.Button("Unlock All", layout.Inset(0.25f)))
				{
					Undo.RegisterFullObjectHierarchyUndo(MapMagic.instance, "MapMagic Mass Unlock Terrain");
					foreach (Chunk chunk in MapMagic.instance.chunks.All(pinnedOnly:true))
						chunk.locked = false;
				}

				layout.margin -= 10; layout.rightMargin -= 5;
				layout.Foreground(anchor);
			}
			#endregion

			#region About
			layout.Par(8); layout.Foldout(ref script.guiAbout, "About");
			if (script.guiAbout)
			{
				Rect anchor = layout.lastRect;
				layout.margin += 10; layout.rightMargin += 5;

				Rect savedCursor = layout.cursor;
				
				layout.Par(100, padding:0);
				layout.Icon("MapMagicAbout", layout.Inset(100,padding:0));

				layout.cursor = savedCursor;
				layout.margin = 115;

				layout.Label("MapMagic " + MapMagic.versionName + " " + MapMagic.versionState.ToVersionString());
				layout.Label("by Denis Pahunov");
				
				layout.Par(10);
				layout.Label(" - Online Documentation", url:"https://gitlab.com/denispahunov/mapmagic/wikis/home");
				layout.Label(" - Video Tutorials", url:"https://www.youtube.com/playlist?list=PL8fjbXLqBxvZb5yqXwp_bn4keyzyg5e0R");
				layout.Label(" - Forum Thread", url:"http://forum.unity3d.com/threads/map-magic-a-node-based-procedural-and-infinite-map-generator-for-asset-store.344440/");
				layout.Label(" - Issues / Ideas", url:"https://gitlab.com/denispahunov/mapmagic/issues");

				layout.margin -= 10; layout.rightMargin -= 5;
				layout.Foreground(anchor);
			}
			#endregion

			Layout.SetInspectorRect(layout.field);
		}



		[MenuItem ("GameObject/3D Object/Map Magic")]
		static void CreateMapMagic () 
		{
			if (FindObjectOfType<MapMagic>() != null)
			{
				Debug.LogError("Could not create new Map Magic instance, it already exists in scene.");
				return;
			}

			GameObject go = new GameObject();
			go.name = "Map Magic";
			MapMagic.instance = go.AddComponent<MapMagic>();

			//new terrains
			MapMagic.instance.chunks = new ChunkGrid<Chunk>();
			MapMagic.instance.seed=12345; MapMagic.instance.terrainSize=1000; MapMagic.instance.terrainHeight=300; MapMagic.instance.resolution=512;
			MapMagic.instance.chunks.Create(new Coord(0,0), MapMagic.instance, pin:true);
			//MapMagic.instance.terrains.maxCount = 5;

			//creating initial generators
			MapMagic.instance.gens = GeneratorsAsset.Default();
			//MapMagic.instance.guiGens = MapMagic.instance.gens;

			//registering undo
			MapMagic.instance.gens.OnBeforeSerialize();
			Undo.RegisterCreatedObjectUndo (go, "MapMagic Create");
			EditorUtility.SetDirty(MapMagic.instance);

			MapMagicWindow.Show(MapMagic.instance.gens, MapMagic.instance, forceOpen:false, asBiome:false);

			/*HeightOutput heightOut =  new HeightOutput();
			heightOut.guiRect = new Rect(43,76,200,20);
			MapMagic.instance.generators.array[1] = heightOut;
			heightOut.input.Link(noiseGen.output, noiseGen);*/
			
		}

		
		[MenuItem ("Window/MapMagic/Editor")]
		public static void ShowEditor ()
		{
			//GeneratorsAsset gens = FindObjectOfType<GeneratorsAsset>();
			MapMagic mm = FindObjectOfType<MapMagic>();
			GeneratorsAsset gens = mm!=null? mm.gens : null;
			MapMagicWindow.Show(gens, mm, forceOpen:true);
		}

	}//class

}//namespace