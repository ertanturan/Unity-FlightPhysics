using UnityEngine;
using System.Collections;

//using Plugins;
using MapMagic;

namespace MapMagicDemo
{
	public class CharRover : MonoBehaviour
	{
		public float speed = 20;
		public float descent = 0.25f;
		public float collisionDist = 200;
		public float collisionRadius = 10;

		public float rotationSmoothness = 0.5f;
		
		public void Update ()
		{
			Vector3 preferredDir = transform.forward;
			preferredDir.y = 0;
			preferredDir = preferredDir.normalized;
			preferredDir.y = -descent;
			preferredDir = preferredDir.normalized;
			preferredDir *= collisionDist;

			Debug.DrawLine(transform.position, transform.position+preferredDir, Color.red);

			Vector3 possibleDir = GetPossibleDirection(transform.position, collisionRadius, preferredDir, 4,4,20); 
			Vector3 moveVector = possibleDir.normalized;
			transform.position += moveVector*speed*Time.deltaTime;

			Quaternion targetRotation = Quaternion.LookRotation(possibleDir);
			transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSmoothness);

			Debug.DrawLine(transform.position, transform.position+possibleDir, Color.green);
		}
		
		/*public Transform origin;
		public Transform dir;
		public void OnDrawGizmos ()
		{
			Vector3 moveDir = dir.position-origin.position;
			Gizmos.color = Color.red; Gizmos.DrawLine(origin.position, origin.position+moveDir);
			
			moveDir = GetPossibleDirection(origin.position, 1, moveDir, 7,4,20);
			Gizmos.color = Color.green; Gizmos.DrawLine(origin.position, origin.position+moveDir);
		}*/

		public static Vector3 GetPossibleDirection (Vector3 pos, float radius, Vector3 direction, float horizontalStep=5, float verticalStep=5, int numSteps=20)
		{
			//angles in radians
			float angle = Mathf.Atan2(direction.x, direction.z);
			float elevation = Mathf.Asin(direction.y/direction.magnitude);

			//Color rayColor = Color.green;
			//float colorStep = 0.01f;

			Coord center = new Coord(0,0);
			foreach (Coord coord in center.DistanceArea(numSteps/2))
			{
				float curAngle = angle + coord.x*horizontalStep*Mathf.Deg2Rad;
				float curElevation = elevation + coord.z*verticalStep*Mathf.Deg2Rad;
				
				Vector3 curDirection = new Vector3( Mathf.Sin(curAngle), 0 , Mathf.Cos(curAngle));
				curDirection *= Mathf.Cos(curElevation);
				curDirection.y = Mathf.Sin(curElevation);
				curDirection *= direction.magnitude;
				Ray curRay = new Ray(pos, curDirection);

				//rayColor.r += colorStep;
				//rayColor.g -= colorStep;
				//Gizmos.color = rayColor;
				//Gizmos.DrawLine(pos, pos+curDirection);

				if (Physics.Raycast(curRay, direction.magnitude)) continue;
				if (Physics.CheckSphere(pos+curDirection, radius)) continue;
				if (!Physics.SphereCast(curRay, radius, direction.magnitude)) return curDirection;
				
			}

			return Vector3.zero;
		}

		/*ITERATIONAL
			Vector3 moveDir = dir.position-origin.position;
			
			moveDir = VisualizeTryMove (origin.position, 1, moveDir, 5, 18);
			Gizmos.color = Color.red; Gizmos.DrawLine(origin.position, origin.position+moveDir);

			Quaternion rot = Quaternion.RotateTowards(
				 Quaternion.LookRotation(moveDir, Vector3.up),
				 Quaternion.LookRotation(dir.position-origin.position, Vector3.up),
				 5);
			moveDir = (rot * Vector3.forward) * moveDir.magnitude;
			Gizmos.color = Color.yellow; Gizmos.DrawLine(origin.position, origin.position+moveDir);
			
			moveDir = VisualizeTryMove (origin.position, 1, moveDir, 1, 20);
			Gizmos.color = Color.green; Gizmos.DrawLine(origin.position, origin.position+moveDir);
		*/
	}
}
