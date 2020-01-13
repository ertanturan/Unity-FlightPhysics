using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using MapMagic;

//using Plugins;

namespace MapMagic
{
	public partial class MapMagicWindow : EditorWindow, ISerializationCallbackReceiver
	{
		//private Layoutscript.layout;
		//private Layout script.toolbarLayout;

		public static MapMagicWindow instance;

		private GUIStyle generatorWindowStyle;
		private GUIStyle groupWindowStyle;


		public IMapMagic mapMagic; //assigned by a window caller
		
		public List<GeneratorsAsset> gensBiomeHierarchy = new List<GeneratorsAsset>(); //gensBiomeHierarchy[0] should be a top-level graph. Could be stack but we need access to element 0, don't wanna linq
		public GeneratorsAsset gens {get {if (gensBiomeHierarchy.Count==0) return null; else return gensBiomeHierarchy[gensBiomeHierarchy.Count-1]; }}
		static public GeneratorsAsset GetGens () { if (instance!=null) return instance.gens; else return null; }

		public Layout toolbarLayout;

		public delegate PopupMenu.MenuItem[] DrawPopupDelegate (PopupMenu.MenuItem[] items, Vector2 mousePos, GeneratorsAsset graph, Generator clickedGen, Group clickedGroup, Generator.Output clickedOutput);
		public static DrawPopupDelegate onDrawPopup;


		#region Serialization
			public MapMagic serializedMM_mapMagic;
			#if VOXELAND
			public Voxeland5.Voxeland serializedMM_voxeland;
			#endif

			public void OnBeforeSerialize () 
			{ 
				if (mapMagic is MapMagic) serializedMM_mapMagic = (MapMagic)mapMagic;
				#if VOXELAND
				if (mapMagic is Voxeland5.Voxeland) serializedMM_voxeland = (Voxeland5.Voxeland)mapMagic;
				#endif
			}
			public void OnAfterDeserialize () 
			{ 
				if (serializedMM_mapMagic != null)  mapMagic = serializedMM_mapMagic;
				#if VOXELAND
				else if (serializedMM_voxeland != null)  mapMagic = serializedMM_voxeland;
				#endif
			}
		#endregion

		#region Undo

			void PerformUndo ()
			{
				//Repaint(); //just to make curve undo work.
				//modifying curve with ascript.layout writes undo the usual way, and it is displayed in undo stack as "MapMaic Generator Change"
				//but somehow GetCurrentGroupName returns the previous action instead, like "Selection Change"
			
				if (!Undo.GetCurrentGroupName().Contains("MapMagic")) return;

				if (mapMagic!=null) 
				{
					mapMagic.ClearResults();
					mapMagic.Generate();
				}

				Repaint();
				
			}
		#endregion

		#region Right-click actions
	
			void CreateGenerator (System.Type type, Vector2 guiPos)
			{
				Undo.RecordObject (gens, "MapMagic Create Generator");
				gens.setDirty = !gens.setDirty;

				Generator gen = gens.CreateGenerator(type, guiPos);

				if (mapMagic != null)
				{
					mapMagic.ClearResults(gen);
					mapMagic.Generate();
				}

				repaint=true; Repaint(); 

				EditorUtility.SetDirty(gens);
			}

			void DeleteGenerator (Generator gen)
			{
				Undo.RecordObject (gens, "MapMagic Delete Generator"); 
				gens.setDirty = !gens.setDirty;

				//removing generator from 'ready' and 'results' arrays 
				//mapMagic.ClearResults(gen);
				//mapMagic.Generate();
	
				//manually resetting all dependent generators ready state
				//for (int g=0; g<gens.list.Length; g++)
				//	if (gens.CheckDependence(gen,gens.list[g])) mapMagic.ChangeGenerator(gens.list[g]);
				
				gens.DeleteGenerator(gen);
				
				//.ChangeGenerator(null);
				Repaint();

				EditorUtility.SetDirty(gens);
			}

			void PreviewOutput (Generator gen, Generator.Output output, bool inWindow)
			{
				if (gen==null || output==null || mapMagic==null) 
				{
					Preview.Clear(); 
				}
				else
				{
					Preview.Show(mapMagic, gen, output);

					if (inWindow) PreviewWindow.ShowWindow();
					else Preview.drawGizmos = true;

					mapMagic.Generate(); 
				}

				//select MapMagic or Voxeland object to make gizmos update
				if (Selection.activeTransform == null)
				{
					if (MapMagic.instance != null) Selection.activeTransform = MapMagic.instance.transform;
					else
					{
						#if VOXELAND
						if (Voxeland5.Voxeland.instances!=null && Voxeland5.Voxeland.instances.Count!=0) Selection.activeTransform = Voxeland5.Voxeland.instances.Any().transform;
						#endif
					}
				}

				SceneView.RepaintAll();
			} 

			void ResetGenerator (Generator gen)
			{
				Undo.RecordObject (gens, "MapMagic Reset Generator"); 
				gens.setDirty = !gens.setDirty;

				gen.ReflectionReset();

				if (mapMagic != null)
				{
					mapMagic.ClearResults(gen);
					mapMagic.Generate();
				}

				EditorUtility.SetDirty(gens);
			}

			private void DuplicateGenerator (Generator gen)
			{
				Undo.RecordObject (gens, "MapMagic Duplicate Generator"); 
				gens.setDirty = !gens.setDirty;

				Generator[] copyGens = gens.SmartDuplicateGenerators(gen);

				if (mapMagic != null)
				{
					mapMagic.ClearResults(copyGens);
					mapMagic.Generate();
				}

				EditorUtility.SetDirty(gens);
			}

			void ExportGenerator (Generator gen, Vector2 pos, string path=null) //if path is defined it will export generator using it
			{
				if (changeLock) return;
				gens.ExportGenerator(gen, pos,path);
			}

			void ImportGenerator (Vector2 pos)
			{
				if (changeLock) return;

				Undo.RecordObject (gens, "MapMagic Import Generators"); 
				gens.setDirty = !gens.setDirty;

				Generator[] copyGens = gens.ImportGenerator(pos);

				if (mapMagic != null)
				{
					mapMagic.ClearResults(copyGens);
					mapMagic.Generate();
				}

				EditorUtility.SetDirty(gens);
				repaint=true; forceAll=true; Repaint();
			}

			static bool updateWarningShowed = false;
			void UpdateGenerator (Generator gen)
			{
				if (!updateWarningShowed) 
				{
					if (EditorUtility.DisplayDialog("Warning", "Updating generator can break your graph. Make a backup before continue", "Go on, I've made a backup", "Cancel"))
						updateWarningShowed = true;
					else return;
				}
				
				Undo.RecordObject (gens, "MapMagic Update Generator"); 
				gens.setDirty = !gens.setDirty;

				//Generator[] copyGens = gens.SmartDuplicateGenerators(gen);

				System.Type type = gen.GetType();
				GeneratorMenuAttribute attribute = System.Attribute.GetCustomAttribute(type, typeof(GeneratorMenuAttribute)) as GeneratorMenuAttribute;

				//finding type to replace
				System.Type newType = null;
				if (attribute.updateType != null) newType = attribute.updateType;
				else
				{
					string name = attribute.name;
					name = name.Split(new string[] {" (Legacy"}, System.StringSplitOptions.None)[0];

					//iterating types starting from current version
					for (int v=MapMagic.version; v>=0; v--)
					{
						//newType = System.Type.GetType("MapMagic." + name + "Generator" + v.ToString()); //does not work
						newType = System.Reflection.Assembly.Load("Assembly-CSharp").GetType("MapMagic." + name + "Generator" + v.ToString());
						if (newType != null) break;
					}

					if (newType == null)
					for (int v=MapMagic.version; v>=0; v--)
					{
						newType = System.Reflection.Assembly.Load("Assembly-CSharp").GetType("MapMagic." + name + v.ToString()); //for generators that have no "Generator" in name
						if (newType != null) break;
					}
				}
				if (newType == null) { Debug.Log("Could not find a proper type to update"); return; }
				
				Generator newGen = System.Activator.CreateInstance(newType) as Generator;

				newGen.ReflectionCopyFrom(gen); 

				//changing links
				for (int g=0; g<gens.list.Length; g++)
					foreach (Generator.Input input in gens.list[g].Inputs())
						if (input != null && input.linkGen == gen) input.linkGen = newGen;

				//replacing in array
				int numInArray = ArrayTools.Find(gens.list, gen);
				gens.list[numInArray] = newGen;

				if (mapMagic != null)
				{
					mapMagic.ClearResults(gen); //copyGens
					mapMagic.Generate();
				}

				EditorUtility.SetDirty(gens);
			}

			bool CouldBeUpdated (Generator gen)
			{
				if (gen == null) return false;
				
				System.Type type = gen.GetType();
				GeneratorMenuAttribute attribute = System.Attribute.GetCustomAttribute(type, typeof(GeneratorMenuAttribute)) as GeneratorMenuAttribute;

				if (attribute.menu == "Legacy") return true;
				else return false;
			}


		#endregion


		//repainting gui to make a animated indicator
		private void OnInspectorUpdate () 
		{ 	
			if (mapMagic!=null && mapMagic.IsWorking) Repaint();
		}

		private void OnEnable ()
		{
			//finding mapmagic object if window is empty (has no gens)
			if (gens==null)
			{
				MapMagic mm = GameObject.FindObjectOfType<MapMagic>();
				if (mm!=null)
				{
					mapMagic = mm;
					gensBiomeHierarchy.Clear();
					gensBiomeHierarchy.Add(mm.gens);
				}

				#if VOXELAND
				else
				{
					Voxeland5.Voxeland voxeland = GameObject.FindObjectOfType<Voxeland5.Voxeland>();
					if (voxeland!=null)
					{
						mapMagic = voxeland;
						gensBiomeHierarchy.Clear();
						if (voxeland.data!=null && voxeland.data.generator!=null && voxeland.data.generator.mapMagicGens!=null) 
							gensBiomeHierarchy.Add(voxeland.data.generator.mapMagicGens);
					}
				}

				#endif
			}

			instance = this;
		}

		private void OnDisable ()
		{
			instance = null;
		}

		public static void RemoveFocusOnControl ()
		/// GUI.FocusControl(null) is not reliable, so creating a temporary control and focusing on it
		{
			UnityEngine.GUI.SetNextControlName("Temp");
			UnityEditor.EditorGUI.FloatField(new Rect(-10,-10,0,0), 0);
			UnityEngine.GUI.FocusControl("Temp");
		}

		private bool repaint = false;
		private bool forceAll = false;
		private void OnGUI() { DrawWindow(); if (repaint) DrawWindow(); repaint = false; } //drawing window, or doing it twice if repaint is needed
		private void DrawWindow()
		{
			if (gens == null) return;

			//un-selecting field on drag
			#if !UNITY_EDITOR_LINUX
			if (Event.current.button != 0  &&  UnityEngine.GUI.GetNameOfFocusedControl() != "Temp") RemoveFocusOnControl();
			#endif

			//startingscript.layout
			
			if (gens.layout==null) 
				{ gens.layout = new Layout(); gens.layout.scroll = gens.guiScroll; gens.layout.zoom = gens.guiZoom; gens.layout.maxZoom = 1f; }
			gens.layout.Zoom(); gens.layout.Scroll(); //scrolling and zooming
			if (gens.layout.zoom < 0.0001f) gens.layout.zoom = 1;
			gens.layout.field = this.position;

			//zoomning with keyboard
			if (Event.current.type == EventType.KeyDown)
			{
				if (Event.current.keyCode==KeyCode.Equals && Event.current.alt) { gens.layout.zoom += gens.layout.zoomStep; if (gens.layout.zoom>1) gens.layout.zoom=1; Event.current.Use(); }
				if (Event.current.keyCode==KeyCode.Minus && Event.current.alt) { gens.layout.zoom -= gens.layout.zoomStep; Event.current.Use(); }
			}
			
			//unity 5.4 beta
			if (Event.current.type == EventType.Layout) return; 

			if (Event.current.type == EventType.MouseDrag) //skip all mouse drags (except when dragging text selection cursor in field)
			{
				if (!UnityEditor.EditorGUIUtility.editingTextField) return;
				if (UnityEngine.GUI.GetNameOfFocusedControl() == "Temp") return; 
			}

			//using middle mouse click events
			if (Event.current.button == 2) Event.current.Use();

			//undo
			Undo.undoRedoPerformed -= PerformUndo;
			Undo.undoRedoPerformed += PerformUndo;

			//setting title content
			titleContent = new GUIContent("Map Magic");
			titleContent.image =gens.layout.GetIcon("MapMagic_WindowIcon");

			//drawing background
			Vector2 windowZeroPos =gens.layout.ToInternal(Vector2.zero);
			windowZeroPos.x = ((int)(windowZeroPos.x/64f)) * 64; 
			windowZeroPos.y = ((int)(windowZeroPos.y/64f)) * 64; 

			Texture2D backTex = gens.layout.GetIcon("MapMagic_Background");
			Rect backRect = new Rect(windowZeroPos - new Vector2(64,64), position.size + new Vector2(127,127));
			UnityEditor.EditorGUI.DrawPreviewTexture(new Rect(0,0,position.width,position.height), backTex, null, ScaleMode.ScaleAndCrop);
			gens.layout.Icon(backTex, backRect, tile:true);

			//drawing test center
			//script.layout.Button("Zero", new Rect(-10,-10,20,20));

			//calculating visible area
			Rect visibleArea = gens.layout.ToInternal( new Rect(0,0,position.size.x,position.size.y) );
			if (forceAll) { visibleArea = new Rect(-200000,-200000,400000,400000); forceAll = false; }
			//visibleArea = new Rect(visibleArea.x+100, visibleArea.y+100, visibleArea.width-200, visibleArea.height-200);
			//layout.Label("Area", helpBox:true, rect:visibleArea);

			//checking if all generators are loaded, and none of them is null
			for (int i=gens.list.Length-1; i>=0; i--)
			{
				if (gens.list[i] == null) { ArrayTools.RemoveAt(ref gens.list, i); continue; }
				foreach (Generator.Input input in gens.list[i].Inputs()) 
				{
					if (input == null) continue;
					if (input.linkGen == null) input.Link(null, null);
				}
			}

			#region Drawing groups
				for(int i=0; i<gens.list.Length; i++)
				{
					if (!(gens.list[i] is Group)) continue;
					Group group = gens.list[i] as Group;

					//checking if this is withinscript.layout field
					if (group.guiRect.x > visibleArea.x+visibleArea.width || group.guiRect.y > visibleArea.y+visibleArea.height ||
						group.guiRect.x+group.guiRect.width < visibleArea.x || group.guiRect.y+group.guiRect.height < visibleArea.y) 
							if (group.guiRect.width > 0.001f && gens.layout.dragState != Layout.DragState.Drag) continue; //if guiRect initialized and not dragging

					//settingscript.layout data
					group.layout.field = group.guiRect;
					group.layout.scroll = gens.layout.scroll;
					group.layout.zoom = gens.layout.zoom;

					group.OnGUI(gens);

					group.guiRect = group.layout.field;
				}
			#endregion

			#region Drawing connections (before generators to make them display under nodes)

				foreach(Generator gen in gens.list)
				{
					foreach (Generator.Input input in gen.Inputs())
					{
						if (input==null || input.link == null) continue; //input could be null in layered generators
						if (gen is Portal)
						{ 
							Portal portal = (Portal)gen;
							if (!portal.drawInputConnection) continue;
						}
						gens.layout.Spline(input.link.guiConnectionPos, input.guiConnectionPos, color:GeneratorsAsset.CanConnect(input.link,input)? input.guiColor : Color.red);
					}
				}
			#endregion

			#region creating connections (after generators to make clicking in inout work)

			int dragIdCounter = gens.list.Length+1;
				foreach (Generator gen in gens.list)
					foreach (Generator.IGuiInout inout in gen.Inouts())
				{
					if (inout == null) continue;
					if (gens.layout.DragDrop(inout.guiRect, dragIdCounter))
					{
						//finding target
						Generator.IGuiInout target = null;
						foreach (Generator gen2 in gens.list)
							foreach (Generator.IGuiInout inout2 in gen2.Inouts())
								if (inout2.guiRect.Contains(gens.layout.dragPos)) target = inout2;

						//converting inout to Input (or Output) and target to Output (or Input)
						Generator.Input input = inout as Generator.Input;		if (input==null) input = target as Generator.Input;
						Generator.Output output = inout as Generator.Output;	if (output==null) output = target as Generator.Output;

						//connection validity test
						bool canConnect = input!=null && output!=null && GeneratorsAsset.CanConnect(output,input);

						//infinite loop test
						if (canConnect)
						{ 
							Generator outputGen = output.GetGenerator(gens.list);
							Generator inputGen = input.GetGenerator(gens.list);
							if (inputGen == outputGen || gens.CheckDependence(inputGen,outputGen)) canConnect = false;
						}

						//drag
						//if (script.layout.dragState==Layout.DragState.Drag) //commented out because will not be displayed on repaint otherwise
						//{
							if (input == null)gens.layout.Spline(output.guiConnectionPos,gens.layout.dragPos, color:Color.red);
							else if (output == null)gens.layout.Spline(gens.layout.dragPos, input.guiConnectionPos, color:Color.red);
							else gens.layout.Spline(output.guiConnectionPos, input.guiConnectionPos, color:canConnect? input.guiColor : Color.red);
						//}

						//release
						if (gens.layout.dragState==Layout.DragState.Released && input!=null) //on release. Do nothing if input not defined
						{
							Undo.RecordObject (gens, "MapMagic Connection"); 
							gens.setDirty = !gens.setDirty;

							input.Unlink();
							if (canConnect) input.Link(output, output.GetGenerator(gens.list));
							if (mapMagic!=null) 
							{
								mapMagic.ClearResults(gen);
								mapMagic.Generate();
							}

							EditorUtility.SetDirty(gens);
						}
					}
					dragIdCounter++;
				}
			#endregion

			#region Drawing generators

				for(int i=0; i<gens.list.Length; i++)
				{
					Generator gen = gens.list[i];
					if (gen is Group) continue; //skipping groups

					//checking if this generator is withinscript.layout field
					if (gen.guiRect.x > visibleArea.x+visibleArea.width || gen.guiRect.y > visibleArea.y+visibleArea.height ||
						gen.guiRect.x+gen.guiRect.width < visibleArea.x || gen.guiRect.y+gen.guiRect.height < visibleArea.y) 
							if (gen.guiRect.width > 0.001f && gens.layout.dragState != Layout.DragState.Drag) continue; //if guiRect initialized and not dragging

					if (gen.layout == null) gen.layout = new Layout();
					gen.layout.field = gen.guiRect;
					gen.layout.field.width = 160; //MapMagic.instance.guiGeneratorWidth;
				
					//gen.layout.OnBeforeChange -= RecordGeneratorUndo;
					//gen.layout.OnBeforeChange += RecordGeneratorUndo;
					gen.layout.undoObject = gens;
					gen.layout.undoName = "MapMagic Generators Change"; 
					gen.layout.dragChange = true;
					gen.layout.disabled = changeLock;

					//copyscript.layout params
					gen.layout.scroll = gens.layout.scroll;
					gen.layout.zoom = gens.layout.zoom;

					//drawing background
					gen.layout.Element("MapMagic_Window", gen.layout.field, new RectOffset(34,34,34,34), new RectOffset(33,33,33,33));

					//resetting layout
					gen.layout.field.height = 0;
					gen.layout.field.width =160;
					gen.layout.cursor = new Rect();
					gen.layout.change = false;
					gen.layout.margin = 1; gen.layout.rightMargin = 1;
					gen.layout.fieldSize = 0.4f;   
					
					//drawing header
					gen.DrawHeader (mapMagic, gens);
					if (gen is OutputGenerator && gen.layout.change && gen.enabled == false) //if just disabled output
						gensBiomeHierarchy[0].OnDisableGenerator(gen);

					//drawing parameters
					#if WDEBUG
					gen.OnGUI(gens);
					#else
					try { gen.OnGUI(gens); }
					catch (UnityException e) { Debug.LogError("Error drawing generator " + GetType() + "\n" + e);} 
					//should be system.exception but it causes ExitGUIException on opening curve/color/texture fields
					//it's so unity...
					//if something goes wrong but no error is displayed - you know where to find it
					#endif
					gen.layout.Par(3);

					//drawing debug generate time
					#if WDEBUG
					if (mapMagic!=null)
					{
						Rect timerRect = new Rect(gen.layout.field.x, gen.layout.field.y+gen.layout.field.height, 200, 20);
						string timeLabel = "g:" + gen.guiGenerateTime + "ms ";
						if (gen is OutputGenerator)
						{
							if (Generator.guiProcessTime.ContainsKey(gen.GetType())) timeLabel += " p:" + Generator.guiProcessTime[gen.GetType()] + "ms ";
							if (Generator.guiApplyTime.ContainsKey(gen.GetType())) timeLabel += " a:" + Generator.guiApplyTime[gen.GetType()] + "ms ";
						}
						gen.layout.Label(timeLabel, timerRect);
					}
					#endif

					//instant generate on params change
					if (gen.layout.change) 
					{
						if (mapMagic!=null) 
						{
							mapMagic.ClearResults(gen);
							mapMagic.Generate();
						}
						repaint=true; Repaint();

						EditorUtility.SetDirty(gens);
					}

					//drawing biome "edit" button. Rather hacky, but we have to call editor method when pressing "Edit"
					if (gen is Biome)
					{
						Biome biome = (Biome)gen;
						if (gen.layout.Button("Edit", disabled:biome.data==null)) 
						{
							MapMagicWindow.Show(biome.data, mapMagic, forceOpen:true,asBiome: true);
							Repaint();
							return; //cancel drawing this graph if biome was opened
						}
						gen.layout.Par(10);
					}

					//changing all of the output generators of the same type (in case this one was disabled to make refresh)
					if (gen.layout.change && !gen.enabled && gen is OutputGenerator)
					{
						foreach (GeneratorsAsset ga in gensBiomeHierarchy)
						foreach (OutputGenerator sameOut in ga.GeneratorsOfType<OutputGenerator>(onlyEnabled:true, checkBiomes:true))
							if (sameOut.GetType() == gen.GetType()) 
							{
								mapMagic.ClearResults(sameOut);
								mapMagic.Generate();
							}
					}

			
					if (gen.guiRect.width<1 && gen.guiRect.height<1) { repaint=true;  Repaint(); } //repainting if some of the generators rect is 0
					gen.guiRect = gen.layout.field;
				}
			#endregion

			#region Toolbar

				if (toolbarLayout==null) toolbarLayout = new Layout();
				toolbarLayout.margin = 0; toolbarLayout.rightMargin = 0;
				toolbarLayout.field.width = this.position.width;
				toolbarLayout.field.height = 18;
				toolbarLayout.cursor = new Rect();
				//toolbarLayout.window = this;
				toolbarLayout.Par(18, padding:0);

				EditorGUI.LabelField(toolbarLayout.field, "", EditorStyles.toolbarButton);

				if (mapMagic!=null  &&  mapMagic.ToString()!="null"  &&  !ReferenceEquals(mapMagic.gameObject,null)) //check game object in case it was deleted
				//mapMagic.ToString()!="null" - the only efficient delete check. Nor Equals neither ReferenceEquals are reliable. I <3 Unity!
				{
					//drawing state icon
					toolbarLayout.Inset(25);
					if (ThreadWorker.IsWorking("MapMagic")) { toolbarLayout.Icon("MapMagic_Loading", new Rect(5,0,16,16), animationFrames:12); Repaint(); }
					else toolbarLayout.Icon("MapMagic_Success", new Rect(5,0,16,16));
					//TODO: changed sign

					//mapmagic name
					Rect nameLabelRect = toolbarLayout.Inset(100); nameLabelRect.y+=1; //nameLabelRect.height-=4;
					EditorGUI.LabelField(nameLabelRect, mapMagic.gameObject.name, EditorStyles.miniLabel);

					//generate buttons
					if (GUI.Button(toolbarLayout.Inset(110,padding:0), "Generate Changed", EditorStyles.toolbarButton)) mapMagic.Generate(force:true);
					if (GUI.Button(toolbarLayout.Inset(110,padding:0), "Force Generate All", EditorStyles.toolbarButton)) 
					{ 
						mapMagic.ClearResults();  

						if (MapMagic.instance != null)
							foreach (Chunk chunk in MapMagic.instance.chunks.All())
								if (chunk.terrain != null) chunk.terrain.transform.RemoveChildren();
							
						mapMagic.Generate(force:true); 
					}

					//seed field
					toolbarLayout.Inset(10);
					Rect seedLabelRect = toolbarLayout.Inset(34); seedLabelRect.y+=1; seedLabelRect.height-=4;
					Rect seedFieldRect = toolbarLayout.Inset(64); seedFieldRect.y+=2; seedFieldRect.height-=4;
				}
				else
				{
					Rect nameLabelRect = toolbarLayout.Inset(300); nameLabelRect.y+=1; //nameLabelRect.height-=4;
					EditorGUI.LabelField(nameLabelRect, "External data '" + AssetDatabase.GetAssetPath(gens) + "'", EditorStyles.miniLabel); 
				}

				//right part
				toolbarLayout.Inset(toolbarLayout.field.width - toolbarLayout.cursor.x - 150 - 22,padding:0);

				//drawing exit biome button
				Rect biomeRect = toolbarLayout.Inset(80, padding:0);
				if (gensBiomeHierarchy.Count>1) 
				{
					if (toolbarLayout.Button("", biomeRect, icon:"MapMagic_ExitBiome", style:EditorStyles.toolbarButton)) 
					{
						gensBiomeHierarchy.RemoveAt(gensBiomeHierarchy.Count-1);
						Repaint();
						return;
					}

					toolbarLayout.Label("Exit Biome", new Rect(toolbarLayout.cursor.x-60, toolbarLayout.cursor.y+3, 60, toolbarLayout.cursor.height), fontSize:9);
				}

				//focus button 
				
			//	if (GUI.Button(script.toolbarLayout.Inset(100,padding:0), "Focus", EditorStyles.toolbarButton)) FocusOnGenerators();
				if (toolbarLayout.Button("", toolbarLayout.Inset(23,padding:0), icon:"MapMagic_Focus", style:EditorStyles.toolbarButton)) FocusOnGenerators();
				
				if (toolbarLayout.Button("", toolbarLayout.Inset(47,padding:0), icon:"MapMagic_Zoom", style:EditorStyles.toolbarButton)) gens.layout.zoom=1;
				toolbarLayout.Label((int)(gens.layout.zoom*100)+"%", new Rect(toolbarLayout.cursor.x-28, toolbarLayout.cursor.y+3, 28, toolbarLayout.cursor.height), fontSize:8);  
				
				toolbarLayout.Inset(3, margin:0);
				toolbarLayout.Label("",  toolbarLayout.Inset(22, margin:0), url:"https://gitlab.com/denispahunov/mapmagic/wikis/Editor%20Window", icon:"MapMagic_Help"); 

			#endregion

			#region Draging

				//dragging generators
				for(int i=gens.list.Length-1; i>=0; i--)
				{
					Generator gen = gens.list[i];
					if (gen is Group) continue;
					gen.layout.field = gen.guiRect;

					//dragging
					if (gens.layout.DragDrop(gen.layout.field, i)) 
					{
						if (gens.layout.dragState == Layout.DragState.Pressed) 
						{
							Undo.RecordObject (gens, "MapMagic Generators Drag");
							gens.setDirty = !gens.setDirty;
						}
						if (gens.layout.dragState == Layout.DragState.Drag || gens.layout.dragState == Layout.DragState.Released) 
						{ 
							//gen.Move(gens.layout.dragDelta,true);
							
							gen.layout.field.position += gens.layout.dragDelta;
							gen.guiRect = gens.layout.field;

							//moving inouts to remove lag
							foreach (Generator.IGuiInout inout in gen.Inouts()) 
								inout.guiRect = new Rect(inout.guiRect.position+gens.layout.dragDelta, inout.guiRect.size);

							//moving group
							if (gen is Group)
							{
								Group group = gen as Group;
								for (int g=0; g<group.generators.Count; g++) //group.generators[g].Move(delta,false);
								{
									group.generators[g].layout.field.position += gens.layout.dragDelta;
									group.generators[g].guiRect = gens.layout.field;

									foreach (Generator.IGuiInout inout in group.generators[g].Inouts())  //moving inouts to remove lag
										inout.guiRect = new Rect(inout.guiRect.position+gens.layout.dragDelta, inout.guiRect.size);
								}
							}

							repaint=true; Repaint(); 

							EditorUtility.SetDirty(gens);
						}
					}

					//saving all generator rects
					gen.guiRect = gen.layout.field;
				}

				//dragging groups
				for (int i=gens.list.Length-1; i>=0; i--)
				{
					//Generator gen = gens.list[i];
					Group group = gens.list[i] as Group;
					if (group == null) continue;
					group.layout.field = group.guiRect;

					//resizing
					group.layout.field =gens.layout.ResizeRect(group.layout.field, i+20000);

					//dragging
					if (gens.layout.DragDrop(group.layout.field, i)) 
					{
						if (gens.layout.dragState == Layout.DragState.Pressed) 
						{
							Undo.RecordObject (gens, "MapMagic Group Drag");
							gens.setDirty = !gens.setDirty;
							group.Populate(gens);
						}
						if (gens.layout.dragState == Layout.DragState.Drag || gens.layout.dragState == Layout.DragState.Released) 
						{ 
							//group.Move(gens.layout.dragDelta,true);
							
							group.layout.field.position += gens.layout.dragDelta;
							group.guiRect = gens.layout.field;

							for (int g=0; g<group.generators.Count; g++) //group.generators[g].Move(delta,false);
							{
								group.generators[g].layout.field.position += gens.layout.dragDelta;
								group.generators[g].guiRect.position += gens.layout.dragDelta; // = gens.layout.field;

					//			foreach (Generator.IGuiInout inout in group.generators[g].Inouts())  //moving inouts to remove lag
					//				inout.guiRect = new Rect(inout.guiRect.position+gens.layout.dragDelta, inout.guiRect.size);
							}

							repaint=true; Repaint(); 

							EditorUtility.SetDirty(gens);
						}
						if (gens.layout.dragState == Layout.DragState.Released && group != null) gens.SortGroups();
					}

					//saving all group rects
					group.guiRect = group.layout.field;
				}

			#endregion

			//right-click menus
			if (Event.current.type == EventType.ContextClick || (Event.current.type == EventType.MouseDown && Event.current.control)) DrawPopup();

			//debug center
			//EditorGUI.HelpBox(script.layout.ToLocal(new Rect(-25,-10,50,20)), "Zero", MessageType.None);

			//assigning portal popup action
			Portal.OnChooseEnter -= DrawPortalSelector; Portal.OnChooseEnter += DrawPortalSelector;

			//saving scroll and zoom
			gens.guiScroll = gens.layout.scroll; gens.guiZoom = gens.layout.zoom;  

			DrawDemoLock();

		}

		public void DrawPopup ()
		{
			//if (MapMagic.instance.guiGens == null) MapMagic.instance.guiGens = MapMagic.instance.gens;
			//GeneratorsAsset gens = MapMagic.instance.guiGens;
			//if (MapMagic.instance.guiGens != null) gens = MapMagic.instance.guiGens;
			
			Vector2 mousePos = gens.layout.ToInternal(Event.current.mousePosition);
				
			//finding something that was clicked
			Generator clickedGenerator = null;
			Group clickedGroup = null;
			Generator.Output clickedOutput = null;

			for (int i=0; i<gens.list.Length; i++) 
			{
				Generator gen = gens.list[i];
				if (gen.guiRect.Contains(mousePos))
				{
					if (!(gen is Group)) clickedGenerator = gens.list[i];
					else clickedGroup = gens.list[i] as Group;
				}
				
				foreach (Generator.Output output in gens.list[i].Outputs())
					if (output.guiRect.Contains(mousePos)) clickedOutput = output; 
			}
			if (clickedGenerator == null) clickedGenerator = clickedGroup;
			
			//create
			Dictionary<string, PopupMenu.MenuItem> itemsDict = new Dictionary<string, PopupMenu.MenuItem>();
			
			foreach (System.Type type in typeof(Generator).Subtypes())
			{
				if (System.Attribute.IsDefined(type, typeof(GeneratorMenuAttribute)))
				{
					GeneratorMenuAttribute attribute = System.Attribute.GetCustomAttribute(type, typeof(GeneratorMenuAttribute)) as GeneratorMenuAttribute;
					System.Type genType = type;

					if (attribute.disabled) continue;

					PopupMenu.MenuItem item = new PopupMenu.MenuItem(attribute.name, delegate () { CreateGenerator(genType, mousePos); });
					item.priority = attribute.priority;

					if (attribute.menu.Length != 0)
					{
						if (!itemsDict.ContainsKey(attribute.menu)) itemsDict.Add(attribute.menu, new PopupMenu.MenuItem(attribute.menu, subs:new PopupMenu.MenuItem[0]));
						ArrayTools.Add(ref itemsDict[attribute.menu].subItems, createElement:() => item);
					}
					else itemsDict.Add(attribute.name, item);
				}
			} 

			itemsDict["Map"].priority = 1;
			itemsDict["Objects"].priority = 2;
			itemsDict["Output"].priority = 3;
			itemsDict["Portal"].priority = 4;
			itemsDict["Group"].priority = 5;
			itemsDict["Biome"].priority = 6;
			itemsDict["Legacy"].priority = 7;

			PopupMenu.MenuItem[] createItems = new PopupMenu.MenuItem[itemsDict.Count];
			itemsDict.Values.CopyTo(createItems, 0);

			//create group
			//PopupMenu.MenuItem createGroupItem = new PopupMenu.MenuItem("Group",  delegate () { CreateGroup(mousePos); });
			//Extensions.ArrayAdd(ref createItems, createItems.Length-1, createGroupItem);

			//additional name
			/*string additionalName = "All";
			if (clickedGenerator != null) 
			{
				additionalName = "Generator";
				if (clickedGenerator is Group) additionalName = "Group";
			}*/

			//preview
			PopupMenu.MenuItem[] previewSubs = new PopupMenu.MenuItem[]
			{
				new PopupMenu.MenuItem("On Terrain", delegate() {PreviewOutput(clickedGenerator, clickedOutput, false);}, disabled:clickedOutput==null||clickedGenerator==null, priority:0), 
				new PopupMenu.MenuItem("In Window", delegate() {PreviewOutput(clickedGenerator, clickedOutput, true);}, disabled:clickedOutput==null||clickedGenerator==null, priority:1),
				new PopupMenu.MenuItem("Clear", delegate() {PreviewOutput(null, null, false);}, priority:2 )//, disabled:MapMagic.instance.previewOutput==null)
			};

			PopupMenu.MenuItem[] popupItems = new PopupMenu.MenuItem[]
			{
				new PopupMenu.MenuItem("Create", createItems, priority:0),
				new PopupMenu.MenuItem("Export",	delegate () { ExportGenerator(clickedGenerator, mousePos); }, priority:10),
				new PopupMenu.MenuItem("Import",	delegate () { ImportGenerator(mousePos); }, priority:20),
				new PopupMenu.MenuItem("Duplicate",	delegate () { DuplicateGenerator(clickedGenerator); }, priority:30),
				new PopupMenu.MenuItem("Update",	delegate () { UpdateGenerator(clickedGenerator); }, disabled:clickedGenerator==null || !CouldBeUpdated(clickedGenerator), priority:40),
				new PopupMenu.MenuItem("Remove",	delegate () { if (clickedGenerator!=null) DeleteGenerator(clickedGenerator); },	disabled:clickedGenerator==null, priority:50),
				new PopupMenu.MenuItem("Reset",		delegate () { if (clickedGenerator!=null) ResetGenerator(clickedGenerator); },	disabled:clickedGenerator==null, priority:60), 
				new PopupMenu.MenuItem("Preview", previewSubs, priority:70)
			};

			if (onDrawPopup != null)
				popupItems = onDrawPopup(popupItems, mousePos, gens, clickedGenerator, clickedGroup, clickedOutput);

			PopupMenu.DrawPopup(popupItems, Event.current.mousePosition, closeAllOther:true);
		}

		public void DrawPortalSelector (Portal exit, Generator.InoutType type)
		{
			//if (MapMagic.instance.guiGens == null) MapMagic.instance.guiGens = MapMagic.instance.gens;
			//GeneratorsAsset gens = MapMagic.instance.guiGens;
			//if (MapMagic.instance.guiGens != null) gens = MapMagic.instance.guiGens;

			int entersNum = 0;
			for (int g=0; g<gens.list.Length; g++)
			{
				Portal portal = gens.list[g] as Portal;
				if (portal == null) continue;
				if (portal.form == Portal.PortalForm.Out) continue;
				if (portal.type != type) continue;

				entersNum++;
			}
			
			PopupMenu.MenuItem[] popupItems = new PopupMenu.MenuItem[entersNum];
			int counter = 0;
			for (int g=0; g<gens.list.Length; g++)
			{
				Portal enter = gens.list[g] as Portal;
				if (enter == null) continue;
				if (enter.form == Portal.PortalForm.Out) continue;
				if (enter.type != type) continue;

				popupItems[counter] = new PopupMenu.MenuItem( enter.name, delegate () 
					{ 
						if (gens.CheckDependence(exit,enter)) { Debug.LogError("MapMagic: Linking portals this way will create dependency loop."); return; }
						exit.input.Link(enter.output, enter); 
						if (mapMagic!=null) 
						{
							mapMagic.ClearResults(exit);
							mapMagic.Generate();
						}
					} );
				counter++;
			}

			PopupMenu.DrawPopup(popupItems, Event.current.mousePosition, closeAllOther:true);

		}

		public void FocusOnGenerators ()
		{
			//if (MapMagic.instance == null) MapMagic.instance = FindObjectOfType<MapMagic>();
			//if (MapMagic.instance.guiGens == null) MapMagic.instance.guiGens = MapMagic.instance.gens;
			
			//finding generators center
			Vector2 min = new Vector2(2000000,2000000); Vector2 max = new Vector2(-2000000,-2000000);
			for (int g=0; g<gens.list.Length; g++)
			{
				Generator gen = gens.list[g];
				if (gen.guiRect.x<min.x) min.x = gen.guiRect.x;
				if (gen.guiRect.y<min.y) min.y = gen.guiRect.y;
				if (gen.guiRect.max.x>max.x) max.x = gen.guiRect.max.x;
				if (gen.guiRect.max.y>max.y) max.y = gen.guiRect.max.y;
			}
			Vector2 center = (min+max)/2f;

			//focusing
			//center =script.layout.ToDisplay(center);
		//	center *= MapMagic.instance.guiZoom;
		//	MapMagic.instance.layout.scroll = -center;
		//	MapMagic.instance.layout.scroll += new Vector2(this.position.width/2f, this.position.height/2f);
			gens.layout.Focus(center);

			

			/*if (script.layout == null)script.layout = new Layout();
			//center =script.layout.ToDisplay(center);
			layout.scroll = -outputRect.center;
			layout.scroll.y += this.position.height / 2;
			layout.scroll.x += this.position.width - outputRect.width; 

			//saving
			if (script==null) script = FindObjectOfType<MapMagic>();
			script.guiScroll =script.layout.scroll; script.guiZoom =script.layout.zoom; //saving*/
		}

		public static void Show (GeneratorsAsset gens, IMapMagic mapMagic, bool forceOpen=true, bool asBiome=false)
		{
			//opening if force open
			if (forceOpen)
				instance = (MapMagicWindow)EditorWindow.GetWindow (typeof (MapMagicWindow));
			
			//finding instance
			if (instance == null)
			{
				MapMagicWindow[] windows = Resources.FindObjectsOfTypeAll<MapMagicWindow>();
				if (windows.Length==0) return;
				instance = windows[0];
			}
			
			instance.mapMagic = mapMagic; 
			if (!asBiome) instance.gensBiomeHierarchy.Clear();
			instance.gensBiomeHierarchy.Add(gens);
		
			instance.Show();
			instance.Repaint();
		}

		#region Demo lock
		public bool demoLock {get{ return false; }}
		public bool changeLock {get{ return false; }}
		public void DrawDemoLock () { }
		#endregion

	}

}//namespace