using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlightPhysics.Components
{
    [RequireComponent(typeof(WheelCollider))]
    public class PlaneWheel : MonoBehaviour
    {
        private WheelCollider _wheelCollider;

        #region BuiltIn Methods

        private void Start()
        {
            _wheelCollider = GetComponent<WheelCollider>();
        }

        #endregion

        #region Custom Methods

        public void Init()
        {
            if (_wheelCollider)
            {
                _wheelCollider.motorTorque = .00000000000001f;
            }
        }

        #endregion




    }


}

