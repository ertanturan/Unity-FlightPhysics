using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FlightPhysics.UI
{
    public class UIController : MonoBehaviour
    {
        private  List<IAirplaneUI> _airplanes = new List<IAirplaneUI>();

        private void Awake()
        {
            _airplanes = GetComponentsInChildren<IAirplaneUI>().ToList();
        }

        private void Update()
        {
            foreach (IAirplaneUI instrument in _airplanes)
            {
                instrument.HandleAirplaneUI();
            }
        }
    }
}
