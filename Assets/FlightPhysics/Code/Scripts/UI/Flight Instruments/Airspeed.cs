using FlightPhysics.Characteristics;
using UnityEngine;


namespace FlightPhysics.UI
{
    public class Airspeed : MonoBehaviour, IAirplaneUI
    {
        [Header("Airspeed Properties")]

        public FlightCharacteristics Characteristics;
        public RectTransform Pointer;

        public float MaxKnots = 160f;

        public const float MphToKnots = 0.868976f;


        public void HandleAirplaneUI()
        {
            if (Pointer && Characteristics)
            {
                float currentKnots = Characteristics.MPH * MphToKnots;

                float normalizedKnots = Mathf.InverseLerp(0f, MaxKnots, currentKnots);
                float targetRotation = 360f * normalizedKnots;
                Pointer.rotation = Quaternion.Euler(0f, 0f, -targetRotation);

            }
        }
    }


}
