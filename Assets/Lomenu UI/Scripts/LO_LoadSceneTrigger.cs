using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class LO_LoadSceneTrigger : MonoBehaviour 
{
	[Header("SETTINGS")]
	public bool onTriggerExit;
	public bool onlyLoadWithTag;

	[Header("STRINGS")]
	public string objectTag;
	public string sceneName;
	public string prefabToLoad;

	private void OnTriggerEnter(Collider other)
	{		
		if(onlyLoadWithTag == true && onTriggerExit == false)
		{
			if (other.gameObject.tag == objectTag) 
			{
				LO_LoadingScreen.prefabName = prefabToLoad;
				LO_LoadingScreen.LoadScene(sceneName);
			}
		}
		else if (onTriggerExit == false)
		{
			LO_LoadingScreen.prefabName = prefabToLoad;
			LO_LoadingScreen.LoadScene(sceneName);
		}
	}

	private void OnTriggerExit(Collider other)
	{		
		if(onlyLoadWithTag == true && onTriggerExit == true)
		{
			if (other.gameObject.tag == objectTag) 
			{
				LO_LoadingScreen.prefabName = prefabToLoad;
				LO_LoadingScreen.LoadScene(sceneName);
			}
		}
		else if (onTriggerExit == true)
		{
			LO_LoadingScreen.prefabName = prefabToLoad;
			LO_LoadingScreen.LoadScene(sceneName);
		}
	}
}