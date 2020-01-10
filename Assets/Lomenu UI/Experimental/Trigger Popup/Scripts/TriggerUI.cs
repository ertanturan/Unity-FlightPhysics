using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerUI : MonoBehaviour {

	[Header("SETTINGS")]
	public bool onTriggerExit;
	public bool onlyShowWithTag;

	[Header("STRINGS")]
	public string objectTag;

	[Header("OBJECTS")]
	public GameObject setActiveObj;

	private void OnTriggerEnter(Collider other)
	{		
		if(onlyShowWithTag == true && onTriggerExit == false)
		{
			if (other.gameObject.tag == objectTag) 
			{
				setActiveObj.SetActive (true);
			}
		}
		else if (onTriggerExit == false)
		{
			setActiveObj.SetActive (true);
		}
	}

	private void OnTriggerExit(Collider other)
	{		
		if(onlyShowWithTag == true && onTriggerExit == true)
		{
			if (other.gameObject.tag == objectTag) 
			{
				setActiveObj.SetActive (true);
			}
		}
		else if (onTriggerExit == true)
		{
			setActiveObj.SetActive (true);
		}
	}
}