using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MapMagic 
{

	[System.Serializable]
	public class Matrix : Matrix2<float>
	{
		public float GetInterpolatedValue (Vector2 pos) //for upscaling - gets value in-between two points
		{
			int x = Mathf.FloorToInt(pos.x); int z = Mathf.FloorToInt(pos.y);
			float xPercent = pos.x-x; float zPercent = pos.y-z;

			//if (!rect.CheckInRange(x+1,z+1)) return 0;

			float val1 = this[x,z];
			float val2 = this[x+1,z];
			float val3 = val1*(1-xPercent) + val2*xPercent;

			float val4 = this[x,z+1];
			float val5 = this[x+1,z+1];
			float val6 = val4*(1-xPercent) + val5*xPercent;
			
			return val3*(1-zPercent) + val6*zPercent;
		}

		public float GetAveragedValue (int x, int z, int steps) //for downscaling
		{
			float sum = 0;
			int div = 0;
			for (int ix=0; ix<steps; ix++)
				for (int iz=0; iz<steps; iz++)
			{
				if (x+ix >= rect.offset.x+rect.size.x) continue;
				if (z+iz >= rect.offset.z+rect.size.z) continue;
				sum += this[x+ix, z+iz];
				div++;
			}
			return sum / div;
		}

		#region Overriding constructors and clone
			
			public Matrix () { array = new float[0]; rect = new CoordRect(0,0,0,0); count = 0; } //for serializer

			public Matrix (CoordRect rect, float[] array=null)
			{
				this.rect = rect;
				count = rect.size.x*rect.size.z;
				if (array != null && array.Length<count) Debug.Log("Array length: " + array.Length + " is lower then matrix capacity: " + count);
				if (array != null && array.Length>=count) this.array = array;
				else this.array = new float[count];
			}

			public Matrix (Coord offset, Coord size, float[] array=null)
			{
				rect = new CoordRect(offset, size);
				count = rect.size.x*rect.size.z;
				if (array != null && array.Length<count) Debug.Log("Array length: " + array.Length + " is lower then matrix capacity: " + count);
				if (array != null && array.Length>=count) this.array = array;
				else this.array = new float[count];
			}

			public Matrix (Texture2D texture)
			{
				rect = new CoordRect(0,0, texture.width, texture.height);
				count = texture.width*texture.height;
				array = new float[count];
				FromTexture(texture);
			}

			public override object Clone () { return Copy(null); } //separate fn for IClonable
			public Matrix Copy (Matrix result=null)
			{
				if (result==null) result = new Matrix(rect);
			
				//copy params
				result.rect = rect;
				result.pos = pos;
				result.count = count;
			
				//copy array
				//result.array = (float[])array.Clone(); //no need to create it any time
				if (result.array.Length != array.Length) result.array = new float[array.Length];
				for (int i=0; i<array.Length; i++)
					result.array[i] = array[i];

				return result;
			}

			/*public Vector2 GetAreaNormal (int cx, int cz, int range)
			{
				CoordRect areaRect = new CoordRect(cx-range-1, cz-range-1, cx+range, cz+range);
				CoordRect intersection = CoordRect.Intersect(rect, areaRect);

				float nx = 0; float nz = 0;

				Coord min = intersection.Min; Coord max = intersection.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					nx += 
				}

			}*/

		#endregion

		#region Sorting

			// ???
			public bool[] InRect (CoordRect area = new CoordRect())
			{
				Matrix2<bool> result = new Matrix2<bool>(rect);
				CoordRect intersection = CoordRect.Intersect(rect,area);
				Coord min = intersection.Min; Coord max = intersection.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
						result[x,z] = true;
				return result.array;
			}

		#endregion

		#region Conversion

			public void Fill (float[,] array, CoordRect arrayRect) //using swapped x and z!
			{
				//arrayRect.z should be equal to array.GetLength(0), and arrayRect.x to array.GetLength(1)
				CoordRect intersection = CoordRect.Intersect(rect, arrayRect);
				Coord min = intersection.Min; Coord max = intersection.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
						this[x,z] = array[z-arrayRect.offset.z,x-arrayRect.offset.x];
			}

			public void Pour (float[,] array, CoordRect arrayRect) //using swapped x and z!
			{
				//arrayRect.z should be equal to array.GetLength(0), and arrayRect.x to array.GetLength(1)
				CoordRect intersection = CoordRect.Intersect(rect, arrayRect);
				Coord min = intersection.Min; Coord max = intersection.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
						array[z-arrayRect.offset.z,x-arrayRect.offset.x] = this[x,z];
			}

			public void Pour (float[,,] array, int channel, CoordRect arrayRect) //using swapped x and z!
			{
				//arrayRect.z should be equal to array.GetLength(0), and arrayRect.x to array.GetLength(1)
				CoordRect intersection = CoordRect.Intersect(rect, arrayRect);
				Coord min = intersection.Min; Coord max = intersection.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
						array[z-arrayRect.offset.z,x-arrayRect.offset.x, channel] = this[x,z];
			}


			public float[,] ReadHeighmap (TerrainData data, float height=1)
			{
				CoordRect intersection = CoordRect.Intersect(rect, new CoordRect(0,0,data.heightmapResolution, data.heightmapResolution));
				
				//get heights
				float[,] array = data.GetHeights(intersection.offset.x, intersection.offset.z, intersection.size.x, intersection.size.z); //returns x and z swapped

				//reading 2d array
				Coord min = intersection.Min; Coord max = intersection.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
						this[x,z] = array[z-min.z, x-min.x] * height;

				//removing borders
				RemoveBorders(intersection);

				return array;
			}

			public void WriteHeightmap (TerrainData data, float[,] array=null, float brushFallof=0.5f, bool delayLod=false)
			{
				CoordRect intersection = CoordRect.Intersect(rect, new CoordRect(0,0,data.heightmapResolution, data.heightmapResolution));
				
				//checking ref array
				if (array == null || array.Length != intersection.size.x*intersection.size.z) array = new float[intersection.size.z,intersection.size.x]; //x and z swapped

				//write to 2d array
				Coord min = intersection.Min; Coord max = intersection.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					float fallofFactor = Fallof(x,z,brushFallof);
					if (Mathf.Approximately(fallofFactor,0)) continue;
					array[z-min.z, x-min.x] = this[x,z]*fallofFactor + array[z-min.z, x-min.x]*(1-fallofFactor);
					//array[z-min.z, x-min.x] += this[x,z];
				}

				if (delayLod) data.SetHeightsDelayLOD(intersection.offset.x, intersection.offset.z, array);
				else data.SetHeights(intersection.offset.x, intersection.offset.z, array);
			}

			public float[,,] ReadSplatmap (TerrainData data, int channel, float[,,] array=null)
			{
				CoordRect intersection = CoordRect.Intersect(rect, new CoordRect(0,0,data.alphamapResolution, data.alphamapResolution));
				
				//get heights
				if (array==null) array = data.GetAlphamaps(intersection.offset.x, intersection.offset.z, intersection.size.x, intersection.size.z); //returns x and z swapped

				//reading array
				Coord min = intersection.Min; Coord max = intersection.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
						this[x,z] = array[z-min.z, x-min.x, channel];

				//removing borders
				RemoveBorders(intersection);

				return array;
			}

			static public void AddSplatmaps (TerrainData data, Matrix[] matrices, int[] channels, float[] opacity, float[,,] array=null, float brushFallof=0.5f)
			{
				int numChannels = data.alphamapLayers;
				bool[] usedChannels = new bool[numChannels];
				for (int i=0; i<channels.Length; i++) usedChannels[channels[i]] = true;
				float[] slice = new float[numChannels];

				Coord dataSize = new Coord(data.alphamapResolution, data.alphamapResolution);
				CoordRect dataRect = new CoordRect(new Coord(0,0), dataSize);
				CoordRect intersection = CoordRect.Intersect(dataRect, matrices[0].rect);
				
				if (array==null) array = data.GetAlphamaps(intersection.offset.x, intersection.offset.z, intersection.size.x, intersection.size.z);

				Coord min = intersection.Min; Coord max = intersection.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					//calculating fallof and opacity
					float fallofFactor = matrices[0].Fallof(x,z,brushFallof);
					if (Mathf.Approximately(fallofFactor,0)) continue;

					//reading slice
					for (int c=0; c<numChannels; c++) slice[c] = array[z-min.z, x-min.x, c];

					//converting matrices to additive
					for (int i=0; i<matrices.Length; i++) matrices[i][x,z] = Mathf.Max(0, matrices[i][x,z] - slice[channels[i]]);

					//apply fallof
					for (int i=0; i<matrices.Length; i++) matrices[i][x,z] *= fallofFactor * opacity[i];

					//calculating sum of adding values
					float addedSum = 0; //the sum of adding channels
					for (int i=0; i<matrices.Length; i++) addedSum += matrices[i][x,z];
					//if (addedSum < 0.00001f) continue; //no need to do anything

					//if addedsum exceeds 1 - equalizing matrices
					if (addedSum > 1f) 
						{ for (int i=0; i<matrices.Length; i++) matrices[i][x,z] /= addedSum; addedSum=1; }

					//multiplying all values on a remaining amount
					float multiplier = 1-addedSum;
					for (int c=0; c<numChannels; c++) slice[c] *= multiplier;

					//adding matrices
					for (int i=0; i<matrices.Length; i++) slice[channels[i]] += matrices[i][x,z];

					//saving slice
					for (int c=0; c<numChannels; c++) array[z-min.z, x-min.x, c] = slice[c];
				}

				data.SetAlphamaps(intersection.offset.x, intersection.offset.z, array);
			}

			public void ToTexture (Texture2D texture=null, Color[] colors=null, float rangeMin=0, float rangeMax=1, bool resizeTexture=false)
			{
				//creating or resizing texture
				if (texture == null) texture = new Texture2D(rect.size.x, rect.size.z);
				if (resizeTexture) texture.Resize(rect.size.x, rect.size.z);
				
				//finding matrix-texture intersection
				Coord textureSize = new Coord(texture.width, texture.height);
				CoordRect textureRect = new CoordRect(new Coord(0,0), textureSize);
				CoordRect intersection = CoordRect.Intersect(textureRect, rect);
				
				//checking ref color array
				if (colors == null || colors.Length != intersection.size.x*intersection.size.z) colors = new Color[intersection.size.x*intersection.size.z];

				//filling texture
				Coord min = intersection.Min; Coord max = intersection.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					float val = this[x,z];

					//adjusting value to range
					val -= rangeMin;
					val /= rangeMax-rangeMin;

					//making color gradient
					float byteVal = val * 256;
					int flooredByteVal = (int)byteVal;
					float remainder = byteVal - flooredByteVal;

					float flooredVal = flooredByteVal/256f;
					float ceiledVal = (flooredByteVal+1)/256f;
					
					//saving to colors
					int tx = x-min.x; int tz = z-min.z;
					colors[tz*(max.x-min.x) + tx] = new Color(flooredVal, remainder>0.333f ? ceiledVal : flooredVal, remainder>0.666f ? ceiledVal : flooredVal);
				}
			
				texture.SetPixels(intersection.offset.x, intersection.offset.z, intersection.size.x, intersection.size.z, colors);
				texture.Apply();
			}

			/*public void FromTexture (Texture2D texture, Coord textureOffset=new Coord(), bool fillBorders=false)
			{
				Coord textureSize = new Coord(texture.width, texture.height);
				CoordRect textureRect = new CoordRect(textureOffset, textureSize);
				CoordRect intersection = CoordRect.Intersect(textureRect, rect);

				Color[] colors = texture.GetPixels(intersection.offset.x - textureOffset.x, intersection.offset.z - textureOffset.z, intersection.size.x, intersection.size.z);

				Coord min = intersection.Min; Coord max = intersection.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					int tx = x-min.x; int tz = z-min.z;
					Color col = colors[tz*(max.x-min.x) + tx];

					this[x,z] = (col.r+col.g+col.b)/3;
				}

				if (fillBorders) RemoveBorders(intersection);
			}*/

			public void FromTexture (Texture2D texture)
			{
				CoordRect textureRect = new CoordRect(0,0, texture.width, texture.height);
				CoordRect intersection = CoordRect.Intersect(textureRect, rect);

				Color[] colors = texture.GetPixels(intersection.offset.x, intersection.offset.z, intersection.size.x, intersection.size.z);

				Coord min = intersection.Min; Coord max = intersection.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					int tx = x-min.x; int tz = z-min.z;
					Color col = colors[tz*(max.x-min.x) + tx];

					this[x,z] = (col.r+col.g+col.b)/3;
				}
			}

			public void FromTextureAlpha (Texture2D texture)
			{
				CoordRect textureRect = new CoordRect(0,0, texture.width, texture.height);
				CoordRect intersection = CoordRect.Intersect(textureRect, rect);

				Color[] colors = texture.GetPixels(intersection.offset.x, intersection.offset.z, intersection.size.x, intersection.size.z);

				Coord min = intersection.Min; Coord max = intersection.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					int tx = x-min.x; int tz = z-min.z;
					Color col = colors[tz*(max.x-min.x) + tx];

					this[x,z] = (col.r+col.g+col.b+col.a)/4;
				}
			}

			public void FromTextureTiled (Texture2D texture)
			{
				Color[] colors = texture.GetPixels();

				int textureWidth = texture.width;
				int textureHeight = texture.height;

				Coord min = rect.Min; Coord max = rect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					int tx = x % textureWidth; if (tx<0) tx += textureWidth;
					int tz = z % textureHeight; if (tz<0) tz += textureHeight;
					Color col = colors[tz*(textureWidth) + tx];

					this[x,z] = (col.r+col.g+col.b)/3;
				}
			}

			public Texture2D SimpleToTexture (Texture2D texture=null, Color[] colors=null, float rangeMin=0, float rangeMax=1, string savePath=null)
			{
				if (texture == null) texture = new Texture2D(rect.size.x, rect.size.z);
				if (texture.width != rect.size.x || texture.height != rect.size.z) texture.Resize(rect.size.x, rect.size.z);
				if (colors == null || colors.Length != rect.size.x*rect.size.z) colors = new Color[rect.size.x*rect.size.z];

				for (int i=0; i<count; i++) 
				{
					float val = array[i];
					val -= rangeMin;
					val /= rangeMax-rangeMin;
					colors[i] = new Color(val, val, val);
				}
			
				texture.SetPixels(colors);
				texture.Apply();
				return texture;
			}

			public void SimpleFromTexture (Texture2D texture)
			{
				ChangeRect(new CoordRect(rect.offset.x, rect.offset.z, texture.width, texture.height));
				
				Color[] colors = texture.GetPixels();

				for (int i=0; i<count; i++) 
				{
					Color col = colors[i];
					array[i] = (col.r+col.g+col.b)/3;
				}
			}

			public void ImportRaw (string path)
			{
				//reading file
				System.IO.FileInfo fileInfo = new System.IO.FileInfo(path);
				System.IO.FileStream stream = fileInfo.Open(System.IO.FileMode.Open, System.IO.FileAccess.Read);

				int size = (int)Mathf.Sqrt(stream.Length/2);
				byte[] vals = new byte[size*size*2];

				stream.Read(vals,0,vals.Length);
				stream.Close();

				//setting matrix
				ChangeRect( new CoordRect(0,0,size,size) );
				int i = 0;
				Coord min = rect.Min; Coord max = rect.Max;
				for (int z=max.z-1; z>=min.z; z--)
					for (int x=min.x; x<max.x; x++)
				{
					this[x,z] = (vals[i+1]*256f+vals[i]) / 65535f;
					i+=2;
				}
			}

			public void Replicate (Matrix source, bool tile=false)
			{
				Coord min = rect.Min; Coord max = rect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					if (source.rect.CheckInRange(x,z)) this[x,z] = source[x,z];

					else if (tile)
					{
						int ox = x - source.rect.offset.x;	int oz = z - source.rect.offset.z;
						int tx = ox % source.rect.size.x;	int tz = oz % source.rect.size.z;
						if (tx<0) tx += source.rect.size.x;	if (tz<0) tz += source.rect.size.z;
						int mx = tx + source.rect.offset.x;	int mz = tz + source.rect.offset.z;

						this[x,z] = source[mx,mz];
					}
				}
			}

		#endregion

		#region Resize

			public float GetArea (int x, int z, int range)
			{
				if (range == 0) 
				{
					if (x<rect.offset.x) x=rect.offset.x; if (x>=rect.offset.x+rect.size.x) x=rect.offset.x+rect.size.x-1;
					if (z<rect.offset.z) z=rect.offset.z; if (z>=rect.offset.z+rect.size.z) z=rect.offset.z+rect.size.z-1;
					//if (x>=rect.offset.x && x<rect.offset.x+rect.size.x && z>=rect.offset.z && z<rect.offset.z+rect.size.z)
					return array[(z-rect.offset.z)*rect.size.x + x - rect.offset.x];
				}

				else 
				{
					float sum = 0; int count = 0;
					
					//if (range < 3) //for small areas
					{
						for (int ix=x-range; ix<=x+range; ix++)
						{
							if (ix<rect.offset.x || ix>=rect.offset.x+rect.size.x) continue;
							for (int iz=z-range; iz<=z+range; iz++)
							{
								if (iz<rect.offset.z || iz>=rect.offset.z+rect.size.z) continue;
								sum += array[(iz-rect.offset.z)*rect.size.x + ix - rect.offset.x];
								count++;
							}
						}
					}
					
					//else //if area is big
					/*{
						CoordRect areaRect = new CoordRect(x-range, z-range, x+range, z+range);
				CoordRect intersection = CoordRect.Intersect(rect, areaRect);

				Coord min = intersection
					}*/

					return sum / count;

				}
			}

			public enum WrapMode { Once, Clamp, Tile, PingPong }
			
			public float GetInterpolated (float x, float z, WrapMode wrap=WrapMode.Once)
			{
				//skipping value if it is out of bounds
				if (wrap==WrapMode.Once)
				{
					if (x<rect.offset.x || x>=rect.offset.x+rect.size.x || z<rect.offset.z || z>=rect.offset.z+rect.size.z) 
						return 0;
				}

				//neig coords
				int px = (int)x; if (x<0) px--; //because (int)-2.5 gives -2, should be -3 
				int nx = px+1;

				int pz = (int)z; if (z<0) pz--; 
				int nz = pz+1;

				//local coordinates (without offset)
				int lpx = px-rect.offset.x; int lnx = nx-rect.offset.x;
				int lpz = pz-rect.offset.z; int lnz = nz-rect.offset.z;

				//wrapping coordinates
				if (wrap==WrapMode.Clamp || wrap==WrapMode.Once)
				{
					if (lpx<0) lpx=0; if (lpx>=rect.size.x) lpx=rect.size.x-1;
					if (lnx<0) lnx=0; if (lnx>=rect.size.x) lnx=rect.size.x-1;
					if (lpz<0) lpz=0; if (lpz>=rect.size.z) lpz=rect.size.z-1;
					if (lnz<0) lnz=0; if (lnz>=rect.size.z) lnz=rect.size.z-1;
				}
				else if (wrap==WrapMode.Tile)
				{
					lpx = lpx % rect.size.x; if (lpx<0) lpx=rect.size.x+lpx;
					lpz = lpz % rect.size.z; if (lpz<0) lpz=rect.size.z+lpz;
					lnx = lnx % rect.size.x; if (lnx<0) lnx=rect.size.x+lnx;
					lnz = lnz % rect.size.z; if (lnz<0) lnz=rect.size.z+lnz;
				}
				else if (wrap==WrapMode.PingPong)
				{
					lpx = lpx % (rect.size.x*2); if (lpx<0) lpx=rect.size.x*2 + lpx; if (lpx>=rect.size.x) lpx = rect.size.x*2 - lpx - 1;
					lpz = lpz % (rect.size.z*2); if (lpz<0) lpz=rect.size.z*2 + lpz; if (lpz>=rect.size.z) lpz = rect.size.z*2 - lpz - 1;
					lnx = lnx % (rect.size.x*2); if (lnx<0) lnx=rect.size.x*2 + lnx; if (lnx>=rect.size.x) lnx = rect.size.x*2 - lnx - 1;
					lnz = lnz % (rect.size.z*2); if (lnz<0) lnz=rect.size.z*2 + lnz; if (lnz>=rect.size.z) lnz = rect.size.z*2 - lnz - 1;
				}

				//reading values
				float val_pxpz = array[lpz*rect.size.x + lpx];
				float val_nxpz = array[lpz*rect.size.x + lnx]; //array[pos_fxfz + 1]; //do not use fast calculations as they are not bounds safe
				float val_pxnz = array[lnz*rect.size.x + lpx]; //array[pos_fxfz + rect.size.z];
				float val_nxnz = array[lnz*rect.size.x + lnx]; //array[pos_fxfz + rect.size.z + 1];

				float percentX = x-px;
				float percentZ = z-pz;

				float val_fz = val_pxpz*(1-percentX) + val_nxpz*percentX;
				float val_cz = val_pxnz*(1-percentX) + val_nxnz*percentX;
				float val = val_fz*(1-percentZ) + val_cz*percentZ;

				return val;
			}

			//public float GetInterpolatedTiled (float x, float z)

			public Matrix Resize (CoordRect newRect, Matrix result=null)
			{
				if (result==null) result = new Matrix(newRect);
				else result.ChangeRect(newRect);

				Coord min = result.rect.Min; Coord max = result.rect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					float percentX = 1f*(x-result.rect.offset.x)/result.rect.size.x; float origX = percentX*this.rect.size.x + this.rect.offset.x; 
					float percentZ = 1f*(z-result.rect.offset.z)/result.rect.size.z; float origZ = percentZ*this.rect.size.z + this.rect.offset.z; 
					result[x,z] = this.GetInterpolated(origX, origZ);
				}

				return result;
			}

			public Matrix Downscale (int factor, Matrix result=null) { return Resize(rect/factor, result); }
			public Matrix Upscale (int factor, Matrix result=null) { return Resize(rect*factor, result); }
			public Matrix BlurredUpscale (int factor)
			{
				Matrix src = new Matrix(rect, new float[count*factor]);
				Matrix dst = new Matrix(rect, new float[count*factor]);
				src.Fill(this);

				int steps = Mathf.RoundToInt(Mathf.Sqrt(factor));
				for (int i=0; i<steps; i++)
				{
					src.Resize(src.rect*2, dst);
					src.ChangeRect(dst.rect);
					src.Fill(dst);
					src.Blur(intensity:0.5f);
				}

				return src;
			}

		#endregion

		#region Outdated Resize
		
			public Matrix OutdatedResize (CoordRect newRect, float smoothness=1, Matrix result=null)
			{
				//calculating ratio
				int upscaleRatio = newRect.size.x / rect.size.x;
				int downscaleRatio = rect.size.x / newRect.size.x;

				//checking if rect could be rescaled
				if (upscaleRatio > 1 && !newRect.Divisible(upscaleRatio)) Debug.LogError("Matrix rect " + rect + " could not be upscaled to " + newRect + " with factor " + upscaleRatio);
				if (downscaleRatio > 1 && !rect.Divisible(downscaleRatio)) Debug.LogError("Matrix rect " + rect + " could not be downscaled to " + newRect + " with factor " + downscaleRatio);

				//scaling
				if (upscaleRatio > 1) result = OutdatedUpscale(upscaleRatio, result:result);
				if (downscaleRatio > 1) result = OutdatedDownscale(downscaleRatio, smoothness:smoothness, result:result);

				//returning clone if all ratios are 1
				if (upscaleRatio <= 1 && downscaleRatio <= 1) return Copy(result);
				else return result;
			}
			
			public Matrix OutdatedUpscale (int factor, Matrix result=null) //scaling both size AND offset
			{
				//preparing resulting array
				if (result == null) result = new Matrix(rect*factor);
				result.ChangeRect(rect*factor);

				//returning clone if ratio is 1
				if (factor == 1) return Copy(result);

				//resizing
				Coord min = rect.Min; Coord last = rect.Max-1;
				float step = 1f/factor;

				for (int x=min.x; x<last.x; x++)
					for (int z=min.z; z<last.z; z++)
				{
					float current = this[x,z];
					float nextX = this[x+1,z];
					float nextZ = this[x,z+1];
					float nextXZ = this[x+1,z+1];

					for (int ix=0; ix<factor; ix++)
						for (int iz=0; iz<factor; iz++)
					{
						float percentX = ix*step;
						float percentZ = iz*step;

						//percentX = 3*percentX*percentX - 2*percentX*percentX*percentX;
						//percentZ = 3*percentZ*percentZ - 2*percentZ*percentZ*percentZ;

						float firstRow = Mathf.Lerp(current, nextZ, percentZ);
						float lastRow = Mathf.Lerp(nextX, nextXZ, percentZ);
						result[x*factor + ix, z*factor + iz] = Mathf.Lerp(firstRow, lastRow, percentX);
					}
				}

				//removing borders
				result.RemoveBorders(0,0,factor+1,factor+1);

				return result;
			}

			public float OutdatedGetInterpolated (float x, float z)
			{
				int floorX = (int)x; int ceilX = (int)(x+1); if (ceilX >= rect.offset.x+rect.size.x) ceilX = rect.offset.x+rect.size.x-1;
				int floorZ = (int)z; int ceilZ = (int)(z+1); if (ceilZ >= rect.offset.z+rect.size.z) ceilZ = rect.offset.z+rect.size.z-1;
				float percentX = x-floorX;
				float percentZ = z-floorZ;

				int pos_fxfz = (floorZ-rect.offset.z)*rect.size.x + floorX - rect.offset.x;
				float val_fxfz = array[pos_fxfz];
				float val_cxfz = array[(floorZ-rect.offset.z)*rect.size.x + ceilX - rect.offset.x]; //array[pos_fxfz + 1]; //do not use fast calculations as they are not bounds safe
				float val_fxcz = array[(ceilZ-rect.offset.z)*rect.size.x + floorX - rect.offset.x]; //array[pos_fxfz + rect.size.z];
				float val_cxcz = array[(ceilZ-rect.offset.z)*rect.size.x + ceilX - rect.offset.x]; //array[pos_fxfz + rect.size.z + 1];
				
				float val_fz = val_fxfz*(1-percentX) + val_cxfz*percentX;
				float val_cz = val_fxcz*(1-percentX) + val_cxcz*percentX;
				float val = val_fz*(1-percentZ) + val_cz*percentZ;

				return val;
			}

			public Matrix TestResize (CoordRect newRect)
			{
				Matrix result = new Matrix(newRect);

				Coord min = result.rect.Min; Coord max = result.rect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					float percentX = 1f*(x-result.rect.offset.x)/result.rect.size.x; float origX = percentX*this.rect.size.x + this.rect.offset.x; 
					float percentZ = 1f*(z-result.rect.offset.z)/result.rect.size.z; float origZ = percentZ*this.rect.size.z + this.rect.offset.z; 
					result[x,z] = this.OutdatedGetInterpolated(origX, origZ);
				}

				return result;
			}

			public Matrix OutdatedDownscale (int factor=2, float smoothness=1, Matrix result=null)
			{
				//preparing resulting array
				if (!rect.Divisible(factor)) Debug.LogError("Matrix rect " + rect + " could not be downscaled with factor " + factor);
				if (result == null) result = new Matrix(rect/factor);
				result.ChangeRect(rect/factor);

				//returning clone if ratio is 1
				if (factor == 1) return Copy(result);
			
				//work coords
				Coord min = rect.Min; //Coord max = rect.Max;
				Coord rmin = result.rect.Min; Coord rmax = result.rect.Max;

				//scaling nearest neightbour
				if (smoothness < 0.0001f)
				for (int x=rmin.x; x<rmax.x; x++)
					for (int z=rmin.z; z<rmax.z; z++)
				{
					int sx = (x-rmin.x)*factor + min.x;
					int sz = (z-rmin.z)*factor + min.z;

					result[x,z] = this[sx, sz];
				}

				//scaling bilinear
				else
				for (int x=rmin.x; x<rmax.x; x++)
					for (int z=rmin.z; z<rmax.z; z++)
				{
					int sx = (x-rmin.x)*factor + min.x;
					int sz = (z-rmin.z)*factor + min.z;

					float sum = 0;
					for (int ix=sx; ix<sx+factor; ix++)
						for (int iz=sz; iz<sz+factor; iz++)
							sum += this[ix,iz];

					result[x,z] = sum/(factor*factor)*smoothness + this[sx, sz]*(1-smoothness);
				}

				return result;
			}

		#endregion

		#region Procedural resize

			public class Stacker
			{
				public CoordRect smallRect;
				public CoordRect bigRect;

				public bool preserveDetail = true;
				
				Matrix downscaled;
				Matrix upscaled;
				Matrix difference;

				bool isDownscaled;

				public Stacker (CoordRect smallRect, CoordRect bigRect)
				{
					this.smallRect = smallRect; this.bigRect = bigRect;
					isDownscaled = false;

					//do not create additional matrices if rect sizes are the same
					if (bigRect==smallRect)
					{
						upscaled = downscaled = new Matrix(bigRect);
					}

					else
					{
						downscaled = new Matrix(smallRect);
						upscaled = new Matrix(bigRect);
						difference = new Matrix(bigRect);
						//once arrays created they should not be resized
					}
				}

				public Matrix matrix
				{
					get { if (isDownscaled) return downscaled; else return upscaled; }
					//set { if (isDownscaled) downscaled=value; else upscaled=value; }
				}

				public void ToSmall ()
				{
					if (bigRect==smallRect) return;
					
					//calculating factor
					//int downscaleRatio = newSize.x / rect.size.x;

					//scaling
					downscaled = upscaled.OutdatedResize(smallRect, result:downscaled);

					//difference
					if (preserveDetail)
					{
						difference = downscaled.OutdatedResize(bigRect, result:difference);
						difference.Blur();
						difference.InvSubtract(upscaled); //difference = original - difference
					}

					isDownscaled = true;
				}

				public void ToBig ()
				{
					if (bigRect==smallRect) return;
					
					upscaled = downscaled.OutdatedResize(bigRect, result:upscaled);
					upscaled.Blur();
					if (preserveDetail) upscaled.Add(difference);

					isDownscaled = false;
				}

			}



		#endregion

		#region Blur

			public void Spread (float strength=0.5f, int iterations=4, Matrix copy=null)
			{
				Coord min = rect.Min; Coord max = rect.Max;

				for (int j=0; j<count; j++) array[j] = Mathf.Clamp(array[j],-1,1);

				if (copy==null) copy = Copy(null);
				else for (int j=0; j<count; j++) copy.array[j] = array[j];

				for (int i=0; i<iterations; i++)
				{
					float prev = 0;

					for (int x=min.x; x<max.x; x++)
					{
						prev = this[x,min.z]; SetPos(x,min.z); for (int z=min.z+1; z<max.z; z++) { prev = (prev+array[pos])/2; array[pos] = prev; pos += rect.size.x; }
						prev = this[x,max.z-1]; SetPos(x,max.z-1); for (int z=max.z-2; z>=min.z; z--) { prev = (prev+array[pos])/2; array[pos] = prev; pos -= rect.size.x; }
					}

					for (int z=min.z; z<max.z; z++)
					{
						prev = this[min.x,z]; SetPos(min.x,z); for (int x=min.x+1; x<max.x; x++) { prev = (prev+array[pos])/2; array[pos] = prev; pos += 1; }
						prev = this[max.x-1,z]; SetPos(max.x-1,z); for (int x=max.x-2; x>=min.x; x--) { prev = (prev+array[pos])/2; array[pos] = prev; pos -= 1; }
					}
				}

				for (int j=0; j<count; j++) array[j] = copy.array[j] + array[j]*2*strength;

				float factor = Mathf.Sqrt(iterations);
				for (int j=0; j<count; j++) array[j] /= factor;
			}


			public void Spread (System.Func<float,float,float> spreadFn=null, int iterations=4)
			{
				Coord min = rect.Min; Coord max = rect.Max;

				for (int i=0; i<iterations; i++)
				{
					float prev = 0;

					for (int x=min.x; x<max.x; x++)
					{
						prev = this[x,min.z]; SetPos(x,min.z); for (int z=min.z+1; z<max.z; z++) { prev = spreadFn(prev,array[pos]); array[pos] = prev; pos += rect.size.x; }
						prev = this[x,max.z-1]; SetPos(x,max.z-1); for (int z=max.z-2; z>=min.z; z--) { prev = spreadFn(prev,array[pos]); array[pos] = prev; pos -= rect.size.x; }
					}

					for (int z=min.z; z<max.z; z++)
					{
						prev = this[min.x,z]; SetPos(min.x,z); for (int x=min.x+1; x<max.x; x++) { prev = spreadFn(prev,array[pos]); array[pos] = prev; pos += 1; }
						prev = this[max.x-1,z]; SetPos(max.x-1,z); for (int x=max.x-2; x>=min.x; x--) { prev = spreadFn(prev,array[pos]); array[pos] = prev; pos -= 1; }
					}
				}
			}

			public void SimpleBlur (int iterations, float strength)
			{
				Coord min = rect.Min; Coord max = rect.Max;

				for (int iteration=0; iteration<iterations; iteration++)
				{
					for (int z=min.z; z<max.z; z++)
					{
						float prev = this[min.x,z];
						for (int x=min.x+1; x<max.x-1; x++)
						{
							int i = (z-rect.offset.z)*rect.size.x + x - rect.offset.x;
							float curr = array[i];
							float next = array[i+1];

							float val = (prev+next)/2*strength + curr*(1-strength);
							array[i] = val;
							prev = val;
						}
					}

					for (int x=min.x; x<max.x; x++)
					{
						float prev = this[x,min.z];
						for (int z=min.z+1; z<max.z-1; z++)
						{
							int i = (z-rect.offset.z)*rect.size.x + x - rect.offset.x;
							float curr = array[i];
							float next = array[i+rect.size.x];

							float val = (prev+next)/2*strength + curr*(1-strength);
							array[i] = val;
							prev = val;
						}
					}
				}
			}


			public void Blur (System.Func<float,float,float,float> blurFn=null, float intensity=0.666f, bool additive=false, bool takemax=false, bool horizontal=true, bool vertical=true, Matrix reference=null)
			{
				if (reference==null) reference = this;
				Coord min = rect.Min; Coord max = rect.Max;

				if (horizontal)
				for (int z=min.z; z<max.z; z++)
				{
					SetPos(min.x,z);

					float prev = reference[min.x,z];
					float curr = prev;
					float next = prev;

					float blurred = 0;

					for (int x=min.x; x<max.x; x++) 
					{
						prev = curr; //reference[x-1,z];
						curr = next; //reference[x,z]; 
						if (x<max.x-1) next = reference.array[pos+1]; //reference[x+1,z];

						//blurring
						if (blurFn==null) blurred = (prev+next)/2f;
						else blurred = blurFn(prev, curr, next);
						blurred = curr*(1-intensity) + blurred*intensity;
						
						//filling
						if (additive) array[pos] += blurred;
						else array[pos] = blurred;

						pos++;
					}
				}

				if (vertical)
				for (int x=min.x; x<max.x; x++)
				{
					SetPos(x,min.z);
				
					float next = reference[x,min.z];
					float curr = next;
					float prev = next;

					float blurred = next;

					for (int z=min.z; z<max.z; z++) 
					{
						prev = curr; //reference[x-1,z];
						curr = next; //reference[x,z]; 
						if (z<max.z-1) next = reference.array[pos+rect.size.x]; //reference[x+1,z];

						//blurring
						if (blurFn==null) blurred = (prev+next)/2f;
						else blurred = blurFn(prev, curr, next);
						blurred = curr*(1-intensity) + blurred*intensity;
						
						//filling
						if (additive) array[pos] += blurred;
						else if (takemax) { if (blurred > array[pos]) array[pos] = blurred; }
						else array[pos] = blurred;

						pos+=rect.size.x;
					}
				}
			}

			public void LossBlur (int step=2, bool horizontal=true, bool vertical=true, Matrix reference=null)
			{
				if (reference==null) reference = this;
				Coord min = rect.Min; Coord max = rect.Max;
				int stepShift = step + step/2;

				if (horizontal)
				for (int z=min.z; z<max.z; z++)
				{
					SetPos(min.x, z);
				
					float sum = 0;
					int div = 0;
				
					float avg = this.array[pos];
					float oldAvg = this.array[pos];

					for (int x=min.x; x<max.x+stepShift; x++) 
					{
						//gathering
						if (x < max.x) sum += reference.array[pos];
						div ++;
						if (x%step == 0) 
						{
							oldAvg=avg; 
							if (x < max.x) avg=sum/div; 
							sum=0; div=0;
						}

						//filling
						if (x-stepShift >= min.x)
						{
							float percent = 1f*(x%step)/step;
							if (percent<0) percent += 1; //for negative x
							this.array[pos-stepShift] = avg*percent + oldAvg*(1-percent);
						}

						pos += 1;
					}
				}

				if (vertical)
				for (int x=min.x; x<max.x; x++)
				{
					SetPos(x, min.z);
				
					float sum = 0;
					int div = 0;
				
					float avg = this.array[pos];
					float oldAvg = this.array[pos];

					for (int z=min.z; z<max.z+stepShift; z++) 
					{
						//gathering
						if (z < max.z) sum += reference.array[pos];
						div ++;
						if (z%step == 0) 
						{
							oldAvg=avg; 
							if (z < max.z) avg=sum/div; 
							sum=0; div=0;
						}

						//filling
						if (z-stepShift >= min.z)
						{
							float percent = 1f*(z%step)/step;
							if (percent<0) percent += 1;
							this.array[pos-stepShift*rect.size.x] = avg*percent + oldAvg*(1-percent);
						}

						pos += rect.size.x;
					}
				}
			}

			#region Outdated
		/*public void OverBlur (int iterations=20)
		{
			Matrix blurred = this.Clone(null);

			for (int i=1; i<=iterations; i++)
			{
				if (i==1 || i==2) blurred.Blur(step:1);
				else if (i==3) { blurred.Blur(step:1); blurred.Blur(step:1); }
				else blurred.Blur(step:i-2); //i:4, step:2

				for (int p=0; p<count; p++) 
				{
					float b = blurred.array[p] * i;
					float a = array[p];

					array[p] = a + b + a*b;
				}
			}
		}*/

		/*public void LossBlur (System.Func<float,float,float,float> blurFn=null, //prev, curr, next = output
			float intensity=0.666f, int step=1, Matrix reference=null, bool horizontal=true, bool vertical=true)
		{
			Coord min = rect.Min; Coord max = rect.Max;

			if (reference==null) reference = this;
			int lastX = max.x-1;
			int lastZ = max.z-1;

			if (horizontal)
			for (int z=min.z; z<=lastZ; z++)
			{
				float next = reference[min.x,z];
				float curr = next;
				float prev = next;

				float blurred = next;
				float lastBlurred = next;

				for (int x=min.x+step; x<=lastX; x+=step) 
				{
					//blurring
					if (blurFn==null) blurred = (prev+next)/2f;
					else blurred = blurFn(prev, curr, next);
					blurred = curr*(1-intensity) + blurred*intensity;

					//shifting values
					prev = curr; //this[x,z];
					curr = next; //this[x+step,z];
					try { next = reference[x+step*2,z]; } //this[x+step*2,z];
					catch { next = reference[lastX,z]; }

					//filling between-steps distance
					if (step==1) this[x,z] = blurred;
					else for (int i=0; i<step; i++) 
					{
						float percent = 1f * i / step;
						this[x-step+i,z] = blurred*percent + lastBlurred*(1-percent);
					}
					lastBlurred = blurred;
				}
			}

			if (vertical)
			for (int x=min.x; x<=lastX; x++)
			{
				float next = reference[x,min.z];
				float curr = next;
				float prev = next;

				float blurred = next;
				float lastBlurred = next;

				for (int z=min.z+step; z<=lastZ; z+=step) 
				{
					//blurring
					if (blurFn==null) blurred = (prev+next)/2f;
					else blurred = blurFn(prev, curr, next);
					blurred = curr*(1-intensity) + blurred*intensity;

					//shifting values
					prev = curr;
					curr = next;
					try { next = reference[x,z+step*2]; }
					catch { next = reference[x,lastZ]; }

					//filling between-steps distance
					if (step==1) this[x,z] = blurred;
					else for (int i=0; i<step; i++) 
					{
						float percent = 1f * i / step;
						this[x,z-step+i] = blurred*percent + lastBlurred*(1-percent);
					}
					lastBlurred = blurred;
				}
			}
		}*/
		#endregion

		#endregion

		#region Other

		/*public float GetOnWorldRect (Vector2 worldPos, Rect worldRect)
			{
				float relativeX = (worldPos.x - worldRect.x) / worldRect.width;
				float relativeZ = (worldPos.y - worldRect.y) / worldRect.height;
				int posX = Mathf.RoundToInt( relativeX*rect.size.x + rect.offset.x );
				int posZ = Mathf.RoundToInt( relativeZ*rect.size.z + rect.offset.z );
				posX = Mathf.Clamp(posX,rect.Min.x+1,rect.Max.x-1); posZ = Mathf.Clamp(posZ,rect.Min.z+1,rect.Max.z-1);

				return this[posX,posZ];
			}*/

			static public void BlendLayers (Matrix[] matrices, float[] opacity=null) //changes splatmaps in photoshop layered style so their summary value does not exceed 1
			{
				//finding any existing matrix
				int anyMatrixNum = -1;
				for (int i=0; i<matrices.Length; i++)
					if (matrices[i]!=null) { anyMatrixNum = i; break; }
				if (anyMatrixNum == -1) { Debug.LogError("No matrices were found to blend " + matrices.Length); return; }

				//finding rect
				CoordRect rect = matrices[anyMatrixNum].rect;

				//checking rect size
				#if WDEBUG
				for (int i=0; i<matrices.Length; i++)
					if (matrices[i]!=null && matrices[i].rect!=rect) { Debug.LogError("Matrix rect mismatch " + rect + " " + matrices[i].rect); return; }
				#endif

				int rectCount = rect.Count;
				for (int pos=0; pos<rectCount; pos++)
				{
					float sum = 0;
					for (int i=matrices.Length-1; i>=0; i--) //layer 0 is background, layer Length-1 is the top one
					{
						if (matrices[i] == null) continue;
						
						float val = matrices[i].array[pos];

						if (opacity != null) val *= opacity[i];
						
						float overly = sum + val - 1; 
						if (overly < 0) overly = 0; //faster then calling Math.Clamp
						if (overly > 1) overly = 1;

						matrices[i].array[pos] = val - overly;
						sum += val - overly;
					}
				}
			}

			static public void NormalizeLayers (Matrix[] matrices, float[] opacity) //changes splatmaps so their summary value does not exceed 1
			{
				//finding any existing matrix
				int anyMatrixNum = -1;
				for (int i=0; i<matrices.Length; i++)
					if (matrices[i]!=null) { anyMatrixNum = i; break; }
				if (anyMatrixNum == -1) { Debug.LogError("No matrices were found to blend " + matrices.Length); return; }

				//finding rect
				CoordRect rect = matrices[anyMatrixNum].rect;

				//checking rect size
				#if WDEBUG
				for (int i=0; i<matrices.Length; i++)
					if (matrices[i]!=null && matrices[i].rect!=rect) { Debug.LogError("Matrix rect mismatch " + rect + " " + matrices[i].rect); return; }
				#endif


				int rectCount = rect.Count;
				for (int pos=0; pos<rectCount; pos++)
				{
					for (int i=0; i<matrices.Length; i++) matrices[i].array[pos] *= opacity[i];

					float sum = 0;
					for (int i=0; i<matrices.Length; i++) sum += matrices[i].array[pos];
					if (sum > 1f) for (int i=0; i<matrices.Length; i++) matrices[i].array[pos] /= sum;
				}
			}

			public float Fallof (int x, int z, float fallof) //returns the relative dist from circle (with radius = size/2 * fallof) located at the center
			{
				if (fallof < 0) return 1;
				
				//relative distance from center
				float radiusX = rect.size.x/2f-1; float relativeX = (x - (rect.offset.x+radiusX)) / radiusX; // (x - center) / radius
				float radiusZ = rect.size.z/2f-1; float relativeZ = (z - (rect.offset.z+radiusZ)) / radiusZ;
				float dist = Mathf.Sqrt(relativeX*relativeX + relativeZ*relativeZ);

				//percent
				float percent = Mathf.Clamp01( (1-dist) / (1-fallof) );
				return 3*percent*percent - 2*percent*percent*percent;

				//advanced control over percent
				//float pinPercent = percent*percent; //*percent;
				//float bubblePercent = 1-(1-percent)*(1-percent) ; //*(1-percent);
				//if (percent > 0.5f) percent = bubblePercent*2 - 1f; //bubblePercent*4 - 3f;
				//else percent = pinPercent*2; //pinPercent*4;
				//return percent;
			}

			public void FillEmpty ()
			{
				float prev = 0;
				Coord min = rect.Min; Coord max = rect.Max;

				for (int x=min.x; x<max.x; x++)
				{
					prev = 0;
					for (int z=min.z; z<max.z; z++)
						{ float val = array[(z-rect.offset.z)*rect.size.x + x - rect.offset.x]; if (val>0.0001f) prev = val; else if (prev>0.0001f) array[(z-rect.offset.z)*rect.size.x + x - rect.offset.x] = prev; }
				}

				for (int z=min.z; z<max.z; z++)
				{
					prev = 0;
					for (int x=min.x; x<max.x; x++)
						{ float val = array[(z-rect.offset.z)*rect.size.x + x - rect.offset.x]; if (val>0.0001f) prev = val; else if (prev>0.0001f) array[(z-rect.offset.z)*rect.size.x + x - rect.offset.x] = prev; }
				}

				for (int x=min.x; x<max.x; x++)
				{
					prev = 0;
					for (int z=max.z-1; z>min.z; z--)
						{ float val = array[(z-rect.offset.z)*rect.size.x + x - rect.offset.x]; if (val>0.0001f) prev = val; else if (prev>0.0001f) array[(z-rect.offset.z)*rect.size.x + x - rect.offset.x] = prev; }
				}

				for (int z=min.z; z<max.z; z++)
				{
					prev = 0;
					for (int x=max.x-1; x>=min.x; x--)
						{ float val = array[(z-rect.offset.z)*rect.size.x + x - rect.offset.x]; if (val>0.0001f) prev = val; else if (prev>0.0001f) array[(z-rect.offset.z)*rect.size.x + x - rect.offset.x] = prev; }
				}
			}

			static public void Blend (Matrix src, Matrix dst, float factor)
			{
				if (dst.rect != src.rect) Debug.LogError("Matrix Blend: maps have different sizes");
				
				for (int i=0; i<dst.count; i++)
				{
					dst.array[i] = dst.array[i]*factor + src.array[i]*(1-factor);
				}
			}

			static public void Mask (Matrix src, Matrix dst, Matrix mask) //changes dst, not src
			{
				if (src != null &&
					(dst.rect != src.rect || dst.rect != mask.rect)) Debug.LogError("Matrix Mask: maps have different sizes");
				
				for (int i=0; i<dst.count; i++)
				{
					float percent = mask.array[i];
					if (percent > 1 || percent < 0) continue;

					dst.array[i] = dst.array[i]*percent + (src==null? 0:src.array[i]*(1-percent));
				}
			}

			static public void SafeBorders (Matrix src, Matrix dst, int safeBorders) //changes dst, not src
			{
				Coord min = dst.rect.Min; Coord max = dst.rect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					int distFromBorder = Mathf.Min( Mathf.Min(x-min.x,max.x-x), Mathf.Min(z-min.z,max.z-z) );
					float percent = 1f*distFromBorder / safeBorders;
					if (percent > 1) continue;

					dst[x,z] = dst[x,z]*percent + (src==null? 0:src[x,z]*(1-percent));
				}
			}



		#endregion

		#region Arithmetic

			public void Add (Matrix add) { for (int i=0; i<count; i++) array[i] += add.array[i]; }
			public void Add (Matrix add, Matrix mask) { for (int i=0; i<count; i++) array[i] += add.array[i]*mask.array[i]; }
			public void Add (float add) { for (int i=0; i<count; i++) array[i] += add; }
			public void Subtract (Matrix m) { for (int i=0; i<count; i++) array[i] -= m.array[i]; }
			//public void Subtract (float v) //use Add with negative value
			public void InvSubtract (Matrix m) { for (int i=0; i<count; i++) array[i] = m.array[i] - array[i]; }
			public void Multiply (Matrix m) { for (int i=0; i<count; i++) array[i] *= m.array[i]; }
			public void Multiply (float m) { for (int i=0; i<count; i++) array[i] *= m; }
			public void Max (Matrix m) { for (int i=0; i<count; i++) if (m.array[i]>array[i]) array[i] = m.array[i]; }
			public bool CheckRange (float min, float max) { for (int i=0; i<count; i++) if (array[i]<min || array[i]>max) return false; return true; } 
			public void Invert() { for (int i=0; i<count; i++) array[i] = -array[i]; }
			public void InvertOne() { for (int i=0; i<count; i++) array[i] = 1-array[i]; }
			public void Clamp01 () 
			{ 
				for (int i=0; i<count; i++) 
				{
					float val = array[i];
					if (val > 1) array[i] = 1;
					else if (val < 0) array[i] = 0;
				}
			}
			public void ClampSubtract (Matrix m) //useful for subtracting layers
			{ 
				for (int i=0; i<count; i++) 
				{
					float val = array[i] - m.array[i]; 
					if (val > 1) val = 1;
					else if (val < 0) val = 0;
					array[i] = val;
				} 
			}

			public bool IsEmpty (float delta=0.0001f) { for (int i=0; i<count; i++) if (array[i] > delta) return false; return true; }
			public float MaxValue () 
			{ 
				float max=-20000000; 
				for (int i=0; i<count; i++) 
				{
					float val = array[i];
					if (val > max) max = val;
				}
				return max; 
			}


		#endregion
	}

}//namespace