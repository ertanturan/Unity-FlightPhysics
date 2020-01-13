using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using MapMagic;

namespace MapMagic
{
	[System.Serializable]
	[GeneratorMenu (menu="Legacy", name ="Voronoi (Legacy)", disengageable = true)]
	public class VoronoiGenerator : Generator
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

			InstanceRandom random = new InstanceRandom(seed + this.seed);
	
			//creating point matrix
			float cellSize = 1f * matrix.rect.size.x / cellCount;
			Matrix2<Vector3> points = new Matrix2<Vector3>( new CoordRect(0,0,cellCount+2,cellCount+2) );
			points.rect.offset = new Coord(-1,-1);

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
					case BlendType.closest: val = minDist / (terrainSize.resolution*16); break;
					case BlendType.secondClosest: val = secondMinDist / (terrainSize.resolution*16); break;
					case BlendType.cellular: val = (secondMinDist-minDist) / (terrainSize.resolution*16); break;
					case BlendType.organic: val = (secondMinDist+minDist)/2 / (terrainSize.resolution*16); break;
				}
				if (mask==null) matrix[x,z] += val*intensity;
				else matrix[x,z] += val*intensity*mask[x,z];
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
			layout.Field(ref cellCount, "Cell Count"); cellCount = Mathf.ClosestPowerOfTwo(cellCount);
			layout.Field(ref uniformity, "Uniformity", min:0, max:1);
			layout.Field(ref seed, "Seed");
		}
	}
	
	[System.Serializable]
	[GeneratorMenu (menu="Legacy", name ="Noise (Legacy)", disengageable = true, disabled = true)]
	public class NoiseGenerator : Generator
	{
		public int seed = 12345;
		public float intensity = 1f;
		public float bias = 0.0f;
		public float size = 200;
		public float detail = 0.5f;
		public Vector2 offset = new Vector2(0,0);
		//public float contrast = 0f;

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

			int totalSeedX = ((int)offset.x + seed + this.seed*7) % 77777;
			int totalSeedZ = ((int)offset.y + seed + this.seed*3) % 73333;

			Noise(matrix, size, intensity, bias, detail, offset, totalSeedX, totalSeedZ, mask);
			
			if (stop!=null && stop(0)) return; //do not write object is generating is stopped
			output.SetObject(results, matrix);
		}

		public static void Noise (Matrix matrix, float size, float intensity=1, float bias=0, float detail=0.5f, Vector2 offset=new Vector2(), int totalSeedX=12345, int totalSeedZ=12345, Matrix mask=null)
		{
			int step = (int)(4096f / matrix.rect.size.x);

			//int totalSeedX = ((int)offset.x + MapMagic.instance.seed + seed*7) % 77777;
			//int totalSeedZ = ((int)offset.y + MapMagic.instance.seed + seed*3) % 73333;

			//get number of iterations
			int numIterations = 1; //max size iteration included
			float tempSize = size;
			for (int i=0; i<100; i++)
			{
				tempSize = tempSize/2;
				if (tempSize<1) break;
				numIterations++;
			}

			//making some noise
			Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;
			for (int x=min.x; x<max.x; x++)
			{
				for (int z=min.z; z<max.z; z++)
				{
					float result = 0.5f;
					float curSize = size*10;
					float curAmount = 1;
				
					//applying noise
					for (int i=0; i<numIterations;i++)
					{
						float perlin = Mathf.PerlinNoise(
						(x + totalSeedX + 1000*(i+1))*step/(curSize+1), 
						(z + totalSeedZ + 100*i)*step/(curSize+1) );
						perlin = (perlin-0.5f)*curAmount + 0.5f;

						//applying overlay
						if (perlin > 0.5f) result = 1 - 2*(1-result)*(1-perlin);
						else result = 2*perlin*result;

						curSize *= 0.5f;
						curAmount *= detail; //detail is 0.5 by default
					}

					//apply contrast and bias
					result = result*intensity;
					result -= 0*(1-bias) + (intensity-1)*bias; //0.5f - intensity*bias;

					if (result < 0) result = 0; 
					if (result > 1) result = 1;

					if (mask==null) matrix[x,z] += result;
					else matrix[x,z] += result*mask[x,z];
				}
			}
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
			layout.Field(ref seed, "Seed");
			layout.Field(ref intensity, "Intensity");
			layout.Field(ref bias, "Bias");
			layout.Field(ref size, "Size", min:1);
			layout.Field(ref detail, "Detail", max:1);
			layout.Field(ref offset, "Offset");
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Legacy", name ="Noise (Legacy 1.7)", disengageable = true)]
	public class NoiseGenerator1 : Generator
	{
		public int seed = 12345;
		public float intensity = 1f;
		public float bias = 0.0f;
		public float size = 200;
		public float detail = 0.5f;
		public Vector2 offset = new Vector2(0,0);
		//public float contrast = 0f;

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

			//Noise noise = new Noise(size, matrix.rect.size.x, MapMagic.instance.seed + seed*7, MapMagic.instance.seed + seed*3);
			//InstanceRandom random = new InstanceRandom(seed);

			//get number of iterations
			int iterations = 1; //max size iteration included
			float tempSize = size;
			for (int i=0; i<100; i++)
			{
				tempSize = tempSize/2;
				if (tempSize<1) break;
				iterations++;
			}

			//seed
			int seedX = (seed + seed*7) % 77777;
			int seedZ = (seed + seed*3) % 73333;

			Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				//float result = noise.Fractal(x+(int)(offset.x), z+(int)(offset.y), detail);
				
				//float result = random.Simplex(x/40f-10,z/40f-10);
				//float result = random.Perlin(x/20f-10,z/20f-10);
				//float result = Mathf.PerlinNoise(x/20f,z/20f);
				//float result = random.GradRandom(x,z);
				//float result = random.Perlin2(x/20f-10,z/20f-10, 1);
					
				//result = Mathf.Abs(result-0.5f);

				float result = 0.5f;
				float curSize = size;
				float curAmount = 1;

				//apply offset
			//	x += (int)(offset.x);
			//	z += (int)(offset.y);

				//making x and z resolution independent
				float rx = 1f*x / matrix.rect.size.x * 512;
				float rz = 1f*z / matrix.rect.size.x * 512;
				
				//applying noise
				for (int i=0; i<iterations;i++)
				{
					float curSizeBkcw = 8/(curSize*10+1); //for backwards compatibility. Use /curSize to get rid of extra calcualtions
						
					float perlin = Mathf.PerlinNoise(
						(rx + seedX + 1000*(i+1))*curSizeBkcw, 
						(rz + seedZ + 100*i)*curSizeBkcw );
					perlin = (perlin-0.5f)*curAmount + 0.5f;

					//applying overlay
					if (perlin > 0.5f) result = 1 - 2*(1-result)*(1-perlin);
					else result = 2*perlin*result;

					curSize *= 0.5f;
					curAmount *= detail; //detail is 0.5 by default
				}

				if (result < 0) result = 0; 
				if (result > 1) result = 1;
	
				//apply contrast and bias
				result = result*intensity;
				result -= 0*(1-bias) + (intensity-1)*bias; //0.5f - intensity*bias;

				if (result < 0) result = 0; 
				if (result > 1) result = 1;
				//not sure that this should be done twice, but I'll leave it here since it's a legacy generator

				if (mask==null) matrix[x,z] += result;
				else matrix[x,z] += result*mask[x,z];
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
			layout.Field(ref seed, "Seed");
			layout.Field(ref intensity, "Intensity");
			layout.Field(ref bias, "Bias");
			layout.Field(ref size, "Size", min:1);
			layout.Field(ref detail, "Detail", max:1);
			layout.Field(ref offset, "Offset");
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Legacy", name ="Raw Input (Legacy)", disengageable = true)]
	public class RawInput : Generator
	{
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return output; }

		public MatrixAsset textureAsset;
		public Matrix previewMatrix;
		[System.NonSerialized] public Texture2D preview;  
		public string texturePath; 
		public float intensity = 1;
		public float scale = 1;
		public Vector2 offset;
		public bool tile = false;

		public void ImportRaw (string path=null)
		{
			#if UNITY_EDITOR
			//importing
			if (path==null) path = UnityEditor.EditorUtility.OpenFilePanel("Import Texture File", "", "raw,r16");
			if (path==null || path.Length==0) return;
			if (textureAsset == null) textureAsset = ScriptableObject.CreateInstance<MatrixAsset>();
			if (textureAsset.matrix == null) textureAsset.matrix = new Matrix( new CoordRect(0,0,1,1) );

			textureAsset.matrix.ImportRaw(path);
			texturePath = path;

			//generating preview
			CoordRect previewRect = new CoordRect(0,0, 70, 70);
			previewMatrix = textureAsset.matrix.Resize(previewRect, previewMatrix);
			preview = previewMatrix.SimpleToTexture();
			#endif
		}

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			Matrix matrix = new Matrix(rect);
			if (!enabled || textureAsset==null || textureAsset.matrix==null) { output.SetObject(results, matrix); return; }
			if (stop!=null && stop(0)) return;

			//matrix = textureMatrix.Resize(matrix.rect);
			
			CoordRect scaledRect = new CoordRect(
				(int)(offset.x), 
				(int)(offset.y), 
				(int)(matrix.rect.size.x*scale),
				(int)(matrix.rect.size.z*scale) );
			Matrix scaledTexture = textureAsset.matrix.Resize(scaledRect);

			matrix.Replicate(scaledTexture, tile:tile);
			matrix.Multiply(intensity);

			if (scale > 1)
			{
				Matrix cpy = matrix.Copy();
				for (int i=0; i<scale-1; i++) matrix.Blur();
				Matrix.SafeBorders(cpy, matrix, Mathf.Max(matrix.rect.size.x/128, 4));
			}
			
			//if (tile) textureMatrix.FromTextureTiled(texture);
			//else textureMatrix.FromTexture(texture);
			
			//if (!Mathf.Approximately(scale,1)) textureMatrix = textureMatrix.Resize(matrix.rect, result:matrix);*/
			if (stop!=null && stop(0)) return;
			output.SetObject(results, matrix);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); output.DrawIcon(layout, "Output");
			layout.Par(5);
			
			//preview texture
			layout.margin = 4;
			#if UNITY_EDITOR
			int previewSize = 70;
			int controlsSize = (int)layout.field.width - previewSize - 10;
			Rect oldCursor = layout.cursor;
			if (preview == null) 
			{
				if (previewMatrix != null) preview = previewMatrix.SimpleToTexture();
				else preview = Extensions.ColorTexture(2,2,Color.black);
			}
			layout.Par(previewSize+3); layout.Inset(controlsSize);
			layout.Icon(preview, layout.Inset(previewSize+4));
			layout.cursor = oldCursor;
			
			//preview params
			layout.Par(); if (layout.Button("Browse", rect:layout.Inset(controlsSize))) { ImportRaw(); layout.change = true; }
			layout.Par(); if (layout.Button("Refresh", rect:layout.Inset(controlsSize))) { ImportRaw(texturePath); layout.change = true; }
			layout.Par(40); layout.Label("Square gray 16bit RAW, PC byte order", layout.Inset(controlsSize), helpbox:true, fontSize:9);
			#endif

			layout.fieldSize = 0.62f;
			layout.Field(ref intensity, "Intensity");
			layout.Field(ref scale, "Scale");
			layout.Field(ref offset, "Offset");
			layout.Toggle(ref tile, "Tile");
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Legacy", name ="Cavity (Legacy)", disengageable = true)]
	public class CavityGenerator : Generator
	{
		public Input input = new Input(InoutType.Map);
		public Output convexOut = new Output(InoutType.Map);
		public Output concaveOut = new Output(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; }
		public override IEnumerable<Output> Outputs() { yield return convexOut;  yield return concaveOut; }
		public float intensity = 1;
		public float spread = 0.5f;

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			//getting input
			Matrix matrix = (Matrix)input.GetObject(results);

			//return on stop/disable/null input
			if ((stop!=null && stop(0)) || !enabled || matrix==null) return; 

			//preparing outputs
			Matrix result = new Matrix(matrix.rect);
			Matrix temp = new Matrix(matrix.rect);

			//cavity
			System.Func<float,float,float,float> cavityFn = delegate(float prev, float curr, float next) 
			{
				float c = curr - (next+prev)/2;
				return (c*c*(c>0?1:-1))*intensity*100000;
			};
			result.Blur(cavityFn, intensity:1, additive:true, reference:matrix); //intensity is set in func
			if (stop!=null && stop(0)) return;

			//borders
			result.RemoveBorders(); 
			if (stop!=null && stop(0)) return;

			//spread
			result.Spread(strength:spread, copy:temp); 
			if (stop!=null && stop(0)) return;

			//clamping and inverting
			for (int i=0; i<result.count; i++) 
			{
				temp.array[i] = 0;
				if (result.array[i]<0) { temp.array[i] = -result.array[i]; result.array[i] = 0; }
			}

			//setting outputs
			if (stop!=null && stop(0)) return;
			convexOut.SetObject(results, result);
			concaveOut.SetObject(results, temp);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); input.DrawIcon(layout, "Input", mandatory:true); convexOut.DrawIcon(layout, "Convex");
			layout.Par(20); concaveOut.DrawIcon(layout, "Concave");
			layout.Par(5);
			
			//params
			layout.Field(ref intensity, "Intensity");
			layout.Field(ref spread, "Spread");
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Legacy", name ="Slope (Legacy)", disengageable = true)]
	public class SlopeGenerator : Generator
	{
		public Input input = new Input(InoutType.Map);
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; }
		public override IEnumerable<Output> Outputs() { yield return output; }
		
		public float steepness = 2.5f;
		public float range = 0.3f;

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			//getting input
			Matrix matrix = (Matrix)input.GetObject(results);

			//return on stop/disable/null input
			if ((stop!=null && stop(0)) || !enabled || matrix==null) return; 

			//preparing output
			Matrix result = new Matrix(matrix.rect);

			//using the terain-height relative values
			float dist = range;
			float start = steepness-dist/4; //4, not 2 because blurring is additive

			//transforming to 0-1 range
			start = start/terrainSize.height;
			dist = dist/terrainSize.height;

			//incline
			System.Func<float,float,float,float> inclineFn = delegate(float prev, float curr, float next) 
			{
				float prevDelta = prev-curr; if (prevDelta < 0) prevDelta = -prevDelta;
				float nextDelta = next-curr; if (nextDelta < 0) nextDelta = -nextDelta;
				float delta = prevDelta>nextDelta? prevDelta : nextDelta; 
				delta *= 1.8f; //for backwards compatibility
				float val = (delta-start)/dist; if (val < 0) val=0; if (val>1) val=1;

				return val;
			};
			result.Blur(inclineFn, intensity:1, additive:true, reference:matrix); //intensity is set in func
			
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
			layout.Field(ref steepness, "Steepness", min:0);
			layout.Field(ref range, "Range", min:0.1f);
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Legacy", name ="Stamp (legacy)", disengageable = true)]
	public class StampGenerator : Generator
	{
		public Input objectsIn = new Input(InoutType.Objects);
		public Input canvasIn = new Input(InoutType.Map);
		public Input maskIn = new Input(InoutType.Map);
		public override IEnumerable<Input> Inputs() {  yield return objectsIn; yield return canvasIn; yield return maskIn; }

		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return output; }

		public float radius = 10;
		public AnimationCurve curve = new AnimationCurve( new Keyframe[] { new Keyframe(0,0,1,1), new Keyframe(1,1,1,1) } );
		public bool useNoise = false;
		public float noiseAmount = 0.1f;
		public float noiseSize = 100;
		public bool maxHeight = true;
		public float sizeFactor = 0;
		public int safeBorders = 0;

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			//getting inputs
			SpatialHash objects = (SpatialHash)objectsIn.GetObject(results);
			Matrix src = (Matrix)canvasIn.GetObject(results);
			
			//return on stop/disable/null input
			if ((stop!=null && stop(0)) || objects==null) return; 
			if (!enabled) { output.SetObject(results, src); return; }

			//preparing output
			Matrix dst; 
			if (src != null) dst = src.Copy(null); 
			else dst = new Matrix(rect);

			//finding maximum radius
			float maxRadius = radius;
			if (sizeFactor > 0.00001f)
			{
				float maxObjSize = 0;
				foreach (SpatialObject obj in objects.AllObjs())
					if (obj.size > maxObjSize) maxObjSize = obj.size;
				maxObjSize = maxObjSize * terrainSize.pixelSize; //transforming to map-space
				maxRadius = radius*(1-sizeFactor) + radius*maxObjSize*sizeFactor;
			}

			//preparing procedural matrices
			Matrix noiseMatrix = new Matrix( new CoordRect(0,0,maxRadius*2+2,maxRadius*2+2) );
			Matrix percentMatrix = new Matrix( new CoordRect(0,0,maxRadius*2+2,maxRadius*2+2) );

			foreach (SpatialObject obj in objects.AllObjs())
			{
				//finding current radius
				float curRadius = radius*(1-sizeFactor) + radius*obj.size*sizeFactor;
				curRadius = curRadius * terrainSize.pixelSize; //transforming to map-space

				//resizing procedural matrices
				CoordRect matrixSize = new CoordRect(0,0,curRadius*2+2,curRadius*2+2);
				noiseMatrix.ChangeRect(matrixSize);
				percentMatrix.ChangeRect(matrixSize);

				//apply stamp
				noiseMatrix.rect.offset = new Coord((int)(obj.pos.x-curRadius-1), (int)(obj.pos.y-curRadius-1));
				percentMatrix.rect.offset = new Coord((int)(obj.pos.x-curRadius-1), (int)(obj.pos.y-curRadius-1));

				CoordRect intersection = CoordRect.Intersect(noiseMatrix.rect, dst.rect);
				Coord min = intersection.Min; Coord max = intersection.Max; 
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					float dist = Mathf.Sqrt((x-obj.pos.x+0.5f)*(x-obj.pos.x+0.5f) + (z-obj.pos.y+0.5f)*(z-obj.pos.y+0.5f));
					float percent = 1f - dist / curRadius; 
					if (percent < 0 || dist > curRadius) percent = 0;

					percentMatrix[x,z] = percent;
				}

				//adjusting value by curve
				Curve c = new Curve(curve);
				for (int i=0; i<percentMatrix.array.Length; i++) percentMatrix.array[i] = c.Evaluate(percentMatrix.array[i]);

				//adding some noise
				if (useNoise) 
				{
					NoiseGenerator.Noise(noiseMatrix, noiseSize, 0.5f, offset:Vector2.zero);

					for (int x=min.x; x<max.x; x++)
						for (int z=min.z; z<max.z; z++)
					{
						float val = percentMatrix[x,z];
						if (val < 0.0001f) continue;

						float noise = noiseMatrix[x,z];
						if (val < 0.5f) noise *= val*2;
						else noise = 1 - (1-noise)*(1-val)*2;

						percentMatrix[x,z] = noise*noiseAmount + val*(1-noiseAmount);
					}
				}

				//applying matrices
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					//float distSq = (x-obj.pos.x)*(x-obj.pos.x) + (z-obj.pos.y)*(z-obj.pos.y);
					//if (distSq > radius*radius) continue;
					
					float percent = percentMatrix[x,z];
					dst[x,z] = (maxHeight? 1:obj.height)*percent + dst[x,z]*(1-percent);
				}
			}

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
			layout.Par(20); objectsIn.DrawIcon(layout, "Objects", mandatory:true); output.DrawIcon(layout, "Output");
			layout.Par(20); canvasIn.DrawIcon(layout, "Canvas");
			layout.Par(20); maskIn.DrawIcon(layout, "Mask");
			layout.Par(5);

			//params
			layout.margin=5;
			layout.Field(ref radius, "Radius");
			layout.Label("Fallof:");
			
			//curve
			Rect cursor = layout.cursor;
			layout.Par(53);
			layout.Curve(curve, rect:layout.Inset(80));

			layout.cursor = cursor; 
			int margin = layout.margin; layout.margin = 86; layout.fieldSize = 0.8f;
			layout.Toggle(ref useNoise, "Noise");
			layout.Field(ref noiseAmount, "A");
			layout.Field(ref noiseSize, "S");
			
			layout.Par(5); layout.margin = margin; layout.fieldSize = 0.5f;
			layout.Field(ref sizeFactor, "Size Factor");
			layout.Field(ref safeBorders, "Safe Borders");
			layout.Toggle(ref maxHeight,"Use Maximum Height");
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Legacy", name ="Preview", disengageable = true)]
	public class PreviewOutput1 : Generator
	{
		public Input input = new Input(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; }

		public bool onTerrain = false;
		public bool inWindow = false;
		public Color blacks = new Color(1,0,0,0); public Color oldBlacks;
		public Color whites = new Color(0,1,0,0); public Color oldWhites;

		public delegate void RefreshWindow(object obj);
		//public event RefreshWindow OnObjectChanged;

		//[System.NonSerialized] public SplatPrototype[] prototypes = new SplatPrototype[2]; 
		public SplatPrototype redPrototype = new SplatPrototype();
		public SplatPrototype greenPrototype = new SplatPrototype();

		public override void OnGUI (GeneratorsAsset gens)
		{
			layout.Par(20); input.DrawIcon(layout, "Matrix", mandatory:true);
			layout.Par(5);

			layout.Field(ref onTerrain, "On Terrain");
			layout.Field(ref inWindow, "In Window");
			layout.Field(ref whites, "Whites");
			layout.Field(ref blacks, "Blacks");
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Legacy", name ="Subtract 0 (Legacy)", disengageable = true)]
	public class SubtractGenerator0 : Generator
	{
		public Input minuendIn = new Input(InoutType.Objects);
		public Input subtrahendIn = new Input(InoutType.Objects);
		public Output minuendOut = new Output(InoutType.Objects);
		public override IEnumerable<Input> Inputs() { yield return minuendIn; yield return subtrahendIn; }
		public override IEnumerable<Output> Outputs() { yield return minuendOut; }

		public float distance = 1;
		public float sizeFactor = 0;

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			//getting inputs
			SpatialHash minuend = (SpatialHash)minuendIn.GetObject(results);
			SpatialHash subtrahend = (SpatialHash)subtrahendIn.GetObject(results);

			//return on stop/disable/null input
			if ((stop!=null && stop(0)) || minuend==null) return;
			if (!enabled || subtrahend==null || subtrahend.Count==0) { minuendOut.SetObject(results, minuend); return; }

			//preparing output
			SpatialHash result = new SpatialHash(minuend.offset, minuend.size, minuend.resolution);

			//transforming distance to map-space
			float dist = distance * terrainSize.pixelSize; 

			//finding maximum seek distance
			float maxObjSize = 0;
			foreach (SpatialObject obj in subtrahend.AllObjs())
				if (obj.size > maxObjSize) maxObjSize = obj.size;
			maxObjSize = maxObjSize * terrainSize.pixelSize; //transforming to map-space
			float maxDist = dist*(1-sizeFactor) + dist*maxObjSize*sizeFactor;

			foreach (SpatialObject obj in minuend.AllObjs())
			{
				bool inRange = false;

				foreach (SpatialObject closeObj in subtrahend.ObjsInRange(obj.pos, maxDist))
				{
					float minDist = (obj.pos - closeObj.pos).magnitude;
					if (minDist < dist*(1-sizeFactor) + dist*closeObj.size*sizeFactor) { inRange = true; break; }
				}

				if (!inRange) result.Add(obj);

				//SpatialObject closestObj = subtrahend.Closest(obj.pos,false);
				//float minDist = (obj.pos - closestObj.pos).magnitude;

				//if (minDist > dist*(1-sizeFactor) + dist*closestObj.size*sizeFactor) result.Add(obj);
			}

			//setting output
			if (stop!=null && stop(0)) return;
			minuendOut.SetObject(results, result);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); minuendIn.DrawIcon(layout, "Input"); minuendOut.DrawIcon(layout, "Output");
			layout.Par(20); subtrahendIn.DrawIcon(layout, "Subtrahend");
			layout.Par(5);
			
			//params
			layout.Field(ref distance, "Distance");
			layout.Field(ref sizeFactor, "Size Factor"); 
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Legacy", name ="Floor (Outdated)", disengageable = true)]
	public class FloorGenerator : Generator
	{
		public Input objsIn = new Input(InoutType.Objects);
		public Input substrateIn = new Input(InoutType.Map);
		public Output objsOut = new Output(InoutType.Objects);
		public override IEnumerable<Input> Inputs() { yield return objsIn; yield return substrateIn; }
		public override IEnumerable<Output> Outputs() { yield return objsOut; }

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			//getting inputs
			SpatialHash objs = (SpatialHash)objsIn.GetObject(results);
			Matrix substrate = (Matrix)substrateIn.GetObject(results);
			
			//return on stop/disable/null input
			if ((stop!=null && stop(0)) || objs==null) return; 
			if (!enabled || substrate==null) { objsOut.SetObject(results, objs); return; }
			
			//preparing output
			objs = objs.Copy();

			for (int c=0; c<objs.cells.Length; c++)
			{
				List<SpatialObject> objList = objs.cells[c].objs;
				int objsCount = objList.Count;
				for (int i=0; i<objsCount; i++)
				{
					SpatialObject obj = objList[i];
					//obj.height = substrate[(int)obj.pos.x, (int)obj.pos.y];
					obj.height = substrate.GetInterpolatedValue(obj.pos);
				}
			}

			//setting output
			if (stop!=null && stop(0)) return;
			objsOut.SetObject(results, objs);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); objsIn.DrawIcon(layout, "Input", mandatory:true); objsOut.DrawIcon(layout, "Output");
			layout.Par(20); substrateIn.DrawIcon(layout, "Height");
			layout.Par(5);
			
			//params
			layout.Par(55);
			layout.Label("Floor Generator is outdated. To floor objects to terrain use \"Relative Height\" toggle in Object Output toggle.", rect:layout.Inset(), helpbox:true, fontSize:9);
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Legacy", name ="Simple Form (Legacy)", disengageable = true)]
	public class SimpleForm : Generator
	{
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return output; }

		public enum FormType { GradientX, GradientZ, Pyramid, Cone }
		public FormType type = FormType.Cone;
		public float intensity = 1;
		public float scale = 1;
		public Vector2 offset;
		public bool tile = false; //outdated
		public Matrix.WrapMode wrap = Matrix.WrapMode.Once;

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			if (!enabled || (stop!=null && stop(0))) return;

			CoordRect scaledRect = new CoordRect(
				(int)(offset.x * terrainSize.resolution / terrainSize.dimensions), 
				(int)(offset.y * terrainSize.resolution / terrainSize.dimensions), 
				(int)(terrainSize.resolution*scale),
				(int)(terrainSize.resolution*scale) );
			Matrix stampMatrix = new Matrix(scaledRect);

			float gradientStep = 1f/stampMatrix.rect.size.x;
			Coord center = scaledRect.Center;
			float radius = stampMatrix.rect.size.x / 2f;
			Coord min = stampMatrix.rect.Min; Coord max = stampMatrix.rect.Max;
			
			switch (type)
			{
				case FormType.GradientX:
					for (int x=min.x; x<max.x; x++)
						for (int z=min.z; z<max.z; z++)
							stampMatrix[x,z] = x*gradientStep;
					break;
				case FormType.GradientZ:
					for (int x=min.x; x<max.x; x++)
						for (int z=min.z; z<max.z; z++)
							stampMatrix[x,z] = z*gradientStep;
					break;
				case FormType.Pyramid:
					for (int x=min.x; x<max.x; x++)
						for (int z=min.z; z<max.z; z++)
						{
							float valX = x*gradientStep; if (valX > 1-valX) valX = 1-valX;
							float valZ = z*gradientStep; if (valZ > 1-valZ) valZ = 1-valZ;
							stampMatrix[x,z] = valX<valZ? valX*2 : valZ*2;
						}
					break;
				case FormType.Cone:
					for (int x=min.x; x<max.x; x++)
						for (int z=min.z; z<max.z; z++)
						{
							float val = 1 - (Coord.Distance(new Coord(x,z), center) / radius);
							if (val<0) val = 0;
							stampMatrix[x,z] = val;
						}
					break;
			}

			Matrix matrix = new Matrix(rect);
			matrix.Replicate(stampMatrix, tile:tile);
			matrix.Multiply(intensity);

			
			//if (tile) textureMatrix.FromTextureTiled(texture);
			//else textureMatrix.FromTexture(texture);
			
			//if (!Mathf.Approximately(scale,1)) textureMatrix = textureMatrix.Resize(matrix.rect, result:matrix);
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
			if (tile) wrap = Matrix.WrapMode.Tile; tile=false;
			layout.Field(ref wrap, "Wrap Mode");
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Legacy", name ="Scatter (Legacy)", disengageable = true)]
	public class ScatterGenerator : Generator
	{
		public Input probability = new Input(InoutType.Map);
		public Output output = new Output(InoutType.Objects);
		public override IEnumerable<Input> Inputs() { yield return probability; }
		public override IEnumerable<Output> Outputs() { yield return output; }

		public int seed = 12345;
		public float count = 10;
		public float uniformity = 0.1f; //aka candidatesNum/100

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			Matrix probMatrix = (Matrix)probability.GetObject(results);
			SpatialHash spatialHash = new SpatialHash(new Vector2(rect.offset.x,rect.offset.z), rect.size.x, 16);
			if (!enabled) { output.SetObject(results, spatialHash); return; }
			if (stop!=null && stop(0)) return; 

			InstanceRandom rnd = new InstanceRandom(seed + this.seed + terrainSize.Seed(rect));

			//Rect terrainRect = terrain.coord.ToRect(terrain.size);
			//terrainRect.position += Vector2.one; terrainRect.size-=Vector2.one*2;
			
			//SpatialHash spatialHash = new SpatialHash(terrain.coord.ToVector2(terrain.size), terrain.size, 16);
			
			
			//float square = terrainRect.width * terrainRect.height;
			//float count = square*(density/1000000); //number of items per terrain
			
			//positioned scatter
			/*float sideCount = Mathf.Sqrt(count);
			float step = spatialHash.size / sideCount;

			//int uniformity = 100;
			//Random.seed = 12345;
			for (float x=spatialHash.offset.x+step/2; x<spatialHash.offset.x+spatialHash.size-step/2; x+=step)
				for (float y=spatialHash.offset.y+step/2; y<spatialHash.offset.y+spatialHash.size-step/2; y+=step)
			{
				Vector2 offset = new Vector2(((Random.value*2-1)*uniformity), ((Random.value*2-1)*uniformity));
				Vector2 point = new Vector2(x,y) + offset;
				if (point.x > spatialHash.size) point.x -= spatialHash.size; if (point.x < 0) point.x += spatialHash.size;
				if (point.y > spatialHash.size) point.y -= spatialHash.size; if (point.y < 0) point.y += spatialHash.size;
				spatialHash.Add(point, 0,0,0);
			}*/

			//realRandom algorithm
			int candidatesNum = (int)(uniformity*100);
			
			for (int i=0; i<count; i++)
			{
				Vector2 bestCandidate = Vector3.zero;
				float bestDist = 0;
				
				for (int c=0; c<candidatesNum; c++)
				{
					Vector2 candidate = new Vector2((spatialHash.offset.x+1) + (rnd.Random()*(spatialHash.size-2.01f)), (spatialHash.offset.y+1) + (rnd.Random()*(spatialHash.size-2.01f)));
				
					//checking if candidate available here according to probability map
					if (probMatrix!=null && probMatrix[candidate] < rnd.Random()+0.0001f) continue;

					//checking if candidate is the furthest one
					float dist = spatialHash.MinDist(candidate);
					if (dist>bestDist) { bestDist=dist; bestCandidate = candidate; }
				}

				if (bestDist>0.001f) spatialHash.Add(bestCandidate, 0, 0, 1); //adding only if some suitable candidate found
			}

			if (stop!=null && stop(0)) return;
			output.SetObject(results, spatialHash);
		}


		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); probability.DrawIcon(layout, "Probability"); output.DrawIcon(layout, "Output");
			layout.Par(5);

			//params
			layout.Field(ref seed, "Seed");
			layout.Field(ref count, "Count");
			layout.Field(ref uniformity, "Uniformity", max:1);
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Legacy", name ="Blend (Legacy)", disengageable = true)]
	public class BlendGenerator : Generator
	{
		public Input baseInput = new Input(InoutType.Map);
		public Input blendInput = new Input(InoutType.Map);
		public Input maskInput = new Input(InoutType.Map);
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return baseInput; yield return blendInput; yield return maskInput; }
		public override IEnumerable<Output> Outputs() { yield return output; }

		public enum Algorithm {mix=0, add=1, subtract=2, multiply=3, divide=4, difference=5, min=6, max=7, overlay=8, hardLight=9, softLight=10} 
		public Algorithm algorithm = Algorithm.mix;
		public float opacity = 1;

		//outdated
		public enum GuiAlgorithm {mix, add, subtract, multiply, divide, difference, min, max, overlay, hardLight, softLight, none} 
		public GuiAlgorithm guiAlgorithm = GuiAlgorithm.mix;

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
			//preparing inputs
			Matrix baseMatrix = (Matrix)baseInput.GetObject(results);
			Matrix blendMatrix = (Matrix)blendInput.GetObject(results);
			Matrix maskMatrix = (Matrix)maskInput.GetObject(results);
			
			//return on stop/disable/null input
			if (stop!=null && stop(0)) return;
			if (!enabled || blendMatrix==null || baseMatrix==null) { output.SetObject(results, baseMatrix); return; }
			
			//preparing output
			baseMatrix = baseMatrix.Copy(null);

			//setting algorithm
			if (guiAlgorithm != GuiAlgorithm.none) { algorithm = (Algorithm)guiAlgorithm; guiAlgorithm = GuiAlgorithm.none; }
			System.Func<float,float,float> algorithmFn = GetAlgorithm(algorithm);


			//special fast cases for mix and add
			/*if (maskMatrix == null && guiAlgorithm == GuiAlgorithm.mix)
				for (int i=0; i<baseMatrix.array.Length; i++)
				{
					float a = baseMatrix.array[i];
					float b = blendMatrix.array[i];
					baseMatrix.array[i] = a*(1-opacity) + b*opacity;
				}
			else if (maskMatrix != null && guiAlgorithm == GuiAlgorithm.mix)
				for (int i=0; i<baseMatrix.array.Length; i++)
				{
					float m = maskMatrix.array[i] * opacity;
					float a = baseMatrix.array[i];
					float b = blendMatrix.array[i];
					baseMatrix.array[i] = a*(1-m) + b*m;
				}
			else if (maskMatrix == null && guiAlgorithm == GuiAlgorithm.add)
				for (int i=0; i<baseMatrix.array.Length; i++)
				{
					float a = baseMatrix.array[i];
					float b = blendMatrix.array[i];
					baseMatrix.array[i] = a + b*opacity;
				}
			else if (maskMatrix != null && guiAlgorithm == GuiAlgorithm.mix)
				for (int i=0; i<baseMatrix.array.Length; i++)
				{
					float m = maskMatrix.array[i] * opacity;
					float a = baseMatrix.array[i];
					float b = blendMatrix.array[i];
					baseMatrix.array[i] = a + b*m;
				}*/

			//generating all other
			//else
				for (int i=0; i<baseMatrix.array.Length; i++)
				{
					float m = (maskMatrix==null ? 1 : maskMatrix.array[i]) * opacity;
					float a = baseMatrix.array[i];
					float b = blendMatrix.array[i];

					baseMatrix.array[i] = a*(1-m) + algorithmFn(a,b)*m;
				}
		
			//setting output
			if (stop!=null && stop(0)) return;
			output.SetObject(results, baseMatrix);
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); baseInput.DrawIcon(layout, "Base", mandatory:true); output.DrawIcon(layout, "Output");
			layout.Par(20); blendInput.DrawIcon(layout, "Blend", mandatory:true);
			layout.Par(20); maskInput.DrawIcon(layout, "Mask");
			layout.Par(5);
			
			//params
			if (guiAlgorithm != GuiAlgorithm.none) { algorithm = (Algorithm)guiAlgorithm; guiAlgorithm = GuiAlgorithm.none; }
			layout.Field(ref algorithm, "Algorithm");
			layout.Field(ref opacity, "Opacity");
		}
	}

		[System.Serializable]
	[GeneratorMenu (menu="Legacy", name ="Blend (Legacy 1.5)", disengageable = true)]
	public class BlendGenerator1 : Generator
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

		public void UnlinkLayer (int num)
		{
			layers[num].input.Link(null,null); //unlink input
			//layers[num].output.UnlinkInActiveGens(); //try to unlink output
		}

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
			Matrix matrix = null;
			if (baseMatrix==null) matrix = new Matrix(rect);
			else matrix = baseMatrix.Copy(null); //7994

			//processing
			for (int l=0; l<layers.Length; l++)
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
					float v = 0;

					switch (layer.algorithm)
					{
						case Algorithm.mix: v = a*(1-m) + b*m; break;
						case Algorithm.add: v = a+b; break;//a*(1-m) + (a+b)*m; break;
						case Algorithm.multiply: v = a*(1-m) + (a*b)*m; break;
						case Algorithm.subtract: v = a*(1-m) + (a-b)*m; break;
						default: v = a*(1-m) + algorithmFn(a,b)*m; break;
					}

					//if (v<0) v=0; if (v>1) v=1;

					matrix.array[i] = v;

					//matrix.array[i] = a*(1-m) + algorithmFn(a,b)*m;
				}
			}

		
			//setting output
			if (stop!=null && stop(0)) return;
			output.SetObject(results, matrix);
		}

		public void OnLayerGUI (Layout layout, bool selected, int num) 
		{
			layout.margin += 10; layout.rightMargin +=5;
			layout.Par(20);
			layers[num].input.DrawIcon(layout, "", mandatory:false);
			layout.Field(ref layers[num].algorithm, rect:layout.Inset(0.5f), disabled:num==0);
			layout.Inset(0.05f);
			layout.Icon("MapMagic_Opacity", rect:layout.Inset(0.1f), horizontalAlign:Layout.IconAligment.center, verticalAlign:Layout.IconAligment.center);
			layout.Field(ref layers[num].opacity, rect:layout.Inset(0.35f), disabled:num==0);
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
			layout.DrawArrayRemove(ref layers, ref guiSelected, layout.Inset(0.15f), reverse:true, onBeforeRemove:UnlinkLayer);
			layout.DrawArrayDown(ref layers, ref guiSelected, layout.Inset(0.15f), dispUp:true);
			layout.DrawArrayUp(ref layers, ref guiSelected, layout.Inset(0.15f), dispDown:true);

			layout.margin = 10;
			layout.Par(5);
			for (int num=layers.Length-1; num>=0; num--)
				layout.DrawLayer(OnLayerGUI, ref guiSelected, num);
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Legacy", name ="Blob (Legacy 1.7)", disengageable = true)]
	public class BlobGenerator1 : Generator
	{
		public Input objectsIn = new Input(InoutType.Objects);
		public Input canvasIn = new Input(InoutType.Map);
		public Input maskIn = new Input(InoutType.Map);
		public override IEnumerable<Input> Inputs() {  yield return objectsIn; yield return canvasIn; yield return maskIn; }

		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return output; }

		public float intensity = 1f;
		public float radius = 10;
		public float sizeFactor = 0;
		public AnimationCurve fallof = new AnimationCurve( new Keyframe[] { new Keyframe(0,0,1,1), new Keyframe(1,1,1,1) } );
		public float noiseAmount = 0.1f;
		public float noiseSize = 100;
		public int safeBorders = 0;

		public static void DrawBlob (Matrix canvas, Vector2 pos, float val, float radius, AnimationCurve fallof, float noiseAmount=0, float noiseSize=20)
		{
			CoordRect blobRect = new CoordRect(
				(int)(pos.x-radius-1), (int)(pos.y-radius-1),
				radius*2+2, radius*2+2 );

			Curve curve = new Curve(fallof);
			InstanceRandom noise = new InstanceRandom(noiseSize, 512, 12345, 123); //TODO: use normal noise instead

			CoordRect intersection = CoordRect.Intersect(canvas.rect, blobRect);
			Coord center = blobRect.Center;
			Coord min = intersection.Min; Coord max = intersection.Max; 
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				float dist = Coord.Distance(center, new Coord(x,z));
				float percent = curve.Evaluate(1f-dist/radius);
				float result = percent;

				if (noiseAmount > 0.001f)
				{
					float maxNoise = percent; if (percent > 0.5f) maxNoise = 1-percent;
					result += (noise.Fractal(x,z)*2 - 1) * maxNoise * noiseAmount;
				}

				//canvas[x,z] = Mathf.Max(result*val, canvas[x,z]);
				canvas[x,z] = val*result + canvas[x,z]*(1-result);
			}
		}

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			//getting inputs
			SpatialHash objects = (SpatialHash)objectsIn.GetObject(results);
			Matrix src = (Matrix)canvasIn.GetObject(results);
			
			//return on stop/disable/null input
			if (stop!=null && stop(0)) return; 
			if (!enabled || objects==null) { output.SetObject(results, src); return; }

			//preparing output
			Matrix dst; 
			if (src != null) dst = src.Copy(null); 
			else dst = new Matrix(rect);

			foreach (SpatialObject obj in objects.AllObjs())
			{
				//finding current radius
				float curRadius = radius*(1-sizeFactor) + radius*obj.size*sizeFactor;
				curRadius = curRadius * terrainSize.pixelSize; //transforming to map-space

				DrawBlob(dst, obj.pos, intensity, curRadius, fallof, noiseAmount, noiseSize);
			}

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
			layout.Par(20); objectsIn.DrawIcon(layout, "Objects", mandatory:true); output.DrawIcon(layout, "Output");
			layout.Par(20); canvasIn.DrawIcon(layout, "Canvas");
			layout.Par(20); maskIn.DrawIcon(layout, "Mask");
			layout.Par(5);

			//params
			layout.margin=5;
			layout.Field(ref intensity, "Intensity");
			layout.Field(ref radius, "Radius");
			layout.Field(ref sizeFactor, "Size Factor");
			layout.Field(ref safeBorders, "Safe Borders");

			layout.Label("Fallof:");
			
			//curve
			Rect cursor = layout.cursor;
			layout.Par(53);
			layout.Curve(fallof, rect:layout.Inset(80));

			//noise
			layout.cursor = cursor; 
			layout.margin = 86; layout.fieldSize = 0.8f;
			layout.Label("Noise");
			layout.Field(ref noiseAmount, "A");
			layout.Field(ref noiseSize, "S");
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Legacy", name ="Flatten (Legacy 1.7)", disengageable = true)]
	public class FlattenGenerator1 : Generator
	{
		public Input objectsIn = new Input(InoutType.Objects);
		public Input canvasIn = new Input(InoutType.Map);
		public Input maskIn = new Input(InoutType.Map);
		public override IEnumerable<Input> Inputs() {  yield return objectsIn; yield return canvasIn; yield return maskIn; }

		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return output; }

		public float radius = 10;
		public float sizeFactor = 0;
		public AnimationCurve fallof = new AnimationCurve( new Keyframe[] { new Keyframe(0,0,1,1), new Keyframe(1,1,1,1) } );
		public float noiseAmount = 0.1f;
		public float noiseSize = 100;
		public int safeBorders = 0;

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			//getting inputs
			SpatialHash objects = (SpatialHash)objectsIn.GetObject(results);
			Matrix src = (Matrix)canvasIn.GetObject(results);
			
			//return on stop/disable/null input
			if (stop!=null && stop(0)) return; 
			if (!enabled || objects==null || src==null) { output.SetObject(results, src); return; }

			//preparing output
			Matrix dst; 
			if (src != null) dst = src.Copy(null); 
			else dst = new Matrix(rect);

			foreach (SpatialObject obj in objects.AllObjs())
			{
				//finding current radius
				float curRadius = radius*(1-sizeFactor) + radius*obj.size*sizeFactor;
				curRadius = curRadius * terrainSize.pixelSize; //transforming to map-space

				float objHeight = src.GetInterpolated(obj.pos.x, obj.pos.y);
				BlobGenerator.DrawBlob(dst, obj.pos, objHeight, curRadius, fallof, noiseAmount, noiseSize);
			}

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
			layout.Par(20); objectsIn.DrawIcon(layout, "Objects", mandatory:true); output.DrawIcon(layout, "Output");
			layout.Par(20); canvasIn.DrawIcon(layout, "Canvas", mandatory:true);
			layout.Par(20); maskIn.DrawIcon(layout, "Mask");
			layout.Par(5);

			//params
			layout.margin=5;
			layout.Field(ref radius, "Radius");
			layout.Field(ref sizeFactor, "Size Factor");
			layout.Field(ref safeBorders, "Safe Borders");

			layout.Label("Fallof:");
			
			//curve
			Rect cursor = layout.cursor;
			layout.Par(53);
			layout.Curve(fallof, rect:layout.Inset(80));

			//noise
			layout.cursor = cursor; 
			layout.margin = 86; layout.fieldSize = 0.8f;
			layout.Label("Noise");
			layout.Field(ref noiseAmount, "A");
			layout.Field(ref noiseSize, "S");
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Legacy", name ="Noise (Legacy 1.8)", disengageable = true)]
	public class NoiseGenerator2 : Generator
	{
		public int seed = 12345;
		public float high = 1f;
		public float low = 0f;
		//public int octaves = 10;
		//public float persistence = 0.5f;
		public float size = 200f;
		public float detail = 0.5f;
		public float turbulence = 0f;
		public Vector2 offset = new Vector2(0,0);
		
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
					float val = noise.LegacyFractal(x,z,size/512f*terrainSize.resolution,iterations,detail,turbulence,type==Type.Unity?-1:(int)type);
					
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
	[GeneratorMenu (menu="Legacy", name ="Scatter (Legacy 1.8.3)", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/object_generators/Scatter")]
	public class ScatterGenerator1 : Generator
	{
		public Input probability = new Input(InoutType.Map);
		public Output output = new Output(InoutType.Objects);
		public override IEnumerable<Input> Inputs() { yield return probability; }
		public override IEnumerable<Output> Outputs() { yield return output; }

		public int seed = 12345;
		public enum Algorithm { Random, SquareCells, HexCells };
		public Algorithm algorithm = Algorithm.Random;
		public float density = 10;
		public float uniformity = 0.1f;
		public float relax = 0.1f;
		public int safeBorders = 2;

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			Matrix probMatrix = (Matrix)probability.GetObject(results);
			if (!enabled || (stop!=null && stop(0))) return;
			SpatialHash spatialHash = new SpatialHash(new Vector2(rect.offset.x,rect.offset.z), rect.size.x, 16);
			
			//initializing random
			InstanceRandom rnd = new InstanceRandom(seed + this.seed + terrainSize.Seed(rect));

			float square = rect.size.x * rect.size.z;
			int count = (int)(square*(density/1000000)); //number of items per terrain
			
			if (algorithm==Algorithm.Random) RandomScatter(count, spatialHash, rnd, probMatrix, stop:stop);  
			else CellScatter(count, spatialHash, rnd, probMatrix, hex:algorithm==Algorithm.HexCells, stop:stop);

			if (stop!=null && stop(0)) return;
			output.SetObject(results, spatialHash);
		}

		public void RandomScatter (int count, SpatialHash spatialHash, InstanceRandom rnd, Matrix probMatrix, Func<float,bool> stop = null)
		{
			int candidatesNum = (int)(uniformity*100);
			if (candidatesNum < 1) candidatesNum = 1;
			
			for (int i=0; i<count; i++)
			{
				if (stop!=null && stop(0)) return;

				Vector2 bestCandidate = Vector3.zero;
				float bestDist = 0;
				
				for (int c=0; c<candidatesNum; c++)
				{
					Vector2 candidate = new Vector2((spatialHash.offset.x+1) + (rnd.Random()*(spatialHash.size-2.01f)), (spatialHash.offset.y+1) + (rnd.Random()*(spatialHash.size-2.01f)));
				
					//checking if candidate available here according to probability map
					//if (probMatrix!=null && probMatrix[candidate] < rnd.Random()+0.0001f) continue;

					//checking if candidate is the furthest one
					float dist = spatialHash.MinDist(candidate);

					//distance to the edge
					float bd = (candidate.x-spatialHash.offset.x)*2; if (bd < dist) dist = bd;
					bd = (candidate.y-spatialHash.offset.y)*2; if (bd < dist) dist = bd;
					bd = (spatialHash.offset.x+spatialHash.size-candidate.x)*2; if (bd < dist) dist = bd;
					bd = (spatialHash.offset.y+spatialHash.size-candidate.y)*2; if (bd < dist) dist = bd;

					if (dist>bestDist) { bestDist=dist; bestCandidate = candidate; }
				}

				if (bestDist>0.001f) 
				{
					spatialHash.Add(bestCandidate, 0, 0, 1); //adding only if some suitable candidate found
				}
			}

			//masking
			for (int c=0; c<spatialHash.cells.Length; c++)
			{
				SpatialHash.Cell cell = spatialHash.cells[c];
				for (int i=cell.objs.Count-1; i>=0; i--)
				{
					if (stop!=null && stop(0)) return;

					Vector2 pos = cell.objs[i].pos;
				
					if (pos.x < spatialHash.offset.x+safeBorders || 
						pos.y < spatialHash.offset.y+safeBorders ||
						pos.x > spatialHash.offset.x+spatialHash.size-safeBorders ||
						pos.y > spatialHash.offset.y+spatialHash.size-safeBorders ) { cell.objs.RemoveAt(i); continue; }

					if (probMatrix!=null && probMatrix[pos] < rnd.Random()+0.0001f) { cell.objs.RemoveAt(i); continue; }
				}
			}
		}

		public void CellScatter (int count, SpatialHash spatialHash, InstanceRandom rnd, Matrix probMatrix, bool hex=true, Func<float,bool> stop = null)
		{
			//finding scatter rect
			CoordRect rect = new CoordRect(spatialHash.offset.x+1, spatialHash.offset.y+1, spatialHash.size-2, spatialHash.size-2);
			rect.Contract(safeBorders);

			//positioned scatter
			float sideCount = Mathf.Sqrt(count);
			//float step = 1f * rect.size.x / sideCount;

			//scattering in hexagonal order
			float heightFactor = 1; if (hex) heightFactor = 0.8660254f;
			Matrix2<Vector2> positions = new Matrix2<Vector2>( new CoordRect(0,0,sideCount,(int)(sideCount/heightFactor)) );
			Vector2 cellSize = new Vector2(spatialHash.size/positions.rect.size.x, spatialHash.size/positions.rect.size.z);

			Coord min = positions.rect.Min; Coord max = positions.rect.Max;
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				if (stop!=null && stop(0)) return;
				
				Vector2 position = new Vector2(x*cellSize.x + spatialHash.offset.x,  z*cellSize.y + spatialHash.offset.y);
				position.x += cellSize.x/2; position.y += cellSize.y/2;
				if (hex && z%2!=0) position.x += cellSize.x/2;
				
				//random
				position += new Vector2( rnd.CoordinateRandom(x,z*2000)-0.5f, rnd.CoordinateRandom(x*3000,z)-0.5f ) * cellSize.x * (1-uniformity); 

				positions[x,z] = position;
			}

			//relaxing
			Matrix2<Vector2> newPositions = new Matrix2<Vector2>(positions.rect);
			Vector2[] closestPositions = new Vector2[8];

			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				if (stop!=null && stop(0)) return;

				if (x==min.x || x==max.x-1 || z==min.z || z==max.z-1) { newPositions[x,z] = positions[x,z]; continue; }

				Vector2 position = positions[x,z];
				Vector2 relaxDir = new Vector2();

				//getting closest positions
				closestPositions[0] = positions[x-1,z+1]; closestPositions[1] = positions[x,z+1]; closestPositions[2] = positions[x+1,z+1];
				closestPositions[3] = positions[x-1,z];											  closestPositions[4] = positions[x+1,z];
				closestPositions[5] = positions[x-1,z-1]; closestPositions[6] = positions[x,z-1]; closestPositions[7] = positions[x+1,z-1];

				//relaxing
				for (int i=0; i<8; i++)
				{
					Vector2 deltaVec = (position-closestPositions[i]) / cellSize.x; //in cells
					float deltaDist = deltaVec.magnitude;
					if (deltaDist > 1) deltaDist = 1;

					float relaxFactor = (1-deltaDist)*(1-deltaDist); //1 / deltaVec.magnitude;

					relaxDir += deltaVec.normalized * relaxFactor;
				}

				//float relaxDirMagnitude = relaxDir.magnitude;
				//relaxDir /= relaxDirMagnitude;
				//relaxDirMagnitude = Mathf.Sqrt(relaxDirMagnitude);

				newPositions[x,z] = position + relaxDir*relax*(cellSize.x/2);
			}

			//masking and converting to spatial hash
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				if (stop!=null && stop(0)) return;
				
				Vector2 pos = newPositions[x,z];

				if (pos.x < spatialHash.offset.x+safeBorders+0.001f || 
					pos.y < spatialHash.offset.y+safeBorders+0.001f ||
					pos.x > spatialHash.offset.x+spatialHash.size-safeBorders-0.001f ||
					pos.y > spatialHash.offset.y+spatialHash.size-safeBorders-0.001f ) continue;

				if (probMatrix!=null && probMatrix[(int)pos.x, (int)pos.y] < rnd.CoordinateRandom(x,z*1000)+0.0001f) continue;

				spatialHash.Add(pos, 0,0,1);
			}
		}


		public override void OnGUI (GeneratorsAsset gens)
		{
			//inouts
			layout.Par(20); probability.DrawIcon(layout, "Probability"); output.DrawIcon(layout, "Output");
			layout.Par(5);

			//params
			layout.Field(ref algorithm, "Algorithm");
			layout.Field(ref seed, "Seed");
			layout.Field(ref density, "Density");
			layout.Field(ref uniformity, "Uniformity", min:0, max:1);
			layout.Field(ref relax, "Relax", min:0, max:1);
			layout.Field(ref safeBorders, "Safe Borders", min:0);
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Legacy", name ="Stamp (Legacy 1.8.3)", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/object_generators/Stamp")]
	public class StampGenerator1 : Generator
	{
		public Input stampIn = new Input(InoutType.Map);
		public Input canvasIn = new Input(InoutType.Map);
		public Input positionsIn = new Input(InoutType.Objects);
		public Input maskIn = new Input(InoutType.Map);
		public override IEnumerable<Input> Inputs() {  yield return positionsIn; yield return canvasIn; yield return stampIn; yield return maskIn; }

		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return output; }

		public BlendGenerator.Algorithm guiAlgorithm = BlendGenerator.Algorithm.max;
		public float radius = 1;
		public float sizeFactor = 1;
		public int safeBorders = 0;

		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			//getting inputs
			Matrix stamp = (Matrix)stampIn.GetObject(results);
			Matrix src = (Matrix)canvasIn.GetObject(results);
			SpatialHash objs = (SpatialHash)positionsIn.GetObject(results);
			
			//return on stop/disable/null input
			if (stop!=null && stop(0)) return; 
			if (!enabled || stamp==null || objs==null) { output.SetObject(results,src); return; }

			//preparing output
			Matrix dst = null;
			if (src==null) dst = new Matrix(rect); 
			else dst = src.Copy(null);

			//algorithm
			System.Func<float,float,float> algorithm = BlendGenerator.GetAlgorithm(guiAlgorithm);

			foreach (SpatialObject obj in objs.AllObjs())
			{
				//finding current radius
				float curRadius = radius*(1-sizeFactor) + radius*obj.size*sizeFactor;
				curRadius = curRadius * terrainSize.pixelSize; //transforming to map-space

				//stamp coordinates
				//float scale = curRadius*2 / stamp.rect.size.x;
				Vector2 stampMin = obj.pos - new Vector2(curRadius, curRadius);
				Vector2 stampMax = obj.pos + new Vector2(curRadius, curRadius);
				Vector2 stampSize = new Vector2(curRadius*2, curRadius*2);

				//calculating rects 
				CoordRect stampRect = new CoordRect(stampMin.x, stampMin.y, stampSize.x, stampSize.y);
				CoordRect intersection = CoordRect.Intersect(stampRect, dst.rect);
				Coord min = intersection.Min; Coord max = intersection.Max; 

				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					//float dist = Mathf.Sqrt((x-obj.pos.x+0.5f)*(x-obj.pos.x+0.5f) + (z-obj.pos.y+0.5f)*(z-obj.pos.y+0.5f));
					//float percent = 1f - dist / curRadius; 
					//if (percent < 0 || dist > curRadius) percent = 0;

					Vector2 relativePos = new Vector2(1f*(x-stampMin.x)/(stampMax.x-stampMin.x), 1f*(z-stampMin.y)/(stampMax.y-stampMin.y));
					float val = stamp.GetInterpolated(relativePos.x*stamp.rect.size.x + stamp.rect.offset.x, relativePos.y*stamp.rect.size.z + stamp.rect.offset.z, Matrix.WrapMode.Clamp);
					//float val = stamp.CheckGet((int)(relativePos.x*stamp.rect.size.x + stamp.rect.offset.x), (int)(relativePos.y*stamp.rect.size.z + stamp.rect.offset.z)); //TODO use bilenear filtering

					//matrix[x,z] = matrix[x,z]+val*scale;
					//matrix[x,z] = Mathf.Max(matrix[x,z],val*scale);
					dst[x,z] = algorithm(dst[x,z],val);
				}
			}

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
			layout.Par(20); positionsIn.DrawIcon(layout, "Positions", mandatory:true); output.DrawIcon(layout, "Output");
			layout.Par(20); canvasIn.DrawIcon(layout, "Canvas");
			layout.Par(20); stampIn.DrawIcon(layout, "Stamp", mandatory:true);
			layout.Par(20); maskIn.DrawIcon(layout, "Mask");
			layout.Par(5);

			//params
			layout.Par(5); layout.fieldSize = 0.5f;
			layout.Field(ref guiAlgorithm, "Algorithm");
			layout.Field(ref radius, "Radius");
			layout.Field(ref sizeFactor, "Size Factor");
			layout.Field(ref safeBorders, "Safe Borders");
		}
	}
}
