using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace FlightPhysics.GroundEffect
{
    [RequireComponent(typeof(Rigidbody))]
    public class GroundEffect : MonoBehaviour
    {
        public LayerMask GroundMask;
        public float MaxDistanceToGround = 3f;
        public float LiftForce = 10f;
        public float MaxSpeed = 15f;

        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (_rb)
            {
                HandleGroundEffect();
            }
        }

        protected virtual void HandleGroundEffect()
        {
            RaycastHit hit;

            if (Physics.Raycast(transform.position,
                Vector3.down, out hit, MaxDistanceToGround * 5, GroundMask))
            {
                if (hit.distance < MaxDistanceToGround)
                {
                    float velocity = _rb.velocity.magnitude;

                    float normalizedVelocity = velocity / MaxSpeed;

                    normalizedVelocity = Mathf.Clamp01(normalizedVelocity);


                    float distance = MaxDistanceToGround - hit.distance;
                    float finalForce = LiftForce * distance * normalizedVelocity;
                    _rb.AddTorque(Vector3.up * finalForce);
                }

            }

        }

    }

}

