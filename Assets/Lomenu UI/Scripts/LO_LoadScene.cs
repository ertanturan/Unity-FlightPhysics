using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class LO_LoadScene : MonoBehaviour
{
	public void ChangeToScene (string sceneName)
	{		
		LO_LoadingScreen.LoadScene(sceneName);
	}
}
