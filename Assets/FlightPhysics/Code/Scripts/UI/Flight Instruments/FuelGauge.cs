using System.Collections;
using System.Collections.Generic;
using FlightPhysics.Components;
using UnityEngine;

namespace FlightPhysics.UI
{
    public class FuelGauge : MonoBehaviour, IAirplaneUI
    {
        public FlightEngine Engine;
        public RectTransform Pointer;
        private AircraftFuel _fuel;
        public float MinRotation = 40f;
        public float MaxRotation = 40f;

        private void Awake()
        {
            if (Engine)
            {
                _fuel = Engine.GetComponent<AircraftFuel>();
            }
        }

        public void HandleAirplaneUI()
        {
            if (Engine && _fuel)
            {
                float rotation = Mathf.Lerp(MinRotation, MaxRotation, _fuel.NormalizedFuel);
                Pointer.rotation = Quaternion.Euler(0f, 0f, -rotation);
            }
        }

    }

}

