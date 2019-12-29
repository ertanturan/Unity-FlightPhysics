using System.Collections.Generic;
using UnityEngine;

namespace FlightPhysics
{
    public class AircraftCollisions : MonoBehaviour
    {
        private List<Vector3> _hitNormals = new List<Vector3>();
        private List<Vector3> _hitPoints = new List<Vector3>();

        private void OnCollisionEnter(Collision collision)
        {
            for (int i = 0; i < collision.contacts.Length; i++)
            {
                _hitPoints.Add(collision.contacts[i].point);
                _hitNormals.Add(collision.contacts[i].normal);
            }
        }
    }
}

