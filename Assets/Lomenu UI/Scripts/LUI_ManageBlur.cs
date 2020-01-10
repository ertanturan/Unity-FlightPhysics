using UnityEngine;

public class LUI_ManageBlur : MonoBehaviour {

    [Header("VARIABLES")]
    public GameObject blurObject;
    public Material blurMaterial;
    [Range(0f, 10f)] public float blurSize = 1.0f;

    void Update()
    {
        blurMaterial.SetFloat("_Radius", blurSize);
    }
}