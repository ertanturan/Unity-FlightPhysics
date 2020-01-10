using UnityEngine;
using System.Collections;

public class LUI_LaunchURL : MonoBehaviour {

	public string URL;
	public string URL2;

	public void urlLinkOrWeb() 
	{
		Application.OpenURL(URL);
	}

	public void urlLinkOrWeb2() 
	{
		Application.OpenURL(URL2);
	}
}
