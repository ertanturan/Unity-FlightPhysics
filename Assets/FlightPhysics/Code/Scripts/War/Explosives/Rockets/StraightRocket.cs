using UnityEngine;

namespace FlightPhysics.War.Explosives
{

    public class StraightRocket : Rocket
    {
        public override void Init()
        {
            base.Init();
            Rb.velocity = Vector3.zero;
            Rb.AddForce(transform.forward * 200);

        }

        void FixedUpdate()
        {
            Debug.DrawRay(transform.position, transform.forward * 20, Color.red);
            Rb.AddForce(transform.forward * 200);
        }
    }
}