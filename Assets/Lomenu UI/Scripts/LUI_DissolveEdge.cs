using System.Collections;
using UnityEngine;

public class LUI_DissolveEdge : MonoBehaviour {

	[Header("VARIABLES")]
	public GameObject dissolveObject;
	public Material dissolveMaterial;
	[Range(0.0f, 1.0f)] public float dissolveValue = 1.0f;
	[Range(0.1f, 100)] public float animationSpeed = 5;

	[Header("SETTINGS")]
	public bool disableObjectAfterAnimation = true;
	public bool fadingOut = false;
	public bool playedOnce = false;

	void Start () 
	{
		playedOnce = false;

		if (fadingOut == false) 
		{
			dissolveValue = 0;
			fadingOut = false;
		} 

		else
		{
			dissolveValue = 1;
			fadingOut = true;
		}
	}

	void Update () 
	{
		if (fadingOut == false && playedOnce == false) 
		{
			dissolveValue += Time.deltaTime / animationSpeed;
			fadingOut = false;

			if (dissolveValue == 1 || dissolveValue >= 1) 
			{
				playedOnce = true;
				fadingOut = true;
				dissolveObject.SetActive (false);
			}
		} 

		else if (fadingOut == true && playedOnce == false) 
		{
			dissolveValue -= Time.deltaTime / animationSpeed;
			fadingOut = true;
	
			if (dissolveValue == 0 || dissolveValue <= 0) 
			{
				playedOnce = true;
				fadingOut = false;
				dissolveObject.SetActive (false);
			}
		} 
		dissolveMaterial.SetFloat ("_Progress", dissolveValue);
	}
}
