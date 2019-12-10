using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlightPhysics.Characteristics
{
    public class FlightCharacteristics : MonoBehaviour
    {
        //MPS : Meter Per Second
        //MPH : Miles Per Hour


        #region Constants

        private const float _mpsToMph = 2.23694f;

        #endregion

        #region Fields

        private Rigidbody _rb;
        private float _beginningDrag;
        private float _beginningAngularDrag;
        private float _maxMPS;
        private float _normalizedMPH;

        [Header("Characteristics")]
        public float ForwardSpeed;
        public float MPH;
        public float MaxMPS = 200f; //200 mps is for f4u corsair only (718 km/h )

        [Header("Lift")]
        public float MaxLiftPower = 800f;
        public AnimationCurve LiftCurve = 
            AnimationCurve.EaseInOut(0f,0f,1f,1f);

        #endregion

        #region Custom Methods

        public void InitCharacteristics(Rigidbody rb)
        {
            //Initialization

            _rb = rb;
            _beginningDrag = _rb.drag;
            _beginningAngularDrag = _rb.angularDrag;

            _maxMPS = MaxMPS / _mpsToMph;
        }

        public void UpdateCharacteristics()
        {
            //process the characteristics
            if (_rb)
            {
                CalculateForwardSpeed();
                CalculateLift();
                CalculateDrag();
            }
        }

        private void CalculateForwardSpeed()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(_rb.velocity);
            ForwardSpeed = Mathf.Max(0f, localVelocity.z) ;
            ForwardSpeed = Mathf.Clamp(ForwardSpeed, 0f, _maxMPS);
            MPH = ForwardSpeed * _mpsToMph;
            MPH = Mathf.Clamp(MPH, 0, MaxMPS);

            _normalizedMPH = Mathf.InverseLerp(0f, MaxMPS, MPH);
        }

        private void CalculateLift()
        {
            Vector3 liftDirection = transform.up;
            float liftPower = LiftCurve.Evaluate(_normalizedMPH) * MaxLiftPower;
            Vector3 finalLiftForce = liftDirection * liftPower;
            _rb.AddForce(finalLiftForce);
        }

        private void CalculateDrag()
        {

        }

        #endregion
    }

}

