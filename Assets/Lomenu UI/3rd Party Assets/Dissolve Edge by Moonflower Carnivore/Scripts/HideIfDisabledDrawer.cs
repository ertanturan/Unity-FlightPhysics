//https://forum.unity3d.com/threads/sharing-is-caring-hiding-optional-material-parameters.349952/

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;

public class HideIfDisabledDrawer : MaterialPropertyDrawer
{
    protected string[] argValue;
    bool bElementHidden;

    //constructor permutations -- params doesn't seem to work for property drawer inputs :( -----------
    public HideIfDisabledDrawer(string name1)
    {
        argValue = new string[] { name1 };
    }

    public HideIfDisabledDrawer(string name1, string name2)
    {
        argValue = new string[] { name1, name2 };
    }

    public HideIfDisabledDrawer(string name1, string name2, string name3)
    {
        argValue = new string[] { name1, name2, name3 };
    }

    public HideIfDisabledDrawer(string name1, string name2, string name3, string name4)
    {
        argValue = new string[] { name1, name2, name3, name4 };
    }

    //-------------------------------------------------------------------------------------------------

    public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
    {
        bElementHidden = false;
        for (int i = 0; i < editor.targets.Length; i++)
        {
            //material object that we're targetting...
            Material mat = editor.targets[i] as Material;
            if (mat != null)
            {
                //check for the dependencies:
                for (int j = 0; j < argValue.Length; j++)
                    bElementHidden |= !mat.IsKeywordEnabled(argValue[j]);
            }
        }

        if (!bElementHidden)
            editor.DefaultShaderProperty(prop, label);
    }

    //We need to override the height so it's not adding any extra (unfortunately texture drawers will still add an extra bit of padding regardless):
    public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
    {
        //@TODO: manually standardise element compaction
        //     float height = base.GetPropertyHeight (prop, label, editor);
        //     return bElementHidden ? 0.0f : height-16;

        return 0;
    }

}
#endif