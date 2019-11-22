namespace FlightPhysics.Input
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class BaseInput : MonoBehaviour
    {
        #region Fields
        protected float pitch = 0f;
        protected float roll = 0f;
        protected float yaw = 0f;
        protected float throttle = 0f;
        protected int flaps = 0;
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
            pitch = Input.GetAxis("Vertical");
            roll = Input.GetAxis("Horizontal");

            Debug.Log("Pitch : "+pitch+" - "+"Roll : "+roll);
        }
        #endregion

    }
}

