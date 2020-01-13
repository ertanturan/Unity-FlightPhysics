using UnityEngine;
using System.Collections.Generic;

//using Plugins;
using MapMagic;

namespace MapMagicDemo
{

	public class FlybyController : MonoBehaviour 
	{
		public Vector3 RayCast (Vector3 start, Vector3 target)
		{
			Vector3 dir = target-start; float dist = dir.magnitude;
			RaycastHit hitData = new RaycastHit();
			if (!Physics.Raycast(start, dir, out hitData, dist)) return target;
			return hitData.point;
		}
	
		public Vector3 SphereCast (Vector3 start, Vector3 target, float radius)
		{
			Vector3 dir = target-start; float dist = dir.magnitude;
			RaycastHit hitData = new RaycastHit();
			if (!Physics.SphereCast(start, radius, dir, out hitData, dist)) return target;
			return start + hitData.distance * (dir/dist);
		}
	
		public Vector3 FindLowestPoint (Vector3 pos, float direction, float angle, float dist, int angleSteps, int distSteps, float sphereRadius)
		{
			Vector3 lowestPoint = pos + new Vector3(0,2000000,0);

			for (int a=0; a<angleSteps; a++)
			{
				float currentAngle = direction - angle/2f + (angle/angleSteps)*a + (angle/angleSteps)/2;
				//currentAngle = Mathf.Repeat(0,360);

				for (int d=0; d<distSteps; d++)
				{
					float currentDist = dist/2 + ((dist/2)/distSteps)*d;

					Vector3 point = new Vector3( Mathf.Sin(currentAngle*Mathf.Deg2Rad), 0, Mathf.Cos(currentAngle*Mathf.Deg2Rad) ) * currentDist + pos;
					point.y = pos.y + 1000;

					Vector3 fPoint = SphereCast(point, point-new Vector3(0,1000,0), sphereRadius);

					if (fPoint.y < lowestPoint.y) lowestPoint = fPoint;


				}
			}

			return lowestPoint;
		}

		public IEnumerable<Vector3> SpokeDirections2D (Vector3 direction, float fov, float steps) //from center to edge
		{
			float angle = direction.Angle();
			float stepAngle = fov/(steps-1);

			if (steps%2 == 0)
			{
				float startAngle = stepAngle/2f;
				int halfSteps = (int)(steps/2);
				for (int a=0; a<halfSteps; a++) 
				{ 
					yield return (angle + startAngle + stepAngle*a).Direction();
					yield return (angle - startAngle - stepAngle*a).Direction();
				}
			}

			else
			{
				yield return angle.Direction();

				int halfSteps = (int)((steps+1)/2);
				for (int a=0; a<halfSteps; a++) 
				{ 
					yield return (angle + stepAngle*a).Direction();
					yield return (angle - stepAngle*a).Direction();
				}
			}
		}

		public IEnumerable<Vector3> LinePositions2D (Vector3 direction, float distance, float steps) //linear
		{
			Vector3 start = -direction*(distance/2);
			float stepDist = distance / (steps-1);

			for (int i=0; i<steps; i++)
				yield return start + direction*stepDist*i;
		}

		public IEnumerable<Vector3> RectPositions2D (Vector3 direction, float distanceAlong, float stepsAlong, float distanceAcross, float stepsAcross) //linear
		{
			float stepDistAlong = distanceAlong / (stepsAlong-1);
			float stepDistAcross = distanceAcross / (stepsAcross-1);

			Vector3 perpendicular = Vector3.Cross(Vector3.up,direction);

			Vector3 startAlong = -direction*(distanceAlong/2);
			Vector3 startAcross = -perpendicular*(distanceAcross/2);

		
			for (int i=0; i<stepsAlong; i++)
			{
				Vector3 pos = startAlong + direction*stepDistAlong*i;

				for (int j=0; j<stepsAcross; j++)
					yield return pos + startAcross + perpendicular*stepDistAcross*j;
			}
		}



		public float evalDist = 300;
		public float evalFov = 60;
		public float evalRadius = 50;


		public Vector3 LowestPoint (Vector3 pos, Vector3 dir, float dist, float fov, float radius)
		{
			Vector3 lowestPoint = pos; lowestPoint.y = pos.y + 20000;
			bool lowestPointFound = false;
	
			foreach (Vector3 spokeDir in SpokeDirections2D(dir, fov, 7))
			{
				foreach (Vector3 lineDir in LinePositions2D (spokeDir, dist/4, 5))
				{
					Vector3 point = pos + spokeDir*(dist/2 + 50) + lineDir;

					//try {
					//Gizmos.color = Color.red;
					//Gizmos.DrawLine(pos, point);
					//Gizmos.DrawWireSphere(point,radius);
					//} catch (System.Exception e) {}

					Vector3 rayPoint = RayCast(point+Vector3.up*2000, point+Vector3.down*1000);
					if (rayPoint.y > lowestPoint.y-radius) continue;

					Vector3 spherePoint = SphereCast(point+Vector3.up*2000, point+Vector3.down*1000, radius);
					if (spherePoint.y > lowestPoint.y) continue;

					if (spherePoint.y < 0) continue; //un-generated terrain
					if (Physics.Linecast(pos,spherePoint)) continue; //behind the cliff
				
					lowestPoint = spherePoint;
					lowestPointFound = true;
				}
			}

			//Gizmos.color = Color.green;
			//Gizmos.DrawSphere(lowestPoint,radius);
			if (lowestPointFound) return lowestPoint;
			else return pos + dir*dist/2;
		}

		private Vector3 target;
		private float targetDist;
		private Vector3 flyDir = new Vector3(1,0,0);
		private Vector3 moveVector;
		private Vector3 rotateVelocity;

		public float speed = 100f;
		public float smooth = 10f;
		//public float rotation = 1f;

		//public Vector3 lookVector;
		//public Vector3 lookVelocity;
		//public float lookRotation;

		public Vector3 debugPos;

		void OnEnable ()
		{
			//flyDir = Camera.main.transform.forward; flyDir.y=0; flyDir = flyDir.normalized;
		
			target = LowestPoint(transform.localPosition, flyDir, evalDist, evalFov, evalRadius);
			targetDist = (target-transform.localPosition).magnitude;
		}

		void Update ()
		{
			if (!Physics.Raycast(transform.localPosition+new Vector3(0,2000,0), Vector3.down, 4000)) return; //above un-generated terrain

			float curTargetDist = (transform.localPosition-target).magnitude;
			if (curTargetDist > targetDist) //re-calculating new target
			{
				debugPos = target;
				target = LowestPoint(target, flyDir, evalDist, evalFov, evalRadius);
				curTargetDist = (target-transform.localPosition).magnitude;
			}
			targetDist = curTargetDist;

			moveVector = Vector3.SmoothDamp(moveVector, target-transform.localPosition, ref rotateVelocity, smooth); //Vector3.Lerp(moveVector, target-transform.localPosition, acceleration*Time.deltaTime);
			moveVector = moveVector.normalized;
			Vector3 pos = transform.localPosition + moveVector*Time.deltaTime*speed;

			//if it is under terrain
			Vector3 flooredPos = RayCast(pos+new Vector3(0,2000,0), pos-new Vector3(0,2000,0)) + Vector3.up*2f;
			if (pos.y < flooredPos.y) pos = flooredPos;

			transform.localPosition = pos;
		}

		void OnDrawGizmos ()
		{
			//Gizmos.color = Color.red;
			//LowestPoint(debugPos, flyDir, evalDist, evalFov, evalRadius);
		
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(target, evalRadius);
		}
	}
}