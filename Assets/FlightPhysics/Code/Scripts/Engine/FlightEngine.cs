using UnityEngine;

namespace FlightPhysics.Components
{
    [RequireComponent(typeof(AircraftFuel))]
    public class FlightEngine : MonoBehaviour
    {

        #region Fields

        [Header("Engine Properties")] public Engine Eng;

        [Header("Propellers")]
        public FlightPropeller Propeller;
        private bool _isShutOff = false;
        private float _lastThrottleValue;
        private float _finalShutoffThrottleValue;

        private AircraftFuel _fuel;

        #endregion

        #region Properties

        public bool ShutEngineOf
        {
            set { _isShutOff = value; }
        }

        private float _currentRPM;

        public float CurrentRPM
        {
            get { return _currentRPM; }
        }

        #endregion

        #region BuiltIn Methods

        private void Start()
        {
            if (!_fuel)
            {
                _fuel = GetComponent<AircraftFuel>();
            }

            _fuel.InitFuel();
        }

        #endregion

        #region Custom Methods

        public Vector3 CalculateForce(float throttle)
        {
            //horsepower
            float finalThrottle = Mathf.Clamp01(throttle);

            if (!_isShutOff)
            {
                finalThrottle = Eng.PowerCurve.Evaluate(finalThrottle);
                _lastThrottleValue = finalThrottle;
            }
            else
            {
                _lastThrottleValue -= Time.deltaTime * Eng.ShutOffSpeed;
                _lastThrottleValue = Mathf.Clamp01(_lastThrottleValue);
                finalThrottle = Eng.PowerCurve.Evaluate(_lastThrottleValue);
            }

            HandleFuel(finalThrottle);

            //rpm
            _currentRPM = finalThrottle * Eng.MaxRPM;
            if (Propeller != null)
            {
                Propeller.HandlePropeller(_currentRPM);
            }

            //force
            float finalPower = finalThrottle * Eng.MaxForce;

            Vector3 finalForce = transform.forward * finalPower;

            return finalForce;
        }

        public void HandleFuel(float passedThrottle)
        {
            //Handle Fuel
            if (_fuel)
            {
                _fuel.UpdateFuel(passedThrottle);
                if (_fuel.CurrentFuel == 0)
                {
                    _isShutOff = true;
                }
            }
        }

        #endregion

    }

}


