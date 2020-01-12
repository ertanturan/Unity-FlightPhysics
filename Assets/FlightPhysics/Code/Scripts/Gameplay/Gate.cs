using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FlightPhysics.Gameplay
{

    public class Gate : MonoBehaviour
    {
        [Header("Gate Properties")]
        public bool ReverseDirection = false;
        public bool IsActive = false;

        [Header("UI Properties")]
        public Image ArrowImage;

        [Header("Gate Events")]
        public UnityEvent OnGateCleared = new UnityEvent();
        public UnityEvent OnGateFailed = new UnityEvent();

        private Vector3 _gateDirection;

        private bool _isCleared = false;

        private void Start()
        {
            _gateDirection = transform.forward;

            if (ReverseDirection)
            {
                _gateDirection = -_gateDirection;
            }



        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Player") && !_isCleared)
            {
                CheckDirection(other.transform.forward);
            }
        }

        public void ActivateGate()
        {
            IsActive = true;
            _isCleared = false;
            if (ArrowImage)
            {
                ArrowImage.enabled = true;
            }

        }

        public void DeActivateGate()
        {
            IsActive = false;
            _isCleared = true;

            if (ArrowImage)
            {
                ArrowImage.enabled = false;
            }
        }

        public void CheckDirection(Vector3 dir)
        {
            float dotValue = Vector3.Dot(_gateDirection, dir);

            if (dotValue > .25f)
            {
                //player cleared gate
                _isCleared = true;
                if (OnGateCleared != null)
                {
                    OnGateCleared.Invoke();
                }

                DeActivateGate();
            }
            else
            {
                //player failed gate
                if (OnGateFailed != null)
                {
                    OnGateFailed.Invoke();
                }
            }

        }


    }

}
