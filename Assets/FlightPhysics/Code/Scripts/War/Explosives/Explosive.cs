using UnityEngine;

namespace FlightPhysics.War.Explosives
{
    public class Explosive : MonoBehaviour, IPooledObject
    {
        public float ExplosionRange = 10f;
        public float Damage = 20f;
        public ExplosionTypes ExplosionType;
        public LayerMask Mask;
        public PooledObjectType PoolType;

        public virtual void Explode()
        {

        }


        public void Init()
        {

        }

        public void OnObjectSpawn()
        {

        }

        public void OnObjectDespawn()
        {

        }

    }


}

