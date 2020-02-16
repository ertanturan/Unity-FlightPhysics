using UnityEngine;

namespace FlightPhysics.War.Explosives
{
    [RequireComponent(typeof(Rigidbody))]
    public class Explosive : MonoBehaviour, IPooledObject
    {
        public float ExplosionRange = 10f;
        public float Damage = 20f;
        public ExplosionTypes ExplosionType;
        public LayerMask Mask;
        public PooledObjectType PoolType;
        protected Rigidbody Rb;

        protected virtual void Awake()
        {
            Rb = GetComponent<Rigidbody>();
        }

        public virtual void Explode()
        {

        }

        public void Init()
        {
            if (!Rb)
            {
                Rb = GetComponent<Rigidbody>();
            }
        }

        public void OnObjectSpawn()
        {
            Rb.AddForce(transform.forward * 10);
        }

        public void OnObjectDespawn()
        {

        }

    }


}

