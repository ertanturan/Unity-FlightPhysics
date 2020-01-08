using FlightPhysics.Input;
using System.Collections.Generic;
using UnityEngine;

public class FlightCameraController : MonoBehaviour
{

    #region Fields

    [Header("Camera Controller Properties")]
    public BaseFlightInput Input;
    private List<Camera> _cameras = new List<Camera>();
    private List<AudioListener> _audioListeners = new List<AudioListener>();

    private int _cameraIndex;

    #endregion


    #region BuiltIn Methods

    private void Awake()
    {
        _cameras.Add(Camera.main);
        _audioListeners.Add(Camera.main.GetComponent<AudioListener>());
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.GetComponent<Camera>())
            {
                _cameras.Add(transform.GetChild(i).gameObject.GetComponent<Camera>());
                _audioListeners.Add(transform.GetChild(i).GetComponent<AudioListener>());
            }
        }
    }

    private void Update()
    {
        if (Input)
        {
            if (Input.CameraSwitch)
            {
                SwitchCamera();
            }
        }
    }
    #endregion

    protected virtual void SwitchCamera()
    {
        if (_cameras.Count > 0)
        {
            _cameras[_cameraIndex].enabled = false;
            _audioListeners[_cameraIndex].enabled = false;

            _cameraIndex++;
            if (_cameraIndex == _cameras.Count)
            {
                _cameraIndex = 0;
            }

            _audioListeners[_cameraIndex].enabled = true;
            _cameras[_cameraIndex].enabled = true;
        }
    }
}
