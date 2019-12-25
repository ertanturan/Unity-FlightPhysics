using System.Collections;
using System.Collections.Generic;
using FlightPhysics.Components;
using UnityEngine;

namespace FlightPhysics.UI
{

    public class Tachometer : MonoBehaviour, IAirplaneUI
    {
        [Header("Tachometer")]
        public RectTransform Pointer;
        public FlightEngine Engine;

        private float _tachometerMaxRPM = 3500f;

        public void HandleAirplaneUI()
        {
            if (Engine && Pointer)
            {
                float normalizedRPM = Mathf.InverseLerp(0f, _tachometerMaxRPM, Engine.CurrentRPM / 5);

                float pointerRotation = normalizedRPM * 240;

                Pointer.rotation = Quaternion.Euler(0f, 0f, -pointerRotation);
            }
        }
    }

}

