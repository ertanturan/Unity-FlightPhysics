namespace FlightPhysics.Input
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class BaseFlightInput : MonoBehaviour
    {
        public KeyCode BrakeKey = KeyCode.Space;

        #region Fields
        protected float pitch = 0f;
        protected float roll = 0f;
        protected float yaw = 0f;
        protected float throttle = 0f;
        protected int flaps = 0;
        public int MaxFlapIncrements=2;
        protected float brake = 0f;
        #endregion

        #region Properties
        public float Pitch { get { return pitch; } }
        public float Roll { get { return roll; } }
        public float Yaw { get { return yaw; } }
        public float Throttle { get { return throttle; } }
        public int Flaps { get { return flaps; } }
        public float Brake { get { return brake; } }
        #endregion

        #region BuiltIn Methods

        private void Start()
        {

        }

        public virtual void Update()
        {
            HandleInput();
        }

        #endregion

        #region Custom Methods
        public virtual void HandleInput()
        {
            //Main controls
            pitch = Input.GetAxis("Vertical");
            roll = Input.GetAxis("Horizontal");
            yaw = Input.GetAxis("Yaw");
            throttle = Input.GetAxis("Throttle");

            //Brakes
            brake = Input.GetKey(BrakeKey) ? 1f : 0f;

            //Flaps
            if (Input.GetKeyDown(KeyCode.F))
            {
                flaps += 1;
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                flaps -= 1;
            }

            flaps = Mathf.Clamp(flaps, 0, MaxFlapIncrements);
        }
        #endregion

    }
}

