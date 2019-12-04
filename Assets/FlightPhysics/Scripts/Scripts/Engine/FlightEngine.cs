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
        public AnimationCurve PowerCurve = AnimationCurve.Linear(0f,
            0f,1f,1f);


        [Header("Propellers")]
        public FlightPropeller Propeller;

        #endregion

        #region Custom Methods

        public Vector3 CalculateForce(float throttle )
        {
            //horsepower
            float finalThrottle = Mathf.Clamp01(throttle);
            finalThrottle = PowerCurve.Evaluate(finalThrottle);

            //rpm
            float currentRPM = finalThrottle * MaxRPM;
            if (Propeller!=null)
            {
                Propeller.HandlePropeller(currentRPM);
            }

            //force
            float finalPower = finalThrottle * MaxForce;

            Vector3 finalForce = transform.forward * finalPower;

            return finalForce;
        }

        #endregion

    }

}


