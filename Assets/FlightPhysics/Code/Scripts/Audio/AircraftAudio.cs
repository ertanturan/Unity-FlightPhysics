using FlightPhysics.Components;
using FlightPhysics.Input;
using UnityEngine;

namespace FlightPhysics.Audio
{
    public class AircraftAudio : MonoBehaviour
    {
        [Header("Audio Properties")]

        public FlightEngine Engine;

        public BaseFlightInput Input;

        [Header("Audio Sources")]
        public AudioSource Idle;

        public AudioSource FullThrottle;

        [Header("Pitch")] public float MaxPitch = 1.2f;
        private float _defaultPitch;
        private float _finalPitch;


        private float _finalVolume;

        private void Start()
        {
            if (FullThrottle)
            {
                FullThrottle.volume = 0f;
            }
        }

        private void Update()
        {
            if (Engine)
            {
                HandleAudio();
            }
        }

        protected virtual void HandleAudio()
        {

            float normalizedRPM = Mathf.InverseLerp(0f, Engine.Eng.MaxRPM, Engine.CurrentRPM);
            _finalVolume = Mathf.Lerp(0f, 1f, normalizedRPM * 1.5f);
            _finalPitch = Mathf.Lerp(1f, MaxPitch, normalizedRPM);

            if (FullThrottle && Idle)
            {
                FullThrottle.volume = _finalVolume;
                FullThrottle.pitch = _finalPitch;

                Idle.volume = 1 - (Input.StickyThrottle * 1.5f);
            }

        }
    }

}

