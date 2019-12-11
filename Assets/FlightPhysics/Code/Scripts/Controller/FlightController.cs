using FlightPhysics.Input;
using System.Collections;
using System.Collections.Generic;
using FlightPhysics.Components;
using UnityEngine;
using FlightPhysics.Characteristics;
using FlightPhysics.ControlSurfaces;

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
        private const float _poundToKilosCOEF = 0.453592f;
        public float Weight = 800f;

        [Header("Engines")]
        public List<FlightEngine> Engines = new List<FlightEngine>();

        [Header("Wheels")]
        public List<PlaneWheel> Wheels = new List<PlaneWheel>();

        [Header("Control Surfaces")]
        public List<ControlSurface> ControlSurfaces = new List<ControlSurface>();

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
                _rb.mass = Weight * _poundToKilosCOEF;
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
                HandleControlSurfaces();
                HandleWheels();
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

        private void HandleWheels()
        {
            if (Wheels.Count>0)
            {
                foreach (PlaneWheel wh in Wheels)
                {
                    wh.HandleWheel(Input);
                }
            }
        }

        private void HandleAltitude()
        {

        }

        private void HandleControlSurfaces()
        {
            if (ControlSurfaces.Count>0)
            {
                foreach (ControlSurface cs in ControlSurfaces)
                {
                    cs.HandleControlSurface(Input);
                }
            }
        }

        #endregion

      }

}

