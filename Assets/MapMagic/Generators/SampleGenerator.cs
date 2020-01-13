using System;
using UnityEngine;
using System.Collections.Generic;
using MapMagic;
//using Plugins;

namespace MapMagic //it's recommended to use your own namespace
{
	[System.Serializable]
	//[GeneratorMenu (menu="MyGenerators", name ="MyGenerator", disengageable = true)]  //uncomment this line to add generator to right-click menu
	public class MyCustomGenerator : Generator
	{
		//input and output properties
		public Input input = new Input(InoutType.Map);
		public Output output = new Output(InoutType.Map);

		//including in enumerator
		public override IEnumerable<Input> Inputs() { yield return input; }
		public override IEnumerable<Output> Outputs() { yield return output; }
		public float level = 1;
		public override void Generate (CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			Matrix src = (Matrix)input.GetObject(results);

			if (src==null || (stop!=null && stop(0))) return;
			if (!enabled) { output.SetObject(results, src); return; }

			Matrix dst = new Matrix(src.rect);

			Coord min = src.rect.Min; Coord max = src.rect.Max;

			for (int x=min.x; x<max.x; x++)
			   for (int z=min.z; z<max.z; z++)
			{
				  float val = level - src[x,z];
				  dst[x,z] = val>0? val : 0;
			}

			if (stop!=null && stop(0)) return;
			output.SetObject(results, dst); 
		}

		public override void OnGUI (GeneratorsAsset gens)
		{
			layout.Par(20); input.DrawIcon(layout, "Input", mandatory:true); output.DrawIcon(layout, "Output");
			layout.Field(ref level, "Level", min:0, max:2);

		}

	}
}


