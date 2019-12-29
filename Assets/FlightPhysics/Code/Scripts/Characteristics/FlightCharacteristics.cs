using FlightPhysics.Input;
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

        [Header("Flight Character")]
        public FlightCharacteristic FlightCharacter;

        [Header("Characteristics")]
        public float ForwardSpeed;
        public float MPH;

        [Header("Lift")]

        public AnimationCurve LiftCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);



        private float _flapDrag;

        [Header("Controls")]
        public AnimationCurve ControlEfficiency =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);


        //
        private float _angleOfAttack;
        private float _pitchAngle;
        private float _rollAngle;
        private float _yawAngle;
        private float _controlSurfaceEfficiency;

        private BaseFlightInput _input;

        #endregion

        #region Custom Methods

        public void InitCharacteristics(Rigidbody rb, BaseFlightInput input)
        {
            //Initialization
            _input = input;
            _rb = rb;
            _beginningDrag = _rb.drag;
            _beginningAngularDrag = _rb.angularDrag;

            _maxMPS = FlightCharacter.MaxMPS / _mpsToMph;
        }

        public void UpdateCharacteristics()
        {
            //process the characteristics
            if (_rb)
            {
                CalculateForwardSpeed();
                CalculateLift();
                CalculateDrag();

                HandleSurfaceEfficiency();
                HandlePitch();
                HandleRoll();
                HandleYaw();
                HandleBanking();

                //HandleRigidbody();
            }
        }

        private void CalculateForwardSpeed() //THRUST
        {
            Vector3 localVelocity = transform.InverseTransformDirection(_rb.velocity);
            ForwardSpeed = Mathf.Max(0f, localVelocity.z);
            ForwardSpeed = Mathf.Clamp(ForwardSpeed, 0f, _maxMPS);
            MPH = ForwardSpeed * _mpsToMph;
            MPH = Mathf.Clamp(MPH, 0, FlightCharacter.MaxMPS);
            _normalizedMPH = Mathf.InverseLerp(0f, FlightCharacter.MaxMPS, MPH);
        }

        private void CalculateLift()
        {
            //calculate the angle of attack
            _angleOfAttack = Vector3.Dot(_rb.velocity.normalized, transform.forward);
            _angleOfAttack *= _angleOfAttack;
            //calculate and add lift
            Vector3 liftDirection = transform.up;
            float liftPower = LiftCurve.Evaluate(_normalizedMPH) * FlightCharacter.MaxLiftPower;

            //add flap lift
            float finalLiftPower = FlightCharacter.FlapLiftPower * _input.NormalizedFlaps;
            //final lift
            Vector3 finalLiftForce = liftDirection * (liftPower + finalLiftPower) * _angleOfAttack;
            _rb.AddForce(finalLiftForce);
        }

        private void CalculateDrag()
        {
            //flap drag
            _flapDrag = Mathf.Lerp(_flapDrag, _input.Flaps * FlightCharacter.FlapDragFactor, .02f);
            //speed drag
            float speedDrag = ForwardSpeed * FlightCharacter.DragFactor;

            //sum of all drag forces
            float finalDrag = _beginningDrag + speedDrag + _flapDrag;
            _rb.drag = finalDrag;
            _rb.angularDrag = _beginningAngularDrag * ForwardSpeed;
        }

        private void HandlePitch()
        {
            Vector3 forwardDir = transform.forward;
            forwardDir.y = 0; // flat forward
            forwardDir = forwardDir.normalized;
            _pitchAngle = Vector3.Angle(transform.forward, forwardDir);

            //even though its called torque its a force that rotates rb
            Vector3 pitchTorque = _input.Pitch * FlightCharacter.PitchSpeed * transform.right * _controlSurfaceEfficiency;

            _rb.AddTorque(pitchTorque);
        }

        private void HandleRoll()
        {
            Vector3 rightDir = transform.right;
            rightDir.y = 0;
            rightDir = rightDir.normalized;
            _rollAngle = Vector3.Angle(transform.right, rightDir);

            Vector3 rollTorque = -_input.Roll * FlightCharacter.RollSpeed * transform.forward * _controlSurfaceEfficiency;

            _rb.AddTorque(rollTorque);
        }

        private void HandleYaw()
        {
            Vector3 yawTorque = _input.Yaw * FlightCharacter.YawSpeed * transform.up * _controlSurfaceEfficiency;
            _rb.AddTorque(yawTorque);
        }

        private void HandleBanking()
        {
            float bankingSide = Mathf.InverseLerp(-90f, 90f, _rollAngle);
            float bankingAmount = Mathf.Lerp(-1f, 1f, bankingSide);

            Vector3 bankTorque = bankingAmount * FlightCharacter.BankingSpeed * transform.up;

            _rb.AddTorque(bankTorque);
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

        private void HandleSurfaceEfficiency()
        {
            _controlSurfaceEfficiency = ControlEfficiency.Evaluate(_normalizedMPH);
        }

        #endregion
    }

}

