using FlightPhysics.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlightPhysics
{

    public class FlightController : BaseRigidbodyController
    {
        #region Variables
        [Header("New Flight Properties")]
        public BaseFlightInput Input;
        public Transform CenterOfGravity;

        [Tooltip("Weight is in pounds...")]
        private const float PoundToKilosCOEF = 0.453592f;
        public float _weight = 800f;

        [Header("Engines")]
        public List<FlightEngine> Engines = new List<FlightEngine>();

        [Header("Wheels")]
        public List<PlaneWheel> Wheels = new List<PlaneWheel>();

        #endregion

        #region Custom Methods

        protected override void Start()
        {
            base.Start();
            _rb.mass = _weight * PoundToKilosCOEF;
            _rb.centerOfMass = CenterOfGravity.localPosition;

            if (Wheels != null && Wheels.Count > 0)
            {
                for (int i = 0; i < Wheels.Count; i++)
                {

                }
            }
        }

        protected override void HandlePhysics()
        {
            base.HandlePhysics();
            HandleEngines();
            HandleAerodynamics();
            HandleSteering();
            HandleBrakes();
            HandleAltitude();
        }

        private void HandleEngines()
        {
            if (Engines != null && Engines.Count > 0)
            {
                for (int i = 0; i < Engines.Count; i++)
                {

                }
            }
        }

        private void HandleAerodynamics()
        {

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

