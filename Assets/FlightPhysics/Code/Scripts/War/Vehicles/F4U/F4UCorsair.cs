using FlightPhysics.War.Explosives;
using System.Collections.Generic;
using UnityEngine;

namespace FlightPhysics.War.Vehicles.F4UCorsair
{
    public class F4UCorsair : MonoBehaviour
    {
        public List<Weapon> BombsList;
        public List<Weapon> RocketList;

        public KeyCode FireRocketKey = KeyCode.F1;
        public KeyCode FireBombKey = KeyCode.F2;



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
        }

        private void FireBomb()
        {
            Debug.Log("Fire bomb");

        }


    }


}
