using UnityEngine;


namespace FlightPhysics.UI
{
    public class Attitude : MonoBehaviour, IAirplaneUI
    {
        [Header("Attitude Properties")]

        public FlightController Controller;

        public RectTransform BackgroundRect;
        public RectTransform ArrowRect;

        public void HandleAirplaneUI()
        {
            if (Controller)
            {
                //create angles

                float bankAngle = Vector3.Dot(Controller.transform.right, Vector3.up) * Mathf.Rad2Deg;
                float pitchAngle = Vector3.Dot(Controller.transform.forward, Vector3.up) * Mathf.Rad2Deg;


                if (BackgroundRect)
                {
                    Quaternion bankRotation = Quaternion.Euler(0f, 0f, bankAngle);
                    BackgroundRect.transform.rotation = bankRotation;

                    Vector3 targetPosition = new Vector3(0f, pitchAngle, 0f);
                    BackgroundRect.anchoredPosition = targetPosition;


                    if (ArrowRect)
                    {
                        ArrowRect.transform.rotation = bankRotation;
                    }
                }


            }

        }

    }

}
