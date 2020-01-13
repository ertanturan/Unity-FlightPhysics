using UnityEngine;
using System.Collections;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

namespace MapMagic
{

	public class WeldTerrains
	{
		public enum Side { prevX, nextX, prevZ, nextZ };

		public static void WeldToPrevX (ref float[,] heights, Terrain neig, int margin)
		{
			#if WDEBUG
			Profiler.BeginSample("Weld Height");
			#endif
			
			int heightSize = heights.GetLength(0);

			float[,] nStrip = neig.terrainData.GetHeights(heightSize-1,0,1,heightSize);
			
			for (int z=0; z<heightSize; z++)
			{
				float delta = nStrip[z,0] - heights[z,0];

				for (int x=0; x<margin; x++)
				{
					float percent = 1-1f*x/margin;
					//percent = 3*percent*percent - 2*percent*percent*percent;
				
					heights[z,x] += delta*percent;
				}	
			}

			#if WDEBUG
			Profiler.EndSample();
			#endif
		}

		public static void WeldToNextX (ref float[,] heights, Terrain neig, int margin)
		{
			#if WDEBUG
			Profiler.BeginSample("Weld Height");
			#endif

			int heightSize = heights.GetLength(0);

			float[,] nStrip = neig.terrainData.GetHeights(0,0,1,heightSize);

			for (int z=0; z<heightSize; z++)
			{
				float delta = nStrip[z,0] - heights[z,heightSize-1];

				for (int x=heightSize-margin; x<heightSize; x++)
				{
					float percent = 1 - 1f*(heightSize-x-1)/margin;
					//percent = 3*percent*percent - 2*percent*percent*percent;
				
					heights[z,x] += delta*percent;
				}	
			}

			#if WDEBUG
			Profiler.EndSample();
			#endif
		}

		public static void WeldToPrevZ (ref float[,] heights, Terrain neig, int margin)
		{
			#if WDEBUG
			Profiler.BeginSample("Weld Height");
			#endif

			int heightSize = heights.GetLength(0);

			float[,] nStrip = neig.terrainData.GetHeights(0,heightSize-1,heightSize,1);

			for (int x=0; x<heightSize; x++)
			{
				float delta = nStrip[0,x] - heights[0,x];

				//float percentFromSide = Mathf.Min( Mathf.Clamp01(1f*x/margin),  Mathf.Clamp01(1 - 1f*(x-(heightSize-1-margin))/margin) );
				//float invPercentFromSide = 2000000000; if (percentFromSide > 0.0001f) invPercentFromSide = 1f/percentFromSide;

				for (int z=0; z<margin; z++)
				{
					float percent = 1-1f*z/margin;
				//	if (percentFromSide < 0.999f) percent = Mathf.Pow(percent, invPercentFromSide);
					//percent = 3*percent*percent - 2*percent*percent*percent;
				
					heights[z,x] += delta*percent;
				}	
			}

			#if WDEBUG
			Profiler.EndSample();
			#endif
		}

		public static void WeldToNextZ (ref float[,] heights, Terrain neig, int margin)
		{
			#if WDEBUG
			Profiler.BeginSample("Weld Height");
			#endif
			
			int heightSize = heights.GetLength(0);

			float[,] nStrip = neig.terrainData.GetHeights(0,0,heightSize,1);

			for (int x=0; x<heightSize; x++)
			{
				float delta = nStrip[0,x] - heights[heightSize-1,x];

			//	float percentFromSide = Mathf.Min( Mathf.Clamp01(1f*x/margin), Mathf.Clamp01(1 - 1f*(x-(heightSize-1-margin))/margin) );
			//	float invPercentFromSide = 2000000000; if (percentFromSide > 0.0001f) invPercentFromSide = 1f/percentFromSide;

				for (int z=heightSize-margin; z<heightSize; z++)
				{
					float percent = 1 - 1f*(heightSize-z-1)/margin;
			//		if (percentFromSide < 0.999f) percent = Mathf.Pow(percent, invPercentFromSide);
					//percent = 3*percent*percent - 2*percent*percent*percent;
				
					heights[z,x] += delta*percent;
				}
			}

			#if WDEBUG
			Profiler.EndSample();
			#endif
		}


		public static void WeldSplatToPrevX (ref float[,,] splats, Terrain neig, int margin)
		{
			#if WDEBUG
			Profiler.BeginSample("Weld Splat");
			#endif
			
			int splatsSize = splats.GetLength(0);
			int numSplats = splats.GetLength(2);
			float[] nRow = new float[numSplats];

			if (margin==0 || neig.terrainData.alphamapResolution!=splatsSize || neig.terrainData.alphamapLayers!=numSplats) return;

			float[,,] nStrip = neig.terrainData.GetAlphamaps(splatsSize-1,0,1,splatsSize);

			for (int z=0; z<splatsSize; z++)
			{
				for (int s=0; s<numSplats; s++) nRow[s] = nStrip[z,0,s];

				//float percentFromSide = Mathf.Min( Mathf.Clamp01(1f*z/margin),  Mathf.Clamp01(1 - 1f*(z-(splatsSize-1-margin))/margin) );
				//float invPercentFromSide = 2000000000; if (percentFromSide > 0.0001f) invPercentFromSide = 1f/percentFromSide;

				for (int x=0; x<margin; x++)
				{
					float percent = 1-1f*x/margin;
					//if (percentFromSide < 0.999f) percent = Mathf.Pow(percent, invPercentFromSide);
					//percent = 3*percent*percent - 2*percent*percent*percent;
						
					for (int s=0; s<numSplats; s++)
						splats[z,x,s] = nRow[s]*percent + splats[z,x,s]*(1-percent);
				}
			}		

			#if WDEBUG
			Profiler.EndSample();
			#endif
		}

		public static void WeldSplatToNextX (ref float[,,] splats, Terrain neig, int margin)
		{
			#if WDEBUG
			Profiler.BeginSample("Weld Splat");
			#endif
			
			int splatsSize = splats.GetLength(0);
			int numSplats = splats.GetLength(2);
			float[] nRow = new float[numSplats];

			if (margin==0 || neig.terrainData.alphamapResolution!=splatsSize || neig.terrainData.alphamapLayers!=numSplats) return;

			float[,,] nStrip = neig.terrainData.GetAlphamaps(0,0,1,splatsSize);

			for (int z=0; z<splatsSize; z++)
			{
				for (int s=0; s<numSplats; s++) nRow[s] = nStrip[z,0,s];

				//float percentFromSide = Mathf.Min( Mathf.Clamp01(1f*z/margin),  Mathf.Clamp01(1 - 1f*(z-(splatsSize-1-margin))/margin) );
				//float invPercentFromSide = 2000000000; if (percentFromSide > 0.0001f) invPercentFromSide = 1f/percentFromSide;

				for (int x=splatsSize-margin; x<splatsSize; x++)
				{
					float percent = 1 - 1f*(splatsSize-x-1)/margin;
					//if (percentFromSide < 0.999f) percent = Mathf.Pow(percent, invPercentFromSide);
					//percent = 3*percent*percent - 2*percent*percent*percent;
						
					for (int s=0; s<numSplats; s++)
						splats[z,x,s] = nRow[s]*percent + splats[z,x,s]*(1-percent);
				}
			}
			
			#if WDEBUG
			Profiler.EndSample();
			#endif		
		}

		public static void WeldSplatToPrevZ (ref float[,,] splats, Terrain neig, int margin)
		{
			#if WDEBUG
			Profiler.BeginSample("Weld Splat");
			#endif

			int splatsSize = splats.GetLength(0);
			int numSplats = splats.GetLength(2);
			float[] nRow = new float[numSplats];

			if (margin==0 || neig.terrainData.alphamapResolution!=splatsSize || neig.terrainData.alphamapLayers!=numSplats) return;

			float[,,] nStrip = neig.terrainData.GetAlphamaps(0,splatsSize-1,splatsSize,1);

			for (int x=0; x<splatsSize; x++)
			{
				for (int s=0; s<numSplats; s++) nRow[s] = nStrip[0,x,s];

				//float percentFromSide = Mathf.Min( Mathf.Clamp01(1f*x/margin),  Mathf.Clamp01(1 - 1f*(x-(splatsSize-1-margin))/margin) );
				//float invPercentFromSide = 2000000000; if (percentFromSide > 0.0001f) invPercentFromSide = 1f/percentFromSide;

				for (int z=0; z<margin; z++)
				{
					float percent = 1-1f*z/margin;
					//if (percentFromSide < 0.999f) percent = Mathf.Pow(percent, invPercentFromSide);
					//percent = 3*percent*percent - 2*percent*percent*percent;
						
					for (int s=0; s<numSplats; s++)
						splats[z,x,s] = nRow[s]*percent + splats[z,x,s]*(1-percent);
				}
			}		

			#if WDEBUG
			Profiler.EndSample();
			#endif
		}

		public static void WeldSplatToNextZ (ref float[,,] splats, Terrain neig, int margin)
		{
			#if WDEBUG
			Profiler.BeginSample("Weld Splat");
			#endif

			int splatsSize = splats.GetLength(0);
			int numSplats = splats.GetLength(2);
			float[] nRow = new float[numSplats];

			if (margin==0 || neig.terrainData.alphamapResolution!=splatsSize || neig.terrainData.alphamapLayers!=numSplats) return;

			float[,,] nStrip = neig.terrainData.GetAlphamaps(0,0,splatsSize,1);

			for (int x=0; x<splatsSize; x++)
			{
				for (int s=0; s<numSplats; s++) nRow[s] = nStrip[0,x,s];
			
				//float percentFromSide = Mathf.Min( Mathf.Clamp01(1f*x/margin), Mathf.Clamp01(1 - 1f*(x-(splatsSize-1-margin))/margin) );
				//float invPercentFromSide = 2000000000; if (percentFromSide > 0.0001f) invPercentFromSide = 1f/percentFromSide;

				for (int z=splatsSize-margin; z<splatsSize; z++)
				{
					float percent = 1 - 1f*(splatsSize-z-1)/margin;
					//if (percentFromSide < 0.999f) percent = Mathf.Pow(percent, invPercentFromSide);
					//percent = 3*percent*percent - 2*percent*percent*percent;
						
					for (int s=0; s<numSplats; s++)
						splats[z,x,s] = nRow[s]*percent + splats[z,x,s]*(1-percent);
				}
			}	
			
			#if WDEBUG
			Profiler.EndSample();
			#endif	
		}

		public static void WeldTextureToPrevX (Texture2D texture, Texture2D neig) //TODO: margin is 1 now
		{
			if (neig==null) return;
			int splatsSize = texture.width;
			Color[] nStrip = neig.GetPixels(splatsSize-1,0,1,splatsSize);
			texture.SetPixels(0,0,1,splatsSize, nStrip);
		}

		public static void WeldTextureToNextX (Texture2D texture, Texture2D neig)
		{
			if (neig==null) return;
			int splatsSize = texture.width;
			Color[] nStrip = neig.GetPixels(0,0,1,splatsSize);
			texture.SetPixels(splatsSize-1,0,1,splatsSize, nStrip);
		}

		public static void WeldTextureToPrevZ (Texture2D texture, Texture2D neig)
		{
			if (neig==null) return;
			int splatsSize = texture.height;
			Color[] nStrip = neig.GetPixels(0,splatsSize-1,splatsSize,1);
			texture.SetPixels(0,0,splatsSize,1, nStrip);
		}

		public static void WeldTextureToNextZ (Texture2D texture, Texture2D neig)
		{
			if (neig==null) return;
			int splatsSize = texture.height;
			Color[] nStrip = neig.GetPixels(0,0,splatsSize,1);
			texture.SetPixels(0,splatsSize-1,splatsSize,1, nStrip);
		}



		public static void WeldHeights (float[,] heights, Terrain prevX, Terrain nextZ, Terrain nextX, Terrain prevZ, int margin)
		{
			int heightSize = heights.GetLength(0);
			if (margin == 0) return;
		
			//creating delta heights
			float[,] deltaHeights = new float[heightSize,heightSize];

			//prev x
			if (prevX != null &&  prevX.terrainData.heightmapResolution==heightSize)
			{
				float[,] nStrip = prevX.terrainData.GetHeights(heightSize-1,0,1,heightSize);

				for (int z=0; z<heightSize; z++)
				{
					float delta = nStrip[z,0] - (heights[z,0]+deltaHeights[z,0]);

					//float percentFromSide = Mathf.Min( Mathf.Clamp01(1f*z/margin),  Mathf.Clamp01(1 - 1f*(z-(heightSize-1-margin))/margin) );
					//float invPercentFromSide = 2000000000; if (percentFromSide > 0.0001f) invPercentFromSide = 1f/percentFromSide;

					deltaHeights[z,0] = delta;
					//deltaHeights[z,1] = heights[z,0] + delta + vector - heights[z,1];

					for (int x=1; x<margin; x++)
					{
						float percent = 1-1f*x/margin;
						//if (percentFromSide < 0.999f) percent = Mathf.Pow(percent, invPercentFromSide);
						percent = 3*percent*percent - 2*percent*percent*percent;
				
						deltaHeights[z,x] += delta*percent;
					}
				}		
			}

			//next x
			if (nextX != null && nextX.terrainData.heightmapResolution==heightSize)
			{
				float[,] nStrip = nextX.terrainData.GetHeights(0,0,1,heightSize);

				for (int z=0; z<heightSize; z++)
				{
					float delta = nStrip[z,0] - heights[z,heightSize-1] - deltaHeights[z,heightSize-1];

					//float percentFromSide = Mathf.Min( Mathf.Clamp01(1f*z/margin),  Mathf.Clamp01(1 - 1f*(z-(heightSize-1-margin))/margin) );
					//float invPercentFromSide = 2000000000; if (percentFromSide > 0.0001f) invPercentFromSide = 1f/percentFromSide;

					for (int x=heightSize-margin; x<heightSize; x++)
					{
						float percent = 1 - 1f*(heightSize-x-1)/margin;
						//if (percentFromSide < 0.999f) percent = Mathf.Pow(percent, invPercentFromSide);
						percent = 3*percent*percent - 2*percent*percent*percent;
				
						deltaHeights[z,x] += delta*percent;
					}
				}	
			}

			//prev z
			if (prevZ != null && prevZ.terrainData.heightmapResolution==heightSize)
			{
				float[,] nStrip = prevZ.terrainData.GetHeights(0,heightSize-1,heightSize,1);

				for (int x=0; x<heightSize; x++)
				{
					float delta = nStrip[0,x] - heights[0,x] - deltaHeights[0,x];

					float percentFromSide = Mathf.Min( Mathf.Clamp01(1f*x/margin),  Mathf.Clamp01(1 - 1f*(x-(heightSize-1-margin))/margin) );
					float invPercentFromSide = 2000000000; if (percentFromSide > 0.0001f) invPercentFromSide = 1f/percentFromSide;

					for (int z=0; z<margin; z++)
					{
						float percent = 1-1f*z/margin;
						if (percentFromSide < 0.999f) percent = Mathf.Pow(percent, invPercentFromSide);
						percent = 3*percent*percent - 2*percent*percent*percent;
				
						deltaHeights[z,x] += delta*percent;
					}
				}	
			}

			//next z
			if (nextZ != null && nextZ.terrainData.heightmapResolution==heightSize)
			{
				float[,] nStrip = nextZ.terrainData.GetHeights(0,0,heightSize,1);

				for (int x=0; x<heightSize; x++)
				{
					float delta = nStrip[0,x] - heights[heightSize-1,x] - deltaHeights[heightSize-1,x];

					float percentFromSide = Mathf.Min( Mathf.Clamp01(1f*x/margin), Mathf.Clamp01(1 - 1f*(x-(heightSize-1-margin))/margin) );
					float invPercentFromSide = 2000000000; if (percentFromSide > 0.0001f) invPercentFromSide = 1f/percentFromSide;

					for (int z=heightSize-margin; z<heightSize; z++)
					{
						float percent = 1 - 1f*(heightSize-z-1)/margin;
						if (percentFromSide < 0.999f) percent = Mathf.Pow(percent, invPercentFromSide);
						percent = 3*percent*percent - 2*percent*percent*percent;
				
						deltaHeights[z,x] += delta*percent;
					}
				}
			}

			//saving delta heights
			for (int z=0; z<heightSize; z++)
				for (int x=0; x<heightSize; x++)
					heights[z,x] += deltaHeights[z,x];

			//setting neighbours
			//current.SetNeighbors(prevX, nextZ, nextX, prevZ);
		}


		public static void WeldSplats (float[,,] splats, Terrain prevX, Terrain nextZ, Terrain nextX, Terrain prevZ, int margin)
		{
			if (margin == 0) return;
			int splatsSize = splats.GetLength(0);
			int numSplats = splats.GetLength(2);
			float[] nRow = new float[numSplats];

			//prev x
			if (prevX != null && prevX.terrainData.alphamapResolution==splatsSize && prevX.terrainData.alphamapLayers==numSplats)
			{
				float[,,] nStrip = prevX.terrainData.GetAlphamaps(splatsSize-1,0,1,splatsSize);

				for (int z=0; z<splatsSize; z++)
				{
					for (int s=0; s<numSplats; s++) nRow[s] = nStrip[z,0,s];

					float percentFromSide = Mathf.Min( Mathf.Clamp01(1f*z/margin),  Mathf.Clamp01(1 - 1f*(z-(splatsSize-1-margin))/margin) );
					float invPercentFromSide = 2000000000; if (percentFromSide > 0.0001f) invPercentFromSide = 1f/percentFromSide;

					for (int x=0; x<margin; x++)
					{
						float percent = 1-1f*x/margin;
						if (percentFromSide < 0.999f) percent = Mathf.Pow(percent, invPercentFromSide);
						percent = 3*percent*percent - 2*percent*percent*percent;
						
						for (int s=0; s<numSplats; s++)
							splats[z,x,s] = nRow[s]*percent + splats[z,x,s]*(1-percent);
					}
				}		
			}

			//next x
			if (nextX != null && nextX.terrainData.alphamapResolution==splatsSize && nextX.terrainData.alphamapLayers==numSplats)
			{
				float[,,] nStrip = nextX.terrainData.GetAlphamaps(0,0,1,splatsSize);

				for (int z=0; z<splatsSize; z++)
				{
					for (int s=0; s<numSplats; s++) nRow[s] = nStrip[z,0,s];

					float percentFromSide = Mathf.Min( Mathf.Clamp01(1f*z/margin),  Mathf.Clamp01(1 - 1f*(z-(splatsSize-1-margin))/margin) );
					float invPercentFromSide = 2000000000; if (percentFromSide > 0.0001f) invPercentFromSide = 1f/percentFromSide;

					for (int x=splatsSize-margin; x<splatsSize; x++)
					{
						float percent = 1 - 1f*(splatsSize-x-1)/margin;
						if (percentFromSide < 0.999f) percent = Mathf.Pow(percent, invPercentFromSide);
						percent = 3*percent*percent - 2*percent*percent*percent;
						
						for (int s=0; s<numSplats; s++)
							splats[z,x,s] = nRow[s]*percent + splats[z,x,s]*(1-percent);
					}
				}		
			}

			//prev z
			if (prevZ != null && prevZ.terrainData.alphamapResolution==splatsSize && prevZ.terrainData.alphamapLayers==numSplats)
			{
				float[,,] nStrip = prevZ.terrainData.GetAlphamaps(0,splatsSize-1,splatsSize,1);

				for (int x=0; x<splatsSize; x++)
				{
					for (int s=0; s<numSplats; s++) nRow[s] = nStrip[0,x,s];

					float percentFromSide = Mathf.Min( Mathf.Clamp01(1f*x/margin),  Mathf.Clamp01(1 - 1f*(x-(splatsSize-1-margin))/margin) );
					float invPercentFromSide = 2000000000; if (percentFromSide > 0.0001f) invPercentFromSide = 1f/percentFromSide;

					for (int z=0; z<margin; z++)
					{
						float percent = 1-1f*z/margin;
						if (percentFromSide < 0.999f) percent = Mathf.Pow(percent, invPercentFromSide);
						percent = 3*percent*percent - 2*percent*percent*percent;
						
						for (int s=0; s<numSplats; s++)
							splats[z,x,s] = nRow[s]*percent + splats[z,x,s]*(1-percent);
					}
				}		
			}

			//next z
			if (nextZ != null && nextZ.terrainData.alphamapResolution==splatsSize && nextZ.terrainData.alphamapLayers==numSplats)
			{
				float[,,] nStrip = nextZ.terrainData.GetAlphamaps(0,0,splatsSize,1);

				for (int x=0; x<splatsSize; x++)
				{
					for (int s=0; s<numSplats; s++) nRow[s] = nStrip[0,x,s];

					float percentFromSide = Mathf.Min( Mathf.Clamp01(1f*x/margin), Mathf.Clamp01(1 - 1f*(x-(splatsSize-1-margin))/margin) );
					float invPercentFromSide = 2000000000; if (percentFromSide > 0.0001f) invPercentFromSide = 1f/percentFromSide;

					for (int z=splatsSize-margin; z<splatsSize; z++)
					{
						float percent = 1 - 1f*(splatsSize-z-1)/margin;
						if (percentFromSide < 0.999f) percent = Mathf.Pow(percent, invPercentFromSide);
						percent = 3*percent*percent - 2*percent*percent*percent;
						
						for (int s=0; s<numSplats; s++)
							splats[z,x,s] = nRow[s]*percent + splats[z,x,s]*(1-percent);
					}
				}		
			}
		}


		public static void WeldTexture (Texture2D texture, Texture2D prevX, Texture2D nextZ, Texture2D nextX, Texture2D prevZ, int margin)
		{
			if (margin == 0) return;
			int splatsSize = texture.width;
			//int numSplats = 4;

			//prev x
			if (prevX != null && prevX.width==splatsSize)
			{
				Color[] nStrip = prevX.GetPixels(splatsSize-1,0,1,splatsSize);
				
				//if (margin==1) 
				texture.SetPixels(0,0,1,splatsSize, nStrip);
				/*else 
					for (int x=0; x<margin; x++)
					{
						Color[] strip = texture.GetPixels(0+x,0,1,splatsSize);

						for (int z=0; z<splatsSize; z++)
						{
							float percentFromSide = Mathf.Min( Mathf.Clamp01(1f*z/margin),  Mathf.Clamp01(1 - 1f*(z-(splatsSize-1-margin))/margin) );
							float invPercentFromSide = 2000000000; if (percentFromSide > 0.0001f) invPercentFromSide = 1f/percentFromSide;

							float percent = 1-1f*x/margin;
							if (percentFromSide < 0.999f) percent = Mathf.Pow(percent, invPercentFromSide);
							//percent = 3*percent*percent - 2*percent*percent*percent;

							strip[z] = nStrip[z]*percent + strip[z]*(1-percent);
						}

						texture.SetPixels(0+x,0,1,splatsSize, strip);
					}*/
			}



			//next x
			if (nextX != null && nextX.width==splatsSize)
			{
				Color[] nStrip = nextX.GetPixels(0,0,1,splatsSize);
				texture.SetPixels(splatsSize-1,0,1,splatsSize, nStrip);
			}

			//prev z
			if (prevZ != null && prevZ.width==splatsSize)
			{
				Color[] nStrip = prevZ.GetPixels(0,splatsSize-1,splatsSize,1);
				texture.SetPixels(0,0,splatsSize,1, nStrip);
			}

			//prev z
			if (nextZ != null && nextZ.width==splatsSize)
			{
				Color[] nStrip = nextZ.GetPixels(0,0,splatsSize,1);
				texture.SetPixels(0,splatsSize-1,splatsSize,1, nStrip);
			}

		}


	}//class
}//plugins