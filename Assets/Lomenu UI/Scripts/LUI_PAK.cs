using UnityEngine;

public class LUI_PAK : MonoBehaviour {

	[Header("VARIABLES")]
	public GameObject mainCanvas;
	public GameObject scriptObject;
	public Animator animatorComponent;
	public string animName;

	void Start ()
	{
		animatorComponent.GetComponent<Animator>();
	}

	void Update ()
	{
		if (Input.anyKeyDown) 
		{
			animatorComponent.Play (animName);
			mainCanvas.SetActive(true);
			Destroy (scriptObject);
		}
	}
}