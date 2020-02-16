namespace FlightPhysics.War.Explosives
{

    public class StraightRocket : Rocket
    {
        public override void Explode()
        {
            base.Explode();
        }

        public override void Fire()
        {
            base.Fire();
            Rb.AddForce(transform.forward * 100);
        }
    }
}