using UnityEngine;
using System.Collections;

namespace MapMagic
{
	[System.Serializable]
	[PreferBinarySerialization]
	public class MatrixAsset : ScriptableObject
	{ 
		public Matrix matrix;
		public Matrix preview; 
		
		/*public void ImportRaw (string path=null)
		{
			#if UNITY_EDITOR
			//importing
			if (path==null) path = UnityEditor.EditorUtility.OpenFilePanel("Import Texture File", "", "raw,r16");
			if (path==null || path.Length==0) return;

			UnityEditor.Undo.RecordObject(this, "Open RAW");

			//creating matrix
			if (matrix==null) matrix = new Matrix( new CoordRect(0,0,1,1) );
			matrix.ImportRaw(path);
			
			//generating preview
			CoordRect previewRect = new CoordRect(0,0, 128, 128);
			preview = matrix.Resize(previewRect, preview);
			#endif
		}

		public Texture2D GetPreviewTexture () { return preview.SimpleToTexture(); }*/

	}
}
