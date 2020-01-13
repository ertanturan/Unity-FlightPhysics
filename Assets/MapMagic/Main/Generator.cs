using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

using MapMagic;

namespace MapMagic 
{
	public class GeneratorMenuAttribute : System.Attribute
	{
		public string menu { get; set; }
		public string name { get; set; }
		public bool disengageable { get; set; }
		public bool disabled { get; set; }
		public int priority { get; set; }
		public string helpLink { get; set; }
		public Type updateType { get; set; }
	}

	[System.Serializable]
	public abstract class Generator
	{
		#region Inout

			public enum InoutType { Map, Objects, Spline, Voxel }

			public interface IGuiInout
			{
				Rect guiRect { get; set; }
				//string guiName { get; set; }
				//Vector2 guiConnectionPos { get; }

				//void DrawIcon (Layout layout, bool drawLabel);
			}

			public class Input : IGuiInout
			{
				public InoutType type;

				//linking
				public Output link; //{get;}
				public Generator linkGen; //{get;}

				//gui
				public Rect guiRect { get; set; }
				//public string guiName { get; set; }
				public Color guiColor 
				{get{
					bool isProSkin = false;
					#if UNITY_EDITOR
					if (UnityEditor.EditorGUIUtility.isProSkin) isProSkin = true;
					#endif

					switch (type)
					{
						//old skin
						case InoutType.Map: return isProSkin? new Color(0.0f, 0.5f, 0.9f) : new Color(0.0f, 0.34f, 0.5f);
						case InoutType.Objects: return isProSkin? new Color(0.0f, 0.76f, 0.0f) : new Color(0.0f, 0.5f, 0.0f);
						case InoutType.Spline: return isProSkin? new Color(0.9f, 0.5f, 0.0f) : new Color(0.63f, 0.296f, 0.0f);
						
						//20 skin
						//case InoutType.Map: return isProSkin? new Color(0f, 0.55f, 0.8f) : new Color(0.0f, 0.325f, 0.5f);
						//case InoutType.Objects: return isProSkin? new Color(0.1f, 0.75f, 0.0f) : new Color(0.07f, 0.45f, 0.0f);
						
						default: return Color.black; 
					}
				}}
				public Vector2 guiConnectionPos {get{ return new Vector2(guiRect.xMin+1, guiRect.center.y); }}

				public void DrawIcon (Layout layout, string label=null, bool mandatory=false, bool setRectOnly=false)
				{ 
					string textureName = "";
					switch (type) 
					{ 
						case InoutType.Map: textureName = "MapMagicMatrix"; break;
						case InoutType.Objects: textureName = "MapMagicScatter"; break;
						case InoutType.Spline: textureName = "MapMagicSpline"; break;
					}

					guiRect = new Rect(layout.field.x-5, layout.cursor.y+layout.field.y, 18,18);
					if (!setRectOnly) layout.Icon(textureName,guiRect);

					if (label != null)
					{
						Rect nameRect = guiRect;
						nameRect.width = 100; nameRect.x += guiRect.width + 2;
						layout.Label(label, nameRect,  fontSize:10);
					}

					if (mandatory && linkGen==null) 
						layout.Icon("MapMagic_Mandatory", new Rect (guiRect.x+10+2, guiRect.y-2, 8,8));
				}


				public Input () {} //default constructor to create with activator
				public Input (InoutType t) { type = t; }
				public Input (string n, InoutType t, bool write=false, bool mandatory=false) { type = t; } //lagacy constructor
			
				public Generator GetGenerator (Generator[] gens) 
				{
					for (int g=0; g<gens.Length; g++) 
						foreach (Input input in gens[g].Inputs())
							if (input == this) return gens[g];
					return null;
				}
				public void Link (Output output, Generator outputGen) { 
				link = output; linkGen = outputGen; }
				public void Unlink () { link = null; linkGen = null; }

				public object GetObject (Chunk.Results tw)
				{ 
					if (link == null) return null;
					if (!tw.results.ContainsKey(link)) return null;
					return tw.results[link];
				}

				public T GetObject<T> (Chunk.Results tw) where T : class
				{
					if (link == null) return null;
					if (!tw.results.ContainsKey(link)) return null;
					object obj = tw.results[link];
					if (obj == null) return null;
					else return (T)obj;
				}

				//serialization
				/*public string Encode (System.Func<object,int> writeClass)
				{
					//return "offsetX=" + offset.x + " offsetZ=" + offset.z + " sizeX=" + size.x + " sizeZ=" + size.z; }
					//public void Decode (string[] lineMembers) { offset.x=(int)lineMembers[2].Parse(typeof(int)); offset.z=(int)lineMembers[3].Parse(typeof(int));
					//size.x=(int)lineMembers[4].Parse(typeof(int)); size.z=(int)lineMembers[5].Parse(typeof(int)); }
					return null;
				}

				public void Decode (string[] lineMembers, System.Func<int,object> readClass) 
				{ 
				
				}*/
			}

			public class Output : IGuiInout
			{
				public InoutType type;
				
				//gui
				public Rect guiRect { get; set; }
				public Vector2 guiConnectionPos {get{ return new Vector2(guiRect.xMax-1, guiRect.center.y); }}
				
				public void DrawIcon (Layout layout, string label=null, bool setRectOnly=false, bool debug=false) 
				{ 
					string textureName = "";
					switch (type) 
					{ 
						case InoutType.Map: textureName = "MapMagicMatrix"; break;
						case InoutType.Objects: textureName = "MapMagicScatter"; break;
						case InoutType.Spline: textureName = "MapMagicSpline"; break;
					}

					guiRect = new Rect(layout.field.x+layout.field.width-18+5, layout.cursor.y+layout.field.y, 18,18);

					if (label!=null)
					{
						Rect nameRect = guiRect;
						nameRect.width = 100; nameRect.x-= 103;
						layout.Label(label, nameRect, textAnchor:TextAnchor.LowerRight, fontSize:10);
					}

					if (!setRectOnly) layout.Icon(textureName, guiRect); //detail:resolution.ToString());

					//drawing obj id
					#if WDEBUG
					if (MapMagic.instance!=null)
					{
						Rect idRect = guiRect;
						idRect.width = 100; idRect.x += 25;
						Chunk closest = MapMagic.instance.chunks.GetClosestObj(new Coord(0,0));
						if (closest != null)
						{
							object obj = closest.results.results.CheckGet(this);
							layout.Label(obj!=null? obj.GetHashCode().ToString() : "null", idRect, textAnchor:TextAnchor.LowerLeft);
						}
					}
					#endif
				}

				public Output () {} //default constructor to create with activator
				public Output (InoutType t) { type = t; }
				public Output (string n, InoutType t) { type = t; } //legacy constructor

				public Generator GetGenerator (Generator[] gens) 
				{
					for (int g=0; g<gens.Length; g++) 
						foreach (Output output in gens[g].Outputs())
							if (output == this) return gens[g];
					return null;
				}

				public Input GetConnectedInput (Generator[] gens)
				{
					for (int g=0; g<gens.Length; g++) 
						foreach (Input input in gens[g].Inputs())
							if (input.link == this) return input;
					return null;
				}

				public void SetObject (Chunk.Results tw, object obj) //TODO: maybe better replace with CheckAdd
				{
					if (tw.results.ContainsKey(this))
					{
						if (obj == null) tw.results.Remove(this);
						else tw.results[this] = obj;
					}
					else
					{
						if (obj != null) tw.results.Add(this, obj);
					}
				}

				public T GetObject<T> (Chunk.Results tw) where T:class
				{
					if (!tw.results.ContainsKey(this)) return null;    
					object obj = tw.results[this];
					if (obj == null) return null;
					else return (T)obj;
				}

				public void UnlinkInActiveGens ()
				{
					//TODO20: each generator should have a link to gens

					object activeGensBoxed = Extensions.CallStaticMethodFrom("Assembly-CSharp-Editor", "MapMagic.MapMagicWindow", "GetGens", null);
					if (activeGensBoxed == null) return;
					GeneratorsAsset activeGens = activeGensBoxed as GeneratorsAsset;  

					Input connectedInput = GetConnectedInput(activeGens.list);
					if (connectedInput != null) connectedInput.Link(null, null);
				}
			}
		#endregion

		public bool enabled = true;
		public bool mandatory = false; //output or preview generator, should be generated with priors

		//gui
		[System.NonSerialized] public Layout layout = new Layout();
		public Rect guiRect; //just for serialization
		
		[System.NonSerialized] public int guiGenerateTime;
		public static Dictionary<System.Type, int> guiApplyTime = new Dictionary<System.Type, int>();
		public static Dictionary<System.Type, int> guiProcessTime = new Dictionary<System.Type, int>();

		[System.NonSerialized] public Biome biome; //assigned automatically in GeneratorsOfType enumerable

		//inputs and outputs
		public virtual IEnumerable<Output> Outputs() { yield break; }
		public virtual IEnumerable<Input> Inputs() { yield break; }
		public IEnumerable<IGuiInout> Inouts() 
		{ 
			foreach (Output i in Outputs()) yield return i; 
			foreach (Input i in Inputs()) yield return i;
		}



		public virtual void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null) {}

		//public static virtual void Process (Chunk chunk)

		//gui
		public void DrawHeader (IMapMagic mapMagic, GeneratorsAsset gens, bool debug=false)
		{
			//drawing header background
			layout.Icon("MapMagic_Window_Header", new Rect(layout.field.x, layout.field.y, layout.field.width, 16));

			//drawing eye icon
			layout.Par(14); layout.Inset(2);
			Rect eyeRect = layout.Inset(18);
			GeneratorMenuAttribute attribute = System.Attribute.GetCustomAttribute(GetType(), typeof(GeneratorMenuAttribute)) as GeneratorMenuAttribute;

			if (attribute != null && attribute.disengageable) 
				layout.Toggle(ref enabled, rect:eyeRect, onIcon:"MapMagic_GeneratorEnabled", offIcon:"MapMagic_GeneratorDisabled");
			else layout.Icon("MapMagic_GeneratorAlwaysOn", eyeRect, Layout.IconAligment.center, Layout.IconAligment.center);
			
			//drawing label
			string genName = "";
			#if WDEBUG
			if (mapMagic!=null)
			{
				int num = -1;
				for (int n=0; n<gens.list.Length; n++) if (gens.list[n]==this) num = n;
				genName += num + ". ";
			}
			#endif
			genName += attribute==null? "Unknown" : attribute.name;

			if (mapMagic!=null && debug && !mapMagic.IsGeneratorReady(this)) genName+="*";

			Rect labelRect = layout.Inset(layout.field.width-18-22); labelRect.height = 25; labelRect.y -= (1f-layout.zoom)*6 + 2;
			layout.Label(genName, labelRect, fontStyle:FontStyle.Bold, fontSize:19-layout.zoom*8);

			//drawing help link
			Rect helpRect = layout.Inset(22);
			if (attribute != null && attribute.helpLink != null && attribute.helpLink.Length != 0)
			{
				layout.Label("", helpRect, url:attribute.helpLink, icon:"MapMagic_Help");
				//if (layout.Button("", helpRect, icon:"MapMagic_Help")) Application.OpenURL(attribute.helpLink); 
				//UnityEditor.EditorGUIUtility.AddCursorRect (layout.ToDisplay(helpRect), UnityEditor.MouseCursor.Link);
			}

			layout.Par(4);
		}

		public abstract void OnGUI (GeneratorsAsset gens);
		/*public void OnGUIBase(IMapMagic mapMagic, GeneratorsAsset gens, bool debug=false)
		{
			//drawing background
			layout.Element("MapMagic_Window", layout.field, new RectOffset(34,34,34,34), new RectOffset(33,33,33,33));

			//resetting layout
			layout.field.height = 0;
			layout.field.width =160;
			layout.cursor = new Rect();
			layout.change = false;
			layout.margin = 1; layout.rightMargin = 1;
			layout.fieldSize = 0.4f;              

			//drawing window header
			if (MapMagic.instance!=null && MapMagic.instance.debug) Profiler.BeginSample("Header");
			layout.Icon("MapMagic_Window_Header", new Rect(layout.field.x, layout.field.y, layout.field.width, 16));

			//drawing eye icon
			layout.Par(14); layout.Inset(2);
			Rect eyeRect = layout.Inset(18);
			GeneratorMenuAttribute attribute = System.Attribute.GetCustomAttribute(GetType(), typeof(GeneratorMenuAttribute)) as GeneratorMenuAttribute;

			if (attribute != null && attribute.disengageable) 
				layout.Toggle(ref enabled, rect:eyeRect, onIcon:"MapMagic_GeneratorEnabled", offIcon:"MapMagic_GeneratorDisabled");
			else layout.Icon("MapMagic_GeneratorAlwaysOn", eyeRect, Layout.IconAligment.center, Layout.IconAligment.center);
			

			//drawing label
			string genName = "";
			if (mapMagic!=null && mapMagic.debug)
			{
				int num = -1;
				for (int n=0; n<gens.list.Length; n++) if (gens.list[n]==this) num = n;
				genName += num + ". ";
			}
			genName += attribute==null? "Unknown" : attribute.name;

			if (mapMagic!=null && mapMagic.debug && !mapMagic.IsGeneratorReady(this)) genName+="*";

			Rect labelRect = layout.Inset(); labelRect.height = 25; labelRect.y -= (1f-layout.zoom)*6 + 2;
			layout.Label(genName, labelRect, fontStyle:FontStyle.Bold, fontSize:19-layout.zoom*8);

			layout.Par(1);
			
			if (MapMagic.instance!=null && MapMagic.instance.debug) Profiler.EndSample();

			//gen params
			if (MapMagic.instance!=null && MapMagic.instance.debug) Profiler.BeginSample("Gen Params");
			layout.Par(3);
			if (!(MapMagic.instance!=null && MapMagic.instance.debug))
			{
				try {OnGUI(gens);}
				catch (System.Exception e) 
					{if (e is System.ArgumentOutOfRangeException || e is System.NullReferenceException) Debug.LogError("Error drawing generator " + GetType() + "\n" + e);}
			}
			else OnGUI(gens);
			layout.Par(3);
			if (MapMagic.instance!=null && MapMagic.instance.debug) Profiler.EndSample();

			//drawing debug generate time
			if (mapMagic!=null && mapMagic.debug)
			{
				Rect timerRect = new Rect(layout.field.x, layout.field.y+layout.field.height, 200, 20);
				string timeLabel = "g:" + guiGenerateTime + "ms ";
				if (this is OutputGenerator)
				{
					if (MapMagic.instance!=null && MapMagic.instance.guiDebugProcessTimes.ContainsKey(this.GetType())) timeLabel += " p:" + MapMagic.instance.guiDebugProcessTimes[this.GetType()] + "ms ";
					if (MapMagic.instance!=null && MapMagic.instance.guiDebugApplyTimes.ContainsKey(this.GetType())) timeLabel += " a:" + MapMagic.instance.guiDebugApplyTimes[this.GetType()] + "ms ";
				}
				layout.Label(timeLabel, timerRect);
				//EditorGUI.LabelField(gen.layout.ToLocal(timerRect), gen.timer.ElapsedMilliseconds + "ms");
			}
		}*/
	}

	[System.Serializable]
	[GeneratorMenu (menu="", name ="Portal", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/Portal", priority = 1)]
	public class Portal : Generator
	{
		public Input input = new Input(InoutType.Map);
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; }
		public override IEnumerable<Output> Outputs() { yield return output; }
		
		//public enum PortalType { enter, exit }
		//public PortalType type;

		public string name = "Portal";
		public InoutType type;
		public enum PortalForm { In, Out }
		public PortalForm form;
		public bool drawConnections;
		
		public delegate void ChooseEnter(Portal sender, InoutType type);
		public static event ChooseEnter OnChooseEnter;

		public bool drawInputConnection
		{get{
			bool result = false;
			if (form==PortalForm.In) result = true;
			else
			{
				if (drawConnections) result = true;
				if (input.linkGen != null && ((Portal)input.linkGen).drawConnections) result = true;
			}
			return result;
		}}

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			object obj = null;
			if (input.link != null && enabled) obj = input.GetObject(results);
			else 
			{ 
				if (type == InoutType.Map) obj = new Matrix(rect);
				if (type == InoutType.Objects) obj = new SpatialHash(new Vector2(rect.offset.x,rect.offset.z), rect.size.x, 16);
			}

			if (stop!=null && stop(0)) return;
			output.SetObject(results, obj); 
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			layout.margin = 18; layout.rightMargin = 15;
			layout.Par(17); 
			if (form==PortalForm.In) 
			{
				input.DrawIcon(layout); 
				if (drawConnections) output.DrawIcon(layout); 
				else output.guiRect=new Rect(guiRect.x+guiRect.width, guiRect.y+25,0,0);
			}
			if (form==PortalForm.Out)
			{
				if (drawConnections) input.DrawIcon(layout); 
				else input.guiRect=new Rect(guiRect.x, guiRect.y+25,0,0);
				output.DrawIcon(layout);
			}

			layout.Field(ref type, rect:layout.Inset(0.39f));
			if (type != input.type) { input.Unlink(); input.type = type; output.type = type; }
			if (layout.lastChange)
			{
				foreach (Portal portal in MapMagic.instance.gens.GeneratorsOfType<Portal>())
					if (portal.input.linkGen == this) portal.input.Unlink();
				//TODO: can't change type without MM instance
			} 

			layout.Field(ref form,rect:layout.Inset(0.30f));
			layout.CheckButton(ref drawConnections, label: "", rect:layout.Inset(20), monitorChange:false, icon:"MapMagic_ShowConnections", tooltip:"Show portal connections");
			if (layout.Button("", layout.Inset(20), monitorChange:false, icon:"MapMagic_Focus_Small", disabled:form==PortalForm.In, tooltip:"Focus on input portal") && 
				input.linkGen != null &&
				MapMagic.instance!=null)
			{
				MapMagic.instance.layout.Focus(input.linkGen.guiRect.center);
			}

			

			//select input/button
			layout.Par(20);
			if (form == PortalForm.In) name = layout.Field(name, rect:layout.Inset());
			if (form == PortalForm.Out)
			{
				string buttonLabel = "Select";
				if (input.linkGen != null) 
				{
					if (!(input.linkGen is Portal)) input.Link(null, null); //in case connected input portal was changet to output
					else buttonLabel = ((Portal)input.linkGen).name;
				}
				Rect buttonRect = layout.Inset();
				buttonRect.height -= 3;
				if (layout.Button(buttonLabel, rect:buttonRect) && OnChooseEnter!=null) OnChooseEnter(this, type);
			}
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="", name ="Group", priority = 2)]
	public class Group : Generator
	{
		public string name = "Group";
		public string comment = "Drag in generators to group them";
		public bool locked;

		[System.NonSerialized] public List<Generator> generators = new List<Generator>();


		public override void OnGUI (GeneratorsAsset gens) 
		{
			//initializing layout
			layout.cursor = new Rect();
			layout.change = false;

			//drawing background
			layout.Element("MapMagic_Group", layout.field, new RectOffset(16,16,16,16), new RectOffset(0,0,0,0));

			//lock sign
			/*Rect lockRect = new Rect(guiRect.x+guiRect.width-14-6, field.y+6, 14, 12); 
			layout.Icon(locked? "MapMagic_LockLocked":"MapMagic_LockUnlocked", lockRect, verticalAlign:Layout.IconAligment.center);
			bool wasLocked = locked;
			#if UNITY_EDITOR
			locked = UnityEditor.EditorGUI.Toggle(layout.ToDisplay(lockRect.Extend(3)), locked, GUIStyle.none);
			#endif
			if (locked && !wasLocked) LockContents();
			if (!locked && wasLocked) UnlockContents();*/

			//name and comment
			layout.margin = 5;
			layout.CheckStyles();
			float nameWidth = layout.boldLabelStyle.CalcSize( new GUIContent(name) ).x * 1.1f / layout.zoom + 10f;
			float commentWidth = layout.labelStyle.CalcSize( new GUIContent(comment) ).x / layout.zoom + 10;
			nameWidth = Mathf.Min(nameWidth,guiRect.width-5); commentWidth = Mathf.Min(commentWidth, guiRect.width-5);

			if (!locked)
			{
				layout.fontSize = 13; layout.Par(22); name = layout.Field(name, rect:layout.Inset(nameWidth), useEvent:true, style:layout.boldLabelStyle); 
				layout.fontSize = 11; layout.Par(18); comment = layout.Field(comment, rect:layout.Inset(commentWidth), useEvent:true, style:layout.labelStyle);
			}
			else
			{
				layout.fontSize = 13; layout.Par(22); layout.Label(name, rect:layout.Inset(nameWidth), fontStyle:FontStyle.Bold); 
				layout.fontSize = 11; layout.Par(18); layout.Label(comment, rect:layout.Inset(commentWidth)); 
			}
		}

		public void Populate (GeneratorsAsset gens)
		{
			generators.Clear();

			for (int i=0; i<gens.list.Length; i++)
			{
				Generator gen = gens.list[i];
				if (layout.field.Contains(gen.layout.field)) generators.Add(gen); 
			}
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="", name ="Biome", disengageable = true, priority = 3)]
	public class Biome : Generator
	{
		public Input mask = new Input(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return mask; }
		
		public GeneratorsAsset data;

		//get static actions using instance
		//public override Action<CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>> GetProces () { return null; }
		//public override System.Func<CoordRect, Terrain, object, Func<float,bool>, IEnumerator> GetApply () { return null; }
		//public override System.Action<CoordRect, Terrain> GetPurge () { return null; }

		public override void OnGUI (GeneratorsAsset gens)
		{
			layout.Par(20); mask.DrawIcon(layout, "Mask");
			layout.Par(5);

			layout.fieldSize = 0.7f;
			data = layout.ScriptableAssetField(data, construct:null);

			//drawing "Edit" button in mmwindow
		}

	}

}