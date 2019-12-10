using FlightPhysics.Input;
using System.Collections;
using System.Collections.Generic;
using FlightPhysics.Components;
using UnityEngine;
using FlightPhysics.Characteristics;

namespace FlightPhysics
{
    [RequireComponent(typeof(FlightCharacteristics))]
    public class FlightController : BaseRigidbodyController
    {

        #region Fields
        [Header("New Flight Properties")]
        public BaseFlightInput Input;

        private FlightCharacteristics _characteristics;
        public Transform CenterOfGravity;

        [Tooltip("Weight is in pounds...")]
        private const float PoundToKilosCOEF = 0.453592f;
        public float _weight = 800f;

        [Header("Engines")]
        public List<FlightEngine> Engines = new List<FlightEngine>();

        [Header("Wheels")]
        public List<PlaneWheel> Wheels = new List<PlaneWheel>();

        #endregion

        #region BuiltIn Methods

        private void Awake()
        {
            _characteristics = GetComponent<FlightCharacteristics>();
        }

        protected override void Start()
        {
            base.Start();

            if (Wheels != null && Wheels.Count > 0)
            {
                for (int i = 0; i < Wheels.Count; i++)
                {
                    Wheels[i].Init();
                }
            }

            if (_rb)
            {
                _rb.mass = _weight * PoundToKilosCOEF;
                _rb.centerOfMass = CenterOfGravity.localPosition;

                if (_characteristics)
                {
                    _characteristics.InitCharacteristics(_rb,Input);
                }
                else
                {
                    Debug.Log("No _characteristics found. Errors may occur !.");
                }
            }
            else
            {
                Debug.LogWarning("No rigidbody detected. Errors may occur !.");
            }

        }

        #endregion

        #region Custom Methods

        protected override void HandlePhysics()
        {
            base.HandlePhysics();
            if (Input)
            {
                base.HandlePhysics();
                HandleEngines();
                HandleAerodynamics();
                HandleSteering();
                HandleBrakes();
                HandleAltitude();
            }
            else
            {
                Debug.LogWarning("No input seemed to be linked !." +
                                 " Physics won't be handled ");
            }

        }

        private void HandleEngines()
        {
            if (Engines != null && Engines.Count > 0)
            {
                for (int i = 0; i < Engines.Count; i++)
                {
                    _rb.AddForce(Engines[i].CalculateForce(Input.StickyThrottle));
                }
            }
        }

        private void HandleAerodynamics()
        {
            if (_characteristics)
            {
                _characteristics.UpdateCharacteristics();
            }
        }

        private void HandleSteering()
        {

        }

        private void HandleBrakes()
        {

        }

        private void HandleAltitude()
        {

        }
        #endregion

      }

}

