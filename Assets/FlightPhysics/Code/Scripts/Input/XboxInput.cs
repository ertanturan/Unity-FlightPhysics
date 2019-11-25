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
            yaw = Input.GetAxis("X_RH_Stick");
            throttle = Input.GetAxis("X_RV_Stick");

            //Brakes
            brake = Input.GetAxis("Fire1");

            //Flaps
            if (Input.GetButtonDown("X_R_Bumper"))
            {
                flaps += 1;
            }

            if (Input.GetButtonDown("X_L_Bumper"))
            {
                flaps -= 1;
            }

            flaps = Mathf.Clamp(flaps, 0, MaxFlapIncrements);
        }
    }
}

