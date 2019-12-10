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

        [Header("Drag")]
        public float DragFactor = .01f; // how much drag do we add as we go faster and faster

        private float _angleOfAttack;

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
                HandleRigidbody();
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
            //calculate the angle of attack
            _angleOfAttack = Vector3.Dot(_rb.velocity.normalized, transform.forward);
            _angleOfAttack *= _angleOfAttack;

            //calculate and add lift
            Vector3 liftDirection = transform.up;
            float liftPower = LiftCurve.Evaluate(_normalizedMPH) * MaxLiftPower;
            Vector3 finalLiftForce = liftDirection * liftPower*_angleOfAttack;
            _rb.AddForce(finalLiftForce);
        }

        private void CalculateDrag()
        {

            float finalDrag = _beginningDrag + (ForwardSpeed * DragFactor);

            _rb.drag = finalDrag;
            _rb.angularDrag = _beginningAngularDrag * ForwardSpeed;
        }

        private void HandleRigidbody()
        {
            if (_rb.velocity.magnitude > 5f)
            {
                Vector3 rbVelocity = Vector3.Slerp(_rb.velocity,
                    transform.forward * ForwardSpeed,
                    ForwardSpeed * _angleOfAttack * Time.deltaTime);

                _rb.velocity = rbVelocity;


                Quaternion rbRotation = Quaternion.Slerp(_rb.rotation,
                    Quaternion.LookRotation(_rb.velocity.normalized, transform.up),
                    Time.deltaTime
                );

                _rb.MoveRotation(rbRotation);

            }
        }

        #endregion
    }

}

