using UnityEngine;
using System.Collections;

namespace MapMagic 
{
	public class InstanceRandom
	{
		private int seed;
		
		private float[] lut;
		private int current;
		
		public InstanceRandom (int s, int lutLength=100) 
		{ 
			seed = s; 
			lut = new float[lutLength];
			for (int i=0; i<lut.Length; i++) lut[i] = Random();
		}

		public float Random ()
		{ 
			seed = 214013*seed + 2531011; 
			return ((seed>>16)&0x7FFF) / 32768f;
		}

		public float Random (float min, float max)
		{
			seed = 214013*seed + 2531011; 
			float rnd = ((seed>>16)&0x7FFF) / 32768f;
			return rnd*(max-min) + min;
		}

		public float Random (Vector2 scope)
		{
			seed = 214013*seed + 2531011; 
			float rnd = ((seed>>16)&0x7FFF) / 32768f;
			return rnd*(scope.y-scope.x) + scope.x;
		}

		public int RandomToInt (float val)
		{
			seed = 214013*seed + 2531011; 
			float random = ((seed>>16)&0x7FFF) / 32768f;

			int integer = (int)val;
			float remain = val - integer;
			if (remain>random) integer++;
			return integer;
		}
		
		//the chance 0-1 iterated steps times
		public float MultipleRandom (int steps)
		{
			float random = Random();
			return (1-Mathf.Pow(random,steps+1)) / (1-random) - 1;
		}

		public float CoordinateRandom (int x)
		{
			current = x % lut.Length;
			return lut[current];
		}

		public float CoordinateRandom (int x, Vector2 scope)
		{
			current = x % lut.Length;
			return lut[current]*(scope.y-scope.x) + scope.x;
		}

		public float CoordinateRandom (int x, int z)
		{
			z+=991; x+=1999;
			current = (x*x)%5453 + Mathf.Abs((z*x)%2677) + (z*z)%1871;
			current = current%lut.Length;
			if (current<0) current = -current;
			return lut[current];
		}

		public float NextCoordinateRandom ()
		{
			current++;
			current = current%lut.Length;
			return lut[current];
		}


		public static float Fractal (int x, int z, float size, float detail=0.5f)
		{
			//x+=1000
		
			float result = 0.5f;
			float curSize = size;
			float curAmount = 1;

			//get number of iterations
			int numIterations = 1; //max size iteration included
			for (int i=0; i<100; i++)
			{
				curSize = curSize/2;
				if (curSize<1) break;
				numIterations++;
			}

			//applying noise
			curSize = size;
			for (int i=0; i<numIterations;i++)
			{	
				float perlin = Mathf.PerlinNoise(x/(curSize+1), z/(curSize+1));
				perlin = (perlin-0.5f)*curAmount + 0.5f;

				//applying overlay
				if (perlin > 0.5f) result = 1 - 2*(1-result)*(1-perlin); //(1 - (1-2*(perlin-0.5f)) * (1-result));
				else result = 2*perlin*result;

				curSize *= 0.5f;
				curAmount *= detail; //detail is 0.5 by default
			}

			if (result < 0) result = 0;
			if (result > 1) result = 1;
				
			return result;
		}


		private readonly int[] permutation = { 151,160,137,91,90,15,                 // Hash lookup table as defined by Ken Perlin.  This is a randomly
			131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,    // arranged array of all numbers from 0-255 inclusive.
			190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
			88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
			77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
			102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
			135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
			5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
			223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
			129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
			251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
			49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
			138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180 };
		private readonly int[] gradX = { 1, -1,  1, -1,  1, -1,  0,  0 };
		private readonly int[] gradZ = { 0,  0,  1,  1, -1, -1,  1, -1 };

		private readonly int[] gradX2 = { 1, -1, 1, -1,  1, -1, 1, -1, 0,0,0,0 };
		private readonly int[] gradZ2 = { 1,  1,-1, -1,  0, 0, 0, 0, 1,-1,1,-1 };

		//private readonly Vector3[] grad = new Vector3[] {new Vector3(1,1,0), new Vector3(-1,1,0), new Vector3(1,-1,0), new Vector3(-1,-1,0),  new Vector3(1,0,1), new Vector3(-1,0,1), new Vector3(1,0,-1), new Vector3(-1,0,-1), new Vector3(0,1,1), new Vector3(0,-1,1), new Vector3(0,1,-1), new Vector3(0,-1,-1) };

		private int[] p; 

		public float Perlin (float x, float z)
		{
			if (p==null) 
			{
				p = new int[512];
				for(int i=0; i<512; i++)
					 p[i] = permutation[i%256];
			}

			//cell (i) and percent (f)
			int xi = x>0? (int)x : (int)x-1; 
			int zi = z>0? (int)z : (int)z-1;

			float xf = x-xi; 
			float zf = z-zi;
			
			xi = xi & 255; 
			zi = zi & 255;

			//hash
			int aa = ( p[ p[xi] + zi ] )&0x7;
			int ab = ( p[ p[xi] + zi+1] )&0x7;
			int ba = ( p[ p[xi+1] + zi] )&0x7;
			int bb = ( p[ p[xi+1] + zi+1] )&0x7;

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
			if (p==null) 
			{
				p = new int[512];
				for(int tmi=0; tmi<512; tmi++)
					 p[tmi] = permutation[tmi%256];
			}

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
			int ii = i & 255;
			int jj = j & 255;
			int gi0 = p[ii+p[jj]] % 8;
			int gi1 = p[ii+i1+p[jj+j1]] % 8;
			int gi2 = p[ii+1+p[jj+1]] % 8; 
			
			// Calculate the contribution from the three corners
			float n0, n1, n2; 

			float t0 = 0.5f - x0*x0 - z0*z0;
			if(t0<0) n0 = 0;
			else n0 = t0*t0*t0*t0 * (gradX2[gi0]*x0 + gradZ2[gi0]*z0);  

			float t1 = 0.5f - x1*x1-z1*z1;
			if(t1<0) n1 = 0;
			else n1 = t1*t1*t1*t1 * (gradX2[gi1]*x1 + gradZ2[gi1]*z1);

			float t2 = 0.5f - x2*x2-z2*z2;
			if(t2<0) n2 = 0;
			else n2 = t2*t2*t2*t2 * (gradX2[gi2]*x2 + gradZ2[gi2]*z2);

			return (float)(70.0 * (n0 + n1 + n2) + 1) / 2;
		}

		public float Fractal2 (int x, int y, float size=200, float persistence=2, int iterations=10)
		{
			float maxAmp = 0;
			float amp = 1;

			//filling the first iteration with simple noise
			float result = GradRandom(x,y);
			maxAmp += amp;
			amp *= persistence;

			//filling the second iteration with interpolated noise
			result += GradRandom(x/2f, y/2f) * amp;
			maxAmp += amp;
			amp *= persistence;

			//other iterations
			float freq = 4f;
			for (int i=2; i<iterations; i++)
			{
				result += Perlin(x/freq, y/freq) * amp;
				freq *= 2;

				maxAmp += amp;
				amp *= persistence;
			}

			return result / maxAmp;

			/*

			for(int i=0; i<num_iterations; i++)
			{
				noise += Perlin(x, y);
				maxAmp += amp;
				amp *= persistence;
				freq *= 2;
			}

			//normalizing
			noise /= maxAmp;
			noise = noise * (high - low) / 2 + (high + low) / 2;

			return GradRandom(x/2f, y/2f);*/
		}



		/*public float Fractal (int x, int z, float detail=0.5f)
		{
			float result = 0.5f;
			float curSize = size;
			float curAmount = 1;

			//making x and z resolution independent
			float rx = 1f*x / resolution * 512;
			float rz = 1f*z / resolution * 512;
				
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
			return result;
		}*/


		public float GradRandom (int x, int z)
		{
			if (p==null) 
			{
				p = new int[512];
				for(int i=0; i<512; i++)
					 p[i] = permutation[i%256];
			}

			x = x % 255;
			z = z % 255;
			return p[ p[x] + z ] / 256f;
		}

		public float GradRandom (float x, float z)
		{
			if (p==null) 
			{
				p = new int[512];
				for(int i=0; i<512; i++)
					 p[i] = permutation[i%256];
			}


			//cell (i) and percent (f)
			int xi = x>0? (int)x : (int)x-1; 
			int zi = z>0? (int)z : (int)z-1;

			float xf = x-xi; 
			float zf = z-zi;
			
			xi = xi & 255; 
			zi = zi & 255;

			//fade
			float xfade = 3*xf*xf - 2*xf*xf*xf;  //xf*xf*xf*(xf* (xf*6 - 15) + 10);
			float zfade = 3*zf*zf - 2*zf*zf*zf;  //zf*zf*zf*(zf* (zf*6 - 15) + 10);

			//random values
			int aa = p[ p[xi] + zi ];
			int ab = p[ p[xi] + zi+1];
			int ba = p[ p[xi+1] + zi];
			int bb = p[ p[xi+1] + zi+1];

			//interpolation
			float x1 = aa*(1-xfade) + ba*xfade;
			float x2 = ab*(1-xfade) + bb*xfade; 
			float z2 = x1*(1-zfade) + x2*zfade;

			return z2/256f;
		}



		#region Legacy Noise

		private int iterations;
		public int seedX;
		public int seedZ;
		public int resolution;
		public float size;

		public InstanceRandom (float size, int resolution, int seedX, int seedZ)
		{
			this.size = size; this.resolution = resolution;
			this.seedX = seedX % 77777; this.seedZ = seedZ % 73333; //for backwards compatibility
			
			//get number of iterations
			iterations = 1; //max size iteration included
			float tempSize = size;
			for (int i=0; i<100; i++)
			{
				tempSize = tempSize/2;
				if (tempSize<1) break;
				iterations++;
			}
		}
		
		public float Fractal (int x, int z, float detail=0.5f)
		{
			float result = 0.5f;
			float curSize = size;
			float curAmount = 1;

			//making x and z resolution independent
			float rx = 1f*x / resolution * 512;
			float rz = 1f*z / resolution * 512;
				
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
			return result;
		}

		//public float Perlin
		#endregion
	}
}
