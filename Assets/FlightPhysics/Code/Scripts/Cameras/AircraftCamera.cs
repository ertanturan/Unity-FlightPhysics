using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlightPhysics.Cameras
{

    public class AircraftCamera : BasicFollowCamera
    {
        [Header("Aircraft Properties")]
        public float MinHeight = 2f;

        public float MinDistance = 6f;
        public LayerMask Mask;

        protected override void HandleCamera()
        {
            RaycastHit hit;

            if (Physics.Raycast(transform.position,Vector3.down,
                out hit,Mask))
            {
                if (hit.distance<MinHeight)
                {
                    float targetHeight = _OriginalHeight + MinHeight - hit.distance;
                    Height = targetHeight;
                }
            }


            base.HandleCamera();
        }
    }
    

}
