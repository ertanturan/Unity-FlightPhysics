using UnityEngine;

namespace FlightPhysics.UI
{

    public class Altimeter : MonoBehaviour, IAirplaneUI
    {
        [Header("Altimeter Properties")]
        public FlightController Controller;
        public RectTransform HunderdsPointer;
        public RectTransform ThousandsPointer;

        public void HandleAirplaneUI()
        {
            if (Controller)
            {
                float currentAlt = Controller.CurrentMSL;
                float currentThousands = currentAlt / 1000f;
                currentThousands = Mathf.Clamp(currentThousands, 0f, 10f);

                float currentHundreds = currentAlt - (Mathf.Floor(currentThousands));
                currentHundreds = Mathf.Clamp(currentHundreds, 0f, 1000);


                if (ThousandsPointer)
                {
                    float normalizedThousands = Mathf.InverseLerp(0f, 10f, currentThousands);
                    float thousandsRotation = normalizedThousands * 360;

                    ThousandsPointer.rotation = Quaternion.Euler(0f, 0f, -thousandsRotation);
                }

                if (HunderdsPointer)
                {
                    float normalizedHunderds = Mathf.InverseLerp(0f, 1000, currentHundreds);
                    float hundredsRotation = 360f * normalizedHunderds;

                    HunderdsPointer.rotation = Quaternion.Euler(0f, 0f, -hundredsRotation);
                }

            }
        }

    }

}
