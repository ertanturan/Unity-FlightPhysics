using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlightPhysics.Characteristics
{
    public class FlightCharacteristics : MonoBehaviour
    {

        #region Fields

        private Rigidbody _rb;
        private float _beginningDrag;
        private float _beginningAngularDrag;

        #endregion


        public void InitCharacteristics(Rigidbody rb)
        {
            //Initialization

            _rb = rb;
            _beginningDrag = _rb.drag;
            _beginningAngularDrag = _rb.angularDrag;
        }

        public void UpdateCharacteristics()
        {
            //process the characteristics
            if (_rb)
            {
                CalculateForwardSpeed();
                CalculateLift();
                CalculateDrag();
            }
        }

        private void CalculateForwardSpeed()
        {

        }

        private void CalculateLift()
        {

        }

        private void CalculateDrag()
        {

        }


    }

}

