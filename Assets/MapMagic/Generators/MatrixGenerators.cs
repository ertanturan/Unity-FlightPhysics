using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using MapMagic;

namespace MapMagic
{

	[System.Serializable]
	//[GeneratorMenu (menu="Map", name ="Test", disengageable = true, disabled = true, priority = 1)]
	public class TestGenerator : Generator
	{
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return output; }
		public int iterations = 100000;
		public float result;

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			Matrix matrix = new Matrix(rect);
			if (!enabled) { output.SetObject(results, matrix); return; }

			//testing matrix
			for (int x=0; x<matrix.rect.size.x; x++)
				for (int z=0; z<matrix.rect.size.z; z++)
					matrix[x+matrix.rect.offset.x, z+matrix.rect.offset.z] = 0.3f*x/matrix.rect.size.x*5f;// + 0.5f*z/matrix.rect.size.z;
			
			//testing performance

			//for (int i=iterations*1000-1; i>=0; i--) 
			//	{ result = InlineFn(result); result = InlineFn(result); result = InlineFn(result); result = InlineFn(result); result = InlineFn(result); }
				//{ result += 0.01f; result += 0.01f; result += 0.01f; result += 0.01f; result += 0.01f; }

			if (stop!=null && stop(0)) return; //do not write object is generating is stopped
			output.SetObject(results, matrix);
		}

		public float InlineFn (float input)
		{
			return input + 0.01f; 
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); output.DrawIcon(layout, "Output");
			layout.Par(5);
			
			//params
			//output.sharedResolution.guiResolution = layout.ComplexField(output.sharedResolution.guiResolution, "Output Resolution");
			layout.fieldSize = 0.55f;
			layout.Field(ref iterations, "K Iterations");
			layout.Field(ref result, "Result");
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Constant", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	public class ConstantGenerator : Generator
	{
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return output; }
		public float level;

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			Matrix matrix = new Matrix(rect);
			if (!enabled) { output.SetObject(results, matrix); return; }
			matrix.Fill(level);

			if (stop!=null && stop(0)) return; //do not write object is generating is stopped
			output.SetObject(results, matrix);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); output.DrawIcon(layout, "Output");
			layout.Par(5);
			
			//params
			//output.sharedResolution.guiResolution = layout.ComplexField(output.sharedResolution.guiResolution, "Output Resolution");
			layout.Field(ref level, "Value", max:1);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Noise", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/noise")]
	public class NoiseGenerator182 : Generator
	{
		public int seed = 12345;
		public float high = 1f;
		public float low = 0f;
		//public int octaves = 10;
		//public float persistence = 0.5f;
		public float size = 200f;
		public float detail = 0.5f;
		public float turbulence = 0f;
		public Coord offset = new Coord(0,0);
		
		public enum Type { Unity=0, Linear=1, Perlin=2, Simplex=3 };
		public Type type = Type.Unity;

		public Input input = new Input(InoutType.Map);
		public Input maskIn = new Input(InoutType.Map);
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; yield return maskIn; }
		public override IEnumerable<Output> Outputs() { yield return output; }

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			Matrix matrix = (Matrix)input.GetObject(results); if (matrix != null) matrix = matrix.Copy(null);
			if (matrix == null) matrix = new Matrix(rect);
			Matrix mask = (Matrix)maskIn.GetObject(results);
			if (!enabled) { output.SetObject(results, matrix); return; }
			if (stop!=null && stop(0)) return;

			Noise noise = new Noise(seed^this.seed, permutationCount:16384);

			//range
			float range = high - low;

			//number of iterations
			int iterations = (int)Mathf.Log(size,2) + 1; //+1 max size iteration

			Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
				{
					float val = noise.LegacyFractal(x+offset.x, z+offset.z, size/512f*terrainSize.resolution,iterations,detail,turbulence,(int)type);
					
					val = val*range + low;

					if (val < 0) val = 0; //before mask?
					if (val > 1) val = 1;

					if (mask!=null) val *= mask[x,z];

					matrix[x,z] += val;
				}

			if (stop!=null && stop(0)) return; //do not write object is generating is stopped
			output.SetObject(results, matrix);
		}


		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); input.DrawIcon(layout, "Input"); output.DrawIcon(layout, "Output");
			layout.Par(20); maskIn.DrawIcon(layout, "Mask"); 
			layout.Par(5);
			
			//params
			layout.fieldSize = 0.6f;
			//output.sharedResolution.guiResolution = layout.ComplexField(output.sharedResolution.guiResolution, "Output Resolution");
			layout.Field(ref type, "Type");
			layout.Field(ref seed, "Seed");
			layout.Field(ref high, "High (Intensity)");
			layout.Field(ref low, "Low");
			//layout.Field(ref octaves, "Octaves", min:1);
			//layout.Field(ref persistence, "Persistance");
			layout.Field(ref size, "Size", min:1);
			layout.Field(ref detail, "Detail", min:0,max:0.8f);
			layout.Field(ref turbulence, "Turbulence");
			layout.Field(ref offset, "Offset");
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Voronoi", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/voronoi")]
	public class VoronoiGenerator1 : Generator
	{
		public float intensity = 1f;
		public int cellCount = 16;
		public float uniformity = 0;
		public int seed = 12345;
		public enum BlendType { flat, closest, secondClosest, cellular, organic }
		public BlendType blendType = BlendType.cellular;
		
		public Input input = new Input(InoutType.Map);
		public Input maskIn = new Input(InoutType.Map);
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; yield return maskIn; }
		public override IEnumerable<Output> Outputs() { yield return output; }


		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			Matrix matrix = (Matrix)input.GetObject(results); if (matrix != null) matrix = matrix.Copy(null);
			if (matrix == null) matrix = new Matrix(rect);
			Matrix mask = (Matrix)maskIn.GetObject(results);
			if (stop!=null && stop(0)) return;
			if (!enabled || intensity==0 || cellCount==0) { output.SetObject(results, matrix); return; } 

			//NoiseGenerator.Noise(matrix,200,0.5f,Vector2.zero);
			//matrix.Multiply(amount);

			InstanceRandom random = new InstanceRandom(seed + this.seed); //should be ^, plus for compatibility reasons
	
			//creating point matrix
			float cellSize = 1f * matrix.rect.size.x / cellCount; //TODO: check whether rect size or terrain res should be used
			Matrix2<Vector3> points = new Matrix2<Vector3>( new CoordRect(0,0,cellCount+2,cellCount+2) );
			points.rect.offset = new Coord(-1,-1);
			float finalIntensity = intensity * cellCount / matrix.rect.size.x * 26; //backward compatibility factor

			Coord matrixSpaceOffset = new Coord((int)(matrix.rect.offset.x/cellSize), (int)(matrix.rect.offset.z/cellSize));
		
			//scattering points
			for (int x=-1; x<points.rect.size.x-1; x++)
				for (int z=-1; z<points.rect.size.z-1; z++)
				{
					Vector3 randomPoint = new Vector3(x+random.CoordinateRandom(x+matrixSpaceOffset.x,z+matrixSpaceOffset.z), 0, z+random.NextCoordinateRandom());
					Vector3 centerPoint = new Vector3(x+0.5f,0,z+0.5f);
					Vector3 point = randomPoint*(1-uniformity) + centerPoint*uniformity;
					point = point*cellSize + new Vector3(matrix.rect.offset.x, 0, matrix.rect.offset.z);
					point.y = random.NextCoordinateRandom();
					points[x,z] = point;
				}

			Coord min = matrix.rect.Min; Coord max = matrix.rect.Max; 
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				//finding current cell
				Coord cell = new Coord((int)((x-matrix.rect.offset.x)/cellSize), (int)((z-matrix.rect.offset.z)/cellSize));
		
				//finding min dist
				float minDist = 200000000; float secondMinDist = 200000000;
				float minHeight = 0; //float secondMinHeight = 0;
				for (int ix=-1; ix<=1; ix++)
					for (int iz=-1; iz<=1; iz++)
				{
					Coord nearCell = new Coord(cell.x+ix, cell.z+iz);
					//if (!points.rect.CheckInRange(nearCell)) continue; //no need to perform test as points have 1-cell border around matrix

					Vector3 point = points[nearCell];
					float dist = (x-point.x)*(x-point.x) + (z-point.z)*(z-point.z);
					if (dist<minDist) 
					{ 
						secondMinDist = minDist; minDist = dist; 
						minHeight = point.y;
					}
					else if (dist<secondMinDist) secondMinDist = dist; 
				}

				float val = 0;
				switch (blendType)
				{
					case BlendType.flat: val = minHeight; break;
					case BlendType.closest: val = minDist / (rect.size.x*16); break;  //(MapMagic.instance.resolution*16);
					case BlendType.secondClosest: val = secondMinDist / (rect.size.x*16); break;
					case BlendType.cellular: val = (secondMinDist-minDist) / (rect.size.x*16); break;
					case BlendType.organic: val = (secondMinDist+minDist)/2 / (rect.size.x*16); break;
				}
				if (mask==null) matrix[x,z] += val*finalIntensity;
				else matrix[x,z] += val*finalIntensity*mask[x,z];
			}

			if (stop!=null && stop(0)) return; //do not write object is generating is stopped
			output.SetObject(results, matrix);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); input.DrawIcon(layout, "Input"); output.DrawIcon(layout, "Output");
			layout.Par(20); maskIn.DrawIcon(layout, "Mask");
			layout.Par(5);
			
			//params
			layout.fieldSize = 0.5f;
			layout.Field(ref blendType, "Type");
			layout.Field(ref intensity, "Intensity");
			layout.Field(ref cellCount, "Cell Count", min:1, max:128); cellCount = Mathf.ClosestPowerOfTwo(cellCount);
			layout.Field(ref uniformity, "Uniformity", min:0, max:1);
			layout.Field(ref seed, "Seed");
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Simple Form", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/simple_form")]
	public class SimpleForm1 : Generator
	{
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return output; }

		public enum FormType { GradientX, GradientZ, Pyramid, Cone }
		public FormType type = FormType.Cone;
		public float intensity = 1;
		public float scale = 1;
		public Vector2 offset;
		public Matrix.WrapMode wrap = Matrix.WrapMode.Once;

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			if (!enabled || (stop!=null && stop(0))) return;
			
			Matrix matrix = new Matrix(rect);
			Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;
			
			float pixelSize = terrainSize.pixelSize;
			float ssize = matrix.rect.size.x; //scaled rect size //TODO: check whether rect size or terrain res should be used
			Vector2 center = new Vector2(matrix.rect.size.x/2f, matrix.rect.size.z/2f);
			float radius = matrix.rect.size.x / 2f;

			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				float sx = (x - offset.x/pixelSize)/scale;
				float sz = (z - offset.y/pixelSize)/scale;

				if (wrap==Matrix.WrapMode.Once && (sx<0 || sx>=ssize || sz<0 || sz>=ssize)) { matrix[x,z] = 0; continue; }
				else if (wrap==Matrix.WrapMode.Clamp)
				{
					if (sx<0) sx=0; if (sx>=ssize) sx=ssize-1;
					if (sz<0) sz=0; if (sz>=ssize) sz=ssize-1;
				}
				else if (wrap==Matrix.WrapMode.Tile)
				{
					sx = sx % ssize; if (sx<0) sx= ssize + sx;
					sz = sz % ssize; if (sz<0) sz= ssize + sz;
				}
				else if (wrap==Matrix.WrapMode.PingPong)
				{
					sx = sx % (ssize*2); if (sx<0) sx=ssize*2 + sx; if (sx>=ssize) sx = ssize*2 - sx - 1;
					sz = sz % (ssize*2); if (sz<0) sz=ssize*2 + sz; if (sz>=ssize) sz = ssize*2 - sz - 1;
				}
				
				float val = 0;
				switch (type)
				{
					case FormType.GradientX:
						val = sx/ssize;
						break;
					case FormType.GradientZ:
						val = sz/ssize;
						break;
					case FormType.Pyramid:
						float valX = sx/ssize; if (valX > 1-valX) valX = 1-valX;
						float valZ = sz/ssize; if (valZ > 1-valZ) valZ = 1-valZ;
						val = valX<valZ? valX*2 : valZ*2;
						break;
					case FormType.Cone:
						val = 1 - ((center-new Vector2(sx,sz)).magnitude)/radius;
						if (val<0) val = 0;
						break;
				}

				matrix[x,z] = val*intensity;
			}

			if (stop!=null && stop(0)) return;
			output.SetObject(results, matrix);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); output.DrawIcon(layout, "Output");
			layout.Par(5);
			
			layout.fieldSize = 0.62f;
			layout.Field(ref type, "Type");
			layout.Field(ref intensity, "Intensity");
			layout.Field(ref scale, "Scale");
			layout.Field(ref offset, "Offset");
			layout.Field(ref wrap, "Wrap Mode");
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Raw Input", disengageable = true, disabled = false, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/raw_input")]
	public class RawInput1 : Generator
	{
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return output; }

		public MatrixAsset refMatrixAsset;
		public float intensity = 1;
		public float scale = 1;
		public Vector2 offset;
		public Matrix.WrapMode wrapMode = Matrix.WrapMode.Once;

		//gui
		[System.NonSerialized] public Texture2D preview; 
		public string texturePath;  
		
		//outdated
		public Matrix textureMatrix; 
		//public Matrix previewMatrix;
		public bool tile = false;

		public void ImportRaw (string path=null)
		{
			#if UNITY_EDITOR
			//importing
			if (path==null) path = UnityEditor.EditorUtility.OpenFilePanel("Import Texture File", "", "raw,r16");
			if (path==null || path.Length==0) return;

			if (MapMagic.instance != null)
			{
				UnityEditor.Undo.RecordObject(MapMagic.instance.gens, "MapMagic Open RAW");
				MapMagic.instance.gens.setDirty = !MapMagic.instance.gens.setDirty;
			}

			//creating ref matrix
			Matrix matrix = new Matrix( new CoordRect(0,0,1,1) );
			matrix.ImportRaw(path);
			texturePath = path;

			//saving asset
			if (refMatrixAsset == null) refMatrixAsset = ScriptableObject.CreateInstance<MatrixAsset>();
			refMatrixAsset.matrix = matrix;

			//generating preview
			CoordRect previewRect = new CoordRect(0,0, 128, 128);
			refMatrixAsset.preview = matrix.Resize(previewRect);
			preview = null;

			if (MapMagic.instance != null)
				UnityEditor.EditorUtility.SetDirty(MapMagic.instance.gens);
			#endif
		}

		public void RefreshPreview ()
		{
			if (refMatrixAsset!=null && refMatrixAsset.preview!=null) preview = refMatrixAsset.preview.SimpleToTexture();
			else preview = Extensions.ColorTexture(2,2,Color.black);
		}

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			if ((stop!=null && stop(0)) || !enabled || refMatrixAsset==null || refMatrixAsset.matrix==null) return;

			//loading ref matrix
			if (textureMatrix!=null && refMatrixAsset==null)
			{
				refMatrixAsset = ScriptableObject.CreateInstance<MatrixAsset>();
				refMatrixAsset.matrix = textureMatrix;
				textureMatrix = null;
			}
			Matrix refMatrix = refMatrixAsset.matrix;

			Matrix matrix = new Matrix(rect);
			Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;
			float pixelSize = terrainSize.pixelSize;

			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				float sx = (x-offset.x/pixelSize)/scale * refMatrix.rect.size.x/matrix.rect.size.x;
				float sz = (z-offset.y/pixelSize)/scale * refMatrix.rect.size.z/matrix.rect.size.z;

				matrix[x,z] = refMatrix.GetInterpolated(sx, sz, wrapMode) * intensity;
			}

			if (scale >= 2f)
			{
				Matrix cpy = matrix.Copy();
				for (int i=1; i<scale-1; i+=2) matrix.Blur();
				Matrix.SafeBorders(cpy, matrix, Mathf.Max(matrix.rect.size.x/128, 4));
			}
			
			if (stop!=null && stop(0)) return;
			output.SetObject(results, matrix);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); output.DrawIcon(layout, "Output");
			layout.Par(5); 

			//loading ref matrix
			if (textureMatrix!=null && refMatrixAsset==null)
			{
				refMatrixAsset = ScriptableObject.CreateInstance<MatrixAsset>();
				refMatrixAsset.matrix = textureMatrix;
				textureMatrix = null;
			}
			
			//preview texture
			layout.margin = 4;
			
			#if UNITY_EDITOR

			layout.fieldSize = 0.6f;
			layout.Field(ref refMatrixAsset, "Imported RAW");
			if (layout.lastChange) RefreshPreview();

			//warning
			bool rawSaved = refMatrixAsset==null || UnityEditor.AssetDatabase.Contains(refMatrixAsset);

			if (!rawSaved)
			{
				//layout.Par(65); layout.Label("It is recommended that the imported .RAW file be saved as a separate .ASSET file.", layout.Inset(), helpbox:true, messageType:2, fontSize:9); 
				layout.Par(85); layout.Label("Warning: If Graph is saved as a separate data imported RAW should be saved too, otherwise RAW FILE WILL NOT BE LOADED.", layout.Inset(), helpbox:true, messageType:3, fontSize:9);
			}

			//save
			if (layout.Button("Save Imported RAW", disabled:refMatrixAsset==null)) 
			{ 
				//releasing data on re-save
				if (UnityEditor.AssetDatabase.Contains(refMatrixAsset))
				{
					MatrixAsset newRefMatrixAsset = ScriptableObject.CreateInstance<MatrixAsset>();
					newRefMatrixAsset.matrix = refMatrixAsset.matrix.Copy();
					newRefMatrixAsset.preview = refMatrixAsset.preview.Copy();
					refMatrixAsset = newRefMatrixAsset;
				}
					

				string path= UnityEditor.EditorUtility.SaveFilePanel(
					"Save Data as Unity Asset",
					"Assets",
					"ImportedRAW.asset", 
					"asset");
				if (path!=null && path.Length!=0)
				{
					path = path.Replace(Application.dataPath, "Assets");
					if (MapMagic.instance != null)
					{
						UnityEditor.Undo.RecordObject(MapMagic.instance, "MapMagic Save Data");
						MapMagic.instance.setDirty = !MapMagic.instance.setDirty;
					}

					UnityEditor.AssetDatabase.CreateAsset(refMatrixAsset, path);
					
					if (MapMagic.instance != null)
						UnityEditor.EditorUtility.SetDirty(MapMagic.instance);
					
					#if VOXELAND
					if (Voxeland5.Voxeland.instances != null)
						foreach (Voxeland5.Voxeland voxeland in Voxeland5.Voxeland.instances)
							UnityEditor.EditorUtility.SetDirty(voxeland);
					#endif
				}
			}
			layout.Par(10);
							
			//preview
			int previewSize = 70;
			int controlsSize = (int)layout.field.width - previewSize - 10;
			Rect oldCursor = layout.cursor;
			if (preview == null) RefreshPreview();
			layout.Par(previewSize+3); layout.Inset(controlsSize);
			layout.Icon(preview, layout.Inset(previewSize+4));
			layout.cursor = oldCursor;
			
			//load raw
			layout.Par(); if (layout.Button("Browse", rect:layout.Inset(controlsSize))) { ImportRaw(); layout.change = true; }
			layout.Par(); if (layout.Button("Refresh", rect:layout.Inset(controlsSize))) { ImportRaw(texturePath); layout.change = true; }
			layout.Par(40); layout.Label("Square gray 16bit RAW, PC byte order", layout.Inset(controlsSize), helpbox:true, fontSize:9);
			
			layout.Par(5);
			#endif



			//checking if matrix asset loaded
			#if WDEBUG
				if (refMatrixAsset == null) layout.Label("Matrix asset is null");
				else
				{
					if (refMatrixAsset.matrix != null) layout.Label("Matrix loaded: " + refMatrixAsset.matrix.rect.size.x);
					else layout.Label("Matrix NOT loaded");
				}

				if (textureMatrix == null) layout.Label("Texture matrix is null");
				else layout.Label("Texture matrix loaded: " + textureMatrix.rect.size.x);
			#endif

			layout.fieldSize = 0.62f;
			layout.Field(ref intensity, "Intensity");
			layout.Field(ref scale, "Scale");
			layout.Field(ref offset, "Offset");
			if (tile) wrapMode = Matrix.WrapMode.Tile; tile=false;
			layout.Field(ref wrapMode, "Wrap Mode");
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Texture Input", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/texture_input")]
	public class TextureInput : Generator
	{
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return output; }

		public Texture2D texture;
		public bool loadEachGen = false;
		
		//[System.NonSerialized] private object matrixLocker = new object();
		public float intensity = 1;
		public float scale = 1;
		public Vector2 offset;
		public Matrix.WrapMode wrapMode = Matrix.WrapMode.Once;
		
		public bool tile = false; //outdated
		[System.NonSerialized] public Matrix textureMatrix = new Matrix();


		public void CheckLoadTexture (bool force=false)
		{
			if (texture==null) return;
			lock (textureMatrix)
			{
				if (textureMatrix.rect.size.x!=texture.width || textureMatrix.rect.size.z!=texture.height || force)
				{
					textureMatrix.ChangeRect( new CoordRect(0,0, texture.width, texture.height), forceNewArray:true );
					try { textureMatrix.FromTexture(texture); }
					catch (UnityException e) { Debug.LogError(e); }
				}
			}
		}

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			if ((stop!=null && stop(0)) || !enabled || textureMatrix==null) return;

			Matrix matrix = new Matrix(rect);
			Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;
			float pixelSize = terrainSize.pixelSize;
			 
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				float sx = (x-offset.x/pixelSize)/scale * textureMatrix.rect.size.x/matrix.rect.size.x;
				float sz = (z-offset.y/pixelSize)/scale * textureMatrix.rect.size.z/matrix.rect.size.z;

				matrix[x,z] = textureMatrix.GetInterpolated(sx, sz, wrapMode) * intensity;
			}

			if (scale >= 2f)
			{
				Matrix cpy = matrix.Copy();
				for (int i=1; i<scale-1; i+=2) matrix.Blur();
				Matrix.SafeBorders(cpy, matrix, Mathf.Max(matrix.rect.size.x/128, 4));
			}

			if (stop!=null && stop(0)) return;
			output.SetObject(results, matrix);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); output.DrawIcon(layout, "Output");
			layout.Par(5);
			
			//preview texture
			layout.fieldSize = 0.62f;
			layout.Field(ref texture, "Texture");
			if (layout.Button("Reload")) CheckLoadTexture(force:true); //ReloadTexture();
			layout.Toggle(ref loadEachGen, "Reload Each Generate");
			layout.Field(ref intensity, "Intensity");
			layout.Field(ref scale, "Scale");
			layout.Field(ref offset, "Offset");
			if (tile) wrapMode = Matrix.WrapMode.Tile; tile=false;
			layout.Field(ref wrapMode, "Wrap Mode");
		}
	}
	
	
	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Intensity/Bias", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/intensity_bias")]
	public class IntensityBiasGenerator : Generator
	{
		public float intensity = 1f;
		public float bias = 0.0f;

		public Input input = new Input(InoutType.Map);//, mandatory:true);
		public Input maskIn = new Input(InoutType.Map);
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; yield return maskIn; }
		public override IEnumerable<Output> Outputs() { yield return output; }

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			//getting input
			Matrix src = (Matrix)input.GetObject(results);

			//return on stop/disable/null input
			if (stop!=null && stop(0)) return;
			if (!enabled || src==null) { output.SetObject(results, src); return; }

			//preparing output
			Matrix dst = src.Copy(null);

			for (int i=0; i<dst.count; i++)
			{
				float result = dst.array[i];
				
				//apply contrast and bias
				result = result*intensity;
				result -= 0*(1-bias) + (intensity-1)*bias; //0.5f - intensity*bias;

				if (result < 0) result = 0; 
				if (result > 1) result = 1;

				dst.array[i] = result;
			}
			
			//mask and safe borders
			if (stop!=null && stop(0)) return;
			Matrix mask = (Matrix)maskIn.GetObject(results);
			if (mask != null) Matrix.Mask(src, dst, mask);

			//setting output
			if (stop!=null && stop(0)) return;
			output.SetObject(results, dst);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); input.DrawIcon(layout, "Input"); output.DrawIcon(layout, "Output");
			layout.Par(20); maskIn.DrawIcon(layout, "Mask");
			layout.Par(5);

			//params
			layout.Field(ref intensity, "Intensity");
			layout.Field(ref bias, "Bias");
		}
	}

	
	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Invert", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/invert")]
	public class InvertGenerator : Generator
	{
		//yap, this one is from the tutorial

		public Input input = new Input(InoutType.Map);
		public Output output = new Output(InoutType.Map);
		public Input maskIn = new Input(InoutType.Map);

		public override IEnumerable<Input> Inputs() { yield return input; yield return maskIn; }
		public override IEnumerable<Output> Outputs() { yield return output; }

		public float level = 1;

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			Matrix src = (Matrix)input.GetObject(results);

			if (stop!=null && stop(0)) return;
			if (!enabled || src==null) { output.SetObject(results, src); return; }

			Matrix dst = new Matrix(src.rect);

			Coord min = src.rect.Min; Coord max = src.rect.Max;
			for (int x=min.x; x<max.x; x++)
			   for (int z=min.z; z<max.z; z++)
			   {
					float val = level - src[x,z];
					dst[x,z] = val>0? val : 0;
				}

			//mask and safe borders
			if (stop!=null && stop(0)) return;
			Matrix mask = (Matrix)maskIn.GetObject(results);
			if (mask != null) Matrix.Mask(src, dst, mask);

			if (stop!=null && stop(0)) return;
			output.SetObject(results, dst);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			layout.Par(20); input.DrawIcon(layout, "Input", mandatory:true); output.DrawIcon(layout, "Output");
			layout.Par(20); maskIn.DrawIcon(layout, "Mask");

			layout.Field(ref level, "Level", min:0, max:1);
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Curve", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/curve")]
	public class CurveGenerator : Generator
	{
		//public override Type guiType { get { return Generator.Type.curve; } }
		
		public AnimationCurve curve = new AnimationCurve( new Keyframe[] { new Keyframe(0,0,1,1), new Keyframe(1,1,1,1) } );
		public bool extended = true;
		//public float inputMax = 1;
		//public float outputMax = 1;
		public Vector2 min = new Vector2(0,0);
		public Vector2 max = new Vector2(1,1);

		public Input input = new Input(InoutType.Map);//, mandatory:true);
		public Input maskIn = new Input(InoutType.Map);
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; yield return maskIn; }
		public override IEnumerable<Output> Outputs() { yield return output; }

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			//getting input
			Matrix src = (Matrix)input.GetObject(results);

			//return on stop/disable/null input
			if (stop!=null && stop(0)) return;
			if (!enabled || src==null) { output.SetObject(results, src); return; }

			//preparing output
			Matrix dst = src.Copy(null);

			//curve
			Curve c = new Curve(curve);
			for (int i=0; i<dst.array.Length; i++) dst.array[i] = c.Evaluate(dst.array[i]);
			
			//mask and safe borders
			if (stop!=null && stop(0)) return;
			Matrix mask = (Matrix)maskIn.GetObject(results);
			if (mask != null) Matrix.Mask(src, dst, mask);

			//setting output
			if (stop!=null && stop(0)) return;
			output.SetObject(results, dst);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); input.DrawIcon(layout, "Input"); output.DrawIcon(layout, "Output");
			layout.Par(20); maskIn.DrawIcon(layout, "Mask");
			layout.Par(5);

			//params
			Rect savedCursor = layout.cursor;
			layout.Par(50, padding:0);
			layout.Inset(3);
			layout.Curve(curve, rect:layout.Inset(80, padding:0), ranges:new Rect(min.x, min.y, max.x-min.x, max.y-min.y));
			layout.Par(3);

			layout.margin = 86;
			layout.cursor = savedCursor;
			layout.Label("Range:");
			//layout.Par(); layout.Label("Min:", rect:layout.Inset(0.999f)); layout.Label("Max:", rect:layout.Inset(1f));
			layout.Field(ref min);
			layout.Field(ref max);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Blend", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/blend")]
	public class BlendGenerator2 : Generator
	{
		public enum Algorithm {mix=0, add=1, subtract=2, multiply=3, divide=4, difference=5, min=6, max=7, overlay=8, hardLight=9, softLight=10} 
		
		public class Layer
		{
			public Input input = new Input(InoutType.Map);
			public Algorithm algorithm = Algorithm.add;
			public float opacity = 1;
		}
		public Layer[] layers = new Layer[] { new Layer(), new Layer() };
		public int guiSelected;

		public Input maskInput = new Input(InoutType.Map);
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Input> Inputs() { for (int i=0; i<layers.Length; i++) yield return layers[i].input; yield return maskInput; }
		public override IEnumerable<Output> Outputs() { yield return output; }

		public int inputsNum = 2;

		public static System.Func<float,float,float> GetAlgorithm (Algorithm algorithm)
		{
			switch (algorithm)
			{
				case Algorithm.mix: return delegate (float a, float b) { return b; };
				case Algorithm.add: return delegate (float a, float b) { return a+b; };
				case Algorithm.subtract: return delegate (float a, float b) { return a-b; };
				case Algorithm.multiply: return delegate (float a, float b) { return a*b; };
				case Algorithm.divide: return delegate (float a, float b) { return a/b; };
				case Algorithm.difference: return delegate (float a, float b) { return Mathf.Abs(a-b); };
				case Algorithm.min: return delegate (float a, float b) { return Mathf.Min(a,b); };
				case Algorithm.max: return delegate (float a, float b) { return Mathf.Max(a,b); };
				case Algorithm.overlay: return delegate (float a, float b) 
				{
					if (a > 0.5f) return 1 - 2*(1-a)*(1-b);
					else return 2*a*b; 
				}; 
				case Algorithm.hardLight: return delegate (float a, float b) 
				{
						if (b > 0.5f) return 1 - 2*(1-a)*(1-b);
						else return 2*a*b; 
				};
				case Algorithm.softLight: return delegate (float a, float b) { return (1-2*b)*a*a + 2*b*a; };
				default: return delegate (float a, float b) { return b; };
			}
		}


		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			//return on stop/disable/null input
			if ((stop!=null && stop(0)) || layers.Length==0) return;
			Matrix baseMatrix = (Matrix)layers[0].input.GetObject(results);
			Matrix maskMatrix = (Matrix)maskInput.GetObject(results);
			if (!enabled || layers.Length==1) { output.SetObject(results,baseMatrix); return; }

			//preparing output
			Matrix matrix = baseMatrix!=null? baseMatrix.Copy() : new Matrix(rect);

			//processing
			for (int l=1; l<layers.Length; l++)
			{
				Layer layer = layers[l];
				Matrix layerMatrix = (Matrix)layer.input.GetObject(results);
				if (layerMatrix==null) continue;

				System.Func<float,float,float> algorithmFn = GetAlgorithm(layer.algorithm);

				for (int i=0; i<matrix.array.Length; i++)
				{
					float m = (maskMatrix==null ? 1 : maskMatrix.array[i]) * layer.opacity;
					float a = matrix.array[i];
					float b = layerMatrix.array[i];

					switch (layer.algorithm)
					{
						case Algorithm.mix: matrix.array[i] = a*(1-m) + b*m; break;
						case Algorithm.add: matrix.array[i] = a*(1-m) + (a+b)*m; break;
						case Algorithm.multiply: matrix.array[i] = a*(1-m) + (a*b)*m; break;
						case Algorithm.subtract: matrix.array[i] = a*(1-m) + (a-b)*m; break;
						default: matrix.array[i] = a*(1-m) + algorithmFn(a,b)*m; break;
					}
				}
			}

			//clamping
			matrix.Clamp01();
		
			//setting output
			if (stop!=null && stop(0)) return;
			output.SetObject(results, matrix);
		}

		public void OnLayerGUI (Layout layout, bool selected, int num) 
		{
			layout.margin += 10; layout.rightMargin +=5;
			layout.Par(20);
			layers[num].input.DrawIcon(layout, "", mandatory:false);
			if (num==0) layout.Label("Base Layer", rect:layout.Inset());
			else
			{
				layout.Field(ref layers[num].algorithm, rect:layout.Inset(0.5f), disabled:num==0);
				layout.Inset(0.05f);
				layout.Icon("MapMagic_Opacity", rect:layout.Inset(0.1f), horizontalAlign:Layout.IconAligment.center, verticalAlign:Layout.IconAligment.center);
				layout.Field(ref layers[num].opacity, rect:layout.Inset(0.35f), disabled:num==0);
			}
			layout.margin -= 10; layout.rightMargin -=5;
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); maskInput.DrawIcon(layout, "Mask"); output.DrawIcon(layout, "Output");
			layout.Par(5);
			
			//params
			layout.Par(16);
			layout.Label("Layers:", layout.Inset(0.4f));
			layout.DrawArrayAdd(ref layers, ref guiSelected, layout.Inset(0.15f), reverse:true, createElement:() => new Layer());
			layout.DrawArrayRemove(ref layers, ref guiSelected, layout.Inset(0.15f), reverse:true);
			layout.DrawArrayDown(ref layers, ref guiSelected, layout.Inset(0.15f), dispUp:true);
			layout.DrawArrayUp(ref layers, ref guiSelected, layout.Inset(0.15f), dispDown:true);

			//layers
			layout.margin = 10; layout.rightMargin = 5;
			layout.Par(5);
			for (int num=layers.Length-1; num>=0; num--)
				layout.DrawLayer(OnLayerGUI, ref guiSelected, num);

			//layout.DrawLayered(layers, ref guiSelected, min:0, max:layers.Length, reverseOrder:true, onLayerGUI:OnLayerGUI);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Normalize", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/normalize")]
	public class NormalizeGenerator : Generator
	{
		public enum Algorithm { sum, layers };
		public Algorithm algorithm;

		//layer
		public class Layer
		{
			public Input input = new Input(InoutType.Map);
			public Output output = new Output(InoutType.Map);
			public float opacity = 1;
		}

		public Layer[] baseLayers = new Layer[] { new Layer(){} };
		public int guiSelected;

		//generator
		public override IEnumerable<Input> Inputs() 
		{ 
			if (baseLayers==null) baseLayers = new Layer[0];
			for (int i=0; i<baseLayers.Length; i++) 
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

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			if ((stop!=null && stop(0)) || !enabled) return;
			
			//loading inputs
			Matrix[] matrices = new Matrix[baseLayers.Length];
			for (int i=0; i<baseLayers.Length; i++)
			{
				if (baseLayers[i].input != null) 
				{
					matrices[i] = (Matrix)baseLayers[i].input.GetObject(results);
					if (matrices[i] != null) matrices[i] = matrices[i].Copy(null);
				}
				if (matrices[i] == null) matrices[i] = new Matrix(rect);
			}

			//background matrix
			//matrices[0] = terrain.defaultMatrix; //already created
			//matrices[0].Fill(1);
			
			//populating opacity array
			float[] opacities = new float[matrices.Length];
			for (int i=0; i<baseLayers.Length; i++)
				opacities[i] = baseLayers[i].opacity;
			opacities[0] = 1;

			//blending layers
			if (algorithm==Algorithm.layers) Matrix.BlendLayers(matrices, opacities);
			else Matrix.NormalizeLayers(matrices, opacities);

			//saving changed matrix results
			for (int i=0; i<baseLayers.Length; i++) 
			{
				if (stop!=null && stop(0)) return; //do not write object is generating is stopped
				baseLayers[i].output.SetObject(results, matrices[i]);
			}
		}

		public void UnlinkLayer (int num)
		{
			baseLayers[num].input.Link(null,null); //unlink input
			baseLayers[num].output.UnlinkInActiveGens(); //try to unlink output
		}

		public void OnLayerGUI (Layout layout, bool selected, int num) 
		{
			layout.Par(20);
			
			baseLayers[num].input.DrawIcon(layout);
			layout.Inset(0.1f);
			layout.Icon("MapMagic_Opacity", rect:layout.Inset(0.1f), horizontalAlign:Layout.IconAligment.center, verticalAlign:Layout.IconAligment.center);
			layout.Field(ref baseLayers[num].opacity, rect:layout.Inset(0.7f));
			layout.Inset(0.1f);
			baseLayers[num].output.DrawIcon(layout);
		}

		public override void OnGUI (GeneratorsAsset gens) 
		{
			layout.Field(ref algorithm, "Algorithm");
			//layout.DrawLayered(this, "Layers:", selectable:false, drawButtons:true);

			layout.margin=5;
			layout.Par(16);
			layout.Label("Layers:", layout.Inset(0.4f));
			layout.DrawArrayAdd(ref baseLayers, ref guiSelected, layout.Inset(0.15f), createElement:() => new Layer()); 
			layout.DrawArrayRemove(ref baseLayers, ref guiSelected, layout.Inset(0.15f), onBeforeRemove:UnlinkLayer);
			layout.DrawArrayUp(ref baseLayers, ref guiSelected, layout.Inset(0.15f));
			layout.DrawArrayDown(ref baseLayers, ref guiSelected, layout.Inset(0.15f));
			layout.Par(5);
			
			layout.margin = 10; layout.rightMargin = 10; layout.fieldSize = 1f;
			for (int num=0; num<baseLayers.Length; num++)
				layout.DrawLayer(OnLayerGUI, ref guiSelected, num);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Blur", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/blur")]
	public class BlurGenerator : Generator
	{
		public Input input = new Input(InoutType.Map);
		public Input maskIn = new Input(InoutType.Map);
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; yield return maskIn; }
		public override IEnumerable<Output> Outputs() { yield return output; }

		public int iterations = 1;
		public float intensity = 1f;
		public int loss = 1;
		public int safeBorders = 5;

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			//getting input
			Matrix src = (Matrix)input.GetObject(results); 

			//return on stop/disable/null input
			if (stop!=null && stop(0)) return; 
			if (!enabled || src==null) { output.SetObject(results, src); return; }
			
			//preparing output
			Matrix dst = src.Copy(null);

			//blurring beforehead if loss is on
			if (loss!=1) for (int i=0; i<iterations;i++) dst.Blur(intensity:0.666f);

			//blur with loss
			int curLoss = loss;
			while (curLoss>1)
			{
				dst.LossBlur(curLoss);
				curLoss /= 2;
			}
			
			//main blur (after loss)
			for (int i=0; i<iterations;i++) dst.Blur(intensity:1f);

			//mask and safe borders
			if (intensity < 0.9999f) Matrix.Blend(src, dst, intensity);
			Matrix mask = (Matrix)maskIn.GetObject(results);
			if (mask != null) Matrix.Mask(src, dst, mask);
			if (safeBorders != 0) Matrix.SafeBorders(src, dst, safeBorders);

			//setting output
			if (stop!=null && stop(0)) return;
			output.SetObject(results, dst);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); input.DrawIcon(layout, "Input", mandatory:true); output.DrawIcon(layout, "Output");
			layout.Par(20); maskIn.DrawIcon(layout, "Mask"); 
			layout.Par(5);
			
			//params
			layout.Field(ref intensity, "Intensity", max:1);
			layout.Field(ref iterations, "Iterations", min:1);
			layout.Field(ref loss, "Loss", min:1);
			layout.Field(ref safeBorders, "Safe Borders");
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Cavity", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/cavity")]
	public class CavityGenerator1 : Generator
	{
		public Input input = new Input(InoutType.Map);
		public Input maskIn = new Input(InoutType.Map);
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; yield return maskIn; }
		public override IEnumerable<Output> Outputs() { yield return output; }

		public enum CavityType { Convex, Concave }
		public CavityType type = CavityType.Convex;
		public float intensity = 1;
		public float spread = 0.5f;
		public bool normalize = true;
		public int safeBorders = 3;

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			//getting input
			Matrix src = (Matrix)input.GetObject(results);

			//return on stop/disable/null input
			if (stop!=null && stop(0)) return; 
			if (!enabled || src==null) { output.SetObject(results, src); return; }; 

			//preparing outputs
			Matrix dst = new Matrix(src.rect);

			//cavity
			System.Func<float,float,float,float> cavityFn = delegate(float prev, float curr, float next) 
			{
				float c = curr - (next+prev)/2;
				return (c*c*(c>0?1:-1))*intensity*100000;
			};
			dst.Blur(cavityFn, intensity:1, additive:true, reference:src); //intensity is set in func
			if (stop!=null && stop(0)) return;

			//borders
			dst.RemoveBorders(); 
			if (stop!=null && stop(0)) return;

			//inverting
			if (type == CavityType.Concave) dst.Invert();
			if (stop!=null && stop(0)) return;

			//normalizing
			if (!normalize) dst.Clamp01();
			if (stop!=null && stop(0)) return;

			//spread
			dst.Spread(strength:spread); 
			if (stop!=null && stop(0)) return;

			dst.Clamp01();
			
			//mask and safe borders
			if (intensity < 0.9999f) Matrix.Blend(src, dst, intensity);
			Matrix mask = (Matrix)maskIn.GetObject(results);
			if (mask != null) Matrix.Mask(null, dst, mask);
			if (safeBorders != 0) Matrix.SafeBorders(null, dst, safeBorders);

			//setting outputs
			output.SetObject(results, dst);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//creating mask input in case a previous version of the generator was loaded
			if (maskIn == null) maskIn = new Input(InoutType.Map);
			
			//inouts
			layout.Par(20); input.DrawIcon(layout, "Input", mandatory:true); output.DrawIcon(layout, "Output");
			layout.Par(20); maskIn.DrawIcon(layout, "Mask");
			layout.Par(5);
			
			//params
			layout.Field(ref type, "Type");
			layout.Field(ref intensity, "Intensity");
			layout.Field(ref spread, "Spread");
			layout.Par(3);
			layout.Toggle(ref normalize, "Normalize");
			layout.Par(15); layout.Inset(20); layout.Label(label:"Convex + Concave", rect:layout.Inset(), textAnchor:TextAnchor.LowerLeft);
			layout.Field(ref safeBorders, "Safe Borders");
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Slope", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/slope")]
	public class SlopeGenerator1 : Generator
	{
		public Input input = new Input(InoutType.Map);
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; }
		public override IEnumerable<Output> Outputs() { yield return output; }
		
		public Vector2 steepness = new Vector2(45,90);
		public float range = 5f;

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			//getting input
			Matrix matrix = (Matrix)input.GetObject(results);

			//return on stop/disable/null input
			if (stop!=null && stop(0)) return; 
			if (!enabled || matrix==null) { output.SetObject(results, matrix); return; }; 

			//preparing output
			Matrix result = new Matrix(matrix.rect);

			//using the terain-height relative values
			float pixelSize = terrainSize.pixelSize;
			
			float min0 = Mathf.Tan((steepness.x-range/2)*Mathf.Deg2Rad) * pixelSize / terrainSize.height;
			float min1 = Mathf.Tan((steepness.x+range/2)*Mathf.Deg2Rad) * pixelSize / terrainSize.height;
			float max0 = Mathf.Tan((steepness.y-range/2)*Mathf.Deg2Rad) * pixelSize / terrainSize.height;
			float max1 = Mathf.Tan((steepness.y+range/2)*Mathf.Deg2Rad) * pixelSize / terrainSize.height;

			//dealing with 90-degree
			if (steepness.y-range/2 > 89.9f) max0 = 20000000; if (steepness.y+range/2 > 89.9f) max1 = 20000000;

			//ignoring min if it is zero
			if (steepness.x<0.0001f) { min0=0; min1=0; }

			//delta map
			System.Func<float,float,float,float> inclineFn = delegate(float prev, float curr, float next) 
			{
				float prevDelta = prev-curr; if (prevDelta < 0) prevDelta = -prevDelta;
				float nextDelta = next-curr; if (nextDelta < 0) nextDelta = -nextDelta;
				return prevDelta>nextDelta? prevDelta : nextDelta; 
			};
			result.Blur(inclineFn, intensity:1, takemax:true, reference:matrix); //intensity is set in func

			//slope map
			for (int i=0; i<result.array.Length; i++)
			{
				float delta = result.array[i];
				
				if (steepness.x<0.0001f) result.array[i] = 1-(delta-max0)/(max1-max0);
				else
				{
					float minVal = (delta-min0)/(min1-min0);
					float maxVal = 1-(delta-max0)/(max1-max0);
					float val = minVal>maxVal? maxVal : minVal;
					if (val<0) val=0; if (val>1) val=1;

					result.array[i] = val;
				}
			}

			//setting output
			if (stop!=null && stop(0)) return;
			output.SetObject(results, result);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); input.DrawIcon(layout, "Input", mandatory:true); output.DrawIcon(layout, "Output");
			layout.Par(5);
			
			//params
			layout.fieldSize = 0.6f;
			layout.Field(ref steepness, "Steepness", min:0, max:90);
			layout.Field(ref range, "Range", min:0.1f);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Level Select", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/slope")]
	public class LevelSelectGenerator1 : Generator
	{
		public Input input = new Input(InoutType.Map);
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; }
		public override IEnumerable<Output> Outputs() { yield return output; }
		
		public Vector2 level = new Vector2(0,50);
		public float range = 0;
		public bool relative = false;

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			//getting input
			Matrix matrix = (Matrix)input.GetObject(results);

			//return on stop/disable/null input
			if (stop!=null && stop(0)) return; 
			if (!enabled || matrix==null) { output.SetObject(results, matrix); return; }; 

			//preparing output
			//Matrix result = new Matrix(matrix.rect);
			Matrix result = matrix.Copy(null);

			//using the terain-height relative values
			float pixelSize = terrainSize.pixelSize;
			
			float min0 = level.x - range/2;
			float min1 = level.x + range/2;
			float max0 = level.y - range/2;
			float max1 = level.y + range/2;

			if (!relative)
			{
				min0 /= terrainSize.height; min1 /= terrainSize.height;
				max0 /= terrainSize.height; max1 /= terrainSize.height;
			}

			//level map
			for (int i=0; i<result.array.Length; i++)
			{
				float height = result.array[i];
				
				if (range<0.000001f)
				{
					result.array[i] = (height<min0 || height>max0) ? 0 : 1;
				}
				else if (level.x<0.000001f) result.array[i] = 1-(height-max0)/(max1-max0);
				else
				{
					float minVal = (height-min0)/(min1-min0);
					float maxVal = 1-(height-max0)/(max1-max0);
					float val = minVal>maxVal? maxVal : minVal;
					if (val<0) val=0; if (val>1) val=1;

					result.array[i] = val;
				}
			}

			//setting output
			if (stop!=null && stop(0)) return;
			output.SetObject(results, result);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); input.DrawIcon(layout, "Input", mandatory:true); output.DrawIcon(layout, "Output");
			layout.Par(5);
			
			//params
			layout.fieldSize = 0.6f;
			layout.Field(ref level, "Level", min:0);
			layout.Field(ref range, "Range", min:0);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Terrace", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/terrace")]
	public class TerraceGenerator : Generator
	{
		public Input input = new Input(InoutType.Map);
		public Input maskIn = new Input(InoutType.Map);
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; yield return maskIn; }
		public override IEnumerable<Output> Outputs() { yield return output; }

		public int seed = 12345;
		public int num = 10;
		public float uniformity = 0.5f;
		public float steepness = 0.5f;
		public float intensity = 1f;

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			//getting inputs
			Matrix src = (Matrix)input.GetObject(results);

			//return on stop/disable/null input
			if (stop!=null && stop(0)) return; 
			if (!enabled || num <= 1 || src==null) { output.SetObject(results, src); return; }
			
			//preparing output
			Matrix dst = src.Copy(null);

			//creating terraces
			float[] terraces = new float[num];
			InstanceRandom random = new InstanceRandom(seed + 12345);
			
			float step = 1f / (num-1);
			for (int t=1; t<num; t++)
				terraces[t] = terraces[t-1] + step;

			for (int i=0; i<10; i++)
				for (int t=1; t<num-1; t++)
				{
					float rndVal = random.Random(terraces[t-1], terraces[t+1]);
					terraces[t] = terraces[t]*uniformity + rndVal*(1-uniformity);
				}

			//adjusting matrix
			if (stop!=null && stop(0)) return;
			for (int i=0; i<dst.count; i++)
			{
				float val = dst.array[i];
				if (val > 0.999f) continue;	//do nothing with values that are out of range

				int terrNum = 0;		
				for (int t=0; t<num-1; t++)
				{
					if (terraces[terrNum+1] > val || terrNum+1 == num) break;
					terrNum++;
				}

				//kinda curve evaluation
				float delta = terraces[terrNum+1] - terraces[terrNum];
				float relativePos = (val - terraces[terrNum]) / delta;

				float percent = 3*relativePos*relativePos - 2*relativePos*relativePos*relativePos;

				percent = (percent-0.5f)*2;
				bool minus = percent<0; percent = Mathf.Abs(percent);

				percent = Mathf.Pow(percent,1f-steepness);

				if (minus) percent = -percent;
				percent = percent/2 + 0.5f;

				dst.array[i] = (terraces[terrNum]*(1-percent) + terraces[terrNum+1]*percent)*intensity + dst.array[i]*(1-intensity);
				//matrix.array[i] = a*keyVals[keyNum] + b*keyOutTangents[keyNum]*delta + c*keyInTangents[keyNum+1]*delta + d*keyVals[keyNum+1];
			}

			//mask and safe borders
			Matrix mask = (Matrix)maskIn.GetObject(results);
			if (mask != null) Matrix.Mask(src, dst, mask);

			//setting output
			if (stop!=null && stop(0)) return;
			output.SetObject(results, dst);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); input.DrawIcon(layout, "Input", mandatory:true); output.DrawIcon(layout, "Output");
			layout.Par(20); maskIn.DrawIcon(layout, "Mask");
			layout.Par(5);
			
			//params
			layout.Field(ref num, "Treads Num", min:2);
			layout.Field(ref uniformity, "Uniformity", min:0, max:1);
			layout.Field(ref steepness, "Steepness", min:0, max:1);
			layout.Field(ref intensity, "Intensity", min:0, max:1);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Erosion", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/erosion")]
	public class ErosionGenerator : Generator
	{
		public Input heightIn = new Input(InoutType.Map);
		public Input maskIn = new Input(InoutType.Map);
		public Output heightOut = new Output(InoutType.Map);
		public Output cliffOut = new Output(InoutType.Map);
		public Output sedimentOut = new Output(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return heightIn; yield return maskIn; }
		public override IEnumerable<Output> Outputs() { yield return heightOut; yield return cliffOut; yield return sedimentOut; }

		public int iterations = 5;
		public float terrainDurability=0.9f;
		public float erosionAmount=1f;
		public float sedimentAmount=0.75f;
		public int fluidityIterations=3;
		public float ruffle=0.4f;
		public int safeBorders = 10;
		public float cliffOpacity = 1f;
		public float sedimentOpacity = 1f;


		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			//getting inputs
			Matrix src = (Matrix)heightIn.GetObject(results);
			
			//return
			if (stop!=null && stop(0)) return; 
			if (!enabled || iterations <= 0 || src==null) { heightOut.SetObject(results, src); return; }

			//creating output arrays
			Matrix dst = new Matrix(src.rect);
			Matrix dstErosion = new Matrix(src.rect);
			Matrix dstSediment = new Matrix(src.rect);

			//crating temporary arrays (with margins)
			int margins = 10;
			Matrix height = new Matrix(src.rect.offset-margins, src.rect.size+margins*2);
			height.Fill(src, removeBorders:true);

			Matrix erosion = new Matrix(height.rect);
			Matrix sediment = new Matrix(height.rect);
			Matrix internalTorrents = new Matrix(height.rect);
			int[] stepsArray = new int[1000001];
			int[] heightsInt = new int[height.count];
			int[] order = new int[height.count];

			//calculate erosion
			for (int i=0; i<iterations; i++) 
			{
				if (stop!=null && stop(0)) return;

				Erosion.ErosionIteration (height, erosion, sediment, area:height.rect,
							erosionDurability:terrainDurability, erosionAmount:erosionAmount, sedimentAmount:sedimentAmount, erosionFluidityIterations:fluidityIterations, ruffle:ruffle, 
							torrents:internalTorrents, stepsArray:stepsArray, heightsInt:heightsInt, order:order);

				Coord min = dst.rect.Min; Coord max = dst.rect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
						{ dstErosion[x,z] += erosion[x,z]*cliffOpacity*30f; dstSediment[x,z] += sediment[x,z]*sedimentOpacity; }
			}

			//fill dst
			dst.Fill(height);
			
			//expanding sediment map 1 pixel
			//dstSediment.Spread(strength:1, iterations:1);

			//mask and safe borders
			Matrix mask = (Matrix)maskIn.GetObject(results);
			if (mask != null) { Matrix.Mask(src, dst, mask); Matrix.Mask(null, dstErosion, mask); Matrix.Mask(null, dstSediment, mask); }
			if (safeBorders != 0) { Matrix.SafeBorders(src, dst, safeBorders); Matrix.SafeBorders(null, dstErosion, safeBorders); Matrix.SafeBorders(null, dstSediment, safeBorders); }
			
			//finally
			if (stop!=null && stop(0)) return;
			heightOut.SetObject(results, dst);
			cliffOut.SetObject(results, dstErosion);
			sedimentOut.SetObject(results, dstSediment);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); heightIn.DrawIcon(layout, "Heights", mandatory:true); heightOut.DrawIcon(layout, "Heights");
			layout.Par(20); maskIn.DrawIcon(layout, "Mask"); cliffOut.DrawIcon(layout, "Cliff");
			layout.Par(20); sedimentOut.DrawIcon(layout, "Sediment");
			layout.Par(5);
			
			//params
			//layout.SmartField(ref downscale, "Downscale", min:1); //downscale = Mathf.NextPowerOfTwo(downscale);
			//layout.ComplexField(ref preserveDetail, "Preserve Detail");
			layout.Par(30);
			layout.Label("Generating erosion takes significant amount of time", rect:layout.Inset(), helpbox:true, fontSize:9);
			layout.Par(2);
			layout.Field(ref iterations, "Iterations");
			layout.Field(ref terrainDurability, "Durability");
			layout.Field(ref erosionAmount, "Erosion", min:0, max:1);
			layout.Field(ref sedimentAmount, "Sediment");
			layout.Field(ref fluidityIterations, "Fluidity");
			layout.Field(ref ruffle, "Ruffle");
			layout.Field(ref safeBorders, "Safe Borders");
			layout.Field(ref cliffOpacity, "Cliff Opacity");
			layout.Field(ref sedimentOpacity, "Sediment Opacity");
		}
	}


	[System.Serializable]
	//[GeneratorMenu (menu="Map", name ="Noise Mask", disengageable = true)]
	public class NoiseMaskGenerator : Generator
	{
		public Input inputIn = new Input(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return inputIn; }

		public Output maskedOut = new Output(InoutType.Map);
		public Output invMaskedOut = new Output(InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return maskedOut; yield return invMaskedOut; }

		public float opacity = 1f;
		public float size = 200;
		public Vector2 offset = new Vector2(0,0);
		public AnimationCurve curve = new AnimationCurve( new Keyframe[] { new Keyframe(0,0,1,1), new Keyframe(1,1,1,1) } );

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			//getting inputs
			Matrix input = (Matrix)inputIn.GetObject(results);
			Matrix masked = new Matrix(rect);
			Matrix invMasked = new Matrix(rect);

			//return
			if (stop!=null && stop(0)) return; 
			if (!enabled || input==null) { maskedOut.SetObject(results, input); return; }
			
			//generating noise
			NoiseGenerator.Noise(masked, size, 1, 0.5f, offset:offset);
			if (stop!=null && stop(0)) return;
			
			//adjusting curve
			Curve c = new Curve(curve);
			for (int i=0; i<masked.array.Length; i++) masked.array[i] = c.Evaluate(masked.array[i]);
			if (stop!=null && stop(0)) return;

			//get inverse mask
			for (int i=0; i<masked.array.Length; i++)
				invMasked.array[i] = 1f - masked.array[i];
			if (stop!=null && stop(0)) return;
			
			//multiply masks by input
			if (input != null)
			{
				for (int i=0; i<masked.array.Length; i++) masked.array[i] = input.array[i]*masked.array[i]*opacity + input.array[i]*(1f-opacity);
				for (int i=0; i<invMasked.array.Length; i++) invMasked.array[i] = input.array[i]*invMasked.array[i]*opacity + input.array[i]*(1f-opacity);
			}

			if (stop!=null && stop(0)) return;
			maskedOut.SetObject(results, masked);
			invMaskedOut.SetObject(results, invMasked);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); inputIn.DrawIcon(layout, "Input"); maskedOut.DrawIcon(layout, "Masked");
			layout.Par(20); invMaskedOut.DrawIcon(layout, "InvMasked");
			layout.Par(5);
			
			//params
			Rect cursor = layout.cursor; layout.rightMargin = 90; layout.fieldSize = 0.75f;
			layout.Field(ref opacity, "A", max:1);
			layout.Field(ref size, "S", min:1);
			layout.Field(ref offset, "O");
			
			layout.cursor = cursor; layout.rightMargin = layout.margin; layout.margin = (int)layout.field.width - 85 - layout.margin*2;
			layout.Par(53);
			layout.Curve(curve, layout.Inset());
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Shore", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/shore")]
	public class ShoreGenerator : Generator
	{
		public Input heightIn = new Input(InoutType.Map);
		public Input maskIn = new Input(InoutType.Map);
		public Input ridgeNoiseIn = new Input(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return heightIn; yield return maskIn; yield return ridgeNoiseIn; }

		public Output heightOut = new Output(InoutType.Map);
		public Output sandOut = new Output(InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return heightOut; yield return sandOut; }

		public float intensity = 1f;
		public float beachLevel = 20f;
		public float beachSize = 10f;
		public float ridgeMinGlobal = 2;
		public float ridgeMaxGlobal = 10;

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			Matrix src = (Matrix)heightIn.GetObject(results);

			if (stop!=null && stop(0)) return;
			if (!enabled || src==null) { heightOut.SetObject(results, src); return; }

			Matrix dst = new Matrix(src.rect);
			Matrix ridgeNoise = (Matrix)ridgeNoiseIn.GetObject(results);

			//preparing sand
			Matrix sands = new Matrix(src.rect);

			//converting ui values to internal
			float beachMin = beachLevel / terrainSize.height;
			float beachMax = (beachLevel+beachSize) / terrainSize.height;
			float ridgeMin = ridgeMinGlobal / terrainSize.height;
			float ridgeMax = ridgeMaxGlobal / terrainSize.height;

			Coord min = src.rect.Min; Coord max = src.rect.Max;
			for (int x=min.x; x<max.x; x++)
			   for (int z=min.z; z<max.z; z++)
			{
				float srcHeight = src[x,z];

				//creating beach
				float height = srcHeight;
				if (srcHeight > beachMin && srcHeight < beachMax) height = beachMin;
				
				float sand = 0;
				if (srcHeight <= beachMax) sand = 1;

				//blurring ridge
				float curRidgeDist = 0;
				float noise = 0; if (ridgeNoise != null) noise = ridgeNoise[x,z];
				curRidgeDist = ridgeMin*(1-noise) + ridgeMax*noise;
				
				if (srcHeight >= beachMax && srcHeight <= beachMax+curRidgeDist) 
				{
					float percent = (srcHeight-beachMax) / curRidgeDist;
					percent = Mathf.Sqrt(percent);
					percent = 3*percent*percent - 2*percent*percent*percent;
					
					height = beachMin*(1-percent) + srcHeight*percent;
					
					sand = 1-percent;
				}

				//setting height
				height = height*intensity + srcHeight*(1-intensity);
				dst[x,z] = height;
				sands[x,z] = sand;
			}

			//mask
			Matrix mask = (Matrix)maskIn.GetObject(results);
			if (mask != null) 
			{ 
				Matrix.Mask(src, dst, mask);  
				Matrix.Mask(null, sands, mask); 
			}

			if (stop!=null && stop(0)) return;
			heightOut.SetObject(results, dst); 
			sandOut.SetObject(results, sands);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			layout.Par(20); heightIn.DrawIcon(layout, "Height"); heightOut.DrawIcon(layout, "Output");
			layout.Par(20); maskIn.DrawIcon(layout, "Mask"); sandOut.DrawIcon(layout, "Sand");
			layout.Par(20); ridgeNoiseIn.DrawIcon(layout, "Ridge Noise"); 

			layout.Field(ref intensity, "Intensity", min:0);
			layout.Field(ref beachLevel, "Water Level", min:0);
			layout.Field(ref beachSize, "Beach Size", min:0.0001f);
			layout.Field(ref ridgeMinGlobal, "Ridge Step Min", min:0);
			layout.Field(ref ridgeMaxGlobal, "Ridge Step Max", min:0);
		}
	}

}
