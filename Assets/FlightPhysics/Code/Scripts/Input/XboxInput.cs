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
            yaw = Input.GetAxis("XBOX_RH_Stick");
            throttle = Input.GetAxis("XBOX_RV_Stick");
            ThrottleControl();
            //Brakes
            brake = Input.GetAxis("Fire1");

            //Flaps
            if (Input.GetButtonDown("XBOX_R_Bumper"))
            {
                flaps += 1;
            }

            if (Input.GetButtonDown("XBOX_L_Bumper"))
            {
                flaps -= 1;
            }

            flaps = Mathf.Clamp(flaps, 0, MaxFlapIncrements);

            _CameraSwitch = Input.GetKeyDown("XBOX_Y_Button");
        }
     
    }
}

