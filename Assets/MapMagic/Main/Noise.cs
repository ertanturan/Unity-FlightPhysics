using UnityEngine;
using System.Collections;

namespace MapMagic
{
	[System.Serializable]
	public class Noise
	{
		private short[] permutation;
		private int permutationCount = 1024;
		private int permutationCountMinusOne = 1023;
		private readonly int[] gradX = { 1, -1,  1, -1,  1, -1,  0,  0 };
		private readonly int[] gradZ = { 0,  0,  1,  1, -1, -1,  1, -1 };
		


		public Noise (int seed, int permutationCount=1024) { CreatePermutation(seed, permutationCount); }


		private void CreatePermutation (int seed, int count)
		{
			permutation = new short[count*2];
			permutationCount = count;
			permutationCountMinusOne = count - 1;

			//random part
			for (int i=0; i<permutationCount; i++)
			{
				//random
				seed = 214013*seed + 2531011; 
				float val = ((seed>>16)&0x7FFF) / 32768f;

				permutation[i] = (short)(val*permutationCount);

				//if ((byte)(val*256) > 255) Debug.Log("Excced");
			}

			//clone part
			for (int i=0; i<permutationCount; i++)
				permutation[i+permutationCount] = permutation[i];
		}


		public float Random (int x)
		{
			return 1f * (x & permutationCountMinusOne) / permutationCount;
		}

		public float Random (int x, int z)
		{
			x = x & permutationCountMinusOne;
			z = z & permutationCountMinusOne;

			x = permutation[x + permutation[z]];

			return 1f*x / permutationCount;
		}

		public float Random (int x, int y, int z)
		{
			x = x & permutationCountMinusOne;
			y = y & permutationCountMinusOne;
			z = z & permutationCountMinusOne;

			x = permutation[x + permutation[z + permutation[y]]];

			return 1f*x / permutationCount;
		}

		public float UniqueRandom (int x, int z)
		{
			int f = ((x^z)/permutationCount^x) & permutationCountMinusOne;

			x = x & permutationCountMinusOne;
			z = z & permutationCountMinusOne;

			x = permutation[x + permutation[z + permutation[f]]];

			return 1f*x / permutationCount;
		}

		public float Linear (float x, float z)
		{
			//cell (i) and percent (f)
			int xi = x>0? (int)x : (int)x-1; 
			int zi = z>0? (int)z : (int)z-1;

			float xf = x-xi; 
			float zf = z-zi;
			
			xi = xi & permutationCountMinusOne; 
			zi = zi & permutationCountMinusOne;
			//int f = ((xi^zi)/permutationCount^xi) & permutationCountMinusOne;

			//random corners
			int aa = permutation[ permutation[xi] + zi ];
			int ab = permutation[ permutation[xi] + zi+1];
			int ba = permutation[ permutation[xi+1] + zi];
			int bb = permutation[ permutation[xi+1] + zi+1];

			//fade
			float xfade = 3*xf*xf - 2*xf*xf*xf;  //xf*xf*xf*(xf* (xf*6 - 15) + 10);
			float zfade = 3*zf*zf - 2*zf*zf*zf;  //zf*zf*zf*(zf* (zf*6 - 15) + 10);

			//interpolation
			float x1 = aa*(1-xfade) + ba*xfade;
			float x2 = ab*(1-xfade) + bb*xfade; 
			float z2 = x1*(1-zfade) + x2*zfade;

			return 1f*z2 / permutationCount;
		}

		public float Perlin (float x, float z)
		{
			//cell (i) and percent (f)
			int xi = x>0? (int)x : (int)x-1; 
			int zi = z>0? (int)z : (int)z-1;

			float xf = x-xi; 
			float zf = z-zi;

			xi = xi & permutationCountMinusOne; 
			zi = zi & permutationCountMinusOne;

			//hash
			int aa = ( permutation[ permutation[xi] + zi ] )&0x7;
			int ab = ( permutation[ permutation[xi] + zi+1] )&0x7;
			int ba = ( permutation[ permutation[xi+1] + zi] )&0x7;
			int bb = ( permutation[ permutation[xi+1] + zi+1] )&0x7;

			//grad and dot
			float aa_gd = gradX[aa]*xf + gradZ[aa]*zf;
			float ab_gd = gradX[ab]*xf + gradZ[ab]*(zf-1);
			float ba_gd = gradX[ba]*(xf-1) + gradZ[ba]*zf;
			float bb_gd = gradX[bb]*(xf-1) + gradZ[bb]*(zf-1);

			//fade
			float xfade = 3*xf*xf - 2*xf*xf*xf;  //xf*xf*xf*(xf* (xf*6 - 15) + 10);
			float zfade = 3*zf*zf - 2*zf*zf*zf;  //zf*zf*zf*(zf* (zf*6 - 15) + 10);

			//interpolation
			float x1 = aa_gd*(1-xfade) + ba_gd*xfade;
			float x2 = ab_gd*(1-xfade) + bb_gd*xfade; 
			float z2 = x1*(1-zfade) + x2*zfade;

			return (z2+1) / 2;
		}

		public float Simplex (float x, float z) // based on Stefan Gustavson's code (http://webstaff.itn.liu.se/~stegu/simplexnoise/simplexnoise.pdf)
		{
			float f2 = 0.5f*(1.732050807568877f-1.0f);
			float g2 = (3.0f-1.732050807568877f)/6.0f;
			float s = (x+z) * f2; // Hairy factor for 2D

			int i = (x+s)>0? (int)(x+s) : (int)(x+s)-1; 
			int j = (z+s)>0? (int)(z+s) : (int)(z+s)-1;

			float t = (i+j) * g2;

			float X0 = i-t;
			float Z0 = j-t;
			float x0 = x-X0;
			float z0 = z-Z0;

			// For the 2D case, the simplex shape is an equilateral triangle.
			int i1 = x0>z0? 1 : 0; 
			int j1 = x0>z0? 0 : 1;
			
			float x1 = x0 - i1 + g2; 
			float z1 = z0 - j1 + g2;
			float x2 = x0 - 1 + 2*g2; 
			float z2 = z0 - 1 + 2*g2;  
			
			// Work out the hashed gradient indices of the three simplex corners
			int ii = i & permutationCountMinusOne;
			int jj = j & permutationCountMinusOne;
			int gi0 = permutation[ii+permutation[jj]] % 8;
			int gi1 = permutation[ii+i1+permutation[jj+j1]] % 8;
			int gi2 = permutation[ii+1+permutation[jj+1]] % 8; 
			
			// Calculate the contribution from the three corners
			float n0, n1, n2; 

			float t0 = 0.5f - x0*x0 - z0*z0;
			if(t0<0) n0 = 0;
			else n0 = t0*t0*t0*t0 * (gradX[gi0]*x0 + gradZ[gi0]*z0);  

			float t1 = 0.5f - x1*x1-z1*z1;
			if(t1<0) n1 = 0;
			else n1 = t1*t1*t1*t1 * (gradX[gi1]*x1 + gradZ[gi1]*z1);

			float t2 = 0.5f - x2*x2-z2*z2;
			if(t2<0) n2 = 0;
			else n2 = t2*t2*t2*t2 * (gradX[gi2]*x2 + gradZ[gi2]*z2);

			return (float)(70.0 * (n0 + n1 + n2) + 1) / 2;
		}


		public float Fractal (int x, int y, float persistence=2, int iterations=10, float turbulence=0, int type=0)
		{
			return 0.5f;
			/*float maxAmp = 0;
			float amp = 1;
			//float absTurbulence = turbulence>0? turbulence : -turbulence;

			//filling the first iteration with simple noise
			float result = Random(x,y);
			maxAmp += amp;
			amp *= persistence;

			//filling the second iteration with interpolated noise
			result += Linear(x/2f, y/2f) * amp;
			maxAmp += amp;
			amp *= persistence;

			//other iterations
			float freq = 4f;
			for (int i=2; i<iterations; i++)
			{
				if (i>=4)
				{
					//noise
					float val = 0;
					switch (type)
					{
						case 0: val = Mathf.PerlinNoise(x/freq, y/freq); break;
						case 1: val = Linear(x/freq, y/freq); break;
						case 2: val = Perlin(x/freq, y/freq); break;
						case 3: val = Simplex(x/freq, y/freq); break;
						default: val = 0; break; //should not happen
					}

					//turbulennce
					float turb = val*2 - 1;
					if (turb<0) turb = -turb;
					if (turbulence>0) turb = 1-turb;
				
					//enhancing turbulence
					//float sqturb = turb*2 - 1;
					//sqturb = sqturb*sqturb;
					//sqturb = (sqturb+1) / 2;
					//turb = turb*(1-absTurbulence) + sqturb*absTurbulence;

					//result += val*amp; //*(1-absTurbulence) + turb*amp*absTurbulence;

					//intensity
				//	val = (val-0.5f)*amp + 0.5f;

					//applying overlay
					if (val > 0.5f) result = 1 - 2*(1-result)*(1-val); //(1 - (1-2*(perlin-0.5f)) * (1-result));
					else result = 2*val*result;
					
					maxAmp += amp;
					amp *= persistence;
				}

				freq *= 2;
				
			}

			return result / maxAmp;*/
		}

		public float OverlayFractal (int x, int y, float persistence=2, int iterations=10, float turbulence=0, int type=0)
		{
			float result = 0.5f;

			float freq = Mathf.Pow(2,iterations);
			float amp = 1;

			for (int i=iterations-1; i>=0; i--) //symbolize iterating from high-size noise to low one
			{
				float val = 0;
				switch (type)
				{
					case 0: val = Mathf.PerlinNoise(x/freq, y/freq); break;
					case 1: val = Linear(x/freq, y/freq); break;
					case 2: val = Perlin(x/freq, y/freq); break;
					case 3: val = Simplex(x/freq, y/freq); break;
					default: val = 0; break; //should not happen
				}

				val = (val-0.5f)*amp + 0.5f;

				if (result > 0.5f) result = 1-2*(1-result)*(1-val); //(1 - (1-2*(perlin-0.5f)) * (1-result));
				else result = 2*val*result;

				freq /= 2;
				amp /= persistence;
			}

			return result;//result/ampMax;
		}

		public float LegacyFractal (int x, int y, float size, int iterations=-1, float detail=0.5f, float turbulence=0, int type=0)
		{
			float result = 0; //0.5f;
			float freq = size;
			float amp = 1;
			float absTurbulence = turbulence>0? turbulence : -turbulence;

			//get number of iterations
			if (iterations < 0) iterations = (int)Mathf.Log(size,2) + 1; //+1 max size iteration

			//applying noise
			freq = size;
			for (int i=0; i<iterations;i++)
			{	
				freq++;
				float val = 0;
				switch (type)
				{
					case -1: val = Mathf.PerlinNoise(x/freq + (permutation[0]+permutation[1])/1000f, y/freq + (permutation[2]+permutation[3])/1000f); break;
					case 0: val = Mathf.PerlinNoise(x/freq + (permutation[0]+permutation[1]), y/freq + (permutation[2]+permutation[3])); break;  // +arg should not be int, noise repeats itself
					case 1: val = Linear(x/freq, y/freq); break;
					case 2: val = Perlin(x/freq, y/freq); break;
					case 3: val = Simplex(x/freq, y/freq); break;
					default: val = 0; break; //should not happen
				}

				//turbulence
				if (absTurbulence > 0.001f)
				{
					float turb = val*2 - 1;
					if (turb<0) turb = -turb;
					if (turbulence>0) turb = 1-turb;

					val = val*(1-absTurbulence) + turb*absTurbulence;
				}

				//standard mode
				result += val*amp;

				freq = (freq-1) / 2f; //previously freq was increased by 1
				amp *= detail; //detail is 0.5 by default
			}
				
			return result * (1-detail);  //TODO: factor is 0 on the large detail
		}
	}
}


