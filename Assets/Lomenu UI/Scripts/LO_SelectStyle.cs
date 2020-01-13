using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LO_SelectStyle : MonoBehaviour 
{
	public void SetStyle(string prefabToLoad)
	{
		LO_LoadingScreen.prefabName = prefabToLoad;
	}
}