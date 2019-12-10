using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlightPhysics.Characteristics
{
    public class FlightCharacteristics : MonoBehaviour
    {

        #region Constants

        private const float _mpsToMph = 2.23694f;

        #endregion

        #region Fields

        private Rigidbody _rb;
        private float _beginningDrag;
        private float _beginningAngularDrag;

        [Header("Characteristics")]
        public float ForwardSpeed;
        public float MPH;

        [Header("Lift")]
        public float MaxLiftPower = 800f;


        #endregion

        #region Custom Methods

        public void InitCharacteristics(Rigidbody rb)
        {
            //Initialization

            _rb = rb;
            _beginningDrag = _rb.drag;
            _beginningAngularDrag = _rb.angularDrag;
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
            ForwardSpeed = localVelocity.z;
            MPH = ForwardSpeed * _mpsToMph;
            Debug.DrawRay(transform.position,
                transform.position + localVelocity, Color.blue);


        }

        private void CalculateLift()
        {
            Vector3 liftDirection = transform.up;
            float liftPower = ForwardSpeed * MaxLiftPower;

            Vector3 finalLiftForce = liftDirection * liftPower;
            _rb.AddForce(finalLiftForce);
        }

        private void CalculateDrag()
        {

        }

        #endregion
    }

}

