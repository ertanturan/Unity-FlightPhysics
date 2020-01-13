using UnityEngine;
using System.Collections;

public class LO_LaunchURL : MonoBehaviour {

	public void urlLinkOrWeb(string URL) 
	{
		Application.OpenURL(URL);
	}
}
