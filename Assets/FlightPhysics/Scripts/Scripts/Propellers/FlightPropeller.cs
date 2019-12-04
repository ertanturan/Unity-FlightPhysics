using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlightPropeller : MonoBehaviour
{

    public void HandlePropeller(float currentRPM)
    {
        //Degrees Per Second = (RPM*360)/60
        float dps = ((currentRPM * 360) / 60) * Time.deltaTime;
        transform.Rotate(Vector3.forward,dps);
    }

}
