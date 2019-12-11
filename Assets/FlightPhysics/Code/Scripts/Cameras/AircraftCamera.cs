using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlightPhysics.Cameras
{

    public class AircraftCamera : BasicFollowCamera
    {

        #region Fields

        [Header("Aircraft Properties")]
        public float MinHeight = 2f;

        public float MinDistance = 6f;
        public LayerMask Mask;

        #endregion

        #region Custom Methods

        protected override void HandleCamera()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down,
                out hit, Height * 2, Mask))
            {

                if (hit.distance < MinHeight)
                {
                    float targetHeight = _OriginalHeight + MinHeight - hit.distance;
                    Height = targetHeight;
                }
            }


            base.HandleCamera();
        }

        #endregion

    }


}
