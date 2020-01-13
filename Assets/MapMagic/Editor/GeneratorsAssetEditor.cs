
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using MapMagic;

namespace MapMagic 
{
	[CustomEditor(typeof(GeneratorsAsset))]
	public class GeneratorsAssetEditor : Editor
	{
		Layout layout;

		public override void OnInspectorGUI () 
		{
			GeneratorsAsset gens = (GeneratorsAsset)target;
			
			if (layout == null) layout = new Layout();
			layout.margin = 0;
			layout.field = Layout.GetInspectorRect();
			layout.cursor = new Rect();
			layout.undoObject = gens;
			layout.undoName =  "MapMagic Generator change";

			layout.Par(24); if (layout.Button("Show Editor", rect:layout.Inset(), icon:"MapMagic_EditorIcon"))
			{
				MapMagicWindow.Show(gens, null, forceOpen:true, asBiome:false);
			}

			Layout.SetInspectorRect(layout.field);
		}
		

	}
}