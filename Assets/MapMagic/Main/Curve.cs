using UnityEngine;
using System.Collections;

namespace MapMagic
{
	public class Curve 
	{
		public struct Point
		{
			public float time;
			public float val;
			public float inTangent;
			public float outTangent;

			public Point (Keyframe key) { time=key.time; val=key.value; inTangent=key.inTangent; outTangent=key.outTangent; }
		}

		public Point[] points;


		public Curve (AnimationCurve animCurve)
		{
			points = new Point[animCurve.keys.Length];
			for (int p=0; p<points.Length; p++) points[p] = new Point(animCurve.keys[p]);
		}

		public float Evaluate (float time)
		{
			if (time <= points[0].time) return points[0].val;
			if (time >= points[points.Length-1].time) return points[points.Length-1].val;

			for (int p=0; p<points.Length-1; p++)
			{
				if (time > points[p].time && time <= points[p+1].time)
				{
					Point prev = points[p];
					Point next = points[p+1];

					float delta = next.time - prev.time;
					float relativeTime = (time - prev.time) / delta;

					float timeSq = relativeTime * relativeTime;
					float timeCu = timeSq * relativeTime;
     
					float a = 2*timeCu - 3*timeSq + 1;
					float b = timeCu - 2*timeSq + relativeTime;
					float c = timeCu - timeSq;
					float d = -2*timeCu + 3*timeSq;

					return a*prev.val + b*prev.outTangent*delta + c*next.inTangent*delta + d*next.val;
				}
				else continue;
			}

			return 0;
		}
	}
}