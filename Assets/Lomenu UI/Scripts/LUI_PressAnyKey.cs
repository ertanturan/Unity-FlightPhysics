using UnityEngine;

public class LUI_PressAnyKey : MonoBehaviour {

	[Header("OBJECTS")]
	public GameObject scriptObject;
	public Animator animatorComponent;

    [Header("ANIM NAMES")]
    public string outAnim;

    void Start ()
	{
		animatorComponent.GetComponent<Animator>();
	}

	void Update ()
	{
		if (Input.anyKeyDown) 
		{
			animatorComponent.Play (outAnim);
			Destroy (scriptObject);
		}
	}
}