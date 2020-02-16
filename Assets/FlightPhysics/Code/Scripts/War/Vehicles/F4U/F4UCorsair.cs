using UnityEngine;

namespace FlightPhysics.War.Vehicles.F4UCorsair
{
    public class F4UCorsair : MonoBehaviour
    {
        public Transform BombSpawn;
        public Transform[] RocketSpawns;

        public KeyCode FireRocketKey = KeyCode.F1;
        public KeyCode FireBombKey = KeyCode.F2;
        private int _rocketFireIndex = 0;

        private void Update()
        {

            if (UnityEngine.Input.GetKeyDown(FireRocketKey))
            {
                //fire rocket
                FireRocket();
            }

            if (UnityEngine.Input.GetKeyDown(FireBombKey))
            {
                //fire bomb
                FireBomb();
            }

        }

        private void FireRocket()
        {
            Debug.Log("Fire rocket");
            if (_rocketFireIndex % 2 == 0)
            {
                ObjectPooler.Instance.SpawnFromPool(PooledObjectType.F4URocket,
                    RocketSpawns[0].position, Quaternion.identity);
            }
            else
            {
                ObjectPooler.Instance.SpawnFromPool(PooledObjectType.F4URocket,
                    RocketSpawns[1].position, Quaternion.identity);
            }

            _rocketFireIndex++;

        }

        private void FireBomb()
        {
            Debug.Log("Fire bomb");
            ObjectPooler.Instance.SpawnFromPool(PooledObjectType.F4UBomb,
                BombSpawn.position, Quaternion.identity);

        }


    }


}
