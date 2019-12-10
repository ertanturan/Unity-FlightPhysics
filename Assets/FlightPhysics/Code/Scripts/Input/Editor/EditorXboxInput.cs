using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FlightPhysics.Input
{
    [CustomEditor(typeof(XboxInput))]
    public class EditorXboxInput : Editor
    {
        #region Vars

        private XboxInput _targetInput;

        #endregion


        #region BuiltIn Methods

        private void OnEnable()
        {
            _targetInput = (XboxInput)target;
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


