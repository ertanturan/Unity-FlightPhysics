using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlightPhysics
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(AudioSource))]
    public class BaseRigidbodyController : MonoBehaviour
    {
        #region Fields

        protected Rigidbody _rb;
        protected AudioSource _audioSource;
        #endregion

        #region BuiltIn Methods

        protected virtual void Start()
        {
            if (GetComponent<Rigidbody>())
            {
                _rb = GetComponent<Rigidbody>();
            }
            else
            {
                _rb = this.gameObject.AddComponent<Rigidbody>();
            }
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource)
            {
                _audioSource.playOnAwake = false;
            }
        }

        protected virtual void FixedUpdate()
        {
            HandlePhysics();
        }

        #endregion

        #region Custom Methods

        protected virtual void HandlePhysics()
        {

        }


        #endregion


    }

}


