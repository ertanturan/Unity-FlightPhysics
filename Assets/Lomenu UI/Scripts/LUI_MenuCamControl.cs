using UnityEngine;
using System.Collections;

public class LUI_MenuCamControl : MonoBehaviour {

	[Header("OBJECTS")]
	public Transform currentMount;
	public Camera camera;

	[Header("SETTINGS")]
	[Tooltip("Set 1.1 for instant fly")]
	[Range(0.01f,1.1f)]public float speed = 0.1f;
	public float zoom = 1.0f;

	private Vector3 lastPosition;

	void Start ()
	{
		lastPosition = transform.position;
	}

	void Update ()
	{
		transform.position = Vector3.Lerp(transform.position,currentMount.position,speed);
		transform.rotation = Quaternion.Slerp(transform.rotation,currentMount.rotation,speed);
		camera.fieldOfView = 60 + zoom;
		lastPosition = transform.position;
	}

	public void setMount (Transform newMount)
	{
		currentMount = newMount;
	}
}