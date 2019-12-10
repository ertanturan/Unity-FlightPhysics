using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FlightPhysics
{
    public static class FlightMenusEditor 
    {
        [MenuItem("FlightPhysics Tools/Create New Airplane")]
        public static void CreateNewAirplane()
        {
            GameObject curSelected = Selection.activeGameObject;
            if (curSelected)
            {
                FlightController flightController = 
                    curSelected.AddComponent<FlightController>();

                GameObject currentCOG  = new GameObject(curSelected.name +" - COG");
                //COG : Center of Gravity
                currentCOG.transform.SetParent(curSelected.transform);
                flightController.CenterOfGravity = currentCOG.transform;
            }
            else
            {
                Debug.LogError("NO GAMEOBJECT SELECTED IN THE EDITOR !.");
            }
        }
    }


}

