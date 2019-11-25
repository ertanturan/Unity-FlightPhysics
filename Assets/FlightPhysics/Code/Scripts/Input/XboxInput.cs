namespace FlightPhysics.Input
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class XboxInput : BaseFlightInput
    {
        protected override void HandleInput()
        {
            pitch = Input.GetAxis("Vertical");
            roll = Input.GetAxis("Horizontal");
            yaw = Input.GetAxis("Yaw");
            throttle = Input.GetAxis("Throttle");

            //Brakes
            brake = Input.GetAxis("Fire1");

            //Flaps
            if (Input.GetKeyDown(KeyCode.F))
            {
                flaps += 1;
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                flaps -= 1;
            }

            flaps = Mathf.Clamp(flaps, 0, MaxFlapIncrements);
        }
    }
}

