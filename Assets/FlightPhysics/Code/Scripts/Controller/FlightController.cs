using FlightPhysics.Characteristics;
using FlightPhysics.Components;
using FlightPhysics.ControlSurfaces;
using FlightPhysics.Input;
using System.Collections.Generic;
using UnityEngine;

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
        public float Weight = 800f;

        [Header("Engines")]
        public List<FlightEngine> Engines = new List<FlightEngine>();

        [Header("Wheels")]
        public List<PlaneWheel> Wheels = new List<PlaneWheel>();

        [Header("Control Surfaces")]
        public List<ControlSurface> ControlSurfaces = new List<ControlSurface>();

        [Header("Ground Check")]
        [SerializeField]
        private LayerMask _mask;

        [Header("State")]
        public PlaneState State = PlaneState.GROUNDED;

        [SerializeField]
        private bool _isGrounded = true;
        [SerializeField]
        private bool _isLanded = true;
        [SerializeField]
        private bool _isFlying = false;
        [SerializeField]
        private bool _isCrashed = false;

        #endregion

        #region  Properties

        private float _currentMSL;

        public float CurrentMSL
        {
            get { return _currentMSL; }
        }

        private float _currentAGL;

        public float CurrentAGL
        {
            get { return _currentAGL; }
        }
        #endregion

        #region Constants
        private const float _poundToKilosCOEF = 0.453592f;
        private const float _metersToFeet = 3.28084f;
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
                    _characteristics.InitCharacteristics(_rb, Input);
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

            InvokeRepeating("CheckGrounded", 1f, 1f);
            State = PlaneState.GROUNDED;
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
            if (Wheels.Count > 0)
            {
                foreach (PlaneWheel wh in Wheels)
                {
                    wh.HandleWheel(Input);
                    WheelCollider temp = wh.GetComponent<WheelCollider>();
                    temp.ConfigureVehicleSubsteps(1, 12, 15);
                }
            }
        }

        private void HandleAltitude()
        {
            _currentMSL = transform.position.y * _metersToFeet;
            RaycastHit hit;
            if (Physics.Raycast(transform.position,
                Vector3.down, out hit, 1000 * _metersToFeet, _mask))
            {
                _currentAGL = (transform.position.y - hit.point.y) * _metersToFeet;
            }

        }

        private void HandleControlSurfaces()
        {
            if (ControlSurfaces.Count > 0)
            {
                foreach (ControlSurface cs in ControlSurfaces)
                {
                    cs.HandleControlSurface(Input);
                }
            }
        }

        private void CheckGrounded()
        {
            if (Wheels.Count > 0)
            {
                int groundedCount = 0;

                for (int i = 0; i < Wheels.Count; i++)
                {
                    if (Wheels[i].IsGrounded)
                    {
                        groundedCount++;
                    }
                }

                _isGrounded = groundedCount == 1 || groundedCount == 2
                    || groundedCount == 3 ? true : false;

                if (_isGrounded)
                {
                    State = PlaneState.GROUNDED;
                    _isFlying = false;
                    if (_rb.velocity.magnitude > 1f && _rb.velocity.magnitude < 5)
                    {
                        _isLanded = true;
                        State = PlaneState.LANDED;
                    }
                    else
                    {
                        _isLanded = false;
                    }
                }
                else
                {
                    _isLanded = false;
                    _isGrounded = false;
                    _isFlying = true;
                    State = PlaneState.FLYING;
                }



            }
        }

        #endregion

    }

}

