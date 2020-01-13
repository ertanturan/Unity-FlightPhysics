using UnityEngine;
using System.Collections;

//using Plugins;
using MapMagic;

namespace MapMagicDemo
{

	public class CameraController : MonoBehaviour 
	{
		public Camera cam;
		public Transform hero;
		
		public bool movable;
		public float velocity = 4;
		public float follow = 0.1f;
		
		private Vector3 pivot = new Vector3(0,0,0);
		
		public int rotateMouseButton = 0;
		public bool lockCursor = false; //no mouse 1 reqired
		public float elevation = 1.5f;
		public float sensitivity = 1f;

		public float rotationX = 0;
		public float rotationY = 190;

		private Vector3 oldPos;
	
	
		public void Start ()
		{
			if (cam==null) cam = Camera.main;
			//if (hero==null) hero = ((CharController)FindObjectOfType(typeof(CharController))).transform;
			pivot = cam.transform.position;
		}
		
		public void LateUpdate () //updating after hero is moved and all other scene changes made
		{
			//locking cursor
			if (lockCursor)
			{
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			}
			else
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
			
			//reading controls
			if (Input.GetMouseButton(rotateMouseButton) || lockCursor)
			{
				rotationY += Input.GetAxis("Mouse X")*sensitivity; //note that axises from screen-space to world-space are swept!
				rotationX -= Input.GetAxis("Mouse Y")*sensitivity;
				rotationX = Mathf.Min(rotationX, 89.9f);
			}
			
			//setting cam
			if (hero!=null) pivot = hero.position + new Vector3(0, elevation, 0);
			
			//moving
			if (movable)
			{
				if (Input.GetKey (KeyCode.W)) pivot += transform.forward * velocity * Time.deltaTime;
				if (Input.GetKey (KeyCode.S)) pivot -= transform.forward * velocity * Time.deltaTime;
				if (Input.GetKey (KeyCode.D)) pivot += transform.right * velocity * Time.deltaTime;
				if (Input.GetKey (KeyCode.A)) pivot -= transform.right * velocity * Time.deltaTime;
			}

			//following move dir
			if (follow > 0.000001f)
			{
				Vector3 moveVector = cam.transform.position - oldPos;
				float moveRotationY = moveVector.Angle();
				float delta = Mathf.DeltaAngle(rotationY, moveRotationY);
				
				if (Mathf.Abs(delta) > follow*Time.deltaTime) rotationY += (delta>0? 1 : -1) * follow * Time.deltaTime;
				else rotationY = moveRotationY;
			}
			oldPos = cam.transform.position;
			
			cam.transform.localEulerAngles = new Vector3(rotationX, rotationY, 0); //note that this is never smoothed
			cam.transform.position = pivot;
		}
	}

}
