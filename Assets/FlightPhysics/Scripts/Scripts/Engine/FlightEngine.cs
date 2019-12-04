using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlightPhysics.Components
{
    public class FlightEngine : MonoBehaviour
    {

        #region Variables

        public float MaxForce = 200f;
        public float MaxRPM = 2550f;


        #endregion

        #region Custom Methods

        public void Init()
        {

        }

        public Vector3 CalculateForce(float throttle )
        {

            float finalThrottle = Mathf.Clamp01(throttle);
            float finalPower = finalThrottle * MaxForce;

            Vector3 finalForce = transform.forward * finalPower;

            return finalForce;
        }

        #endregion



    }

}


