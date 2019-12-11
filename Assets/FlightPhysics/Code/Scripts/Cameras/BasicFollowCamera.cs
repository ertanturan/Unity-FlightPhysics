using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlightPhysics.Cameras
{
    public class BasicFollowCamera : MonoBehaviour
    {

        [Header("Follow Camera Properties")]
        public Transform Target;
        [Space]
        public float Distance = 10f;
        public float Height = 10f;
        public float CameraSmoothnes = .25f;
        private Vector3 _currentVelocityRef;


        #region BuiltIn Methods

        protected float _OriginalHeight;

        protected virtual void Awake()
        {
            _OriginalHeight = Height;
        }

        private void FixedUpdate()
        {
            if (Target)
            {
                HandleCamera();
            }
        }

        #endregion

        #region Custom Methods

        protected virtual void HandleCamera()
        {
            //follow target

            Vector3 expectedPosition = Target.position + (-Target.forward * Distance)
               + (Vector3.up * Height);

            transform.position = Vector3.SmoothDamp(transform.position, 
                expectedPosition,ref _currentVelocityRef,CameraSmoothnes
                );

            transform.LookAt(Target);
        }

        protected virtual void HandleCameraCinematic()
        {

            // ???
            Vector3 expectedPosition = Target.position + (-Target.forward * Distance)
                                                       + (Vector3.up * Height);
            transform.position = expectedPosition;

            transform.LookAt(Target);
        }

        #endregion

    }

}


