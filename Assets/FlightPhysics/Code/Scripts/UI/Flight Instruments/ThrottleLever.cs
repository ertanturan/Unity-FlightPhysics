using FlightPhysics.Input;
using UnityEngine;

namespace FlightPhysics.UI
{
    public class ThrottleLever : MonoBehaviour, IAirplaneUI
    {
        [Header("Throttle Lever")]
        public BaseFlightInput Input;
        public RectTransform ParentRect;
        public RectTransform HandleRect;
        public float HandleSpeed = 2f;

        public void HandleAirplaneUI()
        {
            if (Input && HandleRect && ParentRect)
            {
                float height = ParentRect.rect.height;
                Vector2 targetPos = new Vector2(0f, height * Input.StickyThrottle);
                HandleRect.anchoredPosition = Vector2.Lerp(HandleRect.anchoredPosition,
                    targetPos, Time.deltaTime * HandleSpeed);
            }
        }
    }


}
