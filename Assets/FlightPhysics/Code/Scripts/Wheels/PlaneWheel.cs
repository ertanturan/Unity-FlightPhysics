using FlightPhysics.Input;
using UnityEngine;

namespace FlightPhysics.Components
{
    [RequireComponent(typeof(WheelCollider))]
    public class PlaneWheel : MonoBehaviour
    {
        #region Fields

        [Header("Properties")]
        private WheelCollider _wheelCollider;
        private Vector3 _worldPos;
        private Quaternion _worldRot;
        public Transform TargetObj;

        [Header("Braking and Steering")]
        public bool IsBreaking = false;
        public float BrakePower = 5f;
        public bool IsSteering = false;
        public float SteeringAngle;
        private float _smoothSteerAngle = 2f;
        public float SteerSmoothSpeed;

        #endregion

        #region Properties
        public bool IsGrounded { get { return _wheelCollider.isGrounded; } }

        #endregion

        #region BuiltIn Methods

        private void Start()
        {
            _wheelCollider = GetComponent<WheelCollider>();
        }

        #endregion

        #region Custom Methods

        public void Init()
        {
            if (_wheelCollider)
            {
                _wheelCollider.motorTorque = .00000000000001f;
            }
        }

        public void HandleWheel(BaseFlightInput input)
        {
            if (_wheelCollider)
            {
                _wheelCollider.GetWorldPose(out _worldPos, out _worldRot);
                if (TargetObj)
                {
                    TargetObj.rotation = _worldRot;
                    TargetObj.position = _worldPos;
                }

                if (IsBreaking)
                {
                    if (input.Brake == 1)
                    {
                        //brake
                        _wheelCollider.brakeTorque = input.Brake * BrakePower;
                    }
                    else
                    {
                        _wheelCollider.brakeTorque = 0f;
                        _wheelCollider.motorTorque = .00000000000001f;
                    }
                }

                if (IsSteering)
                {
                    _smoothSteerAngle = Mathf.Lerp(_smoothSteerAngle,
                        input.Yaw * SteeringAngle, Time.deltaTime * SteerSmoothSpeed);
                    _wheelCollider.steerAngle = _smoothSteerAngle;
                }

                //check if wheel grounded


            }
        }

        #endregion
    }
}

