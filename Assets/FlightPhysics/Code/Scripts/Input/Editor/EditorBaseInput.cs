using System.Collections;
using System.Collections.Generic;
using FlightPhysics.Input;
using UnityEditor;
using UnityEngine;

namespace FlightPhysics.Input
{
    [CustomEditor(typeof(BaseFlightInput))]
    public class EditorBaseInput : Editor
    {
        #region Vars

        private BaseFlightInput _targetInput;

        #endregion

        #region BuiltIn Methods

        private void OnEnable()
        {
            _targetInput = (BaseFlightInput)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            //Custom editor changes
            string debugInfo = "";
            debugInfo += "Pitch : " + _targetInput.Pitch;
            debugInfo += "\nRoll : " + _targetInput.Roll;
            debugInfo += "\nYaw : " + _targetInput.Yaw;
            debugInfo += "\nThrottle : " + _targetInput.Throttle;
            debugInfo += "\nFlaps : " + _targetInput.Flaps;
            debugInfo += "\nBrake : " + _targetInput.Brake;

            GUILayout.Space(20);
            EditorGUILayout.TextArea(debugInfo,
                GUILayout.Height(100)
                );
            GUILayout.Space(20);
            Repaint();

        }

        #endregion
    }
}


