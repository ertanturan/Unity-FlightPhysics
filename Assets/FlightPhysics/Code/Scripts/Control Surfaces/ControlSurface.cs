using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FlightPhysics.Input;

namespace FlightPhysics.ControlSurfaces
{

    public class ControlSurface : MonoBehaviour
    {

        [Header("Properties")]
        public ControlSurfaceType Type;

        public Transform TargetObj;

        public float MaxAngle = 30f;
        public float SmoothSpeed;
        private float _expectedAngle;

        public Vector3 _axis = Vector3.right;

        private void Update()
        {
            if (TargetObj)
            {
                Vector3 finalAngle = _axis * _expectedAngle;

                TargetObj.localRotation = Quaternion.Slerp(
                    TargetObj.localRotation, Quaternion.Euler(finalAngle),
                    Time.deltaTime * SmoothSpeed);
            }
        }

        public void HandleControlSurface(BaseFlightInput input)
        {
            float inputValue = 0f;

            switch (Type)
            {

                case ControlSurfaceType.Aileron:
                    inputValue = input.Roll;
                    break;
                case ControlSurfaceType.Elevator:
                    inputValue = input.Pitch;
                    break;
                case ControlSurfaceType.Flap:
                    inputValue = -input.Flaps;
                    break;
                case ControlSurfaceType.Rudder:
                    inputValue = input.Yaw;
                    break;

            }

            _expectedAngle = MaxAngle * inputValue;
        }
    }

}

