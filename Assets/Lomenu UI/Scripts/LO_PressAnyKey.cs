using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LO_PressAnyKey : MonoBehaviour {

	[Header("OBJECTS")]
	public GameObject scriptObject;
	public Animator animatorComponent;

	void Start ()
	{
		animatorComponent.GetComponent<Animator>();
	}

	void Update ()
	{
		if (Input.anyKeyDown) 
		{
			animatorComponent.Play ("PAK Fade-out");
			Destroy (scriptObject);
		}
	}
}
