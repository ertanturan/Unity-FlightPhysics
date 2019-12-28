using FlightPhysics.Input;
using UnityEngine;

namespace FlightPhysics.UI
{
    public class FlapLever : MonoBehaviour, IAirplaneUI
    {
        [Header("Flap Lever Properties")]
        public BaseFlightInput Input;

        public RectTransform ParentRect;
        public RectTransform HandleRect;

        public float HandleSpeed = 2f;

        public void HandleAirplaneUI()
        {
            if (Input && HandleRect && ParentRect)
            {
                float height = ParentRect.rect.height;
                Vector2 targetPos = new Vector2(0f, -height * Input.NormalizedFlaps);
                HandleRect.anchoredPosition = Vector2.Lerp(HandleRect.anchoredPosition,
                    targetPos, Time.deltaTime * HandleSpeed);
            }
        }
    }


}
